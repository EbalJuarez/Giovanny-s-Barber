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

    public static async Task<SesionUsuario?> AutenticarAsync(string usuario, string contrasena)
    {
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        using var consulta = new SqlCommand("SELECT id_usuario, nombre_usuario, contrasena_hash, rol, estado FROM dbo.Usuario WHERE nombre_usuario = @usuario", conexion);
        consulta.Parameters.Add("@usuario", SqlDbType.VarChar, 50).Value = usuario;
        int id;
        string nombre, rol, estado, clave;
        using (var lector = await consulta.ExecuteReaderAsync())
        {
            if (!await lector.ReadAsync()) return null;
            id = lector.GetInt32(0);
            nombre = lector.GetString(1);
            clave = lector.IsDBNull(2) ? "" : lector.GetString(2);
            rol = lector.IsDBNull(3) ? "" : lector.GetString(3).Trim();
            estado = lector.IsDBNull(4) ? "" : lector.GetString(4).Trim();
            if (await lector.ReadAsync()) throw new InvalidOperationException("Hay nombres de usuario duplicados. Consulte al administrador.");
        }
        if (!ValidarContrasena(contrasena, clave)) return null;
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
        if (string.IsNullOrEmpty(almacenada)) return false;
        if (almacenada.StartsWith("$2", StringComparison.Ordinal))
        {
            try { return BCrypt.Net.BCrypt.Verify(ingresada, almacenada); }
            catch (ArgumentException) { return false; }
            catch (BCrypt.Net.SaltParseException) { return false; }
        }
        // Compatibilidad con las claves en texto existentes; no interpreta prefijos ficticios como hashes.
        return string.Equals(ingresada, almacenada, StringComparison.Ordinal);
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
                        ('Cajera', N'Clientes'), ('Cajera', N'Facturación'),
                        ('Cajero', N'Clientes'), ('Cajero', N'Facturación');
                END;
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
        ("Trabajadores", "Directorio") => "SELECT t.id_trabajador AS ID, CONCAT(t.nombres,' ',t.apellidos) AS Nombre, t.telefono AS Teléfono, t.direccion AS Dirección, u.nombre_usuario AS Usuario, u.rol AS Rol, u.estado AS Estado FROM dbo.Trabajador t LEFT JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario ORDER BY t.nombres",
        ("Trabajadores", "Usuarios") => "SELECT id_usuario AS ID, nombre_usuario AS Usuario, rol AS Rol, estado AS Estado FROM dbo.Usuario ORDER BY nombre_usuario",
        ("Inventario", "Categorías") => "SELECT id_categoria AS ID, nombre AS Nombre, descripcion AS Descripción FROM dbo.Categoria ORDER BY nombre",
        ("Facturación", "Servicios") => "SELECT id_servicio AS ID, nombre AS Servicio, precio AS Precio, descripcion AS Descripción, categoria_servicio AS Categoría FROM dbo.Servicio ORDER BY nombre",
        ("Facturación", "Pagos") => "SELECT p.id_pago AS ID, p.id_factura AS Factura, p.fecha AS Fecha, m.nombre AS Método, p.monto AS Monto, p.referencia AS Referencia, p.estado AS Estado FROM dbo.Pago p LEFT JOIN dbo.Metodo_Pago m ON m.id_metodo_pago=p.id_metodo_pago ORDER BY p.fecha DESC",
        ("Contabilidad", "Cajas") => "SELECT id_caja AS ID, nombre AS Caja, estado AS Estado FROM dbo.Caja ORDER BY nombre",
        ("Trabajadores", "Asistencia") => "SELECT a.fecha AS Fecha, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, a.hora_entrada AS Entrada, a.hora_salida AS Salida FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador ORDER BY a.fecha DESC",
        ("Mi jornada", "Mi asistencia") => "SELECT a.fecha AS Fecha, a.hora_entrada AS Entrada, a.hora_salida AS Salida FROM dbo.Asistencia a JOIN dbo.Trabajador t ON t.id_trabajador=a.id_trabajador WHERE t.id_usuario=@usuario ORDER BY a.fecha DESC",
        ("Trabajadores", "Préstamos") => "SELECT p.id_prestamo AS ID, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, p.fecha_solicitud AS Fecha, p.monto AS Monto, p.saldo_pendiente AS Saldo, p.estado AS Estado FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador ORDER BY p.fecha_solicitud DESC",
        ("Mi jornada", "Mis préstamos") => "SELECT p.fecha_solicitud AS Fecha, p.monto AS Monto, p.saldo_pendiente AS Saldo, p.estado AS Estado FROM dbo.Prestamo p JOIN dbo.Trabajador t ON t.id_trabajador=p.id_trabajador WHERE t.id_usuario=@usuario ORDER BY p.fecha_solicitud DESC",
        ("Trabajadores", "Actividades") or ("Clientes", "Historial de servicios") => "SELECT f.fecha AS Fecha, c.nombres AS Cliente, s.nombre AS Servicio, CONCAT(t.nombres,' ',t.apellidos) AS Trabajador, d.cantidad AS Cantidad, d.subtotal AS Total FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente LEFT JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador ORDER BY f.fecha DESC",
        ("Mi jornada", "Mis servicios") => "SELECT f.fecha AS Fecha, c.nombres AS Cliente, s.nombre AS Servicio, d.cantidad AS Cantidad, d.subtotal AS Total FROM dbo.Detalle_Servicio d JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Servicio s ON s.id_servicio=d.id_servicio JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador LEFT JOIN dbo.Cliente c ON c.id_cliente=f.id_cliente WHERE t.id_usuario=@usuario ORDER BY f.fecha DESC",
        ("Mi jornada", "Mis ventas") => "SELECT f.fecha AS Fecha, p.nombre AS Producto, d.cantidad AS Cantidad, d.precio_unitario AS Precio, d.subtotal AS Total FROM dbo.Detalle_Producto d JOIN dbo.Producto p ON p.id_producto=d.id_producto JOIN dbo.Factura f ON f.id_factura=d.id_factura JOIN dbo.Trabajador t ON t.id_trabajador=f.id_trabajador WHERE t.id_usuario=@usuario ORDER BY f.fecha DESC",
        ("Clientes", "Clientes") => "SELECT id_cliente AS ID, nombres AS Nombre, telefono AS Teléfono, correo AS Correo FROM dbo.Cliente ORDER BY nombres",
        ("Inventario", "Productos") => "SELECT p.id_producto AS ID, p.nombre AS Producto, c.nombre AS Categoría, p.precio_venta AS Precio, p.stock AS Existencias, p.stock_minimo AS [Stock mínimo] FROM dbo.Producto p LEFT JOIN dbo.Categoria c ON c.id_categoria=p.id_categoria ORDER BY p.nombre",
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
}


public partial class ConexionBD
{
    public static bool PuedeRegistrar(SesionUsuario sesion, string tipo) => tipo switch
    {
        "Empleado" or "Clave" or "Servicio" or "Método de pago" => sesion.EsAdministrador,
        "Cliente" => sesion.PuedeAbrir("Clientes"),
        "Producto" or "Categoría" or "Movimiento" => sesion.PuedeAbrir("Inventario"),
        "Transacción" or "Caja" or "Cierre" => sesion.PuedeAbrir("Contabilidad"),
        "Pago" => sesion.PuedeAbrir("Facturación"),
        "Factura" => sesion.PuedeAbrir("Facturación") || sesion.PuedeAbrir("Mi jornada"),
        "Asistencia" or "Préstamo" => sesion.PuedeAbrir("Trabajadores") || sesion.PuedeAbrir("Mi jornada"),
        _ => false
    };

    public static async Task<List<OpcionRegistro>> OpcionesRegistroAsync(SesionUsuario sesion, string tipo, string catalogo)
    {
        if (!PuedeRegistrar(sesion, tipo)) throw new UnauthorizedAccessException("No tiene permiso para esta operación.");
        string sql = (tipo, catalogo) switch
        {
            ("Empleado", "rol") => "SELECT 1 AS ID, 'Administrador' AS Nombre UNION ALL SELECT 2, 'Trabajador' UNION ALL SELECT 3, 'Cajera' UNION ALL SELECT 4, 'Cajero'",
            ("Clave", "usuario") => "SELECT id_usuario, nombre_usuario FROM dbo.Usuario ORDER BY nombre_usuario",
            ("Producto", "categoria") => "SELECT id_categoria, nombre FROM dbo.Categoria ORDER BY nombre",
            ("Factura", "cliente") => "SELECT id_cliente, nombres FROM dbo.Cliente ORDER BY nombres",
            ("Factura", "producto") or ("Movimiento", "producto") => "SELECT id_producto, nombre, precio_venta FROM dbo.Producto ORDER BY nombre",
            ("Factura", "servicio") => "SELECT id_servicio, nombre, precio FROM dbo.Servicio ORDER BY nombre",
            ("Factura", "trabajador") or ("Asistencia", "trabajador") or ("Préstamo", "trabajador") =>
                "SELECT t.id_trabajador, CONCAT(t.nombres,' ',t.apellidos) FROM dbo.Trabajador t JOIN dbo.Usuario u ON u.id_usuario=t.id_usuario WHERE u.estado='Activo' AND (@todos=1 OR t.id_usuario=@usuario) ORDER BY t.nombres",
            ("Pago", "factura") => "SELECT f.id_factura, CONCAT('Factura ',f.id_factura,' · Saldo Q ',f.total-COALESCE(p.pagado,0)) FROM dbo.Factura f OUTER APPLY (SELECT SUM(monto) AS pagado FROM dbo.Pago WHERE id_factura=f.id_factura AND estado='Completado') p WHERE f.estado<>'Anulada' AND f.total>COALESCE(p.pagado,0) ORDER BY f.id_factura DESC",
            ("Pago", "metodo") => "SELECT id_metodo_pago, nombre FROM dbo.Metodo_Pago WHERE estado='Activo' ORDER BY nombre",
            ("Pago", "caja") or ("Transacción", "caja") or ("Cierre", "caja") => "SELECT id_caja, nombre FROM dbo.Caja WHERE estado='Abierta' ORDER BY nombre",
            _ => throw new ArgumentException("Catálogo no permitido para esta operación.")
        };
        using var conexion = ObtenerConexion();
        await conexion.OpenAsync();
        using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@usuario", SqlDbType.Int).Value = sesion.IdUsuario;
        bool todos = tipo == "Factura" ? sesion.PuedeAbrir("Facturación") : sesion.PuedeAbrir("Trabajadores");
        comando.Parameters.Add("@todos", SqlDbType.Bit).Value = todos;
        using var lector = await comando.ExecuteReaderAsync();
        var opciones = new List<OpcionRegistro>();
        while (await lector.ReadAsync()) opciones.Add(new OpcionRegistro(lector.GetInt32(0), lector.GetString(1), lector.FieldCount > 2 && !lector.IsDBNull(2) ? lector.GetDecimal(2) : 0));
        return opciones;
    }

    public static async Task GuardarRegistroAsync(SesionUsuario sesion, string tipo, IReadOnlyDictionary<string, object> datos, IReadOnlyList<LineaRegistro>? lineas = null)
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
        try
        {
            switch (tipo)
            {
                case "Empleado":
                    string usuario = Texto("usuario", 50);
                    string clave = Clave();
                    string rol = Texto("rol", 30);
                    if (rol is not ("Administrador" or "Trabajador" or "Cajera" or "Cajero")) throw new ArgumentException("Rol no válido.");
                    if (await Escalar("SELECT id_usuario FROM dbo.Usuario WHERE nombre_usuario=@nombre", ("@nombre", usuario)) != null) throw new InvalidOperationException("Ese nombre de usuario ya existe.");
                    int nuevoUsuario = Convert.ToInt32(await Escalar("INSERT INTO dbo.Usuario(nombre_usuario,contrasena_hash,rol,estado) OUTPUT INSERTED.id_usuario VALUES(@nombre,@clave,@rol,'Activo')", ("@nombre", usuario), ("@clave", BCrypt.Net.BCrypt.HashPassword(clave)), ("@rol", rol)));
                    await Ejecutar("INSERT INTO dbo.Trabajador(id_usuario,nombres,apellidos,telefono,direccion) VALUES(@usuario,@nombres,@apellidos,@telefono,@direccion)", ("@usuario", nuevoUsuario), ("@nombres", Texto("nombres",80)), ("@apellidos", Texto("apellidos",80)), ("@telefono", Texto("telefono",20,false)), ("@direccion", Texto("direccion",200,false)));
                    break;
                case "Clave":
                    string nuevaClave = Clave();
                    if (await Ejecutar("UPDATE dbo.Usuario SET contrasena_hash=@clave WHERE id_usuario=@id", ("@clave", BCrypt.Net.BCrypt.HashPassword(nuevaClave)), ("@id", Entero("usuario"))) != 1) throw new InvalidOperationException("El usuario ya no existe.");
                    break;
                case "Cliente":
                    await Ejecutar("INSERT INTO dbo.Cliente(nombres,telefono,correo) VALUES(@nombre,@telefono,@correo)", ("@nombre",Texto("nombre",100)), ("@telefono",Texto("telefono",20,false)), ("@correo",Texto("correo",100,false)));
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
                    await Ejecutar("INSERT INTO dbo.Prestamo(id_trabajador,fecha_solicitud,monto,saldo_pendiente,estado) VALUES(@trabajador,CAST(GETDATE() AS date),@monto,0,'Pendiente')", ("@trabajador",await Trabajador()), ("@monto",Numero("monto",0.01m)));
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
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }
}

namespace Giovanny_Barbers
{
    public sealed record OpcionRegistro(int Id, string Nombre, decimal Precio = 0);
    public sealed record LineaRegistro(string Tipo, int Id, int Cantidad, decimal PrecioEsperado);
}
