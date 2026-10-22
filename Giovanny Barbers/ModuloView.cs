namespace Giovanny_Barbers;

// Cada instancia conserva sus filtros y pestaña al navegar entre módulos.
// Las consultas se validan por módulo y las vistas personales se filtran por sesión.
public sealed class ModuloView : UserControl
{
    public SesionUsuario Sesion { get; }
    private readonly string modulo;
    private readonly TabControl tabs;
    private readonly Dictionary<string, (DataGridView Tabla, Label Estado, Button Actualizar)> paginas = new();
    private readonly CancellationTokenSource cancelacion = new();
    private readonly HashSet<string> cargando = new();
    private readonly Dictionary<string, Filtro> filtros = new();

    // RF3 y CU-09: filtros de historial por período, por persona y por texto, aplicados sobre los datos ya consultados.
    private sealed record Filtro(DateTimePicker? Desde, DateTimePicker? Hasta, ComboBox? Persona, string? Columna, TextBox? Buscar, string[] ColumnasBusqueda);

    public ModuloView(string modulo, SesionUsuario sesion)
    {
        Sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        Sesion.ExigirAcceso(modulo);
        this.modulo = modulo;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(246, 247, 249);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = modulo, Font = new Font("Segoe UI", 27, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(new Label { Text = modulo == "Mi jornada" ? "Consulta tu asistencia, servicios, ventas y préstamos." : "Consulta los datos registrados en la barbería.", ForeColor = Color.DimGray, Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) }, 0, 1);
        tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Padding = new Point(14, 10) };
        layout.Controls.Add(tabs, 0, 2);
        foreach (string seccion in Secciones(modulo))
        {
            if (seccion == "Usuarios" && !Sesion.EsAdministrador) continue;
            var tab = new TabPage(seccion) { BackColor = Color.White, Padding = new Padding(18) };
            tabs.TabPages.Add(tab);
            var (filtraFecha, columnaPersona, columnasBusqueda) = Filtros(modulo, seccion);
            bool conFiltro = filtraFecha || columnaPersona != null || columnasBusqueda.Length > 0;
            var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = conFiltro ? 4 : 3 };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            if (conFiltro) cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            tab.Controls.Add(cuerpo);
            var cabecera = new Panel { Dock = DockStyle.Fill };
            var actualizar = new Button { Text = "Actualizar", Dock = DockStyle.Right, Width = 115, Height = 36 };
            cabecera.Controls.Add(new Label { Text = Descripcion(seccion), Dock = DockStyle.Fill, ForeColor = Color.FromArgb(65, 70, 75) });
            cabecera.Controls.Add(actualizar);
            foreach (string operacion in Operaciones(modulo, seccion).Where(tipo => ConexionBD.PuedeRegistrar(Sesion, tipo)))
            {
                var nuevo = new Button { Text = RegistroForm.EsAccion(operacion) ? RegistroForm.Titulo(operacion) : "+ " + operacion, Dock = DockStyle.Right, Width = 170 };
                cabecera.Controls.Add(nuevo);
                nuevo.Click += async (_, _) =>
                {
                    using var ventana = new RegistroForm(Sesion, operacion);
                    var resultado = ventana.ShowDialog(FindForm());
                    if (resultado == DialogResult.OK)
                    {
                        if (ventana.FacturaEmitida is int emitida &&
                            MessageBox.Show(FindForm(), $"Factura {emitida} emitida. ¿Desea ver e imprimir el comprobante?", "Comprobante", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            AbrirComprobante(emitida, false);
                        await ActualizarAsync();
                    }
                    else if (ventana.SesionFinalizada != null) (FindForm() as Home)?.FinalizarSesion(ventana.SesionFinalizada);
                };
            }
            if ((modulo, seccion) == ("Facturación", "Facturas"))
            {
                // CU-15 y CU-16: ver y reimprimir el comprobante de la factura seleccionada.
                var verComprobante = new Button { Text = "Ver comprobante", Dock = DockStyle.Right, Width = 170 };
                cabecera.Controls.Add(verComprobante);
                verComprobante.Click += (_, _) => AbrirComprobanteSeleccionado(seccion);
            }
            cuerpo.Controls.Add(cabecera, 0, 0);
            var tabla = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false };
            tabla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 240);
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 37, 41);
            tabla.ColumnHeadersHeight = 42;
            int filaTabla = conFiltro ? 2 : 1;
            cuerpo.Controls.Add(tabla, 0, filaTabla);
            var estado = new Label { Text = "Seleccione la pestaña para consultar sus datos.", Dock = DockStyle.Fill, ForeColor = Color.DimGray, Padding = new Padding(0, 12, 0, 0) };
            cuerpo.Controls.Add(estado, 0, filaTabla + 1);
            paginas.Add(seccion, (tabla, estado, actualizar));
            if ((modulo, seccion) == ("Facturación", "Facturas"))
                tabla.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AbrirComprobanteSeleccionado(seccion); };
            if (conFiltro) cuerpo.Controls.Add(CrearFiltro(seccion, filtraFecha, columnaPersona, columnasBusqueda), 0, 1);
            actualizar.Click += async (_, _) => await ActualizarAsync();
        }
        tabs.SelectedIndexChanged += async (_, _) => await ActualizarAsync();
    }

    public async Task ActualizarAsync()
    {
        string? seccion = tabs.SelectedTab?.Text;
        if (IsDisposed || seccion == null || !paginas.TryGetValue(seccion, out var pagina) || !cargando.Add(seccion)) return;
        pagina.Actualizar.Enabled = false;
        pagina.Estado.ForeColor = Color.DimGray;
        pagina.Estado.Text = "Cargando datos…";
        try
        {
            var datos = await ConexionBD.ConsultarModuloAsync(Sesion, modulo, seccion, cancelacion.Token);
            if (IsDisposed) { datos.Dispose(); return; }
            var anteriores = pagina.Tabla.DataSource as System.Data.DataTable;
            pagina.Tabla.DataSource = datos;
            anteriores?.Dispose();
            foreach (DataGridViewColumn columna in pagina.Tabla.Columns)
            {
                columna.MinimumWidth = 85;
                if (columna.ValueType == typeof(decimal)) columna.DefaultCellStyle.Format = "N2";
                if (columna.ValueType == typeof(DateTime)) columna.DefaultCellStyle.Format = "dd/MM/yyyy";
            }
            if (filtros.ContainsKey(seccion)) AplicarFiltro(seccion, true);
            else pagina.Estado.Text = datos.Rows.Count == 0 ? "No hay registros para esta consulta." : $"{datos.Rows.Count} registros · Actualizado a las {DateTime.Now:HH:mm}";
        }
        catch (OperationCanceledException) { }
        catch (SesionInvalidaException ex) { (FindForm() as Home)?.FinalizarSesion(ex.Message); }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                pagina.Tabla.DataSource = null;
                pagina.Estado.ForeColor = Color.Firebrick;
                pagina.Estado.Text = "No se pudieron cargar los datos: " + ex.Message;
            }
        }
        finally
        {
            cargando.Remove(seccion);
            if (!IsDisposed) pagina.Actualizar.Enabled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cancelacion.Cancel();
            foreach (var pagina in paginas.Values)
                (pagina.Tabla.DataSource as System.Data.DataTable)?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void AbrirComprobanteSeleccionado(string seccion)
    {
        var pagina = paginas[seccion];
        if (pagina.Tabla.CurrentRow?.DataBoundItem is not System.Data.DataRowView fila || fila["Número"] is not int numero)
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "Seleccione una factura de la lista.";
            return;
        }
        AbrirComprobante(numero, true);
    }

    private void AbrirComprobante(int factura, bool reimpresion)
    {
        using var ventana = new ComprobanteForm(Sesion, factura, reimpresion);
        ventana.ShowDialog(FindForm());
        if (ventana.SesionFinalizada != null) (FindForm() as Home)?.FinalizarSesion(ventana.SesionFinalizada);
    }

    private Control CrearFiltro(string seccion, bool filtraFecha, string? columnaPersona, string[] columnasBusqueda)
    {
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        DateTimePicker? desde = null, hasta = null;
        ComboBox? persona = null;
        TextBox? buscar = null;
        Label Etiqueta(string texto) => new() { Text = texto, AutoSize = true, Margin = new Padding(0, 9, 6, 0) };
        if (columnasBusqueda.Length > 0)
        {
            buscar = new TextBox { Width = 260, MaxLength = 100, PlaceholderText = string.Join(", ", columnasBusqueda), Margin = new Padding(0, 4, 16, 0) };
            barra.Controls.AddRange([Etiqueta("Buscar"), buscar]);
            buscar.TextChanged += (_, _) => AplicarFiltro(seccion);
        }
        if (filtraFecha)
        {
            desde = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 140, Margin = new Padding(0, 4, 16, 0) };
            hasta = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 140, Margin = new Padding(0, 4, 16, 0) };
            barra.Controls.AddRange([Etiqueta("Desde"), desde, Etiqueta("Hasta"), hasta]);
            desde.ValueChanged += (_, _) => AplicarFiltro(seccion);
            hasta.ValueChanged += (_, _) => AplicarFiltro(seccion);
        }
        if (columnaPersona != null)
        {
            persona = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Margin = new Padding(0, 4, 16, 0) };
            persona.Items.Add("Todos");
            persona.SelectedIndex = 0;
            barra.Controls.AddRange([Etiqueta(columnaPersona), persona]);
            persona.SelectedIndexChanged += (_, _) => AplicarFiltro(seccion);
        }
        var limpiar = new Button { Text = "Limpiar filtros", AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
        limpiar.Click += (_, _) =>
        {
            if (desde != null) desde.Checked = false;
            if (hasta != null) hasta.Checked = false;
            if (persona != null) persona.SelectedIndex = 0;
            if (buscar != null) buscar.Clear();
            AplicarFiltro(seccion);
        };
        barra.Controls.Add(limpiar);
        filtros.Add(seccion, new Filtro(desde, hasta, persona, columnaPersona, buscar, columnasBusqueda));
        return barra;
    }

    // Escapa el texto para una comparación LIKE de DataView: los comodines se buscan como caracteres literales.
    private static string TextoLike(string texto)
    {
        var resultado = new System.Text.StringBuilder();
        foreach (char c in texto)
        {
            if (c is '[' or ']' or '*' or '%') resultado.Append('[').Append(c).Append(']');
            else if (c == '\'') resultado.Append("''");
            else resultado.Append(c);
        }
        return resultado.ToString();
    }

    private void AplicarFiltro(string seccion, bool datosNuevos = false)
    {
        if (!filtros.TryGetValue(seccion, out var filtro) || !paginas.TryGetValue(seccion, out var pagina)) return;
        if (pagina.Tabla.DataSource is not System.Data.DataTable datos) return;
        var columnas = datos.Columns;
        if (datosNuevos && filtro.Persona != null && filtro.Columna != null && columnas.Contains(filtro.Columna))
        {
            // Se conserva la persona elegida al actualizar si sigue apareciendo en los datos.
            string? elegida = filtro.Persona.SelectedIndex > 0 ? filtro.Persona.SelectedItem as string : null;
            var nombres = datos.Rows.Cast<System.Data.DataRow>().Select(f => f[filtro.Columna] as string).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Order().ToArray();
            filtro.Persona.BeginUpdate();
            filtro.Persona.Items.Clear();
            filtro.Persona.Items.Add("Todos");
            filtro.Persona.Items.AddRange(nombres!);
            filtro.Persona.EndUpdate();
            filtro.Persona.SelectedIndex = elegida != null && filtro.Persona.Items.Contains(elegida) ? filtro.Persona.Items.IndexOf(elegida) : 0;
        }
        // CU-09 y CU-10: un rango invertido no se aplica; se pide corregirlo.
        if (filtro.Desde?.Checked == true && filtro.Hasta?.Checked == true && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "La fecha inicial es posterior a la final. Corrija el rango.";
            return;
        }
        var condiciones = new List<string>();
        string busqueda = filtro.Buscar?.Text.Trim() ?? "";
        var columnasTexto = filtro.ColumnasBusqueda.Where(c => columnas.Contains(c) && columnas[c]!.DataType == typeof(string)).ToArray();
        if (busqueda.Length > 0 && columnasTexto.Length > 0)
            condiciones.Add("(" + string.Join(" OR ", columnasTexto.Select(c => $"[{c}] LIKE '%{TextoLike(busqueda)}%'")) + ")");
        if (columnas.Contains("Fecha"))
        {
            // RowFilter exige las fechas en formato invariable MM/dd/yyyy.
            static string Literal(DateTime fecha) => "#" + fecha.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture) + "#";
            if (filtro.Desde?.Checked == true) condiciones.Add("Fecha >= " + Literal(filtro.Desde.Value.Date));
            if (filtro.Hasta?.Checked == true) condiciones.Add("Fecha < " + Literal(filtro.Hasta.Value.Date.AddDays(1)));
        }
        if (filtro.Persona?.SelectedIndex > 0 && filtro.Columna != null && columnas.Contains(filtro.Columna))
            condiciones.Add($"[{filtro.Columna}] = '{((string)filtro.Persona.SelectedItem!).Replace("'", "''")}'");
        datos.DefaultView.RowFilter = string.Join(" AND ", condiciones);
        int visibles = datos.DefaultView.Count;
        string resumen = condiciones.Count == 0 ? $"{visibles} registros" : $"{visibles} de {datos.Rows.Count} registros";
        var filas = datos.DefaultView.Cast<System.Data.DataRowView>().ToArray();
        if (columnas.Contains("Visitas"))
            resumen += $" · Visitas: {filas.Sum(f => Convert.ToInt64(f["Visitas"])):N0}";
        string? columnaTotal = new[] { "Total", "Monto", "Ingresos" }.FirstOrDefault(c => columnas.Contains(c) && columnas[c]!.DataType == typeof(decimal));
        if (columnaTotal != null)
            resumen += $" · {columnaTotal}: Q {filas.Sum(f => f[columnaTotal] as decimal? ?? 0):N2}";
        pagina.Estado.ForeColor = Color.DimGray;
        pagina.Estado.Text = datos.Rows.Count == 0 ? "No hay registros para esta consulta."
            : visibles == 0 ? "No hay operaciones para los filtros elegidos."
            : $"{resumen} · Actualizado a las {DateTime.Now:HH:mm}";
    }

    private static (bool Fecha, string? Persona, string[] Busqueda) Filtros(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Trabajadores", "Asistencia" or "Actividades" or "Préstamos" or "Descuentos") => (true, "Trabajador", []),
        ("Clientes", "Clientes") => (false, null, ["Nombre", "NIT", "Teléfono", "Correo"]),
        // CU-13: búsqueda por producto o categoría.
        ("Inventario", "Productos") => (false, "Categoría", ["Producto", "Categoría", "Disponibilidad"]),
        ("Inventario", "Movimientos") => (true, "Producto", []),
        ("Facturación", "Facturas") => (true, "Cliente", ["Trabajador", "Estado"]),
        ("Clientes", "Historial" or "Reporte diario" or "Reporte semanal" or "Reporte mensual") => (true, "Cliente", []),
        ("Mi jornada", _) => (true, null, []),
        _ => (false, null, [])
    };

    private static string[] Secciones(string modulo) => modulo switch
    {
        "Trabajadores" => ["Directorio", "Asistencia", "Actividades", "Préstamos", "Descuentos", "Usuarios"],
        "Mi jornada" => ["Mi asistencia", "Mis servicios", "Mis ventas", "Mis préstamos"],
        "Clientes" => ["Clientes", "Historial", "Reporte diario", "Reporte semanal", "Reporte mensual"],
        "Inventario" => ["Productos", "Movimientos", "Categorías"],
        "Facturación" => ["Facturas", "Detalle de factura", "Servicios", "Pagos"],
        "Contabilidad" => ["Ingresos y egresos", "Cuadre de caja", "Cierres diarios", "Cajas"],
        _ => []
    };

    private static string[] Operaciones(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Trabajadores", "Directorio") => ["Empleado", "Editar empleado"],
        ("Trabajadores", "Usuarios") => ["Clave"],
        ("Trabajadores", "Asistencia") or ("Mi jornada", "Mi asistencia") => ["Asistencia"],
        ("Trabajadores", "Préstamos") => ["Préstamo", "Resolver préstamo", "Descuento"],
        ("Trabajadores", "Descuentos") => ["Descuento"],
        ("Mi jornada", "Mis préstamos") => ["Préstamo"],
        ("Clientes", "Clientes") => ["Cliente", "Editar cliente"],
        ("Inventario", "Productos") => ["Producto", "Editar producto"],
        ("Inventario", "Movimientos") => ["Movimiento"],
        ("Inventario", "Categorías") => ["Categoría"],
        ("Facturación", "Servicios") => ["Servicio"],
        ("Facturación", "Pagos") => ["Pago", "Método de pago"],
        ("Contabilidad", "Ingresos y egresos") => ["Transacción"],
        ("Contabilidad", "Cajas") => ["Caja"],
        ("Contabilidad", "Cuadre de caja") or ("Contabilidad", "Cierres diarios") => ["Cierre"],
        ("Facturación", "Facturas") or ("Facturación", "Detalle de factura") or ("Trabajadores", "Actividades") or ("Clientes", "Historial") or ("Mi jornada", "Mis servicios") or ("Mi jornada", "Mis ventas") => ["Factura"],
        _ => []
    };

    private static string Descripcion(string seccion) => seccion switch
    {
        "Clientes" => "Busca al cliente antes de registrarlo para no duplicarlo. Sin NIT se registra como CF.",
        "Historial" => "Servicios por categoría (corte de pelo, arreglo de barba, pelo y barba, prótesis y mantenimiento) y productos comprados.",
        "Reporte diario" or "Reporte semanal" or "Reporte mensual" => "Visitas (facturas) e ingresos por cliente. El filtro de fechas usa el inicio de cada período.",
        "Préstamos" => "Solicitudes de trabajadores para revisión y gestión del administrador.",
        "Mis préstamos" => "Consulta tus solicitudes y su estado de aprobación.",
        "Cuadre de caja" => "Compara el saldo registrado con el efectivo contado al finalizar la jornada.",
        "Detalle de factura" => "Cliente, trabajador, servicios, productos, cantidades y precios del comprobante.",
        "Movimientos" => "Entradas, salidas y ajustes del inventario.",
        "Productos" => "Busca por producto o categoría. Las existencias cambian con entradas, salidas y ventas.",
        _ => "Consulta y gestión de " + seccion.ToLowerInvariant() + "."
    };

}

// Ventana modal reutilizada por los registros; la autorización también se valida al guardar.
public sealed class RegistroForm : Form
{
    private readonly SesionUsuario sesion;
    private readonly string tipo;
    private readonly TableLayoutPanel campos = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(8) };
    private readonly Dictionary<string, Control> controles = new();
    private readonly Dictionary<string, string> catalogos = new();
    private readonly Button guardar = new() { Text = "Guardar", Width = 145, Height = 40, BackColor = Color.FromArgb(171,140,57), FlatStyle = FlatStyle.Flat };
    private readonly Label estado = new() { Dock = DockStyle.Fill, ForeColor = Color.DimGray, Padding = new Padding(10) };
    private bool guardando;
    // Motivo por el que la sesión dejó de ser válida; quien abrió la ventana cierra la sesión.
    public string? SesionFinalizada { get; private set; }
    // Número de la factura recién emitida, para ofrecer su comprobante.
    public int? FacturaEmitida { get; private set; }
    private readonly DataGridView detalle = new() { Height = 175, Dock = DockStyle.Top, BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly ComboBox tipoLinea = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly ComboBox articulo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250, DisplayMember = "Nombre" };
    private readonly NumericUpDown cantidad = new() { Minimum = 1, Maximum = 100000, Value = 1, Width = 75 };
    private readonly Label total = new() { Text = "Total estimado: Q 0.00", Height = 30, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleRight };
    private readonly Dictionary<string, List<OpcionRegistro>> articulos = new();

    public RegistroForm(SesionUsuario sesion, string tipo)
    {
        if (!ConexionBD.PuedeRegistrar(sesion,tipo)) throw new UnauthorizedAccessException("No tiene permiso para abrir este registro.");
        this.sesion = sesion;
        this.tipo = tipo;
        Text = tipo == "Clave" ? "Restablecer contraseña" : Titulo(tipo);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI",10);
        BackColor = Color.FromArgb(246,247,249);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96,96);
        ClientSize = new Size(tipo == "Factura" ? 850 : 670, tipo == "Factura" ? 720 : 620);
        MinimumSize = new Size(620,520);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        var estructura = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(18) };
        estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        estructura.RowStyles.Add(new RowStyle(SizeType.Absolute,55));
        estructura.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        estructura.RowStyles.Add(new RowStyle(SizeType.Absolute,110));
        Controls.Add(estructura);
        estructura.Controls.Add(new Label { Text = Text, Dock = DockStyle.Fill, Font = new Font("Segoe UI",20,FontStyle.Bold) },0,0);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(campos);
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        estructura.Controls.Add(scroll,0,1);
        var pie = new Panel { Dock = DockStyle.Fill };
        var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.RightToLeft };
        var cancelar = new Button { Text = "Cancelar", Width = 120, Height = 40, DialogResult = DialogResult.Cancel };
        botones.Controls.Add(guardar);
        botones.Controls.Add(cancelar);
        pie.Controls.Add(estado);
        pie.Controls.Add(botones);
        estructura.Controls.Add(pie,0,2);
        AcceptButton = guardar;
        CancelButton = cancelar;
        switch(tipo)
        {
            case "Empleado":
                Texto("nombres","Nombres",80); Texto("apellidos","Apellidos",80); Texto("telefono","Teléfono",20); Texto("direccion","Dirección",200);
                Numero("salario","Salario mensual",true);
                Texto("usuario","Usuario",50); Texto("clave","Contraseña nueva",72,true); Texto("confirmar","Confirmar contraseña",72,true);
                Lista("rol","Rol","Trabajador","Cajera","Cajero","Administrador");
                break;
            case "Editar empleado":
                Catalogo("trabajador","Trabajador");
                Texto("nombres","Nombres",80); Texto("apellidos","Apellidos",80); Texto("telefono","Teléfono",20); Texto("direccion","Dirección",200);
                Numero("salario","Salario mensual",true);
                Lista("estado","Estado de la cuenta","Activo","Inactivo");
                ((ComboBox)controles["trabajador"]).SelectionChangeCommitted += async (_,_) => await CargarEdicionAsync();
                break;
            case "Resolver préstamo": Catalogo("prestamo","Solicitud pendiente"); Lista("decision","Decisión","Autorizar","Rechazar"); break;
            case "Descuento": Catalogo("prestamo","Préstamo autorizado"); Numero("monto","Monto a descontar",true); Texto("observacion","Período o nota",100); break;
            case "Clave": Catalogo("usuario","Usuario"); Texto("clave","Contraseña nueva",72,true); Texto("confirmar","Confirmar contraseña",72,true); break;
            case "Cliente": Texto("nombre","Nombre",100); Texto("nit","NIT",15); Texto("telefono","Teléfono",20); Texto("correo","Correo",100); break;
            case "Editar cliente":
                Catalogo("cliente","Cliente");
                Texto("nombre","Nombre",100); Texto("nit","NIT",15); Texto("telefono","Teléfono",20); Texto("correo","Correo",100);
                ((ComboBox)controles["cliente"]).SelectionChangeCommitted += async (_,_) => await CargarEdicionAsync();
                break;
            case "Categoría": Texto("nombre","Nombre",80); Texto("descripcion","Descripción",250); break;
            case "Producto": Catalogo("categoria","Categoría"); Texto("nombre","Nombre",100); Numero("precio","Precio de venta",true); Numero("stock","Existencia inicial"); Numero("minimo","Stock mínimo"); break;
            case "Editar producto":
                Catalogo("producto","Producto");
                Catalogo("categoria","Categoría"); Texto("nombre","Nombre",100); Numero("precio","Precio de venta",true); Numero("minimo","Stock mínimo");
                controles["precio"].Enabled = sesion.EsAdministrador;
                ((ComboBox)controles["producto"]).SelectionChangeCommitted += async (_,_) => await CargarEdicionAsync();
                break;
            case "Movimiento": Catalogo("producto","Producto"); Lista("tipo","Tipo","Entrada","Salida"); Numero("cantidad","Cantidad",false,1); Texto("motivo","Motivo",250); break;
            case "Servicio": Texto("nombre","Nombre",100); Numero("precio","Precio",true); Texto("descripcion","Descripción",250); Lista("categoria","Tipo de servicio","Corte de pelo","Arreglo de barba","Pelo y barba","Instalación de prótesis","Mantenimiento"); break;
            case "Caja": Texto("nombre","Nombre de caja",80); break;
            case "Método de pago": Texto("nombre","Nombre",50); break;
            case "Transacción": Catalogo("caja","Caja"); Lista("tipo","Tipo","Ingreso","Egreso"); Texto("concepto","Concepto",200); Numero("monto","Monto",true); break;
            case "Cierre": Catalogo("caja","Caja"); Numero("contado","Dinero contado",true); break;
            case "Asistencia": Catalogo("trabajador","Trabajador"); Lista("tipo","Marca","Entrada","Salida"); break;
            case "Préstamo": Catalogo("trabajador","Trabajador"); Numero("monto","Monto solicitado",true); break;
            case "Pago": Catalogo("factura","Factura"); Catalogo("metodo","Método de pago"); Catalogo("caja","Caja"); Numero("monto","Monto",true); Texto("referencia","Referencia",100); break;
            case "Factura": Catalogo("cliente","Cliente"); Catalogo("trabajador","Trabajador"); CrearDetalle(); break;
        }
        estado.Text = tipo switch
        {
            "Clave" or "Empleado" => "La contraseña debe tener al menos 8 caracteres. Confírmala antes de guardar.",
            "Factura" => "Se emitirá la factura y se descontará el inventario. Registra el cobro después en la pestaña Pagos.",
            "Asistencia" => "Se registrará la fecha y hora actuales.",
            "Préstamo" => "La solicitud quedará pendiente de aprobación.",
            "Editar empleado" => "Selecciona un trabajador para cargar sus datos. Una cuenta inactiva no puede iniciar sesión.",
            "Cliente" => "Deja el NIT vacío para consumidor final (CF). No se permiten dos clientes con el mismo NIT.",
            "Editar cliente" => "Selecciona un cliente para cargar sus datos.",
            "Editar producto" => sesion.EsAdministrador ? "Selecciona un producto para cargar sus datos. Las existencias se ajustan con movimientos."
                : "Selecciona un producto. Solo el administrador puede cambiar el precio.",
            "Resolver préstamo" => "Al autorizar, el saldo pendiente será el monto solicitado. La decisión no se puede cambiar.",
            "Descuento" => "Se descontará del salario y se restará del saldo. Al llegar a cero, el préstamo queda pagado.",
            "Cierre" => "Se guardará el cuadre con el saldo registrado y la diferencia encontrada.",
            _ => "Completa los campos y pulsa Guardar."
        };
        guardar.Enabled = false;
        Shown += async (_,_) => await CargarCatalogosAsync();
        guardar.Click += async (_,_) => await GuardarAsync();
        FormClosing += (_,e) => { if (guardando) e.Cancel=true; };
    }

    // Operaciones que modifican registros existentes: el botón muestra la acción en lugar de "+ tipo".
    internal static bool EsAccion(string tipo) => tipo is "Clave" or "Editar empleado" or "Editar cliente" or "Editar producto" or "Resolver préstamo" or "Descuento";
    internal static string Titulo(string tipo) => tipo switch
    {
        "Clave" => "Restablecer clave",
        "Editar empleado" or "Editar cliente" or "Editar producto" or "Resolver préstamo" => tipo,
        "Descuento" => "Descontar de salario",
        _ => "Registrar " + tipo.ToLowerInvariant()
    };

    // El primer campo de una edición es el catálogo del registro que se modifica.
    private string? CatalogoEdicion => tipo switch { "Editar empleado" => "trabajador", "Editar cliente" => "cliente", "Editar producto" => "producto", _ => null };

    private async Task CargarEdicionAsync()
    {
        if (CatalogoEdicion == null || ((ComboBox)controles[CatalogoEdicion]).SelectedItem is not OpcionRegistro opcion) return;
        guardar.Enabled = false;
        try
        {
            var datos = await ConexionBD.ObtenerParaEditarAsync(sesion, tipo, opcion.Id);
            if (IsDisposed) return;
            foreach (var dato in datos)
            {
                if (controles[dato.Key] is NumericUpDown numero) numero.Value = Math.Clamp(Convert.ToDecimal(dato.Value), numero.Minimum, numero.Maximum);
                // Un catálogo recibe el Id del registro relacionado; una lista fija recibe el texto.
                else if (controles[dato.Key] is ComboBox lista) lista.SelectedItem = dato.Value is int id ? lista.Items.OfType<OpcionRegistro>().FirstOrDefault(o => o.Id == id) : dato.Value;
                else controles[dato.Key].Text = (string)dato.Value;
            }
            estado.ForeColor = Color.DimGray;
            estado.Text = "Modifica los datos y pulsa Guardar.";
            guardar.Enabled = true;
        }
        catch(SesionInvalidaException ex) { if(!IsDisposed) { SesionFinalizada=ex.Message; DialogResult=DialogResult.Abort; } }
        catch(Exception ex) { if(!IsDisposed) { estado.ForeColor=Color.Firebrick; estado.Text="No se pudieron cargar los datos: " + ex.Message; } }
    }

    private void Agregar(string nombre, string titulo, Control control)
    {
        int fila = campos.RowCount++;
        campos.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(4,6,4,12);
        campos.Controls.Add(new Label { Text = titulo, AutoSize = true, Margin = new Padding(4,10,4,12) },0,fila);
        campos.Controls.Add(control,1,fila);
        controles.Add(nombre,control);
    }
    private void Texto(string nombre, string titulo, int longitud, bool clave = false) => Agregar(nombre,titulo,new TextBox { MaxLength=longitud, UseSystemPasswordChar=clave });
    private void Numero(string nombre, string titulo, bool moneda=false, int minimo=0) => Agregar(nombre,titulo,new NumericUpDown { DecimalPlaces=moneda?2:0, Minimum=minimo, Maximum=moneda?99999999.99m:100000, ThousandsSeparator=true });
    private void Lista(string nombre, string titulo, params string[] valores)
    {
        var combo = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList };
        combo.Items.AddRange(valores);
        combo.SelectedIndex=0;
        Agregar(nombre,titulo,combo);
    }
    private void Catalogo(string nombre,string titulo)
    {
        Agregar(nombre,titulo,new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, DisplayMember="Nombre" });
        catalogos.Add(nombre,titulo);
    }
    private void CrearDetalle()
    {
        var panel = new Panel { Height=300, Dock=DockStyle.Top };
        var selector = new FlowLayoutPanel { Dock=DockStyle.Top, Height=85, AutoScroll=true };
        tipoLinea.Items.AddRange(new object[] { "Servicio","Producto" });
        tipoLinea.SelectedIndex=0;
        var agregar = new Button { Text="Agregar línea", AutoSize=true };
        var quitar = new Button { Text="Quitar línea", AutoSize=true };
        selector.Controls.AddRange(new Control[] { tipoLinea,articulo,cantidad,agregar,quitar });
        detalle.Columns.Add("tipo","Tipo"); detalle.Columns.Add("nombre","Descripción"); detalle.Columns.Add("cantidad","Cantidad"); detalle.Columns.Add("precio","Precio"); detalle.Columns.Add("subtotal","Subtotal");
        detalle.Columns[1].FillWeight=200;
        detalle.Columns[3].DefaultCellStyle.Format="N2";
        detalle.Columns[4].DefaultCellStyle.Format="N2";
        panel.Controls.Add(total); panel.Controls.Add(detalle); panel.Controls.Add(selector);
        int fila=campos.RowCount++;
        campos.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        campos.Controls.Add(panel,0,fila); campos.SetColumnSpan(panel,2);
        tipoLinea.SelectedIndexChanged += (_,_) => CambiarArticulos();
        agregar.Click += (_,_) =>
        {
            if (articulo.SelectedItem is not OpcionRegistro opcion) return;
            if(detalle.Rows.Count>=200) { estado.Text="Máximo 200 líneas por factura."; return; }
            int unidades=(int)cantidad.Value;
            int indice=detalle.Rows.Add(tipoLinea.Text,opcion.Nombre,unidades,opcion.Precio,opcion.Precio*unidades);
            detalle.Rows[indice].Tag=new LineaRegistro(tipoLinea.Text,opcion.Id,unidades,opcion.Precio);
            ActualizarTotal();
        };
        quitar.Click += (_,_) => { if(detalle.CurrentRow!=null) detalle.Rows.Remove(detalle.CurrentRow); ActualizarTotal(); };
    }
    private void CambiarArticulos()
    {
        if(articulos.TryGetValue(tipoLinea.Text,out var lista)) articulo.DataSource=lista;
    }
    private void ActualizarTotal() => total.Text="Total estimado: Q " + detalle.Rows.Cast<DataGridViewRow>().Sum(f=>Convert.ToDecimal(f.Cells[4].Value)).ToString("N2");
    private async Task CargarCatalogosAsync()
    {
        try
        {
            foreach(var catalogo in catalogos)
            {
                var lista=await ConexionBD.OpcionesRegistroAsync(sesion,tipo,catalogo.Key);
                if(IsDisposed) return;
                ((ComboBox)controles[catalogo.Key]).DataSource=lista;
            }
            if(tipo=="Factura")
            {
                articulos["Servicio"]=await ConexionBD.OpcionesRegistroAsync(sesion,tipo,"servicio");
                articulos["Producto"]=await ConexionBD.OpcionesRegistroAsync(sesion,tipo,"producto");
                if(IsDisposed) return;
                CambiarArticulos();
            }
            // Al editar, Guardar se habilita hasta que los datos del trabajador estén cargados.
            if(CatalogoEdicion!=null) await CargarEdicionAsync();
            else if(!IsDisposed) guardar.Enabled=true;
        }
        catch(SesionInvalidaException ex) { if(!IsDisposed) { SesionFinalizada=ex.Message; DialogResult=DialogResult.Abort; } }
        catch(Exception ex) { if(!IsDisposed) { estado.ForeColor=Color.Firebrick; estado.Text="No se pudieron cargar las opciones: " + ex.Message; } }
    }
    private async Task GuardarAsync()
    {
        if(guardando) return;
        try
        {
            var datos=new Dictionary<string,object>();
            foreach(var campo in controles)
            {
                if(campo.Value is ComboBox combo)
                {
                    if(combo.SelectedItem==null) throw new ArgumentException("Faltan opciones de " + (catalogos.GetValueOrDefault(campo.Key) ?? campo.Key) + ". Regístralas primero en su módulo.");
                    datos[campo.Key]=combo.SelectedItem is OpcionRegistro opcion ? opcion.Id : combo.Text;
                }
                else if(campo.Value is NumericUpDown numero) datos[campo.Key]=numero.Value;
                else datos[campo.Key]=campo.Value.Text;
            }
            if(controles.ContainsKey("confirmar") && controles["clave"].Text != controles["confirmar"].Text) throw new ArgumentException("Las contraseñas no coinciden.");
            guardando=true; guardar.Enabled=false;
            estado.ForeColor=Color.DimGray; estado.Text="Guardando…";
            FacturaEmitida = await ConexionBD.GuardarRegistroAsync(sesion,tipo,datos,detalle.Rows.Cast<DataGridViewRow>().Select(f=>(LineaRegistro)f.Tag!).ToArray());
            guardando=false;
            DialogResult=DialogResult.OK;
            Close();
        }
        catch(SesionInvalidaException ex)
        {
            SesionFinalizada=ex.Message;
            DialogResult=DialogResult.Abort;
        }
        catch(Exception ex)
        {
            estado.ForeColor=Color.Firebrick;
            estado.Text=ex.Message;
        }
        finally { guardando=false; if(!IsDisposed) guardar.Enabled=true; }
    }
}
