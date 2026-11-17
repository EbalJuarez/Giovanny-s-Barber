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

    // RF3 y CU-09: filtros de historial por texto, período y persona, aplicados sobre los datos ya consultados.
    // Las fechas solo se muestran cuando la consulta tiene columna Fecha.
    private sealed record Filtro(TextBox Buscar, ComboBox En, Control[] ControlesFecha, DateTimePicker Desde, DateTimePicker Hasta, (ComboBox Lista, string Columna)[] Listas);
    // Columnas ocultas con el texto normalizado de cada celda; "todo" reúne la fila completa.
    private const string PrefijoBusqueda = "__buscar:";

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
        layout.Controls.Add(new Label { Text = modulo switch { "Mi jornada" => "Consulta tu asistencia, servicios, ventas y préstamos.", "Auditoría" => "Bitácora de operaciones y registros desactivados.", _ => "Consulta los datos registrados en la barbería." }, ForeColor = Color.DimGray, Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) }, 0, 1);
        tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Padding = new Point(14, 10) };
        layout.Controls.Add(tabs, 0, 2);
        foreach (string seccion in Secciones(modulo))
        {
            if (seccion == "Usuarios" && !Sesion.EsAdministrador) continue;
            var tab = new TabPage(seccion) { BackColor = Color.White, Padding = new Padding(18) };
            tabs.TabPages.Add(tab);
            var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            tab.Controls.Add(cuerpo);
            var cabecera = new Panel { Dock = DockStyle.Fill };
            var actualizar = new Button { Text = "Actualizar", Dock = DockStyle.Right, Width = 115, Height = 36 };
            cabecera.Controls.Add(new Label { Text = Descripcion(modulo, seccion), Dock = DockStyle.Fill, ForeColor = Color.FromArgb(65, 70, 75) });
            cabecera.Controls.Add(actualizar);
            foreach (string operacion in Operaciones(modulo, seccion).Where(tipo => ConexionBD.PuedeRegistrar(Sesion, tipo)))
            {
                var nuevo = new Button { Text = RegistroForm.EsAccion(operacion) ? RegistroForm.Titulo(operacion) : "+ " + operacion, Dock = DockStyle.Right, AutoSize = true, MinimumSize = new Size(110, 0), Padding = new Padding(6, 0, 6, 0) };
                cabecera.Controls.Add(nuevo);
                nuevo.Click += async (_, _) =>
                {
                    using var ventana = new RegistroForm(Sesion, operacion);
                    var resultado = ventana.ShowDialog(FindForm());
                    if (resultado == DialogResult.OK)
                    {
                        if (ventana.FacturaEmitida is int emitida) CobrarFactura(emitida);
                        if (ventana.CierreRegistrado is int cierre) AbrirCierre(cierre, false);
                        await ActualizarAsync();
                    }
                    else if (ventana.SesionFinalizada != null) (FindForm() as Home)?.FinalizarSesion(ventana.SesionFinalizada);
                };
            }
            if ((modulo, seccion) == ("Contabilidad", "Cierres diarios"))
            {
                // RF14: reimpresión del reporte de un cierre.
                var verCierre = new Button { Text = "Ver reporte de cierre", Dock = DockStyle.Right, AutoSize = true, MinimumSize = new Size(110, 0), Padding = new Padding(6, 0, 6, 0) };
                cabecera.Controls.Add(verCierre);
                verCierre.Click += (_, _) => AbrirCierreSeleccionado(seccion);
            }
            if ((modulo, seccion) == ("Facturación", "Facturas"))
            {
                // CU-15 y CU-16: ver y reimprimir el comprobante de la factura seleccionada.
                var verComprobante = new Button { Text = "Ver comprobante", Dock = DockStyle.Right, Width = 170 };
                cabecera.Controls.Add(verComprobante);
                verComprobante.Click += (_, _) => AbrirComprobanteSeleccionado(seccion);
            }
            // Exporta lo que se ve en la tabla (con filtros) a un archivo que abre Excel.
            var exportar = new Button { Text = "Exportar a Excel", Dock = DockStyle.Right, AutoSize = true, MinimumSize = new Size(110, 0), Padding = new Padding(6, 0, 6, 0) };
            cabecera.Controls.Add(exportar);
            exportar.Click += (_, _) => ExportarSeccion(seccion);
            if ((modulo, seccion) == ("Auditoría", "Bitácora") && Sesion.EsAdministrador)
            {
                var respaldar = new Button { Text = "Respaldar base de datos", Dock = DockStyle.Right, AutoSize = true, MinimumSize = new Size(110, 0), Padding = new Padding(6, 0, 6, 0) };
                cabecera.Controls.Add(respaldar);
                respaldar.Click += async (_, _) => await RespaldarAsync(seccion, respaldar);
            }
            if ((modulo, seccion) == ("Auditoría", "Registros eliminados") && ConexionBD.PuedeRegistrar(Sesion, "Reactivar"))
            {
                var reactivar = new Button { Text = "Reactivar seleccionado", Dock = DockStyle.Right, AutoSize = true, MinimumSize = new Size(110, 0), Padding = new Padding(6, 0, 6, 0) };
                cabecera.Controls.Add(reactivar);
                reactivar.Click += async (_, _) => await ReactivarSeleccionadoAsync(seccion);
            }
            cuerpo.Controls.Add(cabecera, 0, 0);
            var tabla = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false };
            tabla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 240);
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 37, 41);
            tabla.ColumnHeadersHeight = 42;
            const int filaTabla = 2;
            cuerpo.Controls.Add(tabla, 0, filaTabla);
            var estado = new Label { Text = "Seleccione la pestaña para consultar sus datos.", Dock = DockStyle.Fill, ForeColor = Color.DimGray, Padding = new Padding(0, 12, 0, 0) };
            cuerpo.Controls.Add(estado, 0, filaTabla + 1);
            paginas.Add(seccion, (tabla, estado, actualizar));
            if ((modulo, seccion) == ("Facturación", "Facturas"))
                tabla.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AbrirComprobanteSeleccionado(seccion); };
            if ((modulo, seccion) == ("Contabilidad", "Cierres diarios"))
                tabla.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AbrirCierreSeleccionado(seccion); };
            cuerpo.Controls.Add(CrearFiltro(seccion, Persona(modulo, seccion)), 0, 1);
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
            IndexarBusqueda(datos);
            var anteriores = pagina.Tabla.DataSource as System.Data.DataTable;
            pagina.Tabla.DataSource = datos;
            anteriores?.Dispose();
            foreach (DataGridViewColumn columna in pagina.Tabla.Columns)
            {
                if (columna.DataPropertyName.StartsWith(PrefijoBusqueda)) { columna.Visible = false; continue; }
                columna.MinimumWidth = 85;
                if (columna.ValueType == typeof(decimal)) columna.DefaultCellStyle.Format = "N2";
                // La bitácora muestra también la hora de cada operación.
                if (columna.ValueType == typeof(DateTime)) columna.DefaultCellStyle.Format = modulo == "Auditoría" ? "dd/MM/yyyy HH:mm:ss" : "dd/MM/yyyy";
                if (columna.ValueType == typeof(TimeSpan)) columna.DefaultCellStyle.Format = @"hh\:mm\:ss";
            }
            AplicarFiltro(seccion, true);
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

    // Tras emitir una factura se registra su pago, y el comprobante se ofrece hasta que el pago queda guardado.
    private void CobrarFactura(int factura)
    {
        if (!ConexionBD.PuedeRegistrar(Sesion, "Pago")) return;
        using var pago = new RegistroForm(Sesion, "Pago", factura);
        var resultado = pago.ShowDialog(FindForm());
        if (resultado == DialogResult.OK && pago.ResultadoPos == "Rechazada")
        {
            // CU-17: un cobro rechazado no se aplica; se puede intentar con otra tarjeta u otro método.
            if (MessageBox.Show(FindForm(), $"El POS rechazó el cobro de la factura {factura}. Quedó registrado como rechazado y la factura sigue pendiente.\n\n¿Desea registrar otro pago?", "Cobro rechazado", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                CobrarFactura(factura);
        }
        else if (resultado == DialogResult.OK && pago.ResultadoPos == "Pendiente")
            MessageBox.Show(FindForm(), $"El cobro de la factura {factura} quedó pendiente de confirmar. Consulte la terminal POS y confírmelo en Facturación › Transacciones POS; después podrá imprimir el comprobante.", "Cobro por confirmar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        else if (resultado == DialogResult.OK)
        {
            if (MessageBox.Show(FindForm(), $"Pago de la factura {factura} registrado. ¿Desea ver e imprimir el comprobante?", "Comprobante", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                AbrirComprobante(factura, false);
        }
        else if (pago.SesionFinalizada != null) (FindForm() as Home)?.FinalizarSesion(pago.SesionFinalizada);
        else MessageBox.Show(FindForm(), $"La factura {factura} quedó pendiente de pago. Regístralo en la pestaña Pagos para imprimir el comprobante.", "Pago pendiente", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AbrirCierreSeleccionado(string seccion)
    {
        var pagina = paginas[seccion];
        if (pagina.Tabla.CurrentRow?.DataBoundItem is not System.Data.DataRowView fila || fila["ID"] is not int numero)
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "Seleccione un cierre de la lista.";
            return;
        }
        AbrirCierre(numero, true);
    }

    private void AbrirCierre(int cuadre, bool reimpresion)
    {
        using var ventana = ComprobanteForm.Cierre(Sesion, cuadre, reimpresion);
        ventana.ShowDialog(FindForm());
        if (ventana.SesionFinalizada != null) (FindForm() as Home)?.FinalizarSesion(ventana.SesionFinalizada);
    }

    private void AbrirComprobante(int factura, bool reimpresion)
    {
        using var ventana = new ComprobanteForm(Sesion, factura, reimpresion);
        ventana.ShowDialog(FindForm());
        if (ventana.SesionFinalizada != null) (FindForm() as Home)?.FinalizarSesion(ventana.SesionFinalizada);
    }

    private async Task ReactivarSeleccionadoAsync(string seccion)
    {
        var pagina = paginas[seccion];
        if (pagina.Tabla.CurrentRow?.DataBoundItem is not System.Data.DataRowView fila || fila["ID"] is not int id || fila["Tipo"] is not string tipoRegistro)
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "Seleccione un registro de la lista.";
            return;
        }
        if (tipoRegistro is "Factura" or "Pago")
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "Las facturas y los pagos anulados no se pueden reactivar. Registre uno nuevo.";
            return;
        }
        if (MessageBox.Show(FindForm(), $"¿Reactivar {tipoRegistro.ToLowerInvariant()} «{fila["Nombre"]}»? Volverá a aparecer en las listas.", "Reactivar registro", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            await ConexionBD.GuardarRegistroAsync(Sesion, "Reactivar", new Dictionary<string, object> { ["entidad"] = tipoRegistro, ["id"] = id });
            await ActualizarAsync();
        }
        catch (SesionInvalidaException ex) { (FindForm() as Home)?.FinalizarSesion(ex.Message); }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = ex.Message;
        }
    }

    // CSV con el separador de listas de Windows y BOM UTF-8, para que Excel muestre bien tildes y columnas.
    private void ExportarSeccion(string seccion)
    {
        var pagina = paginas[seccion];
        if (pagina.Tabla.DataSource is not System.Data.DataTable datos || datos.DefaultView.Count == 0)
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "No hay registros para exportar.";
            return;
        }
        using var dialogo = new SaveFileDialog
        {
            Filter = "Archivo para Excel (*.csv)|*.csv",
            FileName = $"{modulo} - {seccion} {DateTime.Now:yyyy-MM-dd HHmm}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialogo.ShowDialog(FindForm()) != DialogResult.OK) return;
        var cultura = System.Globalization.CultureInfo.CurrentCulture;
        string separador = cultura.TextInfo.ListSeparator;
        string Campo(string texto) => texto.Contains(separador) || texto.Contains('"') || texto.Contains('\n') || texto.Contains('\r')
            ? "\"" + texto.Replace("\"", "\"\"") + "\"" : texto;
        // Mismo orden y columnas visibles que la tabla en pantalla.
        var columnas = pagina.Tabla.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).Select(c => c.DataPropertyName).ToArray();
        try
        {
            using var archivo = new StreamWriter(dialogo.FileName, false, new System.Text.UTF8Encoding(true));
            archivo.WriteLine(string.Join(separador, columnas.Select(Campo)));
            foreach (System.Data.DataRowView fila in datos.DefaultView)
                archivo.WriteLine(string.Join(separador, columnas.Select(c => Campo(fila[c] switch
                {
                    DBNull => "",
                    decimal monto => monto.ToString("0.00", cultura),
                    DateTime fecha => fecha.ToString(fecha.TimeOfDay == TimeSpan.Zero ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm:ss"),
                    TimeSpan hora => hora.ToString(@"hh\:mm\:ss"),
                    var otro => Convert.ToString(otro, cultura) ?? ""
                }))));
            pagina.Estado.ForeColor = Color.DimGray;
            pagina.Estado.Text = $"Se exportaron {datos.DefaultView.Count} registros a {dialogo.FileName}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "No se pudo guardar el archivo (¿está abierto en Excel?): " + ex.Message;
        }
    }

    private async Task RespaldarAsync(string seccion, Button boton)
    {
        var pagina = paginas[seccion];
        if (MessageBox.Show(FindForm(), "Se creará una copia completa de la base de datos. Puede tardar unos segundos. ¿Continuar?", "Respaldar base de datos", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        boton.Enabled = false;
        pagina.Estado.ForeColor = Color.DimGray;
        pagina.Estado.Text = "Creando respaldo…";
        try
        {
            string ruta = await ConexionBD.RespaldarBaseDatosAsync(Sesion);
            if (IsDisposed) return;
            await ActualizarAsync();
            MessageBox.Show(FindForm(), "Respaldo creado en:\n" + ruta + "\n\nCópielo a una memoria USB o a la nube para protegerlo si la computadora falla.", "Respaldo completado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (SesionInvalidaException ex) { (FindForm() as Home)?.FinalizarSesion(ex.Message); }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "No se pudo crear el respaldo: " + ex.Message;
        }
        finally { if (!IsDisposed) boton.Enabled = true; }
    }

    private Control CrearFiltro(string seccion, string[] columnasLista)
    {
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        Label Etiqueta(string texto) => new() { Text = texto, AutoSize = true, Margin = new Padding(0, 9, 6, 0) };
        var buscar = new TextBox { Width = 280, MaxLength = 100, PlaceholderText = "Escribe una o varias palabras…", Margin = new Padding(0, 4, 8, 0) };
        var en = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Margin = new Padding(0, 4, 16, 0) };
        en.Items.Add("Todas las columnas");
        en.SelectedIndex = 0;
        barra.Controls.AddRange([Etiqueta("Buscar"), buscar, Etiqueta("en"), en]);
        // Con muchos registros se espera a que termine de escribir antes de filtrar.
        var espera = new System.Windows.Forms.Timer { Interval = 300 };
        espera.Tick += (_, _) => { espera.Stop(); AplicarFiltro(seccion); };
        barra.Disposed += (_, _) => espera.Dispose();
        buscar.TextChanged += (_, _) => { espera.Stop(); espera.Start(); };
        buscar.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; espera.Stop(); AplicarFiltro(seccion); } };
        en.SelectedIndexChanged += (_, _) => AplicarFiltro(seccion);
        var desde = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 140, Margin = new Padding(0, 4, 16, 0) };
        var hasta = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Width = 140, Margin = new Padding(0, 4, 16, 0) };
        Control[] controlesFecha = [Etiqueta("Desde"), desde, Etiqueta("Hasta"), hasta];
        foreach (var control in controlesFecha) control.Visible = false;
        barra.Controls.AddRange(controlesFecha);
        desde.ValueChanged += (_, _) => AplicarFiltro(seccion);
        hasta.ValueChanged += (_, _) => AplicarFiltro(seccion);
        var listas = columnasLista.Select(columna =>
        {
            var lista = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = columnasLista.Length > 1 ? 150 : 220, Margin = new Padding(0, 4, 16, 0) };
            lista.Items.Add("Todos");
            lista.SelectedIndex = 0;
            barra.Controls.AddRange([Etiqueta(columna), lista]);
            lista.SelectedIndexChanged += (_, _) => AplicarFiltro(seccion);
            return (lista, columna);
        }).ToArray();
        var limpiar = new Button { Text = "Limpiar filtros", AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
        limpiar.Click += (_, _) =>
        {
            desde.Checked = false;
            hasta.Checked = false;
            foreach (var (lista, _) in listas) lista.SelectedIndex = 0;
            en.SelectedIndex = 0;
            buscar.Clear();
            espera.Stop();
            AplicarFiltro(seccion);
        };
        barra.Controls.Add(limpiar);
        filtros.Add(seccion, new Filtro(buscar, en, controlesFecha, desde, hasta, listas));
        return barra;
    }

    // Minúsculas y sin tildes, para que "jose" encuentre "José".
    private static string Normalizar(string texto)
    {
        var resultado = new System.Text.StringBuilder(texto.Length);
        foreach (char c in texto.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) resultado.Append(c);
        return resultado.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    // Texto de la celda como se ve en la tabla; los montos también sin separador de miles para encontrar "1500".
    private static string TextoCelda(object valor) => valor switch
    {
        DBNull => "",
        decimal monto => monto.ToString("N2") + " " + monto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
        DateTime fecha => fecha.ToString(fecha.TimeOfDay == TimeSpan.Zero ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm:ss"),
        TimeSpan hora => hora.ToString(@"hh\:mm\:ss"),
        _ => Convert.ToString(valor) ?? ""
    };

    // Se prepara una sola vez por consulta, así cada búsqueda solo compara texto ya normalizado.
    private static void IndexarBusqueda(System.Data.DataTable datos)
    {
        var visibles = datos.Columns.Cast<System.Data.DataColumn>().ToArray();
        var ocultas = visibles.Select(c => datos.Columns.Add(PrefijoBusqueda + c.ColumnName, typeof(string))).ToArray();
        var todo = datos.Columns.Add(PrefijoBusqueda + "todo", typeof(string));
        datos.BeginLoadData();
        foreach (System.Data.DataRow fila in datos.Rows)
        {
            var partes = new string[visibles.Length];
            for (int i = 0; i < visibles.Length; i++) fila[ocultas[i]] = partes[i] = Normalizar(TextoCelda(fila[visibles[i]]));
            fila[todo] = string.Join(" | ", partes);
        }
        datos.EndLoadData();
        datos.AcceptChanges();
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
        bool filtraFecha = columnas.Contains("Fecha") && columnas["Fecha"]!.DataType == typeof(DateTime);
        if (datosNuevos)
        {
            // Se conservan la columna y la persona elegidas al actualizar si siguen apareciendo en los datos.
            foreach (var control in filtro.ControlesFecha) control.Visible = filtraFecha;
            string? columnaElegida = filtro.En.SelectedIndex > 0 ? filtro.En.SelectedItem as string : null;
            var nombresColumnas = columnas.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).Where(c => !c.StartsWith(PrefijoBusqueda)).ToArray();
            filtro.En.BeginUpdate();
            filtro.En.Items.Clear();
            filtro.En.Items.Add("Todas las columnas");
            filtro.En.Items.AddRange(nombresColumnas);
            filtro.En.EndUpdate();
            filtro.En.SelectedIndex = columnaElegida != null && filtro.En.Items.Contains(columnaElegida) ? filtro.En.Items.IndexOf(columnaElegida) : 0;
        }
        foreach (var (lista, columna) in filtro.Listas.Where(l => datosNuevos && columnas.Contains(l.Columna)))
        {
            string? elegida = lista.SelectedIndex > 0 ? lista.SelectedItem as string : null;
            var nombres = datos.Rows.Cast<System.Data.DataRow>().Select(f => f[columna] as string).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Order().ToArray();
            lista.BeginUpdate();
            lista.Items.Clear();
            lista.Items.Add("Todos");
            lista.Items.AddRange(nombres!);
            lista.EndUpdate();
            lista.SelectedIndex = elegida != null && lista.Items.Contains(elegida) ? lista.Items.IndexOf(elegida) : 0;
        }
        // CU-09 y CU-10: un rango invertido no se aplica; se pide corregirlo.
        if (filtraFecha && filtro.Desde.Checked && filtro.Hasta.Checked && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
        {
            pagina.Estado.ForeColor = Color.Firebrick;
            pagina.Estado.Text = "La fecha inicial es posterior a la final. Corrija el rango.";
            return;
        }
        var condiciones = new List<string>();
        // Cada palabra debe aparecer en alguna columna (o en la elegida), en cualquier orden.
        string destino = PrefijoBusqueda + (filtro.En.SelectedIndex > 0 ? (string)filtro.En.SelectedItem! : "todo");
        if (columnas.Contains(destino))
            foreach (string palabra in Normalizar(filtro.Buscar.Text).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct())
                condiciones.Add($"[{destino}] LIKE '%{TextoLike(palabra)}%'");
        if (filtraFecha)
        {
            // RowFilter exige las fechas en formato invariable MM/dd/yyyy.
            static string Literal(DateTime fecha) => "#" + fecha.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture) + "#";
            if (filtro.Desde.Checked) condiciones.Add("Fecha >= " + Literal(filtro.Desde.Value.Date));
            if (filtro.Hasta.Checked) condiciones.Add("Fecha < " + Literal(filtro.Hasta.Value.Date.AddDays(1)));
        }
        foreach (var (lista, columna) in filtro.Listas.Where(l => l.Lista.SelectedIndex > 0 && columnas.Contains(l.Columna)))
            condiciones.Add($"[{columna}] = '{((string)lista.SelectedItem!).Replace("'", "''")}'");
        datos.DefaultView.RowFilter = string.Join(" AND ", condiciones);
        int visibles = datos.DefaultView.Count;
        string resumen = condiciones.Count == 0 ? $"{visibles} registros" : $"{visibles} de {datos.Rows.Count} registros";
        var filas = datos.DefaultView.Cast<System.Data.DataRowView>().ToArray();
        if (columnas.Contains("Visitas"))
            resumen += $" · Visitas: {filas.Sum(f => Convert.ToInt64(f["Visitas"])):N0}";
        string? columnaTotal = new[] { "Total", "Monto", "Ingresos" }.FirstOrDefault(c => columnas.Contains(c) && columnas[c]!.DataType == typeof(decimal));
        if (columnaTotal != null)
            resumen += $" · {columnaTotal}: Q {filas.Sum(f => f[columnaTotal] as decimal? ?? 0):N2}";
        // CU-21: el reporte contable también totaliza egresos y saldo del período filtrado.
        foreach (string extra in new[] { "Egresos", "Saldo", "Diferencia de caja" }.Where(c => columnas.Contains(c) && columnas[c]!.DataType == typeof(decimal)))
            resumen += $" · {extra}: Q {filas.Sum(f => f[extra] as decimal? ?? 0):N2}";
        pagina.Estado.ForeColor = Color.DimGray;
        pagina.Estado.Text = datos.Rows.Count == 0 ? "No hay registros para esta consulta."
            : visibles == 0 ? "No hay registros que coincidan con la búsqueda o los filtros."
            : $"{resumen} · Actualizado a las {DateTime.Now:HH:mm}";
    }

    // Columnas (persona, caja, tipo…) que se pueden elegir de una lista para filtrar.
    private static string[] Persona(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Trabajadores", "Asistencia" or "Actividades" or "Préstamos" or "Descuentos" or "Rendimiento") => ["Trabajador"],
        ("Inventario", "Productos") => ["Categoría"],
        ("Inventario", "Movimientos") => ["Producto"],
        ("Facturación", "Facturas") => ["Cliente"],
        ("Facturación", "Transacciones POS") => ["Estado", "Método"],
        ("Clientes", "Historial" or "Reporte diario" or "Reporte semanal" or "Reporte mensual") => ["Cliente"],
        // CU-19: movimientos por usuario y tipo, además de la caja; la fecha usa Desde y Hasta.
        ("Contabilidad", "Ingresos y egresos") => ["Caja", "Usuario", "Tipo"],
        ("Contabilidad", "Cierres diarios") => ["Caja", "Usuario"],
        ("Contabilidad", "Reporte diario" or "Reporte semanal" or "Reporte mensual") => ["Caja"],
        ("Contabilidad", "Ingresos por método") => ["Método"],
        ("Auditoría", "Bitácora") => ["Usuario"],
        ("Auditoría", "Registros eliminados") => ["Tipo"],
        _ => []
    };

    private static string[] Secciones(string modulo) => modulo switch
    {
        "Trabajadores" => ["Directorio", "Asistencia", "Actividades", "Rendimiento", "Préstamos", "Descuentos", "Usuarios"],
        "Mi jornada" => ["Mi asistencia", "Mis servicios", "Mis ventas", "Mis préstamos"],
        "Clientes" => ["Clientes", "Historial", "Reporte diario", "Reporte semanal", "Reporte mensual"],
        "Inventario" => ["Productos", "Movimientos", "Categorías"],
        "Facturación" => ["Facturas", "Detalle de factura", "Servicios", "Pagos", "Transacciones POS"],
        "Contabilidad" => ["Ingresos y egresos", "Cuadre de caja", "Cierres diarios", "Reporte diario", "Reporte semanal", "Reporte mensual", "Ingresos por método", "Cajas"],
        "Auditoría" => ["Bitácora", "Registros eliminados"],
        _ => []
    };

    private static string[] Operaciones(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Trabajadores", "Directorio") => ["Empleado", "Editar empleado", "Desactivar empleado"],
        ("Trabajadores", "Usuarios") => ["Clave"],
        ("Trabajadores", "Asistencia") => ["Asistencia", "Editar asistencia", "Desactivar asistencia"],
        ("Mi jornada", "Mi asistencia") => ["Asistencia", "Editar asistencia", "Desactivar asistencia"],
        ("Trabajadores", "Préstamos") => ["Préstamo", "Resolver préstamo", "Descuento"],
        ("Trabajadores", "Descuentos") => ["Descuento"],
        ("Mi jornada", "Mis préstamos") => ["Préstamo"],
        ("Clientes", "Clientes") => ["Cliente", "Editar cliente", "Desactivar cliente"],
        ("Inventario", "Productos") => ["Producto", "Editar producto", "Desactivar producto"],
        ("Inventario", "Movimientos") => ["Movimiento"],
        ("Inventario", "Categorías") => ["Categoría", "Editar categoría", "Desactivar categoría"],
        ("Facturación", "Servicios") => ["Servicio", "Editar servicio", "Desactivar servicio"],
        ("Facturación", "Pagos") => ["Pago", "Método de pago", "Editar pago", "Editar método de pago", "Anular pago", "Desactivar método de pago"],
        ("Facturación", "Facturas") => ["Factura", "Editar factura", "Anular factura"],
        ("Contabilidad", "Ingresos y egresos") => ["Transacción"],
        ("Contabilidad", "Cajas") => ["Caja", "Editar caja", "Desactivar caja"],
        ("Contabilidad", "Cuadre de caja") or ("Contabilidad", "Cierres diarios") => ["Cierre"],
        ("Facturación", "Transacciones POS") => ["Confirmar transacción POS"],
        ("Facturación", "Detalle de factura") or ("Trabajadores", "Actividades") or ("Clientes", "Historial") or ("Mi jornada", "Mis servicios") or ("Mi jornada", "Mis ventas") => ["Factura"],
        _ => []
    };

    private static string Descripcion(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Contabilidad", "Reporte diario" or "Reporte semanal" or "Reporte mensual") => "CU-21: ingresos, egresos, saldo y cierres por período y caja. Elige Desde y Hasta para ver los totales.",
        ("Contabilidad", "Ingresos y egresos") => "Movimientos de cada caja con el usuario que los registró. Filtra por fecha, caja, usuario o tipo.",
        ("Contabilidad", "Cuadre de caja") => "Período abierto de cada caja: el efectivo esperado se compara con lo contado al cerrar. Tarjeta y otros métodos se concilian aparte.",
        ("Contabilidad", "Cierres diarios") => "Historial de cierres. Selecciona uno y pulsa Ver reporte de cierre para reimprimirlo.",
        ("Facturación", "Transacciones POS") => "Cobros con tarjeta y los datos del voucher del POS. Confirma aquí los que quedaron pendientes.",
        _ => DescripcionSeccion(seccion)
    };

    private static string DescripcionSeccion(string seccion) => seccion switch
    {
        "Clientes" => "Busca al cliente antes de registrarlo para no duplicarlo. Sin NIT se registra como CF.",
        "Historial" => "Servicios por categoría (corte de pelo, arreglo de barba, pelo y barba, prótesis y mantenimiento) y productos comprados.",
        "Reporte diario" or "Reporte semanal" or "Reporte mensual" => "Visitas (facturas) e ingresos por cliente. El filtro de fechas usa el inicio de cada período.",
        "Préstamos" => "Solicitudes de trabajadores para revisión y gestión del administrador.",
        "Mis préstamos" => "Consulta tus solicitudes y su estado de aprobación.",
        "Detalle de factura" => "Cliente, trabajador, servicios, productos, cantidades y precios del comprobante.",
        "Movimientos" => "Entradas, salidas y ajustes del inventario.",
        "Productos" => "Busca por producto o categoría. Las existencias cambian con entradas, salidas y ventas.",
        "Bitácora" => "Quién hizo cada operación, cuándo y qué cambió.",
        "Ingresos por método" => "Cobros vigentes por método de pago, para cuadrar efectivo y vouchers de tarjeta.",
        "Rendimiento" => "Facturas, servicios e ingresos de cada trabajador por mes; sirve de base para comisiones.",
        "Registros eliminados" => "Registros desactivados y anulados. Selecciona uno y pulsa Reactivar seleccionado para volver a usarlo.",
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
    private bool catalogosCargados;
    // Registro cuyos datos están en pantalla; Guardar exige que sea el elegido para no guardar datos de otro.
    private int? edicionCargada;
    private int cargasPendientes;
    // Motivo por el que la sesión dejó de ser válida; quien abrió la ventana cierra la sesión.
    public string? SesionFinalizada { get; private set; }
    // Número de la factura recién emitida, para ofrecer su comprobante.
    public int? FacturaEmitida { get; private set; }
    // Número del cierre registrado, para mostrar su reporte.
    public int? CierreRegistrado { get; private set; }
    // Respuesta del POS de un cobro con tarjeta: Aprobada, Rechazada o Pendiente; null en los demás registros.
    public string? ResultadoPos { get; private set; }
    // Etiqueta de cada campo, para ocultarla junto con su control.
    private readonly Dictionary<string, Label> etiquetas = new();
    private readonly DataGridView detalle = new() { Height = 175, Dock = DockStyle.Top, BackgroundColor = Color.White, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly ComboBox tipoLinea = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly ComboBox articulo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250, DisplayMember = "Nombre" };
    private readonly NumericUpDown cantidad = new() { Minimum = 1, Maximum = 100000, Value = 1, Width = 75 };
    private readonly Label total = new() { Text = "Total estimado: Q 0.00", Height = 30, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleRight };
    private readonly Dictionary<string, List<OpcionRegistro>> articulos = new();
    // Factura que se selecciona al abrir el registro de un pago justo después de emitirla.
    private readonly int? facturaInicial;

    public RegistroForm(SesionUsuario sesion, string tipo, int? facturaInicial = null)
    {
        if (!ConexionBD.PuedeRegistrar(sesion,tipo)) throw new UnauthorizedAccessException("No tiene permiso para abrir este registro.");
        this.sesion = sesion;
        this.tipo = tipo;
        this.facturaInicial = facturaInicial;
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
                Numero("salario","Salario quincenal",true,1000);
                Texto("usuario","Usuario",50); Texto("clave","Contraseña nueva",72,true); Texto("confirmar","Confirmar contraseña",72,true);
                Lista("rol","Rol","Trabajador","Administrador");
                break;
            case "Editar empleado":
                Catalogo("trabajador","Trabajador");
                Texto("nombres","Nombres",80); Texto("apellidos","Apellidos",80); Texto("telefono","Teléfono",20); Texto("direccion","Dirección",200);
                Numero("salario","Salario quincenal",true,1000);
                Lista("estado","Estado de la cuenta","Activo","Inactivo");
                break;
            case "Resolver préstamo": Catalogo("prestamo","Solicitud pendiente"); Lista("decision","Decisión","Autorizar","Rechazar"); break;
            case "Descuento": Catalogo("prestamo","Préstamo autorizado"); Numero("monto","Monto a descontar",true); Texto("observacion","Período o nota",100); break;
            case "Clave": Catalogo("usuario","Usuario"); Texto("clave","Contraseña nueva",72,true); Texto("confirmar","Confirmar contraseña",72,true); break;
            case "Cliente": Texto("nombre","Nombre",100); Texto("nit","NIT",15); Texto("telefono","Teléfono",20); Texto("correo","Correo",100); break;
            case "Editar cliente":
                Catalogo("cliente","Cliente");
                Texto("nombre","Nombre",100); Texto("nit","NIT",15); Texto("telefono","Teléfono",20); Texto("correo","Correo",100);
                break;
            case "Categoría": Texto("nombre","Nombre",80); Texto("descripcion","Descripción",250); break;
            case "Producto": Catalogo("categoria","Categoría"); Texto("nombre","Nombre",100); Numero("precio","Precio de venta",true); Numero("stock","Existencia inicial"); Numero("minimo","Stock mínimo"); break;
            case "Editar producto":
                Catalogo("producto","Producto");
                Catalogo("categoria","Categoría"); Texto("nombre","Nombre",100); Numero("precio","Precio de venta",true); Numero("minimo","Stock mínimo");
                controles["precio"].Enabled = sesion.EsAdministrador;
                break;
            case "Movimiento": Catalogo("producto","Producto"); Lista("tipo","Tipo","Entrada","Salida"); Numero("cantidad","Cantidad",false,1); Texto("motivo","Motivo",250); break;
            case "Servicio": Texto("nombre","Nombre",100); Numero("precio","Precio",true); Texto("descripcion","Descripción",250); Lista("categoria","Tipo de servicio",ConexionBD.TiposServicio); break;
            case "Caja": Texto("nombre","Nombre de caja",80); break;
            case var desactivar when Desactivaciones.TryGetValue(desactivar, out var registro):
                Catalogo(registro.Campo, registro.Titulo);
                guardar.Text = tipo.StartsWith("Anular ") ? "Anular" : "Desactivar";
                guardar.BackColor = Color.Firebrick;
                guardar.ForeColor = Color.White;
                break;
            case "Editar servicio":
                Catalogo("servicio","Servicio");
                Texto("nombre","Nombre",100); Numero("precio","Precio",true); Texto("descripcion","Descripción",250); Lista("categoria","Tipo de servicio",ConexionBD.TiposServicio);
                break;
            case "Editar categoría": Catalogo("categoria","Categoría"); Texto("nombre","Nombre",80); Texto("descripcion","Descripción",250); break;
            case "Editar método de pago": Catalogo("metodo","Método de pago"); Texto("nombre","Nombre",50); Lista("estado","Estado","Activo","Inactivo"); break;
            case "Editar caja": Catalogo("caja","Caja"); Texto("nombre","Nombre de caja",80); Lista("estado","Estado","Abierta","Cerrada"); break;
            case "Editar pago": Catalogo("pago","Pago"); Catalogo("metodo","Método de pago"); Texto("referencia","Referencia",100); break;
            case "Editar asistencia": Catalogo("asistencia","Registro"); Texto("entrada","Entrada (HH:mm)",5); Texto("salida","Salida (HH:mm)",5); break;
            case "Editar factura": Catalogo("factura","Factura"); Catalogo("cliente","Cliente"); Catalogo("trabajador","Trabajador"); Lista("estado","Estado","Vigente","Anulada"); break;
            case "Método de pago": Texto("nombre","Nombre",50); break;
            case "Transacción": Catalogo("caja","Caja"); Lista("tipo","Tipo","Ingreso","Egreso"); Texto("concepto","Concepto",200); Numero("monto","Monto",true); break;
            case "Cierre": Catalogo("caja","Caja"); Numero("contado","Efectivo contado",true); Numero("fondo","Fondo que queda en caja",true); break;
            case "Confirmar transacción POS":
                Catalogo("transaccion","Transacción pendiente"); Lista("resultado","Resultado en el POS","Aprobada","Rechazada");
                Texto("autorizacion","Código de autorización",12); Texto("referencia","Referencia del voucher",100);
                ((ComboBox)controles["resultado"]).SelectedIndexChanged += (_,_) => MostrarVoucher(controles["resultado"].Text == "Aprobada");
                break;
            case "Asistencia": Catalogo("trabajador","Trabajador"); Lista("tipo","Marca","Entrada","Salida"); break;
            case "Préstamo": Catalogo("trabajador","Trabajador"); Numero("monto","Monto solicitado",true); break;
            case "Pago":
                Catalogo("factura","Factura"); Catalogo("metodo","Método de pago"); Catalogo("caja","Caja"); Numero("monto","Monto",true);
                // CU-17: con tarjeta se copian del voucher del POS el resultado, la autorización y la referencia.
                Lista("resultado","Resultado en el POS","Aprobada","Rechazada","Pendiente");
                Texto("autorizacion","Código de autorización",12); Texto("referencia","Referencia",100);
                // El monto se llena con el saldo pendiente de la factura al elegirla o al elegir el método de pago.
                ((ComboBox)controles["factura"]).SelectedIndexChanged += (_,_) => MontoPendiente();
                ((ComboBox)controles["metodo"]).SelectionChangeCommitted += (_,_) => MontoPendiente();
                ((ComboBox)controles["metodo"]).SelectedIndexChanged += (_,_) => MostrarCamposTarjeta();
                ((ComboBox)controles["resultado"]).SelectedIndexChanged += (_,_) => MostrarCamposTarjeta();
                MostrarCamposTarjeta();
                break;
            case "Factura": Catalogo("cliente","Cliente"); Catalogo("trabajador","Trabajador"); CrearDetalle(); break;
        }
        estado.Text = tipo switch
        {
            "Clave" or "Empleado" => "La contraseña debe tener al menos 8 caracteres. Confírmala antes de guardar.",
            "Factura" => "Se emitirá la factura y se descontará el inventario. Después se abrirá el registro del pago.",
            "Asistencia" => "Se registrará la fecha y hora actuales.",
            "Préstamo" => "La solicitud quedará pendiente de aprobación.",
            "Editar empleado" => "Selecciona un trabajador para cargar sus datos. Una cuenta inactiva no puede iniciar sesión.",
            "Cliente" => "Deja el NIT vacío para consumidor final (CF). No se permiten dos clientes con el mismo NIT.",
            "Editar cliente" => "Selecciona un cliente para cargar sus datos.",
            "Editar pago" => "Solo se corrigen el método y la referencia; el monto no cambia para no descuadrar la caja.",
            "Editar asistencia" => "Escribe las horas como HH:mm. Deja la salida vacía si aún no ha salido.",
            "Editar factura" => "Al anularla, los productos vuelven al inventario y los pagos se devuelven como egreso de su caja.",
            "Editar servicio" or "Editar categoría" or "Editar método de pago" or "Editar caja" => "Selecciona un registro para cargar sus datos.",
            "Anular factura" => "Los productos vuelven al inventario y los pagos se devuelven como egreso de su caja. Una factura anulada no se reactiva.",
            "Anular pago" => "Se registrará un egreso en su caja y la factura quedará con saldo pendiente. Un pago anulado no se reactiva.",
            "Desactivar empleado" => "Deja de aparecer en las listas y su cuenta no podrá iniciar sesión. Se puede reactivar desde Auditoría.",
            _ when tipo.StartsWith("Desactivar ") => "Deja de aparecer en las listas, pero se conserva su historial. Se puede reactivar desde Auditoría.",
            "Editar producto" => sesion.EsAdministrador ? "Selecciona un producto para cargar sus datos. Las existencias se ajustan con movimientos."
                : "Selecciona un producto. Solo el administrador puede cambiar el precio.",
            "Resolver préstamo" => "Al autorizar, el saldo pendiente será el monto solicitado. La decisión no se puede cambiar.",
            "Descuento" => "Se descontará del salario y se restará del saldo. Al llegar a cero, el préstamo queda pagado.",
            "Cierre" => "Cuenta solo el efectivo. Con fondo 0 la caja queda en ceros para el siguiente período; el resto se retira. No se puede cerrar con facturas sin cobrar ni cobros POS por confirmar.",
            "Pago" => "Con tarjeta, copia del voucher del POS el resultado, el código de autorización y la referencia.",
            "Confirmar transacción POS" => "Consulta el estado en la terminal POS. Si se aprobó, copia la autorización y la referencia del voucher.",
            _ => "Completa los campos y pulsa Guardar."
        };
        // Cada vez que cambia el registro elegido (con el ratón o escribiendo su nombre) se cargan sus datos.
        if (CatalogoEdicion is string edicion)
            ((ComboBox)controles[edicion]).SelectedIndexChanged += async (_,_) => { if (catalogosCargados) await CargarEdicionAsync(); };
        guardar.Enabled = false;
        Shown += async (_,_) => await CargarCatalogosAsync();
        guardar.Click += async (_,_) => await GuardarAsync();
        FormClosing += (_,e) => { if (guardando) e.Cancel=true; };
    }

    // Operaciones que modifican registros existentes: el botón muestra la acción en lugar de "+ tipo".
    internal static bool EsAccion(string tipo) => tipo is "Clave" or "Resolver préstamo" or "Descuento" || tipo.StartsWith("Editar ") || tipo.StartsWith("Desactivar ") || tipo.StartsWith("Anular ");
    internal static string Titulo(string tipo) => tipo switch
    {
        "Clave" => "Restablecer clave",
        "Resolver préstamo" => tipo,
        _ when tipo.StartsWith("Editar ") || tipo.StartsWith("Desactivar ") || tipo.StartsWith("Anular ") => tipo,
        "Descuento" => "Descontar de salario",
        _ => "Registrar " + tipo.ToLowerInvariant()
    };

    // Campo y etiqueta del catálogo del registro que se desactiva o anula.
    private static readonly Dictionary<string, (string Campo, string Titulo)> Desactivaciones = new()
    {
        ["Desactivar empleado"] = ("trabajador", "Trabajador"), ["Desactivar cliente"] = ("cliente", "Cliente"), ["Desactivar producto"] = ("producto", "Producto"),
        ["Desactivar categoría"] = ("categoria", "Categoría"), ["Desactivar servicio"] = ("servicio", "Servicio"), ["Desactivar método de pago"] = ("metodo", "Método de pago"),
        ["Desactivar caja"] = ("caja", "Caja"), ["Anular pago"] = ("pago", "Pago"), ["Desactivar asistencia"] = ("asistencia", "Registro"), ["Anular factura"] = ("factura", "Factura")
    };

    // El primer campo de una edición es el catálogo del registro que se modifica.
    private string? CatalogoEdicion => tipo switch
    {
        "Editar empleado" => "trabajador", "Editar cliente" => "cliente", "Editar producto" => "producto",
        "Editar servicio" => "servicio", "Editar categoría" => "categoria", "Editar método de pago" => "metodo", "Editar caja" => "caja",
        "Editar pago" => "pago", "Editar asistencia" => "asistencia", "Editar factura" => "factura",
        _ => null
    };

    private async Task CargarEdicionAsync()
    {
        if (CatalogoEdicion == null || controles[CatalogoEdicion] is not ComboBox elegido || elegido.SelectedItem is not OpcionRegistro opcion) return;
        guardar.Enabled = false;
        edicionCargada = null;
        cargasPendientes++;
        try
        {
            var datos = await ConexionBD.ObtenerParaEditarAsync(sesion, tipo, opcion.Id);
            // Si mientras tanto se eligió otro registro, su propia carga llena el formulario.
            if (IsDisposed || !ReferenceEquals(elegido.SelectedItem, opcion)) return;
            foreach (var dato in datos)
            {
                if (controles[dato.Key] is NumericUpDown numero)
                {
                    if (dato.Value != DBNull.Value) numero.Value = Math.Clamp(Convert.ToDecimal(dato.Value), numero.Minimum, numero.Maximum);
                }
                // Un catálogo recibe el Id del registro relacionado; una lista fija recibe el texto.
                else if (controles[dato.Key] is ComboBox lista) lista.SelectedItem = dato.Value is int id ? lista.Items.OfType<OpcionRegistro>().FirstOrDefault(o => o.Id == id) : dato.Value;
                else controles[dato.Key].Text = (string)dato.Value;
            }
            edicionCargada = opcion.Id;
            estado.ForeColor = Color.DimGray;
            estado.Text = "Modifica los datos y pulsa Guardar.";
        }
        catch(SesionInvalidaException ex) { if(!IsDisposed) { SesionFinalizada=ex.Message; DialogResult=DialogResult.Abort; } }
        catch(Exception ex) { if(!IsDisposed) { estado.ForeColor=Color.Firebrick; estado.Text="No se pudieron cargar los datos: " + ex.Message; } }
        finally { if (--cargasPendientes == 0 && !IsDisposed) guardar.Enabled = true; }
    }

    private void Agregar(string nombre, string titulo, Control control)
    {
        int fila = campos.RowCount++;
        campos.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(4,6,4,12);
        var etiqueta = new Label { Text = titulo, AutoSize = true, Margin = new Padding(4,10,4,12) };
        campos.Controls.Add(etiqueta,0,fila);
        campos.Controls.Add(control,1,fila);
        controles.Add(nombre,control);
        etiquetas.Add(nombre,etiqueta);
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
    // Se puede escribir en la lista para encontrar la opción; al guardar solo vale una opción de la lista.
    private void Catalogo(string nombre,string titulo)
    {
        var combo = new ComboBox { DropDownStyle=ComboBoxStyle.DropDown, DisplayMember="Nombre", AutoCompleteMode=AutoCompleteMode.SuggestAppend, AutoCompleteSource=AutoCompleteSource.ListItems };
        combo.Leave += (_,_) => ElegirEscrito(combo);
        Agregar(nombre,titulo,combo);
        catalogos.Add(nombre,titulo);
    }
    // Selecciona la opción cuyo nombre se escribió; devuelve false si el texto no coincide con ninguna.
    private static bool ElegirEscrito(ComboBox combo)
    {
        if (combo.SelectedItem != null && combo.Text == combo.GetItemText(combo.SelectedItem)) return true;
        int indice = combo.FindStringExact(combo.Text);
        if (indice < 0) return false;
        combo.SelectedIndex = indice;
        return true;
    }
    private void CrearDetalle()
    {
        var panel = new Panel { Height=300, Dock=DockStyle.Top };
        var selector = new FlowLayoutPanel { Dock=DockStyle.Top, Height=85, AutoScroll=true };
        articulo.DropDownStyle=ComboBoxStyle.DropDown;
        articulo.AutoCompleteMode=AutoCompleteMode.SuggestAppend;
        articulo.AutoCompleteSource=AutoCompleteSource.ListItems;
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
            if (!ElegirEscrito(articulo) || articulo.SelectedItem is not OpcionRegistro opcion) { estado.Text="Seleccione un artículo de la lista."; return; }
            if(detalle.Rows.Count>=200) { estado.Text="Máximo 200 líneas por factura."; return; }
            int unidades=(int)cantidad.Value;
            int indice=detalle.Rows.Add(tipoLinea.Text,opcion.Nombre,unidades,opcion.Precio,opcion.Precio*unidades);
            detalle.Rows[indice].Tag=new LineaRegistro(tipoLinea.Text,opcion.Id,unidades,opcion.Precio);
            ActualizarTotal();
        };
        quitar.Click += (_,_) => { if(detalle.CurrentRow!=null) detalle.Rows.Remove(detalle.CurrentRow); ActualizarTotal(); };
    }
    private void MostrarCampo(string nombre, bool visible)
    {
        controles[nombre].Visible = visible;
        etiquetas[nombre].Visible = visible;
    }
    private void MostrarVoucher(bool visible)
    {
        MostrarCampo("autorizacion", visible);
        MostrarCampo("referencia", visible);
    }
    // Los datos del POS solo se piden con un método de tarjeta; la referencia sirve también para transferencias.
    private void MostrarCamposTarjeta()
    {
        bool tarjeta = ConexionBD.EsMetodoTarjeta((((ComboBox)controles["metodo"]).SelectedItem as OpcionRegistro)?.Nombre);
        MostrarCampo("resultado", tarjeta);
        MostrarCampo("autorizacion", tarjeta && controles["resultado"].Text == "Aprobada");
        MostrarCampo("referencia", !tarjeta || controles["resultado"].Text != "Rechazada");
        etiquetas["referencia"].Text = tarjeta ? "Referencia del voucher" : "Referencia";
    }
    private void MontoPendiente()
    {
        if (((ComboBox)controles["factura"]).SelectedItem is OpcionRegistro factura && controles["monto"] is NumericUpDown monto)
            monto.Value = Math.Clamp(factura.Precio, monto.Minimum, monto.Maximum);
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
            if(facturaInicial is int inicial && controles.TryGetValue("factura",out var control))
            {
                var combo=(ComboBox)control;
                combo.SelectedItem=combo.Items.OfType<OpcionRegistro>().FirstOrDefault(o=>o.Id==inicial) ?? combo.SelectedItem;
                estado.Text=$"Factura {inicial} emitida. Elige el método de pago y la caja; el monto es el saldo pendiente.";
            }
            catalogosCargados=true;
            // Al editar, Guardar se habilita hasta que los datos del registro estén cargados.
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
                    if(combo.Items.Count==0) throw new ArgumentException("Faltan opciones de " + (catalogos.GetValueOrDefault(campo.Key) ?? campo.Key) + ". Regístralas primero en su módulo.");
                    if(catalogos.ContainsKey(campo.Key) && !ElegirEscrito(combo)) throw new ArgumentException("Seleccione " + catalogos[campo.Key].ToLowerInvariant() + " de la lista.");
                    datos[campo.Key]=combo.SelectedItem is OpcionRegistro opcion ? opcion.Id : combo.Text;
                }
                else if(campo.Value is NumericUpDown numero) datos[campo.Key]=numero.Value;
                else datos[campo.Key]=campo.Value.Text;
            }
            // Los datos en pantalla deben ser del registro elegido (por ejemplo, si se acaba de escribir otro nombre).
            if(CatalogoEdicion!=null && (((ComboBox)controles[CatalogoEdicion]).SelectedItem as OpcionRegistro)?.Id != edicionCargada)
            {
                if(cargasPendientes==0) _ = CargarEdicionAsync();
                estado.ForeColor=Color.DimGray;
                estado.Text="Se están cargando los datos del registro elegido. Revísalos y pulsa Guardar.";
                return;
            }
            if(controles.ContainsKey("confirmar") && controles["clave"].Text != controles["confirmar"].Text) throw new ArgumentException("Las contraseñas no coinciden.");
            if(Desactivaciones.TryGetValue(tipo,out var desactivado) &&
               MessageBox.Show(this, tipo.StartsWith("Anular ")
                   ? $"¿Anular «{controles[desactivado.Campo].Text}»? Esta acción no se puede deshacer."
                   : $"¿Desactivar «{controles[desactivado.Campo].Text}»? Podrá reactivarlo desde Auditoría › Registros eliminados.",
                   "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)!=DialogResult.Yes) return;
            // CU-20 paso 5: el cierre se confirma antes de guardarlo porque no se puede repetir el mismo día.
            if(tipo=="Cierre" &&
               MessageBox.Show(this, $"¿Cerrar «{controles["caja"].Text}» con Q {((NumericUpDown)controles["contado"]).Value:N2} contados y Q {((NumericUpDown)controles["fondo"]).Value:N2} de fondo? No se podrá registrar otro cierre ni movimientos en esta caja hasta mañana.",
                   "Confirmar cierre", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)!=DialogResult.Yes) return;
            guardando=true; guardar.Enabled=false;
            estado.ForeColor=Color.DimGray; estado.Text="Guardando…";
            int? devuelto = await ConexionBD.GuardarRegistroAsync(sesion,tipo,datos,detalle.Rows.Cast<DataGridViewRow>().Select(f=>(LineaRegistro)f.Tag!).ToArray());
            if(tipo=="Cierre") CierreRegistrado=devuelto; else FacturaEmitida=devuelto;
            if(tipo=="Pago") ResultadoPos = controles["resultado"].Visible ? controles["resultado"].Text : null;
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
        finally { guardando=false; if(!IsDisposed && cargasPendientes==0) guardar.Enabled=true; }
    }
}
