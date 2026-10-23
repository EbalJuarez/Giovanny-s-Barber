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
        var consultas = new List<(string Titulo, string Sql, bool Moneda)>();
        if (sesion.PuedeAbrir("Contabilidad"))
            consultas.Add(("Ingresos del día", "SELECT COALESCE(SUM(monto),0) FROM dbo.Movimiento_Contable WHERE tipo='Ingreso' AND CAST(fecha AS date)=CAST(GETDATE() AS date)", true));
        if (sesion.PuedeAbrir("Trabajadores"))
            consultas.Add(("Trabajadores activos", "SELECT COUNT(*) FROM dbo.Trabajador t JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE u.estado='Activo'", false));
        if (sesion.PuedeAbrir("Inventario"))
            consultas.Add(("Productos con stock bajo", "SELECT COUNT(*) FROM dbo.Producto WHERE stock<=stock_minimo", false));
        if (sesion.PuedeAbrir("Facturación"))
            consultas.Add(("Facturas del día", "SELECT COUNT(*) FROM dbo.Factura WHERE CAST(fecha AS date)=CAST(GETDATE() AS date)", false));
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
            decimal valor = Convert.ToDecimal(await comando.ExecuteScalarAsync(cancelacion));
            resumen.Add(consulta.Titulo, consulta.Moneda ? "Q " + valor.ToString("N2") : valor.ToString("N0"));
        }
        return resumen;
    }

    // Lista cerrada de consultas: nunca se incorporan nombres o filtros suministrados por el usuario.
    internal static string ConsultaModulo(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Trabajadores", "Directorio") => "SELECT t.id_trabajador AS ID, CONCAT(t.nombres,' ',t.apellidos) AS Nombre, t.telefono AS Teléfono, t.direccion AS Dirección, t.salario AS Salario, u.nombre_usuario AS Usuario, u.rol AS Rol, u.estado AS Estado FROM dbo.Trabajador t LEFT JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario ORDER BY t.nombres",
        ("Trabajadores", "Descuentos") => "SELECT d.fecha AS Fecha, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, d.id_prestamo AS Préstamo, d.monto AS Monto, p.saldo_pendiente AS [Saldo actual], d.observacion AS Observación FROM dbo.Descuento_Prestamo d JOIN dbo.Prestamo p ON p.id_prestamo=d.id_prestamo JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador ORDER BY d.fecha DESC",
        ("Trabajadores", "Actividades") => "SELECT f.fecha AS Fecha, 'Servicio' AS Tipo, s.nombre AS Descripción, c.nombres AS Cliente, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, d.cantidad AS Cantidad, d.subtotal AS Total FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador UNION ALL SELECT f.fecha, 'Venta', p.nombre, c.nombres, CONCAT(t.nombres,' ',t.apellidos), d.cantidad, d.subtotal FROM dbo.Detalle_Producto d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Producto p ON p.id_producto=d.id_producto LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador ORDER BY Fecha DESC",
        ("Trabajadores", "Usuarios") => "SELECT id_usuario AS ID, nombre_usuario AS Usuario, rol AS Rol, estado AS Estado FROM dbo.Usuario ORDER BY nombre_usuario",
        ("Inventario", "Categorías") => "SELECT id_categoria AS ID, nombre AS Nombre, descripcion AS Descripción FROM dbo.Categoria ORDER BY nombre",
        ("Facturación", "Servicios") => "SELECT id_servicio AS ID, nombre AS Servicio, precio AS Precio, descripcion AS Descripción, categoria_servicio AS Categoría FROM dbo.Servicio ORDER BY nombre",
        ("Facturación", "Pagos") => "SELECT p.id_pago AS ID, p.id_factura AS Factura, p.fecha AS Fecha, m.nombre AS Método, p.monto AS Monto, p.referencia AS Referencia, p.estado AS Estado FROM dbo.Pago p LEFT JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago ORDER BY p.id_factura DESC, p.id_pago DESC",
        ("Contabilidad", "Cajas") => "SELECT id_caja AS ID, nombre AS Caja, estado AS Estado FROM dbo.Caja ORDER BY nombre",
        ("Trabajadores", "Asistencia") => "SELECT a.fecha AS Fecha, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, a.hora_entrada AS Entrada, a.hora_salida AS Salida FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador ORDER BY a.fecha DESC",
        ("Mi jornada", "Mi asistencia") => "SELECT a.fecha AS Fecha, a.hora_entrada AS Entrada, a.hora_salida AS Salida FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador WHERE t.id_usuario=@usuario ORDER BY a.fecha DESC",
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
        ("Clientes", "Clientes") => "SELECT id_cliente AS ID, nombres AS Nombre, nit AS NIT, telefono AS Teléfono, correo AS Correo FROM dbo.Cliente ORDER BY nombres",
        ("Inventario", "Productos") => "SELECT p.id_producto AS ID, p.nombre AS Producto, c.nombre AS Categoría, p.precio_venta AS Precio, p.stock AS Existencias, p.stock_minimo AS [Stock mínimo], CASE WHEN p.stock<=p.stock_minimo THEN 'Stock bajo' ELSE 'Disponible' END AS Disponibilidad FROM dbo.Producto p LEFT JOIN dbo.Categoria c ON c.id_categoria=p.id_categoria ORDER BY p.nombre",
        ("Inventario", "Movimientos") => "SELECT m.fecha AS Fecha, p.nombre AS Producto, m.tipo AS Tipo, m.cantidad AS Cantidad, m.motivo AS Motivo FROM dbo.Movimiento_Inventario m LEFT JOIN dbo.Producto p ON p.id_producto=m.id_producto ORDER BY m.fecha DESC",
        ("Facturación", "Facturas") => "SELECT f.id_factura AS Número, f.fecha AS Fecha, c.nombres AS Cliente, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, f.subtotal AS Subtotal, f.total AS Total, f.estado AS Estado FROM dbo.Factura f LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador ORDER BY f.fecha DESC",
        ("Facturación", "Detalle de factura") => "SELECT d.id_factura AS Factura, 'Producto' AS Tipo, p.nombre AS Descripción, d.cantidad AS Cantidad, d.precio_unitario AS Precio, d.subtotal AS Subtotal FROM dbo.Detalle_Producto d JOIN dbo.Producto p ON p.id_producto=d.id_producto UNION ALL SELECT d.id_factura, 'Servicio', s.nombre, d.cantidad, d.precio_unitario, d.subtotal FROM dbo.Detalle_Servicio d JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio ORDER BY Factura DESC",
        ("Contabilidad", "Ingresos y egresos") => "SELECT m.fecha AS Fecha, c.nombre AS Caja, m.concepto AS Concepto, m.tipo AS Tipo, m.monto AS Monto FROM dbo.Movimiento_Contable m LEFT JOIN dbo.Caja c ON c.id_caja=m.id_caja ORDER BY m.fecha DESC",
        ("Contabilidad", "Cuadre de caja") or ("Contabilidad", "Cierres diarios") => "SELECT q.fecha_cierre AS Fecha, c.nombre AS Caja, q.monto_esperado AS [Saldo esperado], q.monto_real AS [Efectivo contado], q.diferencia AS Diferencia, q.estado AS Estado FROM dbo.Cuadre_Caja q LEFT JOIN dbo.Caja c ON c.id_caja=q.id_caja ORDER BY q.fecha_cierre DESC",
        _ => throw new UnauthorizedAccessException("La sección solicitada no pertenece a este módulo.")
    };
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
        _ when tipo.StartsWith("Eliminar ") => sesion.EsAdministrador,
        "Cliente" or "Editar cliente" => sesion.PuedeAbrir("Clientes"),
        "Producto" or "Editar producto" or "Categoría" or "Movimiento" => sesion.PuedeAbrir("Inventario"),
        "Transacción" or "Caja" or "Cierre" => sesion.PuedeAbrir("Contabilidad"),
        "Pago" => sesion.PuedeAbrir("Facturación"),
        "Factura" => sesion.PuedeAbrir("Facturación") || sesion.PuedeAbrir("Mi jornada"),
        "Asistencia" or "Préstamo" => sesion.PuedeAbrir("Trabajadores") || sesion.PuedeAbrir("Mi jornada"),
        _ => false
    };

    public static async Task<List<OpcionRegistro>> OpcionesRegistroAsync(SesionUsuario sesion, string tipo, string catalogo)
    {
        if (!PuedeRegistrar(sesion, tipo)) throw new UnauthorizedAccessException("No tiene permiso para esta operación.");
        // Eliminar un registro usa la misma lista que su edición.
        string origen = tipo.StartsWith("Eliminar ") ? "Editar " + tipo["Eliminar ".Length..] : tipo;
        string sql = (origen, catalogo) switch
        {
            ("Empleado", "rol") => "SELECT 1 AS ID, 'Administrador' AS Nombre UNION ALL SELECT 2, 'Trabajador'",
            ("Clave", "usuario") => "SELECT id_usuario, nombre_usuario FROM dbo.Usuario ORDER BY nombre_usuario",
            ("Editar empleado", "trabajador") => "SELECT t.id_trabajador, CONCAT(t.nombres,' ',t.apellidos,' · ',COALESCE(u.estado,'Sin usuario')) FROM dbo.Trabajador t LEFT JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario ORDER BY t.nombres",
            ("Resolver préstamo", "prestamo") => "SELECT p.id_prestamo, CONCAT('#',p.id_prestamo,' · ',t.nombres,' ',t.apellidos,' · Q ',p.monto,' · ',CONVERT(varchar(10),p.fecha_solicitud,103)) FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE p.estado='Pendiente' ORDER BY p.fecha_solicitud",
            ("Descuento", "prestamo") => "SELECT p.id_prestamo, CONCAT('#',p.id_prestamo,' · ',t.nombres,' ',t.apellidos,' · Saldo Q ',p.saldo_pendiente,' · Salario ',COALESCE(CONCAT('Q ',t.salario),'sin registrar')) FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE p.estado='Autorizado' AND p.saldo_pendiente>0 ORDER BY p.fecha_solicitud",
            ("Producto", "categoria") => "SELECT id_categoria, nombre FROM dbo.Categoria ORDER BY nombre",
            ("Factura", "cliente") or ("Editar cliente", "cliente") or ("Editar factura", "cliente") => "SELECT id_cliente, CONCAT(nombres,' · NIT ',COALESCE(nit,'CF')) FROM dbo.Cliente ORDER BY CASE WHEN nombres='Consumidor Final' AND nit='CF' THEN 0 ELSE 1 END, nombres",
            ("Factura", "producto") => "SELECT id_producto, nombre, precio_venta FROM dbo.Producto ORDER BY nombre",
            // CU-12: la existencia actual se muestra al elegir el producto del movimiento.
            ("Movimiento", "producto") => "SELECT id_producto, CONCAT(nombre,' · Existencia ',stock) FROM dbo.Producto ORDER BY nombre",
            ("Editar producto", "producto") => "SELECT p.id_producto, CONCAT(p.nombre,' · ',COALESCE(c.nombre,'Sin categoría')) FROM dbo.Producto p LEFT JOIN dbo.Categoria c ON c.id_categoria=p.id_categoria ORDER BY p.nombre",
            ("Editar producto", "categoria") => "SELECT id_categoria, nombre FROM dbo.Categoria ORDER BY nombre",
            ("Factura", "servicio") => "SELECT id_servicio, nombre, precio FROM dbo.Servicio ORDER BY nombre",
            ("Factura", "trabajador") or ("Asistencia", "trabajador") or ("Préstamo", "trabajador") =>
                "SELECT t.id_trabajador, CONCAT(t.nombres,' ',t.apellidos) FROM dbo.Trabajador t JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE u.estado='Activo' AND (@todos=1 OR t.id_usuario=@usuario) ORDER BY t.nombres",
            ("Pago", "factura") => "SELECT f.id_factura, CONCAT('Factura ',f.id_factura,' · Saldo Q ',f.total-COALESCE(p.pagado,0)), f.total-COALESCE(p.pagado,0) FROM dbo.Factura f OUTER APPLY (SELECT SUM(monto) AS pagado FROM dbo.Pago WHERE id_factura=f.id_factura AND estado='Completado') p WHERE f.estado<>'Anulada' AND f.total>COALESCE(p.pagado,0) ORDER BY f.id_factura DESC",
            ("Pago", "metodo") => "SELECT id_metodo_pago, nombre FROM dbo.Metodo_Pago WHERE estado='Activo' ORDER BY nombre",
            ("Pago", "caja") or ("Transacción", "caja") or ("Cierre", "caja") => "SELECT id_caja, nombre FROM dbo.Caja WHERE estado='Abierta' ORDER BY nombre",
            ("Editar servicio", "servicio") => "SELECT id_servicio, CONCAT(nombre,' · Q ',precio) FROM dbo.Servicio ORDER BY nombre",
            ("Editar categoría", "categoria") => "SELECT id_categoria, nombre FROM dbo.Categoria ORDER BY nombre",
            ("Editar método de pago", "metodo") => "SELECT id_metodo_pago, CONCAT(nombre,' · ',COALESCE(estado,'Sin estado')) FROM dbo.Metodo_Pago ORDER BY nombre",
            ("Editar caja", "caja") => "SELECT id_caja, CONCAT(nombre,' · ',COALESCE(estado,'Sin estado')) FROM dbo.Caja ORDER BY nombre",
            ("Editar pago", "pago") => "SELECT p.id_pago, CONCAT('Pago ',p.id_pago,' · Factura ',p.id_factura,' · Q ',p.monto,' · ',COALESCE(m.nombre,'Sin método'),' · ',p.estado) FROM dbo.Pago p LEFT JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago ORDER BY p.id_factura DESC, p.id_pago DESC",
            ("Editar pago", "metodo") => "SELECT id_metodo_pago, nombre FROM dbo.Metodo_Pago ORDER BY nombre",
            ("Editar asistencia", "asistencia") => "SELECT TOP (500) a.id_asistencia, CONCAT(CONVERT(varchar(10),a.fecha,103),' · ',t.nombres,' ',t.apellidos) FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador ORDER BY a.fecha DESC, a.id_asistencia DESC",
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

    // Devuelve el número de factura emitida para ofrecer el comprobante; en las demás operaciones devuelve null.
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
                object? existe = await Escalar("SELECT t.id_trabajador FROM dbo.Trabajador t JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE t.id_trabajador=@id AND u.estado='Activo'", ("@id", seleccionado));
                if (existe == null) throw new InvalidOperationException("Seleccione un trabajador activo.");
                return seleccionado;
            }
            object? propio = await Escalar("SELECT id_trabajador FROM dbo.Trabajador WHERE id_usuario=@usuario", ("@usuario", sesion.IdUsuario));
            if (propio == null) throw new InvalidOperationException("Su usuario todavía no está vinculado a un trabajador.");
            return Convert.ToInt32(propio);
        }
        async Task ValidarCaja(int id)
        {
            if (await Escalar("SELECT id_caja FROM dbo.Caja WITH (UPDLOCK) WHERE id_caja=@id AND estado='Abierta'", ("@id",id)) == null)
                throw new InvalidOperationException("Seleccione una caja abierta.");
            if (await Escalar("SELECT TOP (1) id_cuadre FROM dbo.Cuadre_Caja WHERE id_caja=@id AND CAST(fecha_cierre AS date)=CAST(GETDATE() AS date)", ("@id",id)) != null)
                throw new InvalidOperationException("Esta caja ya tiene un cierre registrado hoy.");
        }
        // Un registro con movimientos relacionados no se elimina, para no dejar datos huérfanos.
        async Task SinUso(string sql, int id, string mensaje)
        {
            if (await Escalar(sql, ("@id", id)) != null) throw new InvalidOperationException(mensaje);
        }
        async Task Borrar(string sql, int id, string noExiste)
        {
            if (await Ejecutar(sql, ("@id", id)) != 1) throw new InvalidOperationException(noExiste);
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
            object? otro = await Escalar("SELECT TOP (1) nombres FROM dbo.Cliente WHERE REPLACE(nit,'-','')=REPLACE(@nit,'-','') AND id_cliente<>@id", ("@nit", nit), ("@id", idActual));
            if (otro != null) throw new InvalidOperationException($"Ya existe un cliente con ese NIT: {otro}.");
        }
        try
        {
            await ValidarSesionAsync(conexion, sesion, transaccion);
            switch (tipo)
            {
                case "Empleado":
                    string usuario = Texto("usuario", 50);
                    string clave = Clave();
                    string rol = Texto("rol", 30);
                    if (rol is not ("Administrador" or "Trabajador")) throw new ArgumentException("Rol no válido.");
                    if (await Escalar("SELECT id_usuario FROM dbo.Usuario WHERE nombre_usuario=@nombre", ("@nombre", usuario)) != null) throw new InvalidOperationException("Ese nombre de usuario ya existe.");
                    int nuevoUsuario = Convert.ToInt32(await Escalar("INSERT INTO dbo.Usuario(nombre_usuario,contrasena_hash,rol,estado) OUTPUT INSERTED.id_usuario VALUES(@nombre,@clave,@rol,'Activo')", ("@nombre", usuario), ("@clave", BCrypt.Net.BCrypt.HashPassword(clave)), ("@rol", rol)));
                    await Ejecutar("INSERT INTO dbo.Trabajador(id_usuario,nombres,apellidos,telefono,direccion,salario) VALUES(@usuario,@nombres,@apellidos,@telefono,@direccion,@salario)", ("@usuario", nuevoUsuario), ("@nombres", Texto("nombres",80)), ("@apellidos", Texto("apellidos",80)), ("@telefono", Texto("telefono",20,false)), ("@direccion", Texto("direccion",200,false)), ("@salario", Numero("salario",1000m)));
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
                    await Ejecutar("INSERT INTO dbo.Cliente(nombres,nit,telefono,correo) VALUES(@nombre,@nit,@telefono,@correo)", ("@nombre",Texto("nombre",100)), ("@nit",nitNuevo), ("@telefono",Texto("telefono",20,false)), ("@correo",Correo()));
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
                    if (await Ejecutar("UPDATE dbo.Servicio SET nombre=@nombre,precio=@precio,descripcion=@descripcion,categoria_servicio=@categoria WHERE id_servicio=@id", ("@nombre",Texto("nombre",100)), ("@precio",Numero("precio",0.01m)), ("@descripcion",Texto("descripcion",250,false)), ("@categoria",Texto("categoria",80)), ("@id",Entero("servicio"))) != 1)
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
                    if (await Escalar("SELECT id_metodo_pago FROM dbo.Metodo_Pago WHERE id_metodo_pago=@id", ("@id",Entero("metodo"))) == null) throw new InvalidOperationException("Seleccione un método de pago.");
                    if (await Ejecutar("UPDATE dbo.Pago SET id_metodo_pago=@metodo,referencia=@referencia WHERE id_pago=@id", ("@metodo",Entero("metodo")), ("@referencia",Texto("referencia",100,false)), ("@id",Entero("pago"))) != 1)
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
                    if (!yaAnulada && estadoFactura == "Anulada")
                    {
                        // Al anular: los productos vuelven al inventario y cada pago se devuelve con un egreso en su caja.
                        await Ejecutar("UPDATE p SET stock=p.stock+d.cantidad FROM dbo.Producto p JOIN (SELECT id_producto, SUM(cantidad) AS cantidad FROM dbo.Detalle_Producto WHERE id_factura=@id GROUP BY id_producto) d ON d.id_producto=p.id_producto", ("@id",facturaEditada));
                        await Ejecutar("INSERT INTO dbo.Movimiento_Inventario(id_producto,tipo,cantidad,fecha,motivo) SELECT id_producto,'Entrada',SUM(cantidad),GETDATE(),@motivo FROM dbo.Detalle_Producto WHERE id_factura=@id GROUP BY id_producto", ("@motivo","Anulación factura " + facturaEditada), ("@id",facturaEditada));
                        await Ejecutar("INSERT INTO dbo.Movimiento_Contable(id_caja,id_pago,tipo,concepto,monto,fecha) SELECT m.id_caja,p.id_pago,'Egreso',@concepto,p.monto,GETDATE() FROM dbo.Pago p JOIN dbo.Movimiento_Contable m ON m.id_pago=p.id_pago AND m.tipo='Ingreso' WHERE p.id_factura=@id AND p.estado='Completado'", ("@concepto","Devolución factura anulada " + facturaEditada), ("@id",facturaEditada));
                        await Ejecutar("UPDATE dbo.Pago SET estado='Anulado' WHERE id_factura=@id AND estado='Completado'", ("@id",facturaEditada));
                        await Ejecutar("UPDATE dbo.Factura SET estado='Anulada' WHERE id_factura=@id", ("@id",facturaEditada));
                    }
                    break;
                case "Eliminar empleado":
                    int empleadoBorrado = Entero("trabajador");
                    object? cuentaBorrada = await Escalar("SELECT COALESCE(id_usuario,0) FROM dbo.Trabajador WITH (UPDLOCK) WHERE id_trabajador=@id", ("@id",empleadoBorrado));
                    if (cuentaBorrada == null) throw new InvalidOperationException("El trabajador ya no existe.");
                    int idCuentaBorrada = Convert.ToInt32(cuentaBorrada);
                    if (idCuentaBorrada == sesion.IdUsuario) throw new InvalidOperationException("No puede eliminar su propia cuenta.");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Factura WHERE id_trabajador=@id UNION ALL SELECT TOP (1) 1 FROM dbo.Asistencia WHERE id_trabajador=@id UNION ALL SELECT TOP (1) 1 FROM dbo.Prestamo WHERE id_trabajador=@id", empleadoBorrado,
                        "El trabajador tiene facturas, asistencia o préstamos registrados. Desactívelo con Editar empleado.");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Descuento_Prestamo WHERE id_usuario=@id", idCuentaBorrada, "El usuario registró descuentos de salario. Desactívelo con Editar empleado.");
                    await Ejecutar("DELETE FROM dbo.Trabajador WHERE id_trabajador=@id", ("@id",empleadoBorrado));
                    await Ejecutar("DELETE FROM dbo.Usuario WHERE id_usuario=@id", ("@id",idCuentaBorrada));
                    break;
                case "Eliminar cliente":
                    int clienteBorrado = Entero("cliente");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Cliente WHERE id_cliente=@id AND nombres='Consumidor Final' AND nit='CF'", clienteBorrado, "Consumidor Final es el cliente genérico y no se puede eliminar.");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Factura WHERE id_cliente=@id", clienteBorrado, "El cliente tiene facturas registradas y no se puede eliminar.");
                    await Borrar("DELETE FROM dbo.Cliente WHERE id_cliente=@id", clienteBorrado, "El cliente ya no existe.");
                    break;
                case "Eliminar producto":
                    int productoBorrado = Entero("producto");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Detalle_Producto WHERE id_producto=@id", productoBorrado, "El producto ya se vendió en alguna factura y no se puede eliminar.");
                    await Ejecutar("DELETE FROM dbo.Movimiento_Inventario WHERE id_producto=@id", ("@id",productoBorrado));
                    await Borrar("DELETE FROM dbo.Producto WHERE id_producto=@id", productoBorrado, "El producto ya no existe.");
                    break;
                case "Eliminar categoría":
                    int categoriaBorrada = Entero("categoria");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Producto WHERE id_categoria=@id", categoriaBorrada, "La categoría tiene productos. Cámbielos de categoría o elimínelos primero.");
                    await Borrar("DELETE FROM dbo.Categoria WHERE id_categoria=@id", categoriaBorrada, "La categoría ya no existe.");
                    break;
                case "Eliminar servicio":
                    int servicioBorrado = Entero("servicio");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Detalle_Servicio WHERE id_servicio=@id", servicioBorrado, "El servicio ya se cobró en alguna factura y no se puede eliminar.");
                    await Borrar("DELETE FROM dbo.Servicio WHERE id_servicio=@id", servicioBorrado, "El servicio ya no existe.");
                    break;
                case "Eliminar método de pago":
                    int metodoBorrado = Entero("metodo");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Pago WHERE id_metodo_pago=@id", metodoBorrado, "El método tiene pagos registrados. Desactívelo con Editar método de pago.");
                    await Borrar("DELETE FROM dbo.Metodo_Pago WHERE id_metodo_pago=@id", metodoBorrado, "El método de pago ya no existe.");
                    break;
                case "Eliminar caja":
                    int cajaBorrada = Entero("caja");
                    await SinUso("SELECT TOP (1) 1 FROM dbo.Movimiento_Contable WHERE id_caja=@id UNION ALL SELECT TOP (1) 1 FROM dbo.Cuadre_Caja WHERE id_caja=@id", cajaBorrada, "La caja tiene movimientos o cierres. Ciérrela con Editar caja.");
                    await Borrar("DELETE FROM dbo.Caja WHERE id_caja=@id", cajaBorrada, "La caja ya no existe.");
                    break;
                case "Eliminar asistencia":
                    await Borrar("DELETE FROM dbo.Asistencia WHERE id_asistencia=@id", Entero("asistencia"), "El registro de asistencia ya no existe.");
                    break;
                case "Eliminar pago":
                    // Se quita el pago con su ingreso en caja; la factura vuelve a tener saldo pendiente.
                    int pagoBorrado = Entero("pago");
                    object? facturaDelPago = await Escalar("SELECT id_factura FROM dbo.Pago WITH (UPDLOCK) WHERE id_pago=@id", ("@id",pagoBorrado));
                    if (facturaDelPago == null) throw new InvalidOperationException("El pago ya no existe.");
                    await Ejecutar("DELETE FROM dbo.Transaccion_POS WHERE id_pago=@id", ("@id",pagoBorrado));
                    await Ejecutar("DELETE FROM dbo.Movimiento_Contable WHERE id_pago=@id", ("@id",pagoBorrado));
                    await Ejecutar("DELETE FROM dbo.Pago WHERE id_pago=@id", ("@id",pagoBorrado));
                    if (facturaDelPago != DBNull.Value) await Ejecutar("UPDATE dbo.Factura SET estado='Emitida' WHERE id_factura=@id AND estado='Pagada'", ("@id",facturaDelPago));
                    break;
                case "Eliminar factura":
                    // Se quita la factura con su detalle, pagos y movimientos; los productos vuelven al inventario.
                    int facturaBorrada = Entero("factura");
                    object? estadoBorrada = await Escalar("SELECT estado FROM dbo.Factura WITH (UPDLOCK) WHERE id_factura=@id", ("@id",facturaBorrada));
                    if (estadoBorrada == null) throw new InvalidOperationException("La factura ya no existe.");
                    // Una factura anulada ya devolvió sus productos al anularse.
                    if (Convert.ToString(estadoBorrada) != "Anulada")
                        await Ejecutar("UPDATE p SET stock=p.stock+d.cantidad FROM dbo.Producto p JOIN (SELECT id_producto, SUM(cantidad) AS cantidad FROM dbo.Detalle_Producto WHERE id_factura=@id GROUP BY id_producto) d ON d.id_producto=p.id_producto", ("@id",facturaBorrada));
                    await Ejecutar("DELETE FROM dbo.Movimiento_Inventario WHERE motivo IN (@venta,@anulacion)", ("@venta","Factura " + facturaBorrada), ("@anulacion","Anulación factura " + facturaBorrada));
                    await Ejecutar("DELETE FROM dbo.Transaccion_POS WHERE id_pago IN (SELECT id_pago FROM dbo.Pago WHERE id_factura=@id)", ("@id",facturaBorrada));
                    await Ejecutar("DELETE FROM dbo.Movimiento_Contable WHERE id_pago IN (SELECT id_pago FROM dbo.Pago WHERE id_factura=@id)", ("@id",facturaBorrada));
                    await Ejecutar("DELETE FROM dbo.Pago WHERE id_factura=@id", ("@id",facturaBorrada));
                    await Ejecutar("DELETE FROM dbo.Detalle_Producto WHERE id_factura=@id", ("@id",facturaBorrada));
                    await Ejecutar("DELETE FROM dbo.Detalle_Servicio WHERE id_factura=@id", ("@id",facturaBorrada));
                    await Ejecutar("DELETE FROM dbo.Factura WHERE id_factura=@id", ("@id",facturaBorrada));
                    break;
                case "Categoría":
                    await Ejecutar("INSERT INTO dbo.Categoria(nombre,descripcion) VALUES(@nombre,@descripcion)", ("@nombre",Texto("nombre",80)), ("@descripcion",Texto("descripcion",250,false)));
                    break;
                case "Producto":
                    int productoNuevo = Convert.ToInt32(await Escalar("INSERT INTO dbo.Producto(id_categoria,nombre,precio_venta,stock,stock_minimo) OUTPUT INSERTED.id_producto VALUES(@categoria,@nombre,@precio,@stock,@minimo)", ("@categoria",Entero("categoria")), ("@nombre",Texto("nombre",100)), ("@precio",Numero("precio",0.01m)), ("@stock",Entero("stock",0)), ("@minimo",Entero("minimo",0))));
                    if (Entero("stock",0)>0) await Ejecutar("INSERT INTO dbo.Movimiento_Inventario(id_producto,tipo,cantidad,fecha,motivo) VALUES(@producto,'Entrada',@cantidad,GETDATE(),'Existencia inicial')", ("@producto",productoNuevo), ("@cantidad",Entero("stock",0)));
                    break;
                case "Movimiento":
                    string movimiento = Texto("tipo",20);
                    if (movimiento is not ("Entrada" or "Salida")) throw new ArgumentException("Tipo de movimiento no válido.");
                    int cantidad = Entero("cantidad");
                    int producto = Entero("producto");
                    int delta = movimiento=="Entrada" ? cantidad : -cantidad;
                    if (await Ejecutar("UPDATE dbo.Producto SET stock=stock+@delta WHERE id_producto=@id AND CONVERT(bigint,stock)+@delta BETWEEN 0 AND 2147483647", ("@delta",delta), ("@id",producto)) != 1) throw new InvalidOperationException("Producto no disponible o existencias insuficientes.");
                    await Ejecutar("INSERT INTO dbo.Movimiento_Inventario(id_producto,tipo,cantidad,fecha,motivo) VALUES(@producto,@tipo,@cantidad,GETDATE(),@motivo)", ("@producto",producto), ("@tipo",movimiento), ("@cantidad",cantidad), ("@motivo",Texto("motivo",250)));
                    break;
                case "Servicio":
                    await Ejecutar("INSERT INTO dbo.Servicio(nombre,precio,descripcion,categoria_servicio) VALUES(@nombre,@precio,@descripcion,@categoria)", ("@nombre",Texto("nombre",100)), ("@precio",Numero("precio",0.01m)), ("@descripcion",Texto("descripcion",250,false)), ("@categoria",Texto("categoria",80)));
                    break;
                case "Caja":
                    await Ejecutar("INSERT INTO dbo.Caja(nombre,estado) VALUES(@nombre,'Abierta')", ("@nombre",Texto("nombre",80)));
                    break;
                case "Método de pago":
                    await Ejecutar("INSERT INTO dbo.Metodo_Pago(nombre,estado) VALUES(@nombre,'Activo')", ("@nombre",Texto("nombre",50)));
                    break;
                case "Transacción":
                    await ValidarCaja(Entero("caja"));
                    string tipoMovimiento = Texto("tipo",20);
                    if (tipoMovimiento is not ("Ingreso" or "Egreso")) throw new ArgumentException("Tipo de transacción no válido.");
                    await Ejecutar("INSERT INTO dbo.Movimiento_Contable(id_caja,id_pago,tipo,concepto,monto,fecha) VALUES(@caja,NULL,@tipo,@concepto,@monto,GETDATE())", ("@caja",Entero("caja")), ("@tipo",tipoMovimiento), ("@concepto",Texto("concepto",200)), ("@monto",Numero("monto",0.01m)));
                    break;
                case "Préstamo":
                    await Ejecutar("INSERT INTO dbo.Prestamo(id_trabajador,fecha_solicitud,monto,saldo_pendiente,estado) VALUES(@trabajador,CAST(GETDATE() AS date),@monto,@monto,'Pendiente')", ("@trabajador",await Trabajador()), ("@monto",Numero("monto",0.01m)));
                    break;
                case "Asistencia":
                    int trabajador = await Trabajador();
                    DateTime momento = Convert.ToDateTime(await Escalar("SELECT GETDATE()"));
                    string marca = Texto("tipo",20);
                    if (marca=="Entrada")
                    {
                        if (await Escalar("SELECT id_asistencia FROM dbo.Asistencia WHERE id_trabajador=@id AND fecha=@fecha", ("@id",trabajador), ("@fecha",momento.Date)) != null) throw new InvalidOperationException("Ya existe una entrada registrada hoy.");
                        await Ejecutar("INSERT INTO dbo.Asistencia(id_trabajador,fecha,hora_entrada) VALUES(@id,@fecha,@hora)", ("@id",trabajador), ("@fecha",momento.Date), ("@hora",momento.TimeOfDay));
                    }
                    else if (marca=="Salida")
                    {
                        if (await Ejecutar("UPDATE dbo.Asistencia SET hora_salida=@hora WHERE id_trabajador=@id AND fecha=@fecha AND hora_salida IS NULL AND hora_entrada<=@hora", ("@id",trabajador), ("@fecha",momento.Date), ("@hora",momento.TimeOfDay)) != 1) throw new InvalidOperationException("No hay una entrada pendiente de salida para hoy.");
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
                        object? precioDato = await Escalar(esProducto ? "SELECT precio_venta FROM dbo.Producto WHERE id_producto=@id" : "SELECT precio FROM dbo.Servicio WHERE id_servicio=@id", ("@id",linea.Id));
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
                    facturaEmitida = factura;
                    break;
                case "Pago":
                    await ValidarCaja(Entero("caja"));
                    if (await Escalar("SELECT id_metodo_pago FROM dbo.Metodo_Pago WHERE id_metodo_pago=@id AND estado='Activo'", ("@id",Entero("metodo"))) == null) throw new InvalidOperationException("Seleccione un método de pago activo.");
                    int facturaPago = Entero("factura");
                    object? importeFactura = await Escalar("SELECT total FROM dbo.Factura WITH (UPDLOCK) WHERE id_factura=@id AND estado<>'Anulada'", ("@id",facturaPago));
                    if (importeFactura == null || importeFactura == DBNull.Value) throw new InvalidOperationException("La factura no está disponible.");
                    decimal pagado = Convert.ToDecimal(await Escalar("SELECT COALESCE(SUM(monto),0) FROM dbo.Pago WHERE id_factura=@id AND estado='Completado'", ("@id",facturaPago)));
                    decimal montoPago = Numero("monto",0.01m);
                    if (montoPago>Convert.ToDecimal(importeFactura)-pagado) throw new InvalidOperationException("El monto supera el saldo pendiente de la factura.");
                    int idPago = Convert.ToInt32(await Escalar("INSERT INTO dbo.Pago(id_factura,id_metodo_pago,fecha,monto,referencia,estado) OUTPUT INSERTED.id_pago VALUES(@factura,@metodo,GETDATE(),@monto,@referencia,'Completado')", ("@factura",facturaPago), ("@metodo",Entero("metodo")), ("@monto",montoPago), ("@referencia",Texto("referencia",100,false))));
                    await Ejecutar("INSERT INTO dbo.Movimiento_Contable(id_caja,id_pago,tipo,concepto,monto,fecha) VALUES(@caja,@pago,'Ingreso',@concepto,@monto,GETDATE())", ("@caja",Entero("caja")), ("@pago",idPago), ("@concepto","Pago factura " + facturaPago), ("@monto",montoPago));
                    await Ejecutar("UPDATE dbo.Factura SET estado=@estado WHERE id_factura=@id", ("@estado",pagado+montoPago==Convert.ToDecimal(importeFactura) ? "Pagada" : "Emitida"), ("@id",facturaPago));
                    break;
                case "Cierre":
                    int caja = Entero("caja");
                    await ValidarCaja(caja);
                    decimal contado = Numero("contado");
                    object? esperado = await Escalar("SELECT COALESCE((SELECT TOP (1) monto_real FROM dbo.Cuadre_Caja WHERE id_caja=@caja ORDER BY fecha_cierre DESC,id_cuadre DESC),0) + COALESCE((SELECT SUM(CASE WHEN tipo='Ingreso' THEN monto WHEN tipo='Egreso' THEN -monto ELSE 0 END) FROM dbo.Movimiento_Contable WHERE id_caja=@caja AND fecha>COALESCE((SELECT MAX(fecha_cierre) FROM dbo.Cuadre_Caja WHERE id_caja=@caja),CONVERT(datetime2,'19000101'))),0)", ("@caja",caja));
                    decimal saldo = Convert.ToDecimal(esperado);
                    await Ejecutar("INSERT INTO dbo.Cuadre_Caja(id_caja,fecha_cierre,monto_esperado,monto_real,diferencia,estado) VALUES(@caja,GETDATE(),@esperado,@real,@diferencia,'Cerrada')", ("@caja",caja), ("@esperado",saldo), ("@real",contado), ("@diferencia",contado-saldo));
                    break;
                default: throw new ArgumentException("Operación desconocida.");
            }
            transaccion.Commit();
            return facturaEmitida;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
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
}

namespace Giovanny_Barbers
{
    public sealed record OpcionRegistro(int Id, string Nombre, decimal Precio = 0);
    public sealed record LineaComprobante(string Descripcion, int Cantidad, decimal Precio, decimal Subtotal);
    public sealed record Comprobante(int Numero, DateTime Fecha, string Estado, decimal Total, string Cliente, string Nit, string? Telefono,
        string? Trabajador, decimal Pagado, string? Metodos, List<LineaComprobante> Lineas);
    public sealed record LineaRegistro(string Tipo, int Id, int Cantidad, decimal PrecioEsperado);
}
