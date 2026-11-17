using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;
using Giovanny_Barbers;

public partial class ConexionBD
{
    private static readonly Lazy<IConfiguration> Config = new(() => new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .Build());

    public static SqlConnection ObtenerConexion()
    {
        string cadena = Config.Value.GetConnectionString("GeovannysBarberDB")
            ?? throw new InvalidOperationException("No se encontró la conexión GeovannysBarberDB.");
        return new SqlConnection(cadena);
    }

    // RNF3: tras varios intentos fallidos la cuenta queda bloqueada durante unos minutos.
    private const int IntentosPermitidos = 5;
    private const int MinutosBloqueo = 15;
    // Se verifica contra un hash ficticio cuando el usuario no existe, para que el tiempo de respuesta no lo delate.
    private static readonly Lazy<string> HashFicticio = new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()));

    public static async Task<SesionUsuario?> AutenticarAsync(string usuario, string contrasena)
    {
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        using var consulta = new SqlCommand("""
            SELECT id_usuario, nombre_usuario, contrasena_hash, rol, estado, intentos_fallidos,
                CASE WHEN bloqueado_hasta > SYSDATETIME() THEN DATEDIFF(second, SYSDATETIME(), bloqueado_hasta) END
            FROM dbo.Usuario WHERE nombre_usuario = @usuario
            """, conexion);
        consulta.Parameters.Add("@usuario", SqlDbType.VarChar, 50).Value = usuario;
        int id, intentos;
        int? segundosBloqueo;
        string nombre, rol, estado, clave;
        try
        {
            using var lector = await consulta.ExecuteReaderAsync();
            if (!await lector.ReadAsync())
            {
                ValidarContrasena(contrasena, HashFicticio.Value);
                return null;
            }
            id = lector.GetInt32(0);
            nombre = lector.GetString(1);
            clave = lector.IsDBNull(2) ? "" : lector.GetString(2);
            rol = lector.IsDBNull(3) ? "" : lector.GetString(3).Trim();
            estado = lector.IsDBNull(4) ? "" : lector.GetString(4).Trim();
            intentos = lector.GetInt32(5);
            segundosBloqueo = lector.IsDBNull(6) ? null : lector.GetInt32(6);
            if (await lector.ReadAsync()) throw new InvalidOperationException("Hay nombres de usuario duplicados. Consulte al administrador.");
        }
        catch (SqlException ex) when (ex.Number == 207)
        {
            throw new InvalidOperationException("Falta ejecutar la migración de seguridad del login de Updates.sql.", ex);
        }
        if (segundosBloqueo != null)
            throw new InvalidOperationException($"La cuenta está bloqueada por intentos fallidos. Intente de nuevo en {(segundosBloqueo.Value + 59) / 60} minuto(s).");
        if (!ValidarContrasena(contrasena, clave))
        {
            using var fallo = new SqlCommand("""
                UPDATE dbo.Usuario SET
                    bloqueado_hasta = CASE WHEN intentos_fallidos + 1 >= @maximo THEN DATEADD(minute, @minutos, SYSDATETIME()) ELSE bloqueado_hasta END,
                    intentos_fallidos = CASE WHEN intentos_fallidos + 1 >= @maximo THEN 0 ELSE intentos_fallidos + 1 END
                WHERE id_usuario = @id
                """, conexion);
            fallo.Parameters.Add("@maximo", SqlDbType.Int).Value = IntentosPermitidos;
            fallo.Parameters.Add("@minutos", SqlDbType.Int).Value = MinutosBloqueo;
            fallo.Parameters.Add("@id", SqlDbType.Int).Value = id;
            await fallo.ExecuteNonQueryAsync();
            return null;
        }
        if (intentos > 0)
        {
            using var reinicio = new SqlCommand("UPDATE dbo.Usuario SET intentos_fallidos = 0, bloqueado_hasta = NULL WHERE id_usuario = @id", conexion);
            reinicio.Parameters.Add("@id", SqlDbType.Int).Value = id;
            await reinicio.ExecuteNonQueryAsync();
        }
        if (!string.Equals(estado, "Activo", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Esta cuenta está inactiva. Consulte al administrador.");
        if (string.IsNullOrWhiteSpace(rol)) throw new InvalidOperationException("Esta cuenta no tiene un rol asignado.");

        // La instalación se ejecuta con la conexión real de la aplicación, una vez autenticado.
        await PrepararPermisosAsync(conexion);
        await PrepararBitacoraAsync(conexion);
        await PrepararFase2Async(conexion);
        using var permisos = new SqlCommand("SELECT modulo FROM dbo.RolModulo WHERE rol = @rol", conexion);
        permisos.Parameters.Add("@rol", SqlDbType.VarChar, 30).Value = rol;
        var modulos = new List<string>();
        using (var lector = await permisos.ExecuteReaderAsync())
            while (await lector.ReadAsync()) modulos.Add(lector.GetString(0).Trim());
        return new SesionUsuario(id, nombre, rol, modulos);
    }

    internal static bool ValidarContrasena(string ingresada, string almacenada)
    {
        // Sección 5.3: solo se aceptan contraseñas protegidas con hash; una clave en texto plano nunca valida.
        if (string.IsNullOrEmpty(almacenada) || !almacenada.StartsWith("$2", StringComparison.Ordinal)) return false;
        try { return BCrypt.Net.BCrypt.Verify(ingresada, almacenada); }
        catch (ArgumentException) { return false; }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }

    // Sección 5.3: el estado y el rol se vuelven a leer de la base de datos en cada operación restringida.
    private static async Task ValidarSesionAsync(SqlConnection conexion, SesionUsuario sesion, SqlTransaction? transaccion = null, CancellationToken cancelacion = default)
    {
        using var comando = new SqlCommand("SELECT estado, rol FROM dbo.Usuario WHERE id_usuario = @id", conexion, transaccion);
        comando.Parameters.Add("@id", SqlDbType.Int).Value = sesion.IdUsuario;
        using var lector = await comando.ExecuteReaderAsync(cancelacion);
        if (!await lector.ReadAsync(cancelacion) || lector.IsDBNull(0) ||
            !string.Equals(lector.GetString(0).Trim(), "Activo", StringComparison.OrdinalIgnoreCase))
            throw new SesionInvalidaException("Su cuenta fue desactivada. La sesión se cerrará.");
        if (lector.IsDBNull(1) || !string.Equals(lector.GetString(1).Trim(), sesion.Rol, StringComparison.OrdinalIgnoreCase))
            throw new SesionInvalidaException("Su rol cambió. Inicie sesión de nuevo para cargar sus permisos.");
    }

    private static async Task PrepararPermisosAsync(SqlConnection conexion)
    {
        using var comprobar = new SqlCommand("SELECT OBJECT_ID(N'dbo.RolModulo', N'U')", conexion);
        if (await comprobar.ExecuteScalarAsync() is int) return;
        using var transaccion = conexion.BeginTransaction();
        try
        {
            using var instalar = new SqlCommand("""
                DECLARE @resultado int;
                EXEC @resultado = sys.sp_getapplock @Resource=N'Giovanny.RolModulo',
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
                IF @resultado < 0 THROW 50001, 'No se pudo preparar la tabla de permisos.', 1;
                IF OBJECT_ID(N'dbo.RolModulo', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.RolModulo (
                        rol VARCHAR(30) NOT NULL,
                        modulo NVARCHAR(50) NOT NULL,
                        CONSTRAINT PK_RolModulo PRIMARY KEY (rol, modulo)
                    );
                    INSERT INTO dbo.RolModulo (rol, modulo) VALUES
                        ('Trabajador', N'Mi jornada'),
                        ('Trabajador', N'Clientes'), ('Trabajador', N'Facturación');
                END;
                UPDATE dbo.Usuario SET rol='Trabajador' WHERE rol IN ('Cajera','Cajero');
                DELETE FROM dbo.RolModulo WHERE rol IN ('Cajera','Cajero');
                INSERT INTO dbo.RolModulo (rol, modulo)
                SELECT p.rol, p.modulo
                FROM (VALUES ('Trabajador', N'Mi jornada'), ('Trabajador', N'Clientes'), ('Trabajador', N'Facturación')) p(rol, modulo)
                WHERE NOT EXISTS (SELECT 1 FROM dbo.RolModulo r WHERE r.rol=p.rol AND r.modulo=p.modulo);
                """, conexion, transaccion);
            await instalar.ExecuteNonQueryAsync();
            transaccion.Commit();
        }
        catch (SqlException ex)
        {
            transaccion.Rollback();
            throw new InvalidOperationException("Falta instalar la tabla de permisos y la cuenta de SQL Server no pudo crearla. Ejecute la migración RBAC de Updates.sql con una cuenta autorizada.", ex);
        }
    }

    // Tablas cuyos registros se desactivan en lugar de eliminarse.
    private static readonly string[] TablasDesactivables = ["Trabajador", "Cliente", "Categoria", "Producto", "Servicio", "Metodo_Pago", "Caja", "Asistencia"];

    // Misma migración que "BITÁCORA Y DESACTIVACIÓN" de Updates.sql; se instala sola la primera vez.
    private static async Task PrepararBitacoraAsync(SqlConnection conexion)
    {
        using var comprobar = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.Bitacora', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Asistencia', N'activo') IS NOT NULL THEN 1 ELSE 0 END", conexion);
        if (await comprobar.ExecuteScalarAsync() is 1) return;
        using var transaccion = conexion.BeginTransaction();
        try
        {
            using var instalar = new SqlCommand($"""
                DECLARE @resultado int;
                EXEC @resultado = sys.sp_getapplock @Resource=N'Giovanny.Bitacora',
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
                IF @resultado < 0 THROW 50001, 'No se pudo preparar la bitácora.', 1;
                IF OBJECT_ID(N'dbo.Bitacora', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Bitacora (
                        id_bitacora INT IDENTITY(1,1) PRIMARY KEY,
                        fecha DATETIME2 NOT NULL CONSTRAINT DF_Bitacora_fecha DEFAULT SYSDATETIME(),
                        id_usuario INT NULL REFERENCES dbo.Usuario(id_usuario),
                        usuario VARCHAR(50) NOT NULL,
                        accion NVARCHAR(40) NOT NULL,
                        entidad NVARCHAR(40) NOT NULL,
                        id_registro INT NULL,
                        descripcion NVARCHAR(1000) NULL,
                        antes NVARCHAR(MAX) NULL,
                        despues NVARCHAR(MAX) NULL
                    );
                    CREATE INDEX IX_Bitacora_fecha ON dbo.Bitacora(fecha DESC);
                    CREATE INDEX IX_Bitacora_registro ON dbo.Bitacora(entidad, id_registro);
                END;
                {string.Concat(TablasDesactivables.Select(t => $"IF COL_LENGTH(N'dbo.{t}', N'activo') IS NULL ALTER TABLE dbo.{t} ADD activo BIT NOT NULL CONSTRAINT DF_{t}_activo DEFAULT 1;\n"))}
                """, conexion, transaccion);
            await instalar.ExecuteNonQueryAsync();
            transaccion.Commit();
        }
        catch (SqlException ex)
        {
            transaccion.Rollback();
            throw new InvalidOperationException("Falta instalar la bitácora y la cuenta de SQL Server no pudo crearla. Ejecute la migración BITÁCORA Y DESACTIVACIÓN de Updates.sql con una cuenta autorizada.", ex);
        }
    }

    // CU-08 y sección 3.2.3: tipos fijos de servicio para el desglose por cliente.
    public static readonly string[] TiposServicio = ["Corte de pelo", "Arreglo de barba", "Pelo y barba", "Instalación de prótesis", "Mantenimiento"];

    // CU-17: los métodos cuyo nombre incluye "tarjeta" se cobran en la terminal POS y requieren los datos del voucher.
    public static bool EsMetodoTarjeta(string? nombre) => nombre?.Contains("tarjeta", StringComparison.OrdinalIgnoreCase) == true;

    // Misma migración que "FASE 2: CONTABILIDAD Y POS" de Updates.sql; se instala sola la primera vez.
    // Usuario en cada movimiento (CU-19), quién cerró y el fondo que queda (CU-20 y RF14), caja y usuario de cada transacción POS (CU-17).
    private static async Task PrepararFase2Async(SqlConnection conexion)
    {
        using var comprobar = new SqlCommand("SELECT CASE WHEN COL_LENGTH(N'dbo.Transaccion_POS', N'id_usuario') IS NOT NULL THEN 1 ELSE 0 END", conexion);
        if (await comprobar.ExecuteScalarAsync() is 1) return;
        using var transaccion = conexion.BeginTransaction();
        try
        {
            foreach (string paso in ScriptFase2)
            {
                using var instalar = new SqlCommand(paso, conexion, transaccion);
                await instalar.ExecuteNonQueryAsync();
            }
            transaccion.Commit();
        }
        catch (SqlException ex)
        {
            transaccion.Rollback();
            throw new InvalidOperationException("Falta instalar las columnas de la fase 2 y la cuenta de SQL Server no pudo crearlas. Ejecute la migración FASE 2: CONTABILIDAD Y POS de Updates.sql con una cuenta autorizada.", ex);
        }
    }

    // Cada paso va en su propio lote: SQL Server no deja usar una columna en el mismo lote que la crea.
    private static readonly string[] ScriptFase2 =
    [
        """
        DECLARE @resultado int;
        EXEC @resultado = sys.sp_getapplock @Resource=N'Giovanny.Fase2', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
        IF @resultado < 0 THROW 50001, 'No se pudo preparar la fase 2.', 1;
        IF COL_LENGTH(N'dbo.Movimiento_Contable', N'id_usuario') IS NULL ALTER TABLE dbo.Movimiento_Contable ADD id_usuario INT NULL CONSTRAINT FK_Movimiento_Contable_Usuario REFERENCES dbo.Usuario(id_usuario);
        IF COL_LENGTH(N'dbo.Cuadre_Caja', N'id_usuario') IS NULL ALTER TABLE dbo.Cuadre_Caja ADD id_usuario INT NULL CONSTRAINT FK_Cuadre_Caja_Usuario REFERENCES dbo.Usuario(id_usuario);
        IF COL_LENGTH(N'dbo.Cuadre_Caja', N'fondo_siguiente') IS NULL ALTER TABLE dbo.Cuadre_Caja ADD fondo_siguiente DECIMAL(10,2) NULL;
        IF COL_LENGTH(N'dbo.Transaccion_POS', N'id_caja') IS NULL ALTER TABLE dbo.Transaccion_POS ADD id_caja INT NULL CONSTRAINT FK_Transaccion_POS_Caja REFERENCES dbo.Caja(id_caja);
        """,
        """
        -- Los cierres anteriores dejaban todo lo contado como fondo del siguiente período.
        UPDATE dbo.Cuadre_Caja SET fondo_siguiente = monto_real WHERE fondo_siguiente IS NULL;
        -- El usuario de los movimientos ya guardados se toma de la bitácora.
        UPDATE m SET id_usuario = b.id_usuario
        FROM dbo.Movimiento_Contable m
        CROSS APPLY (SELECT TOP (1) x.id_usuario FROM dbo.Bitacora x
            WHERE (m.id_pago IS NULL AND x.entidad = N'Transacción' AND x.id_registro = m.id_movimiento)
               OR (m.id_pago IS NOT NULL AND x.entidad IN (N'Pago', N'Factura') AND x.id_registro IN (m.id_pago, (SELECT id_factura FROM dbo.Pago WHERE id_pago = m.id_pago)))
            ORDER BY ABS(DATEDIFF(second, x.fecha, m.fecha))) b
        WHERE m.id_usuario IS NULL;
        -- Las categorías libres anteriores pasan a los tipos fijos de servicio.
        UPDATE dbo.Servicio SET categoria_servicio = CASE
            WHEN nombre LIKE N'%barba%' AND (nombre LIKE N'%cabello%' OR nombre LIKE N'%pelo%') THEN N'Pelo y barba'
            WHEN nombre LIKE N'%barba%' THEN N'Arreglo de barba'
            WHEN nombre LIKE N'%mantenimiento%' THEN N'Mantenimiento'
            WHEN nombre LIKE N'%pr_tesis%' THEN N'Instalación de prótesis'
            ELSE N'Corte de pelo' END
        WHERE categoria_servicio IS NULL OR categoria_servicio NOT IN (N'Corte de pelo', N'Arreglo de barba', N'Pelo y barba', N'Instalación de prótesis', N'Mantenimiento');
        """,
        "IF COL_LENGTH(N'dbo.Transaccion_POS', N'id_usuario') IS NULL ALTER TABLE dbo.Transaccion_POS ADD id_usuario INT NULL CONSTRAINT FK_Transaccion_POS_Usuario REFERENCES dbo.Usuario(id_usuario);"
    ];

    public static async Task<DataTable> ConsultarModuloAsync(SesionUsuario sesion, string modulo, string seccion, CancellationToken cancelacion = default)
    {
        sesion.ExigirAcceso(modulo);
        if (seccion == "Usuarios" && !sesion.EsAdministrador) throw new UnauthorizedAccessException("Solo el administrador puede consultar las cuentas.");
        string consulta = ConsultaModulo(modulo, seccion);
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync(cancelacion);
        await ValidarSesionAsync(conexion, sesion, cancelacion: cancelacion);
        using var comando = new SqlCommand(consulta, conexion);
        comando.Parameters.Add("@usuario", SqlDbType.Int).Value = sesion.IdUsuario;
        using var lector = await comando.ExecuteReaderAsync(cancelacion);
        var tabla = new DataTable();
        tabla.Load(lector);
        return tabla;
    }

    public static async Task<Dictionary<string, string>> ObtenerResumenAsync(SesionUsuario sesion, CancellationToken cancelacion = default)
    {
        // Moneda: true = quetzales, false = cantidad, null = texto devuelto por la consulta.
        var consultas = new List<(string Titulo, string Sql, bool? Moneda)>();
        if (sesion.PuedeAbrir("Contabilidad"))
            consultas.Add(("Ingresos del día", "SELECT COALESCE(SUM(monto),0) FROM dbo.Movimiento_Contable WHERE tipo='Ingreso' AND CAST(fecha AS date)=CAST(GETDATE() AS date)", true));
        if (sesion.PuedeAbrir("Trabajadores"))
            consultas.Add(("Trabajadores activos", "SELECT COUNT(*) FROM dbo.Trabajador t JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE u.estado='Activo' AND t.activo=1", false));
        if (sesion.PuedeAbrir("Inventario"))
            consultas.Add((TituloStockBajo, "SELECT COUNT(*) FROM dbo.Producto WHERE activo=1 AND stock<=stock_minimo", false));
        if (sesion.PuedeAbrir("Facturación"))
            consultas.Add(("Facturas del día", "SELECT COUNT(*) FROM dbo.Factura WHERE CAST(fecha AS date)=CAST(GETDATE() AS date) AND estado<>'Anulada'", false));
        // Indicadores del mes para el administrador.
        const string mesActual = "fecha>=DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1) AND fecha<DATEADD(month,1,DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1))";
        if (sesion.EsAdministrador)
        {
            consultas.Add(("Ingresos del mes", "SELECT COALESCE(SUM(monto),0) FROM dbo.Movimiento_Contable WHERE tipo='Ingreso' AND " + mesActual, true));
            consultas.Add(("Egresos del mes", "SELECT COALESCE(SUM(monto),0) FROM dbo.Movimiento_Contable WHERE tipo='Egreso' AND " + mesActual, true));
            consultas.Add(("Servicio top del mes", "SELECT COALESCE((SELECT TOP (1) s.nombre FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio WHERE f.estado<>'Anulada' AND f." + mesActual + " GROUP BY s.nombre ORDER BY SUM(d.cantidad) DESC, s.nombre),'Sin ventas')", null));
            consultas.Add(("Préstamos por resolver", "SELECT COUNT(*) FROM dbo.Prestamo WHERE estado='Pendiente'", false));
        }
        if (!sesion.EsAdministrador && sesion.PuedeAbrir("Mi jornada"))
        {
            consultas.Add(("Mi asistencia hoy", "SELECT COUNT(*) FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador WHERE t.id_usuario=@usuario AND a.fecha=CAST(GETDATE() AS date)", false));
            consultas.Add(("Mis servicios hoy", "SELECT COALESCE(SUM(d.cantidad),0) FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador WHERE t.id_usuario=@usuario AND CAST(f.fecha AS date)=CAST(GETDATE() AS date)", false));
            consultas.Add(("Mis ventas hoy", "SELECT COALESCE(SUM(d.subtotal),0) FROM dbo.Detalle_Producto d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador WHERE t.id_usuario=@usuario AND CAST(f.fecha AS date)=CAST(GETDATE() AS date)", true));
            consultas.Add(("Mis préstamos pendientes", "SELECT COUNT(*) FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE t.id_usuario=@usuario AND p.estado='Pendiente'", false));
        }
        var resumen = new Dictionary<string, string>();
        if (consultas.Count == 0) return resumen;
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync(cancelacion);
        await ValidarSesionAsync(conexion, sesion, cancelacion: cancelacion);
        foreach (var consulta in consultas)
        {
            using var comando = new SqlCommand(consulta.Sql, conexion);
            comando.Parameters.Add("@usuario", SqlDbType.Int).Value = sesion.IdUsuario;
            object? resultado = await comando.ExecuteScalarAsync(cancelacion);
            if (consulta.Moneda == null) { resumen.Add(consulta.Titulo, Convert.ToString(resultado) ?? ""); continue; }
            decimal valor = Convert.ToDecimal(resultado);
            resumen.Add(consulta.Titulo, consulta.Moneda == true ? "Q " + valor.ToString("N2") : valor.ToString("N0"));
        }
        return resumen;
    }

    public const string TituloStockBajo = "Productos con stock bajo";

    // Productos activos en o por debajo de su mínimo, para avisar al entrar.
    public static async Task<List<string>> ProductosStockBajoAsync(SesionUsuario sesion, CancellationToken cancelacion = default)
    {
        sesion.ExigirAcceso("Inventario");
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync(cancelacion);
        await ValidarSesionAsync(conexion, sesion, cancelacion: cancelacion);
        using var comando = new SqlCommand("SELECT CONCAT(nombre,' (',stock,' / mín. ',stock_minimo,')') FROM dbo.Producto WHERE activo=1 AND stock<=stock_minimo ORDER BY stock, nombre", conexion);
        using var lector = await comando.ExecuteReaderAsync(cancelacion);
        var productos = new List<string>();
        while (await lector.ReadAsync(cancelacion)) productos.Add(lector.GetString(0));
        return productos;
    }

    // Copia completa de la base de datos en la carpeta de respaldos de SQL Server; solo el administrador.
    // La escribe el servicio de SQL Server, por eso se usa su carpeta predeterminada y no una elegida por el usuario.
    public static async Task<string> RespaldarBaseDatosAsync(SesionUsuario sesion)
    {
        if (!sesion.EsAdministrador) throw new UnauthorizedAccessException("Solo el administrador puede respaldar la base de datos.");
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        await ValidarSesionAsync(conexion, sesion);
        string? carpeta;
        using (var consulta = new SqlCommand("SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))", conexion))
            carpeta = await consulta.ExecuteScalarAsync() as string;
        if (string.IsNullOrWhiteSpace(carpeta)) throw new InvalidOperationException("SQL Server no informó su carpeta de respaldos.");
        string ruta = Path.Combine(carpeta, $"{conexion.Database}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        using (var respaldo = new SqlCommand("DECLARE @bd sysname = DB_NAME(); BACKUP DATABASE @bd TO DISK = @ruta WITH INIT, CHECKSUM, NAME = @nombre", conexion) { CommandTimeout = 600 })
        {
            respaldo.Parameters.Add("@ruta", SqlDbType.NVarChar, 4000).Value = ruta;
            respaldo.Parameters.Add("@nombre", SqlDbType.NVarChar, 128).Value = "Respaldo Geovanny's Barber " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            await respaldo.ExecuteNonQueryAsync();
        }
        using (var bitacora = new SqlCommand("INSERT INTO dbo.Bitacora(id_usuario,usuario,accion,entidad,id_registro,descripcion) VALUES(@usuario,@nombre,N'Respaldar',N'Base de datos',NULL,@descripcion)", conexion))
        {
            bitacora.Parameters.Add("@usuario", SqlDbType.Int).Value = sesion.IdUsuario;
            bitacora.Parameters.Add("@nombre", SqlDbType.VarChar, 50).Value = sesion.NombreUsuario;
            bitacora.Parameters.Add("@descripcion", SqlDbType.NVarChar, 1000).Value = "Respaldo completo en " + ruta;
            await bitacora.ExecuteNonQueryAsync();
        }
        return ruta;
    }

    // Lista cerrada de consultas: nunca se incorporan nombres o filtros suministrados por el usuario.
    internal static string ConsultaModulo(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Trabajadores", "Directorio") => "SELECT t.id_trabajador AS ID, CONCAT(t.nombres,' ',t.apellidos) AS Nombre, t.telefono AS Teléfono, t.direccion AS Dirección, t.salario AS Salario, u.nombre_usuario AS Usuario, u.rol AS Rol, u.estado AS Estado FROM dbo.Trabajador t LEFT JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE t.activo=1 ORDER BY t.nombres",
        ("Trabajadores", "Descuentos") => "SELECT d.fecha AS Fecha, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, d.id_prestamo AS Préstamo, d.monto AS Monto, p.saldo_pendiente AS [Saldo actual], d.observacion AS Observación FROM dbo.Descuento_Prestamo d JOIN dbo.Prestamo p ON p.id_prestamo=d.id_prestamo JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador ORDER BY d.fecha DESC",
        ("Trabajadores", "Actividades") => "SELECT f.fecha AS Fecha, 'Servicio' AS Tipo, s.nombre AS Descripción, c.nombres AS Cliente, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, d.cantidad AS Cantidad, d.subtotal AS Total FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador UNION ALL SELECT f.fecha, 'Venta', p.nombre, c.nombres, CONCAT(t.nombres,' ',t.apellidos), d.cantidad, d.subtotal FROM dbo.Detalle_Producto d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Producto p ON p.id_producto=d.id_producto LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador ORDER BY Fecha DESC",
        ("Trabajadores", "Usuarios") => "SELECT id_usuario AS ID, nombre_usuario AS Usuario, rol AS Rol, estado AS Estado FROM dbo.Usuario ORDER BY nombre_usuario",
        ("Inventario", "Categorías") => "SELECT id_categoria AS ID, nombre AS Nombre, descripcion AS Descripción FROM dbo.Categoria WHERE activo=1 ORDER BY nombre",
        ("Facturación", "Servicios") => "SELECT id_servicio AS ID, nombre AS Servicio, precio AS Precio, descripcion AS Descripción, categoria_servicio AS Categoría FROM dbo.Servicio WHERE activo=1 ORDER BY nombre",
        ("Facturación", "Pagos") => "SELECT p.id_pago AS ID, p.id_factura AS Factura, p.fecha AS Fecha, m.nombre AS Método, p.monto AS Monto, p.referencia AS Referencia, p.estado AS Estado FROM dbo.Pago p LEFT JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago ORDER BY p.id_factura DESC, p.id_pago DESC",
        ("Contabilidad", "Cajas") => "SELECT id_caja AS ID, nombre AS Caja, estado AS Estado FROM dbo.Caja WHERE activo=1 ORDER BY nombre",
        ("Trabajadores", "Asistencia") => "SELECT a.fecha AS Fecha, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, a.hora_entrada AS Entrada, a.hora_salida AS Salida FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador WHERE a.activo=1 ORDER BY a.fecha DESC",
        ("Mi jornada", "Mi asistencia") => "SELECT a.fecha AS Fecha, a.hora_entrada AS Entrada, a.hora_salida AS Salida FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador WHERE t.id_usuario=@usuario AND a.activo=1 ORDER BY a.fecha DESC",
        ("Trabajadores", "Préstamos") => "SELECT p.id_prestamo AS ID, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, p.fecha_solicitud AS Fecha, p.monto AS Monto, p.saldo_pendiente AS Saldo, p.estado AS Estado FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador ORDER BY p.fecha_solicitud DESC",
        ("Mi jornada", "Mis préstamos") => "SELECT p.fecha_solicitud AS Fecha, p.monto AS Monto, p.saldo_pendiente AS Saldo, p.estado AS Estado FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE t.id_usuario=@usuario ORDER BY p.fecha_solicitud DESC",
        // CU-09: servicios con su categoría y productos comprados, por cliente.
        ("Clientes", "Historial") => "SELECT f.fecha AS Fecha, f.id_factura AS Factura, c.nombres AS Cliente, 'Servicio' AS Tipo, s.categoria_servicio AS Categoría, s.nombre AS Descripción, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, d.cantidad AS Cantidad, d.subtotal AS Total FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador WHERE f.estado<>'Anulada' UNION ALL SELECT f.fecha, f.id_factura, c.nombres, 'Producto', g.nombre, p.nombre, CONCAT(t.nombres,' ',t.apellidos), d.cantidad, d.subtotal FROM dbo.Detalle_Producto d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Producto p ON p.id_producto=d.id_producto LEFT JOIN dbo.Categoria g ON g.id_categoria=p.id_categoria LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador WHERE f.estado<>'Anulada' ORDER BY Fecha DESC",
        // RF5 y CU-10: visitas (facturas) e ingresos por cliente; Fecha es el inicio de cada período.
        ("Clientes", "Reporte diario") => "SELECT CAST(f.fecha AS date) AS Fecha, CONVERT(varchar(10),f.fecha,103) AS Período, c.nombres AS Cliente, c.nit AS NIT, COUNT(*) AS Visitas, SUM(f.total) AS Ingresos FROM dbo.Factura f JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente WHERE f.estado<>'Anulada' GROUP BY CAST(f.fecha AS date), CONVERT(varchar(10),f.fecha,103), c.id_cliente, c.nombres, c.nit ORDER BY Fecha DESC, Cliente",
        ("Clientes", "Reporte semanal") => "SELECT p.inicio AS Fecha, CONCAT(CONVERT(varchar(10),p.inicio,103),' al ',CONVERT(varchar(10),DATEADD(day,6,p.inicio),103)) AS Período, c.nombres AS Cliente, c.nit AS NIT, COUNT(*) AS Visitas, SUM(f.total) AS Ingresos FROM dbo.Factura f JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente CROSS APPLY (SELECT DATEADD(day,-((DATEPART(weekday,f.fecha)+@@DATEFIRST-2)%7),CAST(f.fecha AS date)) AS inicio) p WHERE f.estado<>'Anulada' GROUP BY p.inicio, c.id_cliente, c.nombres, c.nit ORDER BY Fecha DESC, Cliente",
        ("Clientes", "Reporte mensual") => "SELECT p.inicio AS Fecha, FORMAT(p.inicio,'MMMM yyyy','es-GT') AS Período, c.nombres AS Cliente, c.nit AS NIT, COUNT(*) AS Visitas, SUM(f.total) AS Ingresos FROM dbo.Factura f JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente CROSS APPLY (SELECT DATEFROMPARTS(YEAR(f.fecha),MONTH(f.fecha),1) AS inicio) p WHERE f.estado<>'Anulada' GROUP BY p.inicio, c.id_cliente, c.nombres, c.nit ORDER BY Fecha DESC, Cliente",
        ("Mi jornada", "Mis servicios") => "SELECT f.fecha AS Fecha, c.nombres AS Cliente, s.nombre AS Servicio, d.cantidad AS Cantidad, d.subtotal AS Total FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente WHERE t.id_usuario=@usuario ORDER BY f.fecha DESC",
        ("Mi jornada", "Mis ventas") => "SELECT f.fecha AS Fecha, p.nombre AS Producto, d.cantidad AS Cantidad, d.precio_unitario AS Precio, d.subtotal AS Total FROM dbo.Detalle_Producto d JOIN dbo.Producto p ON p.id_producto=d.id_producto JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador WHERE t.id_usuario=@usuario ORDER BY f.fecha DESC",
        ("Clientes", "Clientes") => "SELECT id_cliente AS ID, nombres AS Nombre, nit AS NIT, telefono AS Teléfono, correo AS Correo FROM dbo.Cliente WHERE activo=1 ORDER BY nombres",
        ("Inventario", "Productos") => "SELECT p.id_producto AS ID, p.nombre AS Producto, c.nombre AS Categoría, p.precio_venta AS Precio, p.stock AS Existencias, p.stock_minimo AS [Stock mínimo], CASE WHEN p.stock<=p.stock_minimo THEN 'Stock bajo' ELSE 'Disponible' END AS Disponibilidad FROM dbo.Producto p LEFT JOIN dbo.Categoria c ON c.id_categoria=p.id_categoria WHERE p.activo=1 ORDER BY p.nombre",
        ("Inventario", "Movimientos") => "SELECT m.fecha AS Fecha, p.nombre AS Producto, m.tipo AS Tipo, m.cantidad AS Cantidad, m.motivo AS Motivo FROM dbo.Movimiento_Inventario m LEFT JOIN dbo.Producto p ON p.id_producto=m.id_producto ORDER BY m.fecha DESC",
        ("Facturación", "Facturas") => "SELECT f.id_factura AS Número, f.fecha AS Fecha, c.nombres AS Cliente, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, f.subtotal AS Subtotal, f.total AS Total, f.estado AS Estado FROM dbo.Factura f LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador ORDER BY f.fecha DESC",
        ("Facturación", "Detalle de factura") => "SELECT d.id_factura AS Factura, 'Producto' AS Tipo, p.nombre AS Descripción, d.cantidad AS Cantidad, d.precio_unitario AS Precio, d.subtotal AS Subtotal FROM dbo.Detalle_Producto d JOIN dbo.Producto p ON p.id_producto=d.id_producto UNION ALL SELECT d.id_factura, 'Servicio', s.nombre, d.cantidad, d.precio_unitario, d.subtotal FROM dbo.Detalle_Servicio d JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio ORDER BY Factura DESC",
        // CU-19: movimientos por fecha, usuario y tipo; Origen indica el método del pago o si se registró a mano.
        ("Contabilidad", "Ingresos y egresos") => "SELECT m.fecha AS Fecha, c.nombre AS Caja, m.tipo AS Tipo, m.concepto AS Concepto, CASE WHEN m.id_pago IS NULL THEN 'Manual' ELSE COALESCE(mp.nombre,'Sin método') END AS Origen, m.monto AS Monto, COALESCE(u.nombre_usuario,'Sin registro') AS Usuario FROM dbo.Movimiento_Contable m LEFT JOIN dbo.Caja c ON c.id_caja=m.id_caja LEFT JOIN dbo.Usuario u ON u.id_usuario=m.id_usuario LEFT JOIN dbo.Pago p ON p.id_pago=m.id_pago LEFT JOIN dbo.Metodo_Pago mp ON mp.id_metodo_pago=p.id_metodo_pago ORDER BY m.fecha DESC, m.id_movimiento DESC",
        // CU-21: ingresos, egresos, saldo y cierres por período y caja; Fecha es el inicio de cada período.
        ("Contabilidad", "Reporte diario") => ReporteContable("CAST({0} AS date)", "CONVERT(varchar(10),x.inicio,103)"),
        ("Contabilidad", "Reporte semanal") => ReporteContable("DATEADD(day,-((DATEPART(weekday,{0})+@@DATEFIRST-2)%7),CAST({0} AS date))", "CONCAT(CONVERT(varchar(10),x.inicio,103),' al ',CONVERT(varchar(10),DATEADD(day,6,x.inicio),103))"),
        ("Contabilidad", "Reporte mensual") => ReporteContable("DATEFROMPARTS(YEAR({0}),MONTH({0}),1)", "FORMAT(x.inicio,'MMMM yyyy','es-GT')"),
        // Cobros vigentes por método (efectivo, tarjeta…), útil para cuadrar la caja contra los vouchers del POS.
        ("Contabilidad", "Ingresos por método") => "SELECT CAST(p.fecha AS date) AS Fecha, COALESCE(m.nombre,'Sin método') AS Método, COUNT(*) AS Pagos, SUM(p.monto) AS Monto FROM dbo.Pago p LEFT JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago WHERE p.estado='Completado' GROUP BY CAST(p.fecha AS date), COALESCE(m.nombre,'Sin método') ORDER BY Fecha DESC, Método",
        // Producción mensual por trabajador (base para calcular comisiones); Fecha es el inicio del mes.
        ("Trabajadores", "Rendimiento") => "SELECT p.inicio AS Fecha, FORMAT(p.inicio,'MMMM yyyy','es-GT') AS Período, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, COUNT(DISTINCT f.id_factura) AS Facturas, COALESCE(SUM(s.servicios),0) AS Servicios, COALESCE(SUM(s.importe),0) AS [Ingresos servicios], COALESCE(SUM(v.importe),0) AS [Ingresos productos], COALESCE(SUM(f.total),0) AS Total FROM dbo.Factura f JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador CROSS APPLY (SELECT DATEFROMPARTS(YEAR(f.fecha),MONTH(f.fecha),1) AS inicio) p OUTER APPLY (SELECT SUM(d.cantidad) AS servicios, SUM(d.subtotal) AS importe FROM dbo.Detalle_Servicio d WHERE d.id_factura=f.id_factura) s OUTER APPLY (SELECT SUM(d.subtotal) AS importe FROM dbo.Detalle_Producto d WHERE d.id_factura=f.id_factura) v WHERE f.estado<>'Anulada' GROUP BY p.inicio, t.id_trabajador, t.nombres, t.apellidos ORDER BY Fecha DESC, Total DESC",
        // CU-20 pasos 1 y 2: período abierto de cada caja desde su último cierre. Solo el efectivo se compara con lo contado;
        // los cobros con tarjeta, transferencia o QR se concilian con el cierre del POS o el estado de cuenta.
        ("Contabilidad", "Cuadre de caja") => """
            SELECT c.id_caja AS ID, c.nombre AS Caja, c.estado AS Estado, u.ultimo AS [Último cierre], COALESCE(u.fondo,0) AS [Fondo inicial],
                COALESCE(m.ingresos,0) AS [Ingresos en efectivo], COALESCE(m.egresos,0) AS [Egresos en efectivo],
                COALESCE(u.fondo,0)+COALESCE(m.ingresos,0)-COALESCE(m.egresos,0) AS [Efectivo esperado], COALESCE(m.otros,0) AS [Otros métodos],
                (SELECT COUNT(*) FROM dbo.Transaccion_POS t WHERE t.id_caja=c.id_caja AND t.estado='Pendiente') AS [POS pendientes]
            FROM dbo.Caja c
            OUTER APPLY (SELECT TOP (1) q.fecha_cierre AS ultimo, COALESCE(q.fondo_siguiente,q.monto_real) AS fondo FROM dbo.Cuadre_Caja q WHERE q.id_caja=c.id_caja ORDER BY q.fecha_cierre DESC, q.id_cuadre DESC) u
            OUTER APPLY (SELECT SUM(CASE WHEN e.efectivo=1 AND x.tipo='Ingreso' THEN x.monto ELSE 0 END) AS ingresos, SUM(CASE WHEN e.efectivo=1 AND x.tipo='Egreso' THEN x.monto ELSE 0 END) AS egresos,
                    SUM(CASE WHEN e.efectivo=0 THEN CASE WHEN x.tipo='Ingreso' THEN x.monto ELSE -x.monto END ELSE 0 END) AS otros
                FROM dbo.Movimiento_Contable x LEFT JOIN dbo.Pago p ON p.id_pago=x.id_pago LEFT JOIN dbo.Metodo_Pago mp ON mp.id_metodo_pago=p.id_metodo_pago
                CROSS APPLY (SELECT CASE WHEN x.id_pago IS NULL OR mp.nombre LIKE '%efectivo%' THEN 1 ELSE 0 END AS efectivo) e
                WHERE x.id_caja=c.id_caja AND x.fecha>COALESCE(u.ultimo,CONVERT(datetime2,'19000101'))) m
            WHERE c.activo=1 ORDER BY c.nombre
            """,
        ("Contabilidad", "Cierres diarios") => "SELECT q.id_cuadre AS ID, q.fecha_cierre AS Fecha, c.nombre AS Caja, q.monto_esperado AS [Efectivo esperado], q.monto_real AS [Efectivo contado], q.diferencia AS Diferencia, q.monto_real-COALESCE(q.fondo_siguiente,q.monto_real) AS Retirado, COALESCE(q.fondo_siguiente,q.monto_real) AS [Fondo siguiente], COALESCE(u.nombre_usuario,'Sin registro') AS Usuario, q.estado AS Estado FROM dbo.Cuadre_Caja q LEFT JOIN dbo.Caja c ON c.id_caja=q.id_caja LEFT JOIN dbo.Usuario u ON u.id_usuario=q.id_usuario ORDER BY q.fecha_cierre DESC, q.id_cuadre DESC",
        // CU-17: cobros con tarjeta registrados con los datos del voucher del POS.
        ("Facturación", "Transacciones POS") => "SELECT t.id_transaccion_pos AS ID, t.fecha_hora AS Fecha, p.id_factura AS Factura, mp.nombre AS Método, p.monto AS Monto, t.codigo_autorizacion AS Autorización, t.referencia_pos AS Referencia, t.estado AS Estado, c.nombre AS Caja, COALESCE(u.nombre_usuario,'Sin registro') AS Usuario FROM dbo.Transaccion_POS t LEFT JOIN dbo.Pago p ON p.id_pago=t.id_pago LEFT JOIN dbo.Metodo_Pago mp ON mp.id_metodo_pago=p.id_metodo_pago LEFT JOIN dbo.Caja c ON c.id_caja=t.id_caja LEFT JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario ORDER BY t.fecha_hora DESC, t.id_transaccion_pos DESC",
        ("Auditoría", "Bitácora") => "SELECT b.fecha AS Fecha, b.usuario AS Usuario, b.accion AS Acción, b.entidad AS Registro, b.id_registro AS ID, b.descripcion AS Detalle FROM dbo.Bitacora b ORDER BY b.fecha DESC, b.id_bitacora DESC",
        // Registros desactivados o anulados, con quién lo hizo y cuándo según la bitácora.
        ("Auditoría", "Registros eliminados") => """
            SELECT r.Tipo, r.ID, r.Nombre, b.fecha AS Fecha, b.usuario AS [Desactivado por], CASE WHEN r.Tipo IN (N'Factura', N'Pago') THEN 'No (anulado)' ELSE 'Sí' END AS Reactivable
            FROM (
                SELECT N'Empleado' AS Tipo, id_trabajador AS ID, CONCAT(nombres,' ',apellidos) AS Nombre FROM dbo.Trabajador WHERE activo=0
                UNION ALL SELECT N'Cliente', id_cliente, CONCAT(nombres,' · NIT ',nit) FROM dbo.Cliente WHERE activo=0
                UNION ALL SELECT N'Producto', id_producto, nombre FROM dbo.Producto WHERE activo=0
                UNION ALL SELECT N'Categoría', id_categoria, nombre FROM dbo.Categoria WHERE activo=0
                UNION ALL SELECT N'Servicio', id_servicio, nombre FROM dbo.Servicio WHERE activo=0
                UNION ALL SELECT N'Método de pago', id_metodo_pago, nombre FROM dbo.Metodo_Pago WHERE activo=0
                UNION ALL SELECT N'Caja', id_caja, nombre FROM dbo.Caja WHERE activo=0
                UNION ALL SELECT N'Asistencia', a.id_asistencia, CONCAT(CONVERT(varchar(10),a.fecha,103),' · ',t.nombres,' ',t.apellidos) FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador WHERE a.activo=0
                UNION ALL SELECT N'Factura', id_factura, CONCAT('Factura ',id_factura,' · Q ',total) FROM dbo.Factura WHERE estado='Anulada'
                UNION ALL SELECT N'Pago', id_pago, CONCAT('Pago ',id_pago,' · Factura ',id_factura,' · Q ',monto) FROM dbo.Pago WHERE estado='Anulado'
            ) r
            OUTER APPLY (SELECT TOP (1) fecha, usuario FROM dbo.Bitacora WHERE entidad=r.Tipo AND id_registro=r.ID AND accion IN (N'Desactivar', N'Anular', N'Editar') ORDER BY id_bitacora DESC) b
            ORDER BY b.fecha DESC, r.Tipo, r.ID
            """,
        _ => throw new UnauthorizedAccessException("La sección solicitada no pertenece a este módulo.")
    };

    // CU-21: misma estructura para el reporte diario, semanal y mensual. Los parámetros son expresiones fijas del código, nunca texto del usuario.
    // inicio: inicio del período a partir de la fecha ({0}); periodo: texto del período a partir de x.inicio.
    private static string ReporteContable(string inicio, string periodo) => $"""
        SELECT x.inicio AS Fecha, {periodo} AS Período, COALESCE(c.nombre,'Sin caja') AS Caja,
            SUM(x.ingresos) AS Ingresos, SUM(x.egresos) AS Egresos, SUM(x.ingresos-x.egresos) AS Saldo, SUM(x.movimientos) AS Movimientos,
            SUM(x.cierres) AS Cierres, SUM(x.diferencia) AS [Diferencia de caja]
        FROM (
            SELECT {string.Format(inicio, "m.fecha")} AS inicio, m.id_caja, SUM(CASE WHEN m.tipo='Ingreso' THEN m.monto ELSE 0 END) AS ingresos,
                SUM(CASE WHEN m.tipo='Egreso' THEN m.monto ELSE 0 END) AS egresos, COUNT(*) AS movimientos, 0 AS cierres, CAST(0 AS decimal(10,2)) AS diferencia
            FROM dbo.Movimiento_Contable m GROUP BY {string.Format(inicio, "m.fecha")}, m.id_caja
            UNION ALL
            SELECT {string.Format(inicio, "q.fecha_cierre")}, q.id_caja, 0, 0, 0, COUNT(*), SUM(q.diferencia)
            FROM dbo.Cuadre_Caja q GROUP BY {string.Format(inicio, "q.fecha_cierre")}, q.id_caja
        ) x LEFT JOIN dbo.Caja c ON c.id_caja=x.id_caja
        GROUP BY x.inicio, COALESCE(c.nombre,'Sin caja')
        ORDER BY Fecha DESC, Caja
        """;
}

namespace Giovanny_Barbers
{
    // El contexto se crea al autenticar y se pasa a cada pantalla de la sesión.
    public sealed class SesionUsuario
    {
        private readonly HashSet<string> modulos;

        public int IdUsuario { get; }
        public string NombreUsuario { get; }
        public string Rol { get; }
        public bool EsAdministrador => string.Equals(Rol, "Administrador", StringComparison.OrdinalIgnoreCase);

        public SesionUsuario(int idUsuario, string nombreUsuario, string rol, IEnumerable<string> modulosPermitidos)
        {
            if (idUsuario <= 0 || string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(rol))
                throw new ArgumentException("La sesión requiere un usuario autenticado y un rol válido.");
            IdUsuario = idUsuario;
            NombreUsuario = nombreUsuario;
            Rol = rol.Trim();
            modulos = new HashSet<string>(modulosPermitidos, StringComparer.OrdinalIgnoreCase);
        }

        public bool PuedeAbrir(string modulo) => EsAdministrador ||
            string.Equals(modulo, "Inicio", StringComparison.OrdinalIgnoreCase) || modulos.Contains(modulo);

        public void ExigirAcceso(string modulo)
        {
            if (!PuedeAbrir(modulo))
                throw new UnauthorizedAccessException($"Su rol '{Rol}' no tiene permiso para acceder a {modulo}.");
        }
    }

    // La cuenta se desactivó o cambió de rol con la sesión abierta: la interfaz vuelve al inicio de sesión.
    public sealed class SesionInvalidaException(string mensaje) : UnauthorizedAccessException(mensaje);
}


public partial class ConexionBD
{
    public static bool PuedeRegistrar(SesionUsuario sesion, string tipo) => tipo switch
    {
        "Empleado" or "Editar empleado" or "Clave" or "Servicio" or "Método de pago" => sesion.EsAdministrador,
        // CU-06 y RF4: solo el administrador resuelve préstamos y aplica descuentos al salario.
        "Resolver préstamo" or "Descuento" => sesion.EsAdministrador,
        // Corrección de registros ya guardados: solo el administrador.
        "Editar servicio" or "Editar categoría" or "Editar método de pago" or "Editar caja" or "Editar pago" or "Editar asistencia" or "Editar factura" => sesion.EsAdministrador,
        // Desactivar, anular y reactivar registros: solo el administrador.
        "Reactivar" => sesion.EsAdministrador,
        _ when tipo.StartsWith("Desactivar ") || tipo.StartsWith("Anular ") => sesion.EsAdministrador,
        "Cliente" or "Editar cliente" => sesion.PuedeAbrir("Clientes"),
        "Producto" or "Editar producto" or "Categoría" or "Movimiento" => sesion.PuedeAbrir("Inventario"),
        "Transacción" or "Caja" or "Cierre" => sesion.PuedeAbrir("Contabilidad"),
        "Pago" or "Confirmar transacción POS" => sesion.PuedeAbrir("Facturación"),
        "Factura" => sesion.PuedeAbrir("Facturación") || sesion.PuedeAbrir("Mi jornada"),
        "Asistencia" or "Préstamo" => sesion.PuedeAbrir("Trabajadores") || sesion.PuedeAbrir("Mi jornada"),
        _ => false
    };

    public static async Task<List<OpcionRegistro>> OpcionesRegistroAsync(SesionUsuario sesion, string tipo, string catalogo)
    {
        if (!PuedeRegistrar(sesion, tipo)) throw new UnauthorizedAccessException("No tiene permiso para esta operación.");
        // Desactivar un registro usa la misma lista que su edición.
        string origen = tipo.StartsWith("Desactivar ") ? "Editar " + tipo["Desactivar ".Length..] : tipo;
        string sql = (origen, catalogo) switch
        {
            ("Empleado", "rol") => "SELECT 1 AS ID, 'Administrador' AS Nombre UNION ALL SELECT 2, 'Trabajador'",
            ("Clave", "usuario") => "SELECT id_usuario, nombre_usuario FROM dbo.Usuario ORDER BY nombre_usuario",
            ("Editar empleado", "trabajador") => "SELECT t.id_trabajador, CONCAT(t.nombres,' ',t.apellidos,' · ',COALESCE(u.estado,'Sin usuario')) FROM dbo.Trabajador t LEFT JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE t.activo=1 ORDER BY t.nombres",
            ("Resolver préstamo", "prestamo") => "SELECT p.id_prestamo, CONCAT('#',p.id_prestamo,' · ',t.nombres,' ',t.apellidos,' · Q ',p.monto,' · ',CONVERT(varchar(10),p.fecha_solicitud,103)) FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE p.estado='Pendiente' ORDER BY p.fecha_solicitud",
            ("Descuento", "prestamo") => "SELECT p.id_prestamo, CONCAT('#',p.id_prestamo,' · ',t.nombres,' ',t.apellidos,' · Saldo Q ',p.saldo_pendiente,' · Salario ',COALESCE(CONCAT('Q ',t.salario),'sin registrar')) FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE p.estado='Autorizado' AND p.saldo_pendiente>0 ORDER BY p.fecha_solicitud",
            ("Producto", "categoria") or ("Editar producto", "categoria") or ("Editar categoría", "categoria") => "SELECT id_categoria, nombre FROM dbo.Categoria WHERE activo=1 ORDER BY nombre",
            ("Factura", "cliente") or ("Editar cliente", "cliente") => "SELECT id_cliente, CONCAT(nombres,' · NIT ',COALESCE(nit,'CF')) FROM dbo.Cliente WHERE activo=1 ORDER BY CASE WHEN nombres='Consumidor Final' AND nit='CF' THEN 0 ELSE 1 END, nombres",
            // Incluye clientes desactivados para no perder al de una factura antigua.
            ("Editar factura", "cliente") => "SELECT id_cliente, CONCAT(nombres,' · NIT ',COALESCE(nit,'CF')) FROM dbo.Cliente ORDER BY CASE WHEN nombres='Consumidor Final' AND nit='CF' THEN 0 ELSE 1 END, nombres",
            ("Factura", "producto") => "SELECT id_producto, nombre, precio_venta FROM dbo.Producto WHERE activo=1 ORDER BY nombre",
            // CU-12: la existencia actual se muestra al elegir el producto del movimiento.
            ("Movimiento", "producto") => "SELECT id_producto, CONCAT(nombre,' · Existencia ',stock) FROM dbo.Producto WHERE activo=1 ORDER BY nombre",
            ("Editar producto", "producto") => "SELECT p.id_producto, CONCAT(p.nombre,' · ',COALESCE(c.nombre,'Sin categoría')) FROM dbo.Producto p LEFT JOIN dbo.Categoria c ON c.id_categoria=p.id_categoria WHERE p.activo=1 ORDER BY p.nombre",
            ("Factura", "servicio") => "SELECT id_servicio, nombre, precio FROM dbo.Servicio WHERE activo=1 ORDER BY nombre",
            ("Factura", "trabajador") or ("Asistencia", "trabajador") or ("Préstamo", "trabajador") =>
                "SELECT t.id_trabajador, CONCAT(t.nombres,' ',t.apellidos) FROM dbo.Trabajador t JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE u.estado='Activo' AND t.activo=1 AND (@todos=1 OR t.id_usuario=@usuario) ORDER BY t.nombres",
            // Un cobro con tarjeta pendiente de confirmar también aparta su monto del saldo.
            ("Pago", "factura") => "SELECT f.id_factura, CONCAT('Factura ',f.id_factura,' · Saldo Q ',f.total-COALESCE(p.pagado,0)), f.total-COALESCE(p.pagado,0) FROM dbo.Factura f OUTER APPLY (SELECT SUM(monto) AS pagado FROM dbo.Pago WHERE id_factura=f.id_factura AND estado IN ('Completado','Pendiente')) p WHERE f.estado<>'Anulada' AND f.total>COALESCE(p.pagado,0) ORDER BY f.id_factura DESC",
            ("Pago", "metodo") => "SELECT id_metodo_pago, nombre FROM dbo.Metodo_Pago WHERE estado='Activo' AND activo=1 ORDER BY nombre",
            ("Confirmar transacción POS", "transaccion") => "SELECT t.id_transaccion_pos, CONCAT('#',t.id_transaccion_pos,' · Factura ',p.id_factura,' · Q ',p.monto,' · ',COALESCE(mp.nombre,'Sin método'),' · ',CONVERT(varchar(16),t.fecha_hora,120)) FROM dbo.Transaccion_POS t JOIN dbo.Pago p ON p.id_pago=t.id_pago LEFT JOIN dbo.Metodo_Pago mp ON mp.id_metodo_pago=p.id_metodo_pago WHERE t.estado='Pendiente' ORDER BY t.fecha_hora",
            ("Pago", "caja") or ("Transacción", "caja") or ("Cierre", "caja") => "SELECT id_caja, nombre FROM dbo.Caja WHERE estado='Abierta' AND activo=1 ORDER BY nombre",
            ("Editar servicio", "servicio") => "SELECT id_servicio, CONCAT(nombre,' · Q ',precio) FROM dbo.Servicio WHERE activo=1 ORDER BY nombre",
            ("Editar método de pago", "metodo") => "SELECT id_metodo_pago, CONCAT(nombre,' · ',COALESCE(estado,'Sin estado')) FROM dbo.Metodo_Pago WHERE activo=1 ORDER BY nombre",
            ("Editar caja", "caja") => "SELECT id_caja, CONCAT(nombre,' · ',COALESCE(estado,'Sin estado')) FROM dbo.Caja WHERE activo=1 ORDER BY nombre",
            // Los cobros con tarjeta no se editan: tienen su voucher en Transacciones POS y se corrigen anulándolos.
            ("Editar pago", "pago") => "SELECT p.id_pago, CONCAT('Pago ',p.id_pago,' · Factura ',p.id_factura,' · Q ',p.monto,' · ',COALESCE(m.nombre,'Sin método'),' · ',p.estado) FROM dbo.Pago p LEFT JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago WHERE p.estado='Completado' AND NOT EXISTS (SELECT 1 FROM dbo.Transaccion_POS t WHERE t.id_pago=p.id_pago) ORDER BY p.id_factura DESC, p.id_pago DESC",
            ("Editar pago", "metodo") => "SELECT id_metodo_pago, nombre FROM dbo.Metodo_Pago WHERE nombre NOT LIKE '%tarjeta%' ORDER BY nombre",
            ("Editar asistencia", "asistencia") => "SELECT TOP (500) a.id_asistencia, CONCAT(CONVERT(varchar(10),a.fecha,103),' · ',t.nombres,' ',t.apellidos) FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador WHERE a.activo=1 ORDER BY a.fecha DESC, a.id_asistencia DESC",
            ("Anular pago", "pago") => "SELECT p.id_pago, CONCAT('Pago ',p.id_pago,' · Factura ',p.id_factura,' · Q ',p.monto,' · ',COALESCE(m.nombre,'Sin método')) FROM dbo.Pago p LEFT JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago WHERE p.estado='Completado' ORDER BY p.id_factura DESC, p.id_pago DESC",
            ("Anular factura", "factura") => "SELECT f.id_factura, CONCAT('Factura ',f.id_factura,' · ',CONVERT(varchar(10),f.fecha,103),' · ',COALESCE(c.nombres,'Sin cliente'),' · Q ',f.total,' · ',f.estado) FROM dbo.Factura f LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente WHERE f.estado<>'Anulada' ORDER BY f.id_factura DESC",
            ("Editar factura", "factura") => "SELECT f.id_factura, CONCAT('Factura ',f.id_factura,' · ',CONVERT(varchar(10),f.fecha,103),' · ',COALESCE(c.nombres,'Sin cliente'),' · Q ',f.total,' · ',f.estado) FROM dbo.Factura f LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente ORDER BY f.id_factura DESC",
            // Incluye trabajadores inactivos para no perder al que atendió una factura antigua.
            ("Editar factura", "trabajador") => "SELECT id_trabajador, CONCAT(nombres,' ',apellidos) FROM dbo.Trabajador ORDER BY nombres",
            _ => throw new ArgumentException("Catálogo no permitido para esta operación.")
        };
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        await ValidarSesionAsync(conexion, sesion);
        using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@usuario", SqlDbType.Int).Value = sesion.IdUsuario;
        bool todos = tipo == "Factura" ? sesion.PuedeAbrir("Facturación") : sesion.PuedeAbrir("Trabajadores");
        comando.Parameters.Add("@todos", SqlDbType.Bit).Value = todos;
        using var lector = await comando.ExecuteReaderAsync();
        var opciones = new List<OpcionRegistro>();
        while (await lector.ReadAsync()) opciones.Add(new OpcionRegistro(lector.GetInt32(0), lector.GetString(1), lector.FieldCount > 2 && !lector.IsDBNull(2) ? lector.GetDecimal(2) : 0));
        return opciones;
    }

    // CU-03 y CU-07: datos actuales del registro para precargar el formulario de edición.
    // Cada columna se llama igual que el campo del formulario que la recibe.
    public static async Task<Dictionary<string, object>> ObtenerParaEditarAsync(SesionUsuario sesion, string tipo, int id)
    {
        if (!PuedeRegistrar(sesion, tipo)) throw new UnauthorizedAccessException("No tiene permiso para esta operación.");
        string sql = tipo switch
        {
            "Editar empleado" => "SELECT t.nombres, t.apellidos, t.telefono, t.direccion, t.salario, COALESCE(u.estado,'Activo') AS estado FROM dbo.Trabajador t LEFT JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE t.id_trabajador=@id",
            "Editar cliente" => "SELECT nombres AS nombre, nit, telefono, correo FROM dbo.Cliente WHERE id_cliente=@id",
            "Editar producto" => "SELECT id_categoria AS categoria, nombre, precio_venta AS precio, stock_minimo AS minimo FROM dbo.Producto WHERE id_producto=@id",
            "Editar servicio" => "SELECT nombre, precio, descripcion, categoria_servicio AS categoria FROM dbo.Servicio WHERE id_servicio=@id",
            "Editar categoría" => "SELECT nombre, descripcion FROM dbo.Categoria WHERE id_categoria=@id",
            "Editar método de pago" => "SELECT nombre, estado FROM dbo.Metodo_Pago WHERE id_metodo_pago=@id",
            "Editar caja" => "SELECT nombre, estado FROM dbo.Caja WHERE id_caja=@id",
            "Editar pago" => "SELECT id_metodo_pago AS metodo, referencia FROM dbo.Pago WHERE id_pago=@id",
            "Editar asistencia" => "SELECT CONVERT(varchar(5),hora_entrada,108) AS entrada, CONVERT(varchar(5),hora_salida,108) AS salida FROM dbo.Asistencia WHERE id_asistencia=@id",
            "Editar factura" => "SELECT id_cliente AS cliente, id_trabajador AS trabajador, CASE WHEN estado='Anulada' THEN 'Anulada' ELSE 'Vigente' END AS estado FROM dbo.Factura WHERE id_factura=@id",
            _ => throw new ArgumentException("Operación sin edición.")
        };
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        await ValidarSesionAsync(conexion, sesion);
        using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@id", SqlDbType.Int).Value = id;
        using var lector = await comando.ExecuteReaderAsync();
        if (!await lector.ReadAsync()) throw new InvalidOperationException("El registro ya no existe.");
        var datos = new Dictionary<string, object>();
        for (int i = 0; i < lector.FieldCount; i++)
            datos[lector.GetName(i)] = lector.IsDBNull(i) ? (lector.GetFieldType(i) == typeof(decimal) ? 0m : "") : lector.GetValue(i) is string texto ? texto.Trim() : lector.GetValue(i);
        return datos;
    }

    // Devuelve el número de la factura emitida o del cierre registrado, para ofrecer su comprobante o su reporte; en las demás operaciones devuelve null.
    public static async Task<int?> GuardarRegistroAsync(SesionUsuario sesion, string tipo, IReadOnlyDictionary<string, object> datos, IReadOnlyList<LineaRegistro>? lineas = null)
    {
        if (!PuedeRegistrar(sesion, tipo)) throw new UnauthorizedAccessException("No tiene permiso para guardar esta operación.");
        string Texto(string campo, int maximo, bool requerido = true)
        {
            string valor = datos.TryGetValue(campo, out var dato) ? Convert.ToString(dato)?.Trim() ?? "" : "";
            if ((requerido && valor.Length == 0) || valor.Length > maximo) throw new ArgumentException($"Revise el campo {campo} (máximo {maximo} caracteres).");
            return valor;
        }
        decimal Numero(string campo, decimal minimo = 0, decimal maximo = 99999999.99m)
        {
            if (!datos.TryGetValue(campo, out var valor) || !decimal.TryParse(Convert.ToString(valor), out decimal numero) || numero < minimo || numero > maximo)
                throw new ArgumentException($"Revise el valor de {campo}.");
            if (decimal.Round(numero, 2) != numero) throw new ArgumentException($"{campo} admite como máximo dos decimales.");
            return numero;
        }
        int Entero(string campo, int minimo = 1)
        {
            decimal numero = Numero(campo, minimo, int.MaxValue);
            if (numero != decimal.Truncate(numero)) throw new ArgumentException($"{campo} debe ser un entero.");
            return (int)numero;
        }
        string Clave()
        {
            string clave = datos.TryGetValue("clave", out var valor) ? Convert.ToString(valor) ?? "" : "";
            if (clave.Length < 8 || System.Text.Encoding.UTF8.GetByteCount(clave) > 72)
                throw new ArgumentException("La contraseña debe tener al menos 8 caracteres y un máximo de 72 bytes UTF-8.");
            return clave;
        }
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        using var transaccion = conexion.BeginTransaction(IsolationLevel.Serializable);
        int? facturaEmitida = null;
        // Id del registro creado por un alta, para la bitácora.
        int? registro = null;
        SqlCommand Comando(string sql, params (string Nombre, object Valor)[] valores)
        {
            var cmd = new SqlCommand(sql, conexion, transaccion);
            foreach (var valor in valores)
            {
                var parametro = cmd.Parameters.AddWithValue(valor.Nombre, valor.Valor);
                if (valor.Valor is decimal) { parametro.SqlDbType = SqlDbType.Decimal; parametro.Precision = 18; parametro.Scale = 2; }
            }
            return cmd;
        }
        async Task<int> Ejecutar(string sql, params (string Nombre, object Valor)[] valores)
        {
            using var cmd = Comando(sql, valores);
            return await cmd.ExecuteNonQueryAsync();
        }
        async Task<object?> Escalar(string sql, params (string Nombre, object Valor)[] valores)
        {
            using var cmd = Comando(sql, valores);
            return await cmd.ExecuteScalarAsync();
        }
        async Task<int> Trabajador()
        {
            bool todos = tipo == "Factura" ? sesion.PuedeAbrir("Facturación") : sesion.PuedeAbrir("Trabajadores");
            if (todos)
            {
                int seleccionado = Entero("trabajador");
                object? existe = await Escalar("SELECT t.id_trabajador FROM dbo.Trabajador t JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE t.id_trabajador=@id AND u.estado='Activo' AND t.activo=1", ("@id", seleccionado));
                if (existe == null) throw new InvalidOperationException("Seleccione un trabajador activo.");
                return seleccionado;
            }
            object? propio = await Escalar("SELECT id_trabajador FROM dbo.Trabajador WHERE id_usuario=@usuario", ("@usuario", sesion.IdUsuario));
            if (propio == null) throw new InvalidOperationException("Su usuario todavía no está vinculado a un trabajador.");
            return Convert.ToInt32(propio);
        }
        async Task ValidarCaja(int id)
        {
            if (await Escalar("SELECT id_caja FROM dbo.Caja WITH (UPDLOCK) WHERE id_caja=@id AND estado='Abierta' AND activo=1", ("@id",id)) == null)
                throw new InvalidOperationException("Seleccione una caja abierta.");
            if (await Escalar("SELECT TOP (1) id_cuadre FROM dbo.Cuadre_Caja WHERE id_caja=@id AND CAST(fecha_cierre AS date)=CAST(GETDATE() AS date)", ("@id",id)) != null)
                throw new InvalidOperationException("Esta caja ya tiene un cierre registrado hoy.");
        }
        // Fila completa en JSON (sin la contraseña) para guardar en la bitácora cómo estaba y cómo quedó.
        async Task<string?> Instantanea(string tabla, string llave, int id)
        {
            string columnas = tabla == "Usuario" ? "id_usuario, nombre_usuario, rol, estado" : "*";
            using var cmd = Comando($"SELECT {columnas} FROM dbo.{tabla} WHERE {llave}=@id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES", ("@id", id));
            using var lector = await cmd.ExecuteReaderAsync();
            var json = new System.Text.StringBuilder();
            while (await lector.ReadAsync()) json.Append(lector.GetString(0));
            return json.Length == 0 ? null : json.ToString();
        }
        // Los registros no se borran: se desactivan para conservar el historial y poder reactivarlos.
        async Task Desactivar(string tabla, string llave, int id)
        {
            if (await Ejecutar($"UPDATE dbo.{tabla} SET activo=0 WHERE {llave}=@id AND activo=1", ("@id", id)) != 1)
                throw new InvalidOperationException("El registro ya no existe o ya está desactivado.");
        }
        // Al anular: los productos vuelven al inventario y cada pago se devuelve con un egreso en su caja.
        async Task AnularFactura(int id)
        {
            await Ejecutar("UPDATE p SET stock=p.stock+d.cantidad FROM dbo.Producto p JOIN (SELECT id_producto, SUM(cantidad) AS cantidad FROM dbo.Detalle_Producto WHERE id_factura=@id GROUP BY id_producto) d ON d.id_producto=p.id_producto", ("@id",id));
            await Ejecutar("INSERT INTO dbo.Movimiento_Inventario(id_producto,tipo,cantidad,fecha,motivo) SELECT id_producto,'Entrada',SUM(cantidad),GETDATE(),@motivo FROM dbo.Detalle_Producto WHERE id_factura=@id GROUP BY id_producto", ("@motivo","Anulación factura " + id), ("@id",id));
            await Ejecutar("INSERT INTO dbo.Movimiento_Contable(id_caja,id_pago,tipo,concepto,monto,fecha,id_usuario) SELECT m.id_caja,p.id_pago,'Egreso',@concepto,p.monto,GETDATE(),@usuario FROM dbo.Pago p JOIN dbo.Movimiento_Contable m ON m.id_pago=p.id_pago AND m.tipo='Ingreso' WHERE p.id_factura=@id AND p.estado='Completado'", ("@concepto","Devolución factura anulada " + id), ("@id",id), ("@usuario",sesion.IdUsuario));
            // Los cobros con tarjeta aprobados o pendientes de confirmar también quedan anulados.
            await Ejecutar("UPDATE t SET estado='Anulada' FROM dbo.Transaccion_POS t JOIN dbo.Pago p ON p.id_pago=t.id_pago WHERE p.id_factura=@id AND t.estado IN ('Aprobada','Pendiente')", ("@id",id));
            await Ejecutar("UPDATE dbo.Pago SET estado='Anulado' WHERE id_factura=@id AND estado IN ('Completado','Pendiente')", ("@id",id));
            await Ejecutar("UPDATE dbo.Factura SET estado='Anulada' WHERE id_factura=@id", ("@id",id));
        }
        // Sección 8.3: el NIT es opcional; sin NIT el cliente se registra como consumidor final (CF).
        string Nit()
        {
            string nit = Texto("nit",15,false).Replace(" ","").ToUpperInvariant();
            if (nit is "" or "C/F") nit = "CF";
            if (nit != "CF" && !System.Text.RegularExpressions.Regex.IsMatch(nit, @"^\d{1,12}-?[\dK]$"))
                throw new ArgumentException("El NIT no es válido. Use solo números (puede terminar en K) o CF para consumidor final.");
            return nit;
        }
        string Correo()
        {
            string correo = Texto("correo",100,false);
            if (correo.Length > 0 && !System.Net.Mail.MailAddress.TryCreate(correo, out _)) throw new ArgumentException("El correo no es válido.");
            return correo;
        }
        // CU-07: antes de crear o actualizar se busca otro cliente con el mismo NIT.
        async Task ValidarNitLibre(string nit, int idActual)
        {
            if (nit == "CF") return;
            object? otro = await Escalar("SELECT TOP (1) CONCAT(nombres, CASE WHEN activo=0 THEN ' (desactivado; reactívelo desde Auditoría)' END) FROM dbo.Cliente WHERE REPLACE(nit,'-','')=REPLACE(@nit,'-','') AND id_cliente<>@id", ("@nit", nit), ("@id", idActual));
            if (otro != null) throw new InvalidOperationException($"Ya existe un cliente con ese NIT: {otro}.");
        }
        string TipoServicio()
        {
            string tipoServicio = Texto("categoria",80);
            if (!TiposServicio.Contains(tipoServicio)) throw new ArgumentException("Seleccione un tipo de servicio de la lista.");
            return tipoServicio;
        }
        // CU-17: datos del voucher del POS. Una misma autorización con la misma referencia no se registra dos veces.
        async Task<(string Autorizacion, string Referencia)> Voucher(int transaccionActual)
        {
            string autorizacion = Texto("autorizacion",12,false).ToUpperInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(autorizacion, "^[A-Z0-9]{4,12}$"))
                throw new ArgumentException("Escriba el código de autorización del voucher (de 4 a 12 letras o números).");
            string referencia = Texto("referencia",100,false);
            if (referencia.Length == 0) throw new ArgumentException("Escriba la referencia del voucher del POS.");
            if (await Escalar("SELECT TOP (1) id_transaccion_pos FROM dbo.Transaccion_POS WHERE codigo_autorizacion=@autorizacion AND referencia_pos=@referencia AND estado IN ('Aprobada','Pendiente') AND id_transaccion_pos<>@actual", ("@autorizacion",autorizacion), ("@referencia",referencia), ("@actual",transaccionActual)) != null)
                throw new InvalidOperationException("Ese voucher (autorización y referencia) ya está registrado. Revise Facturación › Transacciones POS antes de cobrar de nuevo.");
            return (autorizacion, referencia);
        }
        // Un cobro aprobado entra como ingreso de su caja y actualiza el estado de la factura.
        async Task AplicarCobro(int idPago, int caja, int facturaCobrada, decimal monto)
        {
            await Ejecutar("INSERT INTO dbo.Movimiento_Contable(id_caja,id_pago,tipo,concepto,monto,fecha,id_usuario) VALUES(@caja,@pago,'Ingreso',@concepto,@monto,GETDATE(),@usuario)", ("@caja",caja), ("@pago",idPago), ("@concepto","Pago factura " + facturaCobrada), ("@monto",monto), ("@usuario",sesion.IdUsuario));
            await Ejecutar("UPDATE dbo.Factura SET estado=CASE WHEN total<=(SELECT COALESCE(SUM(monto),0) FROM dbo.Pago WHERE id_factura=@id AND estado='Completado') THEN 'Pagada' ELSE 'Emitida' END WHERE id_factura=@id AND estado<>'Anulada'", ("@id",facturaCobrada));
        }
        try
        {
            await ValidarSesionAsync(conexion, sesion, transaccion);
            var (accion, entidad, campo) = Operacion(tipo, datos);
            var (tablaBitacora, llaveBitacora, _) = TablaBitacora(entidad);
            int? afectado = campo == null ? null : Entero(campo);
            string? antes = afectado is int previo ? await Instantanea(tablaBitacora, llaveBitacora, previo) : null;
            switch (tipo)
            {
                case "Empleado":
                    string usuario = Texto("usuario", 50);
                    string clave = Clave();
                    string rol = Texto("rol", 30);
                    if (rol is not ("Administrador" or "Trabajador")) throw new ArgumentException("Rol no válido.");
                    if (await Escalar("SELECT id_usuario FROM dbo.Usuario WHERE nombre_usuario=@nombre", ("@nombre", usuario)) != null) throw new InvalidOperationException("Ese nombre de usuario ya existe.");
                    int nuevoUsuario = Convert.ToInt32(await Escalar("INSERT INTO dbo.Usuario(nombre_usuario,contrasena_hash,rol,estado) OUTPUT INSERTED.id_usuario VALUES(@nombre,@clave,@rol,'Activo')", ("@nombre", usuario), ("@clave", BCrypt.Net.BCrypt.HashPassword(clave)), ("@rol", rol)));
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Trabajador(id_usuario,nombres,apellidos,telefono,direccion,salario) OUTPUT INSERTED.id_trabajador VALUES(@usuario,@nombres,@apellidos,@telefono,@direccion,@salario)", ("@usuario", nuevoUsuario), ("@nombres", Texto("nombres",80)), ("@apellidos", Texto("apellidos",80)), ("@telefono", Texto("telefono",20,false)), ("@direccion", Texto("direccion",200,false)), ("@salario", Numero("salario",1000m))));
                    break;
                case "Editar empleado":
                    int editado = Entero("trabajador");
                    string estadoCuenta = Texto("estado",20);
                    if (estadoCuenta is not ("Activo" or "Inactivo")) throw new ArgumentException("Estado de cuenta no válido.");
                    object? cuenta = await Escalar("SELECT id_usuario FROM dbo.Trabajador WITH (UPDLOCK) WHERE id_trabajador=@id", ("@id", editado));
                    if (cuenta == null) throw new InvalidOperationException("El trabajador ya no existe.");
                    if (cuenta is int propia && propia == sesion.IdUsuario && estadoCuenta == "Inactivo") throw new InvalidOperationException("No puede desactivar su propia cuenta.");
                    await Ejecutar("UPDATE dbo.Trabajador SET nombres=@nombres,apellidos=@apellidos,telefono=@telefono,direccion=@direccion,salario=@salario WHERE id_trabajador=@id", ("@nombres", Texto("nombres",80)), ("@apellidos", Texto("apellidos",80)), ("@telefono", Texto("telefono",20,false)), ("@direccion", Texto("direccion",200,false)), ("@salario", Numero("salario",1000m)), ("@id", editado));
                    if (cuenta is int idCuenta) await Ejecutar("UPDATE dbo.Usuario SET estado=@estado WHERE id_usuario=@id", ("@estado", estadoCuenta), ("@id", idCuenta));
                    break;
                case "Resolver préstamo":
                    string decision = Texto("decision",20);
                    if (decision is not ("Autorizar" or "Rechazar")) throw new ArgumentException("Seleccione autorizar o rechazar.");
                    // Al autorizar, el saldo pendiente es el monto completo; una solicitud resuelta no se vuelve a resolver.
                    if (await Ejecutar("UPDATE dbo.Prestamo SET estado=@estado,saldo_pendiente=CASE WHEN @estado='Autorizado' THEN monto ELSE 0 END,fecha_resolucion=SYSDATETIME() WHERE id_prestamo=@id AND estado='Pendiente'", ("@estado", decision=="Autorizar" ? "Autorizado" : "Rechazado"), ("@id", Entero("prestamo"))) != 1)
                        throw new InvalidOperationException("La solicitud ya fue resuelta.");
                    break;
                case "Descuento":
                    int prestamo = Entero("prestamo");
                    decimal descuento = Numero("monto",0.01m);
                    decimal saldoPrestamo;
                    decimal? salario;
                    using (var consulta = Comando("SELECT p.saldo_pendiente, t.salario FROM dbo.Prestamo p WITH (UPDLOCK) JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE p.id_prestamo=@id AND p.estado='Autorizado'", ("@id", prestamo)))
                    using (var lector = await consulta.ExecuteReaderAsync())
                    {
                        if (!await lector.ReadAsync()) throw new InvalidOperationException("El préstamo no está autorizado o ya fue pagado.");
                        saldoPrestamo = lector.GetDecimal(0);
                        salario = lector.IsDBNull(1) ? null : lector.GetDecimal(1);
                    }
                    if (salario is null or 0) throw new InvalidOperationException("Registre el salario del trabajador antes de aplicar el descuento.");
                    if (descuento > saldoPrestamo) throw new InvalidOperationException($"El descuento supera el saldo pendiente (Q {saldoPrestamo:N2}).");
                    if (descuento > salario) throw new InvalidOperationException($"El descuento supera el salario del trabajador (Q {salario:N2}).");
                    await Ejecutar("UPDATE dbo.Prestamo SET saldo_pendiente=saldo_pendiente-@monto,estado=CASE WHEN saldo_pendiente-@monto=0 THEN 'Pagado' ELSE estado END WHERE id_prestamo=@id", ("@monto", descuento), ("@id", prestamo));
                    await Ejecutar("INSERT INTO dbo.Descuento_Prestamo(id_prestamo,fecha,monto,observacion,id_usuario) VALUES(@prestamo,SYSDATETIME(),@monto,@observacion,@usuario)", ("@prestamo", prestamo), ("@monto", descuento), ("@observacion", Texto("observacion",100,false)), ("@usuario", sesion.IdUsuario));
                    break;
                case "Clave":
                    string nuevaClave = Clave();
                    if (await Ejecutar("UPDATE dbo.Usuario SET contrasena_hash=@clave,intentos_fallidos=0,bloqueado_hasta=NULL WHERE id_usuario=@id", ("@clave", BCrypt.Net.BCrypt.HashPassword(nuevaClave)), ("@id", Entero("usuario"))) != 1) throw new InvalidOperationException("El usuario ya no existe.");
                    break;
                case "Cliente":
                    string nitNuevo = Nit();
                    await ValidarNitLibre(nitNuevo, 0);
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Cliente(nombres,nit,telefono,correo) OUTPUT INSERTED.id_cliente VALUES(@nombre,@nit,@telefono,@correo)", ("@nombre",Texto("nombre",100)), ("@nit",nitNuevo), ("@telefono",Texto("telefono",20,false)), ("@correo",Correo())));
                    break;
                case "Editar producto":
                    int productoEditado = Entero("producto");
                    decimal precioNuevo = Numero("precio",0.01m);
                    object? precioActual = await Escalar("SELECT precio_venta FROM dbo.Producto WITH (UPDLOCK) WHERE id_producto=@id", ("@id",productoEditado));
                    if (precioActual == null) throw new InvalidOperationException("El producto ya no existe.");
                    // Sección 4.3: solo la cuenta administradora modifica precios. Las existencias cambian solo con movimientos y ventas.
                    if (!(precioActual is decimal anterior && anterior == precioNuevo) && !sesion.EsAdministrador)
                        throw new UnauthorizedAccessException("Solo el administrador puede modificar el precio.");
                    await Ejecutar("UPDATE dbo.Producto SET id_categoria=@categoria,nombre=@nombre,precio_venta=@precio,stock_minimo=@minimo WHERE id_producto=@id", ("@categoria",Entero("categoria")), ("@nombre",Texto("nombre",100)), ("@precio",precioNuevo), ("@minimo",Entero("minimo",0)), ("@id",productoEditado));
                    break;
                case "Editar cliente":
                    int clienteEditado = Entero("cliente");
                    string nitEditado = Nit();
                    await ValidarNitLibre(nitEditado, clienteEditado);
                    if (await Ejecutar("UPDATE dbo.Cliente SET nombres=@nombre,nit=@nit,telefono=@telefono,correo=@correo WHERE id_cliente=@id", ("@nombre",Texto("nombre",100)), ("@nit",nitEditado), ("@telefono",Texto("telefono",20,false)), ("@correo",Correo()), ("@id",clienteEditado)) != 1)
                        throw new InvalidOperationException("El cliente ya no existe.");
                    break;
                case "Editar servicio":
                    if (await Ejecutar("UPDATE dbo.Servicio SET nombre=@nombre,precio=@precio,descripcion=@descripcion,categoria_servicio=@categoria WHERE id_servicio=@id", ("@nombre",Texto("nombre",100)), ("@precio",Numero("precio",0.01m)), ("@descripcion",Texto("descripcion",250,false)), ("@categoria",TipoServicio()), ("@id",Entero("servicio"))) != 1)
                        throw new InvalidOperationException("El servicio ya no existe.");
                    break;
                case "Editar categoría":
                    if (await Ejecutar("UPDATE dbo.Categoria SET nombre=@nombre,descripcion=@descripcion WHERE id_categoria=@id", ("@nombre",Texto("nombre",80)), ("@descripcion",Texto("descripcion",250,false)), ("@id",Entero("categoria"))) != 1)
                        throw new InvalidOperationException("La categoría ya no existe.");
                    break;
                case "Editar método de pago":
                    string estadoMetodo = Texto("estado",20);
                    if (estadoMetodo is not ("Activo" or "Inactivo")) throw new ArgumentException("Estado no válido.");
                    if (await Ejecutar("UPDATE dbo.Metodo_Pago SET nombre=@nombre,estado=@estado WHERE id_metodo_pago=@id", ("@nombre",Texto("nombre",50)), ("@estado",estadoMetodo), ("@id",Entero("metodo"))) != 1)
                        throw new InvalidOperationException("El método de pago ya no existe.");
                    break;
                case "Editar caja":
                    string estadoCaja = Texto("estado",20);
                    if (estadoCaja is not ("Abierta" or "Cerrada")) throw new ArgumentException("Estado no válido.");
                    if (await Ejecutar("UPDATE dbo.Caja SET nombre=@nombre,estado=@estado WHERE id_caja=@id", ("@nombre",Texto("nombre",80)), ("@estado",estadoCaja), ("@id",Entero("caja"))) != 1)
                        throw new InvalidOperationException("La caja ya no existe.");
                    break;
                case "Editar pago":
                    // El monto no se edita para no descuadrar la caja; solo el método y la referencia.
                    if (await Escalar("SELECT nombre FROM dbo.Metodo_Pago WHERE id_metodo_pago=@id", ("@id",Entero("metodo"))) is not string metodoEditado) throw new InvalidOperationException("Seleccione un método de pago.");
                    if (EsMetodoTarjeta(metodoEditado) || await Escalar("SELECT TOP (1) 1 FROM dbo.Transaccion_POS WHERE id_pago=@id", ("@id",Entero("pago"))) != null)
                        throw new InvalidOperationException("Los cobros con tarjeta llevan los datos del voucher del POS. Anule el pago y regístrelo de nuevo.");
                    if (await Ejecutar("UPDATE dbo.Pago SET id_metodo_pago=@metodo,referencia=@referencia WHERE id_pago=@id AND estado='Completado'", ("@metodo",Entero("metodo")), ("@referencia",Texto("referencia",100,false)), ("@id",Entero("pago"))) != 1)
                        throw new InvalidOperationException("El pago ya no existe.");
                    break;
                case "Editar asistencia":
                    TimeSpan Hora(string campo, bool requerido)
                    {
                        string texto = Texto(campo,5,requerido);
                        if (texto.Length == 0) return TimeSpan.MinValue;
                        if (!TimeSpan.TryParseExact(texto, @"h\:mm", System.Globalization.CultureInfo.InvariantCulture, out var hora) || hora >= TimeSpan.FromDays(1))
                            throw new ArgumentException($"Escriba la hora de {campo} como HH:mm (por ejemplo 08:30).");
                        return hora;
                    }
                    TimeSpan entrada = Hora("entrada",true), salida = Hora("salida",false);
                    if (salida != TimeSpan.MinValue && salida < entrada) throw new ArgumentException("La salida no puede ser antes de la entrada.");
                    if (await Ejecutar("UPDATE dbo.Asistencia SET hora_entrada=@entrada,hora_salida=@salida WHERE id_asistencia=@id", ("@entrada",entrada), ("@salida",salida == TimeSpan.MinValue ? DBNull.Value : (object)salida), ("@id",Entero("asistencia"))) != 1)
                        throw new InvalidOperationException("El registro de asistencia ya no existe.");
                    break;
                case "Editar factura":
                    int facturaEditada = Entero("factura");
                    string estadoFactura = Texto("estado",20);
                    if (estadoFactura is not ("Vigente" or "Anulada")) throw new ArgumentException("Estado no válido.");
                    object? estadoActual = await Escalar("SELECT estado FROM dbo.Factura WITH (UPDLOCK) WHERE id_factura=@id", ("@id",facturaEditada));
                    if (estadoActual == null) throw new InvalidOperationException("La factura ya no existe.");
                    bool yaAnulada = (string)estadoActual == "Anulada";
                    if (yaAnulada && estadoFactura == "Vigente") throw new InvalidOperationException("Una factura anulada no se puede reactivar. Emita una nueva.");
                    if (await Escalar("SELECT id_cliente FROM dbo.Cliente WHERE id_cliente=@id", ("@id",Entero("cliente"))) == null) throw new InvalidOperationException("Seleccione un cliente.");
                    if (await Escalar("SELECT id_trabajador FROM dbo.Trabajador WHERE id_trabajador=@id", ("@id",Entero("trabajador"))) == null) throw new InvalidOperationException("Seleccione un trabajador.");
                    await Ejecutar("UPDATE dbo.Factura SET id_cliente=@cliente,id_trabajador=@trabajador WHERE id_factura=@id", ("@cliente",Entero("cliente")), ("@trabajador",Entero("trabajador")), ("@id",facturaEditada));
                    if (!yaAnulada && estadoFactura == "Anulada") await AnularFactura(facturaEditada);
                    break;
                case "Desactivar empleado":
                    int empleadoDesactivado = Entero("trabajador");
                    object? cuentaDesactivada = await Escalar("SELECT COALESCE(id_usuario,0) FROM dbo.Trabajador WITH (UPDLOCK) WHERE id_trabajador=@id AND activo=1", ("@id",empleadoDesactivado));
                    if (cuentaDesactivada == null) throw new InvalidOperationException("El trabajador ya no existe o ya está desactivado.");
                    if (Convert.ToInt32(cuentaDesactivada) == sesion.IdUsuario) throw new InvalidOperationException("No puede desactivar su propia cuenta.");
                    // Con la cuenta inactiva tampoco puede iniciar sesión.
                    await Desactivar("Trabajador", "id_trabajador", empleadoDesactivado);
                    await Ejecutar("UPDATE dbo.Usuario SET estado='Inactivo' WHERE id_usuario=@id", ("@id",cuentaDesactivada));
                    break;
                case "Desactivar cliente":
                    int clienteDesactivado = Entero("cliente");
                    if (await Escalar("SELECT TOP (1) 1 FROM dbo.Cliente WHERE id_cliente=@id AND nombres='Consumidor Final' AND nit='CF'", ("@id",clienteDesactivado)) != null)
                        throw new InvalidOperationException("Consumidor Final es el cliente genérico y no se puede desactivar.");
                    await Desactivar("Cliente", "id_cliente", clienteDesactivado);
                    break;
                case "Desactivar producto": await Desactivar("Producto", "id_producto", Entero("producto")); break;
                case "Desactivar categoría":
                    int categoriaDesactivada = Entero("categoria");
                    if (await Escalar("SELECT TOP (1) 1 FROM dbo.Producto WHERE id_categoria=@id AND activo=1", ("@id",categoriaDesactivada)) != null)
                        throw new InvalidOperationException("La categoría tiene productos activos. Cámbielos de categoría o desactívelos primero.");
                    await Desactivar("Categoria", "id_categoria", categoriaDesactivada);
                    break;
                case "Desactivar servicio": await Desactivar("Servicio", "id_servicio", Entero("servicio")); break;
                case "Desactivar método de pago": await Desactivar("Metodo_Pago", "id_metodo_pago", Entero("metodo")); break;
                case "Desactivar caja": await Desactivar("Caja", "id_caja", Entero("caja")); break;
                case "Desactivar asistencia": await Desactivar("Asistencia", "id_asistencia", Entero("asistencia")); break;
                case "Anular pago":
                    // El pago queda anulado y su ingreso se devuelve con un egreso en la misma caja; la factura vuelve a tener saldo pendiente.
                    int pagoAnulado = Entero("pago");
                    object? facturaDelPago = await Escalar("SELECT id_factura FROM dbo.Pago WITH (UPDLOCK) WHERE id_pago=@id AND estado='Completado'", ("@id",pagoAnulado));
                    if (facturaDelPago == null) throw new InvalidOperationException("El pago ya no existe o ya está anulado.");
                    await Ejecutar("INSERT INTO dbo.Movimiento_Contable(id_caja,id_pago,tipo,concepto,monto,fecha,id_usuario) SELECT m.id_caja,p.id_pago,'Egreso',@concepto,p.monto,GETDATE(),@usuario FROM dbo.Pago p JOIN dbo.Movimiento_Contable m ON m.id_pago=p.id_pago AND m.tipo='Ingreso' WHERE p.id_pago=@id", ("@concepto","Anulación pago " + pagoAnulado), ("@id",pagoAnulado), ("@usuario",sesion.IdUsuario));
                    await Ejecutar("UPDATE dbo.Pago SET estado='Anulado' WHERE id_pago=@id", ("@id",pagoAnulado));
                    // CU-17: la anulación del cobro con tarjeta queda registrada en su transacción POS.
                    await Ejecutar("UPDATE dbo.Transaccion_POS SET estado='Anulada' WHERE id_pago=@id AND estado='Aprobada'", ("@id",pagoAnulado));
                    if (facturaDelPago != DBNull.Value) await Ejecutar("UPDATE dbo.Factura SET estado='Emitida' WHERE id_factura=@id AND estado='Pagada'", ("@id",facturaDelPago));
                    break;
                case "Anular factura":
                    int facturaAnulada = Entero("factura");
                    if (await Escalar("SELECT id_factura FROM dbo.Factura WITH (UPDLOCK) WHERE id_factura=@id AND estado<>'Anulada'", ("@id",facturaAnulada)) == null)
                        throw new InvalidOperationException("La factura ya no existe o ya está anulada.");
                    await AnularFactura(facturaAnulada);
                    break;
                case "Reactivar":
                    var (tablaReactivada, llaveReactivada, _) = TablaBitacora(Texto("entidad",40));
                    if (tablaReactivada is "Factura" or "Pago") throw new InvalidOperationException("Las facturas y los pagos anulados no se pueden reactivar. Registre uno nuevo.");
                    if (!TablasDesactivables.Contains(tablaReactivada)) throw new ArgumentException("Este registro no se puede reactivar.");
                    int reactivado = Entero("id");
                    if (await Ejecutar($"UPDATE dbo.{tablaReactivada} SET activo=1 WHERE {llaveReactivada}=@id AND activo=0", ("@id",reactivado)) != 1)
                        throw new InvalidOperationException("El registro ya no existe o ya está activo.");
                    if (tablaReactivada == "Trabajador")
                        await Ejecutar("UPDATE u SET estado='Activo' FROM dbo.Usuario u JOIN dbo.Trabajador t ON t.id_usuario=u.id_usuario WHERE t.id_trabajador=@id", ("@id",reactivado));
                    break;
                case "Categoría":
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Categoria(nombre,descripcion) OUTPUT INSERTED.id_categoria VALUES(@nombre,@descripcion)", ("@nombre",Texto("nombre",80)), ("@descripcion",Texto("descripcion",250,false))));
                    break;
                case "Producto":
                    int productoNuevo = (int)(registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Producto(id_categoria,nombre,precio_venta,stock,stock_minimo) OUTPUT INSERTED.id_producto VALUES(@categoria,@nombre,@precio,@stock,@minimo)", ("@categoria",Entero("categoria")), ("@nombre",Texto("nombre",100)), ("@precio",Numero("precio",0.01m)), ("@stock",Entero("stock",0)), ("@minimo",Entero("minimo",0)))));
                    if (Entero("stock",0)>0) await Ejecutar("INSERT INTO dbo.Movimiento_Inventario(id_producto,tipo,cantidad,fecha,motivo) VALUES(@producto,'Entrada',@cantidad,GETDATE(),'Existencia inicial')", ("@producto",productoNuevo), ("@cantidad",Entero("stock",0)));
                    break;
                case "Movimiento":
                    string movimiento = Texto("tipo",20);
                    if (movimiento is not ("Entrada" or "Salida")) throw new ArgumentException("Tipo de movimiento no válido.");
                    int cantidad = Entero("cantidad");
                    int producto = Entero("producto");
                    int delta = movimiento=="Entrada" ? cantidad : -cantidad;
                    if (await Ejecutar("UPDATE dbo.Producto SET stock=stock+@delta WHERE id_producto=@id AND activo=1 AND CONVERT(bigint,stock)+@delta BETWEEN 0 AND 2147483647", ("@delta",delta), ("@id",producto)) != 1) throw new InvalidOperationException("Producto no disponible o existencias insuficientes.");
                    await Ejecutar("INSERT INTO dbo.Movimiento_Inventario(id_producto,tipo,cantidad,fecha,motivo) VALUES(@producto,@tipo,@cantidad,GETDATE(),@motivo)", ("@producto",producto), ("@tipo",movimiento), ("@cantidad",cantidad), ("@motivo",Texto("motivo",250)));
                    break;
                case "Servicio":
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Servicio(nombre,precio,descripcion,categoria_servicio) OUTPUT INSERTED.id_servicio VALUES(@nombre,@precio,@descripcion,@categoria)", ("@nombre",Texto("nombre",100)), ("@precio",Numero("precio",0.01m)), ("@descripcion",Texto("descripcion",250,false)), ("@categoria",TipoServicio())));
                    break;
                case "Caja":
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Caja(nombre,estado) OUTPUT INSERTED.id_caja VALUES(@nombre,'Abierta')", ("@nombre",Texto("nombre",80))));
                    break;
                case "Método de pago":
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Metodo_Pago(nombre,estado) OUTPUT INSERTED.id_metodo_pago VALUES(@nombre,'Activo')", ("@nombre",Texto("nombre",50))));
                    break;
                case "Transacción":
                    await ValidarCaja(Entero("caja"));
                    string tipoMovimiento = Texto("tipo",20);
                    if (tipoMovimiento is not ("Ingreso" or "Egreso")) throw new ArgumentException("Tipo de transacción no válido.");
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Movimiento_Contable(id_caja,id_pago,tipo,concepto,monto,fecha,id_usuario) OUTPUT INSERTED.id_movimiento VALUES(@caja,NULL,@tipo,@concepto,@monto,GETDATE(),@usuario)", ("@caja",Entero("caja")), ("@tipo",tipoMovimiento), ("@concepto",Texto("concepto",200)), ("@monto",Numero("monto",0.01m)), ("@usuario",sesion.IdUsuario)));
                    break;
                case "Préstamo":
                    registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Prestamo(id_trabajador,fecha_solicitud,monto,saldo_pendiente,estado) OUTPUT INSERTED.id_prestamo VALUES(@trabajador,CAST(GETDATE() AS date),@monto,@monto,'Pendiente')", ("@trabajador",await Trabajador()), ("@monto",Numero("monto",0.01m))));
                    break;
                case "Asistencia":
                    int trabajador = await Trabajador();
                    DateTime momento = Convert.ToDateTime(await Escalar("SELECT GETDATE()"));
                    string marca = Texto("tipo",20);
                    if (marca=="Entrada")
                    {
                        if (await Escalar("SELECT id_asistencia FROM dbo.Asistencia WHERE id_trabajador=@id AND fecha=@fecha AND activo=1", ("@id",trabajador), ("@fecha",momento.Date)) != null) throw new InvalidOperationException("Ya existe una entrada registrada hoy.");
                        registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Asistencia(id_trabajador,fecha,hora_entrada) OUTPUT INSERTED.id_asistencia VALUES(@id,@fecha,@hora)", ("@id",trabajador), ("@fecha",momento.Date), ("@hora",momento.TimeOfDay)));
                    }
                    else if (marca=="Salida")
                    {
                        registro = await Escalar("UPDATE dbo.Asistencia SET hora_salida=@hora OUTPUT INSERTED.id_asistencia WHERE id_trabajador=@id AND fecha=@fecha AND activo=1 AND hora_salida IS NULL AND hora_entrada<=@hora", ("@id",trabajador), ("@fecha",momento.Date), ("@hora",momento.TimeOfDay)) as int?
                            ?? throw new InvalidOperationException("No hay una entrada pendiente de salida para hoy.");
                    }
                    else throw new ArgumentException("Seleccione entrada o salida.");
                    break;
                case "Factura":
                    if (lineas == null || lineas.Count == 0 || lineas.Count>200) throw new ArgumentException("Agregue al menos un producto o servicio (máximo 200 líneas).");
                    int factura = Convert.ToInt32(await Escalar("INSERT INTO dbo.Factura(id_cliente,id_trabajador,fecha,subtotal,total,estado) OUTPUT INSERTED.id_factura VALUES(@cliente,@trabajador,GETDATE(),0,0,'Emitida')", ("@cliente",Entero("cliente")), ("@trabajador",await Trabajador())));
                    decimal total = 0;
                    foreach (var linea in lineas)
                    {
                        if (linea.Cantidad<1 || linea.Cantidad>100000 || linea.Id<1 || linea.Tipo is not ("Producto" or "Servicio")) throw new ArgumentException("Detalle de factura no válido.");
                        bool esProducto = linea.Tipo=="Producto";
                        object? precioDato = await Escalar(esProducto ? "SELECT precio_venta FROM dbo.Producto WHERE id_producto=@id AND activo=1" : "SELECT precio FROM dbo.Servicio WHERE id_servicio=@id AND activo=1", ("@id",linea.Id));
                        if (precioDato == null || precioDato == DBNull.Value) throw new InvalidOperationException("Un producto o servicio ya no existe o no tiene precio.");
                        decimal precio = Convert.ToDecimal(precioDato);
                        if (precio != linea.PrecioEsperado) throw new InvalidOperationException("El precio de un artículo cambió. Vuelva a abrir la factura para cargar los precios actuales.");
                        decimal subtotal = decimal.Round(precio * linea.Cantidad,2);
                        if (precio<=0 || subtotal>99999999.99m || total+subtotal>99999999.99m) throw new ArgumentException("El total de la factura o el precio no es válido.");
                        if (esProducto)
                        {
                            if (await Ejecutar("UPDATE dbo.Producto SET stock=stock-@cantidad WHERE id_producto=@id AND stock>=@cantidad", ("@cantidad",linea.Cantidad), ("@id",linea.Id)) != 1) throw new InvalidOperationException("Existencias insuficientes para uno de los productos. No se guardó la factura.");
                            await Ejecutar("INSERT INTO dbo.Detalle_Producto(id_factura,id_producto,cantidad,precio_unitario,subtotal) VALUES(@factura,@id,@cantidad,@precio,@subtotal)", ("@factura",factura), ("@id",linea.Id), ("@cantidad",linea.Cantidad), ("@precio",precio), ("@subtotal",subtotal));
                            await Ejecutar("INSERT INTO dbo.Movimiento_Inventario(id_producto,tipo,cantidad,fecha,motivo) VALUES(@id,'Salida',@cantidad,GETDATE(),@motivo)", ("@id",linea.Id), ("@cantidad",linea.Cantidad), ("@motivo","Factura " + factura));
                        }
                        else await Ejecutar("INSERT INTO dbo.Detalle_Servicio(id_factura,id_servicio,cantidad,precio_unitario,subtotal) VALUES(@factura,@id,@cantidad,@precio,@subtotal)", ("@factura",factura), ("@id",linea.Id), ("@cantidad",linea.Cantidad), ("@precio",precio), ("@subtotal",subtotal));
                        total += subtotal;
                    }
                    await Ejecutar("UPDATE dbo.Factura SET subtotal=@total,total=@total WHERE id_factura=@id", ("@total",total), ("@id",factura));
                    facturaEmitida = registro = factura;
                    break;
                case "Pago":
                    int cajaPago = Entero("caja");
                    await ValidarCaja(cajaPago);
                    if (await Escalar("SELECT nombre FROM dbo.Metodo_Pago WHERE id_metodo_pago=@id AND estado='Activo' AND activo=1", ("@id",Entero("metodo"))) is not string metodoPago) throw new InvalidOperationException("Seleccione un método de pago activo.");
                    int facturaPago = Entero("factura");
                    object? importeFactura = await Escalar("SELECT total FROM dbo.Factura WITH (UPDLOCK) WHERE id_factura=@id AND estado<>'Anulada'", ("@id",facturaPago));
                    if (importeFactura == null || importeFactura == DBNull.Value) throw new InvalidOperationException("La factura no está disponible.");
                    // Lo cobrado más lo que espera confirmación del POS: el mismo saldo no se cobra dos veces.
                    decimal pagado = Convert.ToDecimal(await Escalar("SELECT COALESCE(SUM(monto),0) FROM dbo.Pago WHERE id_factura=@id AND estado IN ('Completado','Pendiente')", ("@id",facturaPago)));
                    decimal montoPago = Numero("monto",0.01m);
                    if (montoPago>Convert.ToDecimal(importeFactura)-pagado) throw new InvalidOperationException("El monto supera el saldo pendiente de la factura (incluye cobros con tarjeta por confirmar).");
                    bool tarjeta = EsMetodoTarjeta(metodoPago);
                    // CU-17: con tarjeta se registra lo que respondió la terminal POS: aprobada, rechazada o sin respuesta clara (pendiente).
                    string resultadoPos = tarjeta ? Texto("resultado",20) : "Aprobada";
                    if (resultadoPos is not ("Aprobada" or "Rechazada" or "Pendiente")) throw new ArgumentException("Seleccione el resultado del POS.");
                    string referenciaPago = Texto("referencia",100,false);
                    string? autorizacionPago = null;
                    if (tarjeta && resultadoPos == "Aprobada") (autorizacionPago, referenciaPago) = await Voucher(0);
                    string estadoPago = resultadoPos switch { "Aprobada" => "Completado", "Rechazada" => "Rechazado", _ => "Pendiente" };
                    int idPago = (int)(registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Pago(id_factura,id_metodo_pago,fecha,monto,referencia,estado) OUTPUT INSERTED.id_pago VALUES(@factura,@metodo,GETDATE(),@monto,@referencia,@estado)", ("@factura",facturaPago), ("@metodo",Entero("metodo")), ("@monto",montoPago), ("@referencia",referenciaPago), ("@estado",estadoPago))));
                    if (tarjeta)
                        await Ejecutar("INSERT INTO dbo.Transaccion_POS(id_pago,codigo_autorizacion,referencia_pos,estado,fecha_hora,id_caja,id_usuario) VALUES(@pago,@autorizacion,@referencia,@estado,GETDATE(),@caja,@usuario)",
                            ("@pago",idPago), ("@autorizacion",(object?)autorizacionPago ?? DBNull.Value), ("@referencia",referenciaPago.Length == 0 ? DBNull.Value : referenciaPago), ("@estado",resultadoPos), ("@caja",cajaPago), ("@usuario",sesion.IdUsuario));
                    if (estadoPago == "Completado") await AplicarCobro(idPago, cajaPago, facturaPago, montoPago);
                    break;
                case "Confirmar transacción POS":
                    // CU-17: una respuesta incierta se confirma después con el voucher o la consulta en la terminal.
                    int transaccionPos = Entero("transaccion");
                    int pagoPos, cajaPos, facturaPos;
                    decimal montoPos;
                    using (var consulta = Comando("SELECT p.id_pago, COALESCE(t.id_caja,0), p.id_factura, p.monto FROM dbo.Transaccion_POS t WITH (UPDLOCK) JOIN dbo.Pago p WITH (UPDLOCK) ON p.id_pago=t.id_pago WHERE t.id_transaccion_pos=@id AND t.estado='Pendiente' AND p.estado='Pendiente'", ("@id",transaccionPos)))
                    using (var lector = await consulta.ExecuteReaderAsync())
                    {
                        if (!await lector.ReadAsync()) throw new InvalidOperationException("La transacción ya fue confirmada, rechazada o anulada.");
                        pagoPos = lector.GetInt32(0); cajaPos = lector.GetInt32(1); facturaPos = lector.GetInt32(2); montoPos = lector.GetDecimal(3);
                    }
                    string resultadoConfirmado = Texto("resultado",20);
                    if (resultadoConfirmado == "Aprobada")
                    {
                        await ValidarCaja(cajaPos);
                        if (await Escalar("SELECT id_factura FROM dbo.Factura WITH (UPDLOCK) WHERE id_factura=@id AND estado<>'Anulada'", ("@id",facturaPos)) == null) throw new InvalidOperationException("La factura fue anulada; marque la transacción como rechazada.");
                        var (autorizacionPos, referenciaPos) = await Voucher(transaccionPos);
                        await Ejecutar("UPDATE dbo.Pago SET estado='Completado', referencia=@referencia WHERE id_pago=@id", ("@referencia",referenciaPos), ("@id",pagoPos));
                        await Ejecutar("UPDATE dbo.Transaccion_POS SET estado='Aprobada', codigo_autorizacion=@autorizacion, referencia_pos=@referencia WHERE id_transaccion_pos=@id", ("@autorizacion",autorizacionPos), ("@referencia",referenciaPos), ("@id",transaccionPos));
                        await AplicarCobro(pagoPos, cajaPos, facturaPos, montoPos);
                    }
                    else if (resultadoConfirmado == "Rechazada")
                    {
                        await Ejecutar("UPDATE dbo.Pago SET estado='Rechazado' WHERE id_pago=@id", ("@id",pagoPos));
                        await Ejecutar("UPDATE dbo.Transaccion_POS SET estado='Rechazada' WHERE id_transaccion_pos=@id", ("@id",transaccionPos));
                    }
                    else throw new ArgumentException("Seleccione aprobada o rechazada.");
                    break;
                case "Cierre":
                    int caja = Entero("caja");
                    await ValidarCaja(caja);
                    DateTime ultimoCierre = Convert.ToDateTime(await Escalar("SELECT COALESCE(MAX(fecha_cierre),CONVERT(datetime2,'19000101')) FROM dbo.Cuadre_Caja WHERE id_caja=@caja", ("@caja",caja)));
                    // CU-20: no se cierra con operaciones pendientes; primero se cobran, confirman o anulan.
                    var pendientes = new List<string>();
                    if (await Escalar("SELECT STRING_AGG(CONCAT('#',id_transaccion_pos),', ') FROM dbo.Transaccion_POS WHERE id_caja=@caja AND estado='Pendiente'", ("@caja",caja)) is string pos)
                        pendientes.Add("• Transacciones POS por confirmar: " + pos + " (Facturación › Transacciones POS).");
                    if (await Escalar("SELECT STRING_AGG(CAST(id_factura AS varchar(12)),', ') FROM (SELECT TOP (10) id_factura FROM dbo.Factura WHERE estado='Emitida' AND fecha>@desde ORDER BY id_factura) f", ("@desde",ultimoCierre)) is string facturas)
                        pendientes.Add("• Facturas del período sin cobrar por completo: " + facturas + " (cóbrelas en Facturación › Pagos o anúlelas).");
                    if (pendientes.Count > 0) throw new InvalidOperationException("No se puede cerrar la caja con operaciones pendientes:" + Environment.NewLine + string.Join(Environment.NewLine, pendientes));
                    decimal contado = Numero("contado");
                    // RF14: lo que no queda como fondo se retira; con fondo 0 la caja inicia el siguiente período en ceros.
                    decimal fondo = Numero("fondo");
                    if (fondo > contado) throw new ArgumentException("El fondo que queda en caja no puede ser mayor que el efectivo contado.");
                    // Solo el efectivo está en la gaveta: los cobros con tarjeta, transferencia o QR se concilian aparte.
                    object? esperado = await Escalar("""
                        SELECT COALESCE((SELECT TOP (1) COALESCE(fondo_siguiente,monto_real) FROM dbo.Cuadre_Caja WHERE id_caja=@caja ORDER BY fecha_cierre DESC,id_cuadre DESC),0)
                            + COALESCE((SELECT SUM(CASE WHEN m.tipo='Ingreso' THEN m.monto WHEN m.tipo='Egreso' THEN -m.monto ELSE 0 END)
                                FROM dbo.Movimiento_Contable m LEFT JOIN dbo.Pago p ON p.id_pago=m.id_pago LEFT JOIN dbo.Metodo_Pago mp ON mp.id_metodo_pago=p.id_metodo_pago
                                WHERE m.id_caja=@caja AND m.fecha>@desde AND (m.id_pago IS NULL OR mp.nombre LIKE '%efectivo%')),0)
                        """, ("@caja",caja), ("@desde",ultimoCierre));
                    decimal saldo = Convert.ToDecimal(esperado);
                    facturaEmitida = registro = Convert.ToInt32(await Escalar("INSERT INTO dbo.Cuadre_Caja(id_caja,fecha_cierre,monto_esperado,monto_real,diferencia,estado,id_usuario,fondo_siguiente) OUTPUT INSERTED.id_cuadre VALUES(@caja,GETDATE(),@esperado,@real,@diferencia,'Cerrada',@usuario,@fondo)", ("@caja",caja), ("@esperado",saldo), ("@real",contado), ("@diferencia",contado-saldo), ("@usuario",sesion.IdUsuario), ("@fondo",fondo)));
                    break;
                default: throw new ArgumentException("Operación desconocida.");
            }
            afectado ??= registro;
            string? despues = afectado is int actual ? await Instantanea(tablaBitacora, llaveBitacora, actual) : null;
            await Ejecutar("INSERT INTO dbo.Bitacora(id_usuario,usuario,accion,entidad,id_registro,descripcion,antes,despues) VALUES(@usuario,@nombre,@accion,@entidad,@registro,@descripcion,@antes,@despues)",
                ("@usuario", sesion.IdUsuario), ("@nombre", sesion.NombreUsuario), ("@accion", accion), ("@entidad", entidad), ("@registro", (object?)afectado ?? DBNull.Value),
                ("@descripcion", Describir(antes, despues)), ("@antes", (object?)antes ?? DBNull.Value), ("@despues", (object?)despues ?? DBNull.Value));
            transaccion.Commit();
            return facturaEmitida;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    // Acción y registro de cada operación para la bitácora; Campo es el dato con el Id del registro existente (las altas no lo tienen).
    private static (string Accion, string Entidad, string? Campo) Operacion(string tipo, IReadOnlyDictionary<string, object> datos)
    {
        string Dato(string campo) => datos.TryGetValue(campo, out var valor) ? Convert.ToString(valor) ?? "" : "";
        foreach (string prefijo in new[] { "Editar ", "Desactivar ", "Anular " })
            if (tipo.StartsWith(prefijo))
            {
                string entidad = char.ToUpper(tipo[prefijo.Length]) + tipo[(prefijo.Length + 1)..];
                return (prefijo.TrimEnd(), entidad, TablaBitacora(entidad).Campo);
            }
        return tipo switch
        {
            "Reactivar" => ("Reactivar", Dato("entidad"), "id"),
            "Clave" => ("Restablecer clave", "Usuario", "usuario"),
            "Resolver préstamo" => (Dato("decision"), "Préstamo", "prestamo"),
            "Descuento" => ("Descontar de salario", "Préstamo", "prestamo"),
            "Movimiento" => (Dato("tipo") + " de inventario", "Producto", "producto"),
            "Asistencia" => ("Marcar " + Dato("tipo").ToLowerInvariant(), "Asistencia", null),
            "Transacción" => ("Registrar " + Dato("tipo").ToLowerInvariant(), "Transacción", null),
            "Cierre" => ("Registrar", "Cierre de caja", null),
            "Confirmar transacción POS" => (Dato("resultado") == "Aprobada" ? "Aprobar" : "Rechazar", "Transacción POS", "transaccion"),
            _ => ("Registrar", tipo, null)
        };
    }

    // Lista cerrada de tablas: el nombre de la tabla nunca viene del usuario.
    private static (string Tabla, string Llave, string Campo) TablaBitacora(string entidad) => entidad switch
    {
        "Empleado" => ("Trabajador", "id_trabajador", "trabajador"),
        "Cliente" => ("Cliente", "id_cliente", "cliente"),
        "Producto" => ("Producto", "id_producto", "producto"),
        "Categoría" => ("Categoria", "id_categoria", "categoria"),
        "Servicio" => ("Servicio", "id_servicio", "servicio"),
        "Método de pago" => ("Metodo_Pago", "id_metodo_pago", "metodo"),
        "Caja" => ("Caja", "id_caja", "caja"),
        "Asistencia" => ("Asistencia", "id_asistencia", "asistencia"),
        "Factura" => ("Factura", "id_factura", "factura"),
        "Pago" => ("Pago", "id_pago", "pago"),
        "Préstamo" => ("Prestamo", "id_prestamo", "prestamo"),
        "Usuario" => ("Usuario", "id_usuario", "usuario"),
        "Transacción" => ("Movimiento_Contable", "id_movimiento", "movimiento"),
        "Cierre de caja" => ("Cuadre_Caja", "id_cuadre", "cuadre"),
        "Transacción POS" => ("Transaccion_POS", "id_transaccion_pos", "transaccion"),
        _ => throw new ArgumentException("Registro desconocido.")
    };

    // Resumen legible para la bitácora: nombre del registro y campos que cambiaron, o los datos guardados en un alta.
    internal static string Describir(string? antes, string? despues)
    {
        static Dictionary<string, string?> Campos(string? json)
        {
            var campos = new Dictionary<string, string?>();
            if (string.IsNullOrEmpty(json)) return campos;
            using var documento = System.Text.Json.JsonDocument.Parse(json);
            foreach (var propiedad in documento.RootElement.EnumerateObject())
                campos[propiedad.Name] = propiedad.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Null => null,
                    System.Text.Json.JsonValueKind.True => "Sí",
                    System.Text.Json.JsonValueKind.False => "No",
                    System.Text.Json.JsonValueKind.String => propiedad.Value.GetString(),
                    _ => propiedad.Value.GetRawText()
                };
            return campos;
        }
        static string Etiqueta(string campo) => (campo.StartsWith("id_") ? campo[3..] : campo).Replace('_', ' ');
        static string Valor(string? valor) => string.IsNullOrEmpty(valor) ? "vacío" : valor;
        var previo = Campos(antes);
        var actual = Campos(despues);
        var referencia = actual.Count > 0 ? actual : previo;
        string? nombre = referencia.GetValueOrDefault("nombres") is string nombres ? (nombres + " " + referencia.GetValueOrDefault("apellidos")).Trim()
            : referencia.GetValueOrDefault("nombre") ?? referencia.GetValueOrDefault("nombre_usuario") ?? referencia.GetValueOrDefault("concepto");
        // La primera columna es la llave del registro, que ya aparece en la columna ID.
        var detalle = previo.Count > 0 && actual.Count > 0
            ? actual.Where(c => previo.GetValueOrDefault(c.Key) != c.Value).Select(c => $"{Etiqueta(c.Key)}: {Valor(previo.GetValueOrDefault(c.Key))} → {Valor(c.Value)}")
            : referencia.Skip(1).Where(c => !string.IsNullOrEmpty(c.Value)).Select(c => $"{Etiqueta(c.Key)}: {c.Value}");
        string texto = ((nombre != null ? $"«{nombre}» " : "") + string.Join("; ", detalle)).Trim();
        return texto.Length > 1000 ? texto[..999] + "…" : texto;
    }

    internal static IConfigurationSection Configuracion(string seccion) => Config.Value.GetSection(seccion);

    // CU-15 y RF8: datos de una factura para su comprobante. Sin acceso a Facturación, solo se ven las facturas propias.
    public static async Task<Comprobante> ObtenerComprobanteAsync(SesionUsuario sesion, int idFactura)
    {
        bool todas = sesion.PuedeAbrir("Facturación");
        if (!todas && !sesion.PuedeAbrir("Mi jornada")) throw new UnauthorizedAccessException("No tiene permiso para consultar comprobantes.");
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        await ValidarSesionAsync(conexion, sesion);
        using var comando = new SqlCommand("""
            SELECT f.id_factura, f.fecha, f.estado, f.total, c.nombres, COALESCE(c.nit,'CF'), c.telefono, CONCAT(t.nombres,' ',t.apellidos),
                COALESCE((SELECT SUM(monto) FROM dbo.Pago WHERE id_factura=f.id_factura AND estado='Completado'),0),
                (SELECT STRING_AGG(m.nombre, ', ') FROM (SELECT DISTINCT m.nombre FROM dbo.Pago p JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago WHERE p.id_factura=f.id_factura AND p.estado='Completado') m)
            FROM dbo.Factura f LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador
            WHERE f.id_factura=@id AND (@todas=1 OR t.id_usuario=@usuario);
            SELECT s.nombre, d.cantidad, d.precio_unitario, d.subtotal FROM dbo.Detalle_Servicio d JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio WHERE d.id_factura=@id
            UNION ALL
            SELECT p.nombre, d.cantidad, d.precio_unitario, d.subtotal FROM dbo.Detalle_Producto d JOIN dbo.Producto p ON p.id_producto=d.id_producto WHERE d.id_factura=@id;
            """, conexion);
        comando.Parameters.Add("@id", SqlDbType.Int).Value = idFactura;
        comando.Parameters.Add("@todas", SqlDbType.Bit).Value = todas;
        comando.Parameters.Add("@usuario", SqlDbType.Int).Value = sesion.IdUsuario;
        using var lector = await comando.ExecuteReaderAsync();
        if (!await lector.ReadAsync()) throw new InvalidOperationException("La factura no existe o no tiene permiso para verla.");
        string? Texto(int i) => lector.IsDBNull(i) ? null : lector.GetString(i).Trim();
        var comprobante = new Comprobante(lector.GetInt32(0), lector.GetDateTime(1), Texto(2) ?? "", lector.IsDBNull(3) ? 0 : lector.GetDecimal(3),
            Texto(4) ?? "Consumidor final", Texto(5) ?? "CF", Texto(6), Texto(7), lector.GetDecimal(8), Texto(9), new List<LineaComprobante>());
        await lector.NextResultAsync();
        while (await lector.ReadAsync())
            comprobante.Lineas.Add(new LineaComprobante(lector.GetString(0), lector.GetInt32(1), lector.GetDecimal(2), lector.GetDecimal(3)));
        return comprobante;
    }

    // RF14 y CU-20: datos de un cierre para su reporte diario: período, movimientos por origen y cuadre del efectivo.
    public static async Task<ReporteCierre> ObtenerCierreAsync(SesionUsuario sesion, int idCuadre)
    {
        sesion.ExigirAcceso("Contabilidad");
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        await ValidarSesionAsync(conexion, sesion);
        using var comando = new SqlCommand("""
            SELECT q.id_cuadre, COALESCE(c.nombre,'Sin caja'), q.fecha_cierre, a.fecha_cierre, COALESCE(a.fondo_siguiente,a.monto_real,0),
                q.monto_esperado, q.monto_real, q.diferencia, COALESCE(q.fondo_siguiente,q.monto_real), u.nombre_usuario, q.id_caja
            FROM dbo.Cuadre_Caja q LEFT JOIN dbo.Caja c ON c.id_caja=q.id_caja LEFT JOIN dbo.Usuario u ON u.id_usuario=q.id_usuario
            OUTER APPLY (SELECT TOP (1) fecha_cierre, fondo_siguiente, monto_real FROM dbo.Cuadre_Caja
                WHERE id_caja=q.id_caja AND (fecha_cierre<q.fecha_cierre OR (fecha_cierre=q.fecha_cierre AND id_cuadre<q.id_cuadre)) ORDER BY fecha_cierre DESC, id_cuadre DESC) a
            WHERE q.id_cuadre=@id
            """, conexion);
        comando.Parameters.Add("@id", SqlDbType.Int).Value = idCuadre;
        ReporteCierre reporte;
        int caja;
        using (var lector = await comando.ExecuteReaderAsync())
        {
            if (!await lector.ReadAsync()) throw new InvalidOperationException("El cierre no existe.");
            reporte = new ReporteCierre(lector.GetInt32(0), lector.GetString(1), lector.GetDateTime(2), lector.IsDBNull(3) ? null : lector.GetDateTime(3),
                lector.GetDecimal(4), lector.GetDecimal(5), lector.GetDecimal(6), lector.GetDecimal(7), lector.GetDecimal(8), lector.IsDBNull(9) ? null : lector.GetString(9), new List<LineaCierre>());
            caja = lector.IsDBNull(10) ? 0 : lector.GetInt32(10);
        }
        using var movimientos = new SqlCommand("""
            SELECT m.tipo, CASE WHEN m.id_pago IS NULL THEN 'Manual' ELSE COALESCE(mp.nombre,'Sin método') END AS origen,
                CAST(CASE WHEN m.id_pago IS NULL OR mp.nombre LIKE '%efectivo%' THEN 1 ELSE 0 END AS bit) AS efectivo, COUNT(*), SUM(m.monto)
            FROM dbo.Movimiento_Contable m LEFT JOIN dbo.Pago p ON p.id_pago=m.id_pago LEFT JOIN dbo.Metodo_Pago mp ON mp.id_metodo_pago=p.id_metodo_pago
            WHERE m.id_caja=@caja AND m.fecha>@desde AND m.fecha<=@hasta
            GROUP BY m.tipo, CASE WHEN m.id_pago IS NULL THEN 'Manual' ELSE COALESCE(mp.nombre,'Sin método') END, CASE WHEN m.id_pago IS NULL OR mp.nombre LIKE '%efectivo%' THEN 1 ELSE 0 END
            ORDER BY m.tipo DESC, efectivo DESC, origen
            """, conexion);
        movimientos.Parameters.Add("@caja", SqlDbType.Int).Value = caja;
        movimientos.Parameters.Add("@desde", SqlDbType.DateTime2).Value = reporte.Desde ?? new DateTime(1900, 1, 1);
        movimientos.Parameters.Add("@hasta", SqlDbType.DateTime2).Value = reporte.Fecha;
        using (var lector = await movimientos.ExecuteReaderAsync())
            while (await lector.ReadAsync())
                reporte.Movimientos.Add(new LineaCierre(lector.GetString(0).Trim(), lector.GetString(1).Trim(), lector.GetBoolean(2), lector.GetInt32(3), lector.GetDecimal(4)));
        return reporte;
    }
}

namespace Giovanny_Barbers
{
    public sealed record OpcionRegistro(int Id, string Nombre, decimal Precio = 0);
    public sealed record LineaComprobante(string Descripcion, int Cantidad, decimal Precio, decimal Subtotal);
    public sealed record Comprobante(int Numero, DateTime Fecha, string Estado, decimal Total, string Cliente, string Nit, string? Telefono,
        string? Trabajador, decimal Pagado, string? Metodos, List<LineaComprobante> Lineas);
    public sealed record LineaRegistro(string Tipo, int Id, int Cantidad, decimal PrecioEsperado);
    public sealed record LineaCierre(string Tipo, string Origen, bool Efectivo, int Cantidad, decimal Monto);
    public sealed record ReporteCierre(int Numero, string Caja, DateTime Fecha, DateTime? Desde, decimal FondoInicial, decimal Esperado,
        decimal Contado, decimal Diferencia, decimal FondoSiguiente, string? Usuario, List<LineaCierre> Movimientos);
}
