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
            var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            tab.Controls.Add(cuerpo);
            var cabecera = new Panel { Dock = DockStyle.Fill };
            var actualizar = new Button { Text = "Actualizar", Dock = DockStyle.Right, Width = 115, Height = 36 };
            cabecera.Controls.Add(new Label { Text = Descripcion(seccion), Dock = DockStyle.Fill, ForeColor = Color.FromArgb(65, 70, 75) });
            cabecera.Controls.Add(actualizar);
            foreach (string operacion in Operaciones(modulo, seccion).Where(tipo => ConexionBD.PuedeRegistrar(Sesion, tipo)))
            {
                var nuevo = new Button { Text = operacion == "Clave" ? "Restablecer clave" : "+ " + operacion, Dock = DockStyle.Right, Width = 145 };
                cabecera.Controls.Add(nuevo);
                nuevo.Click += async (_, _) =>
                {
                    using var ventana = new RegistroForm(Sesion, operacion);
                    if (ventana.ShowDialog(FindForm()) == DialogResult.OK) await ActualizarAsync();
                };
            }
            cuerpo.Controls.Add(cabecera, 0, 0);
            var tabla = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false };
            tabla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 237, 240);
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 37, 41);
            tabla.ColumnHeadersHeight = 42;
            cuerpo.Controls.Add(tabla, 0, 1);
            var estado = new Label { Text = "Seleccione la pestaña para consultar sus datos.", Dock = DockStyle.Fill, ForeColor = Color.DimGray, Padding = new Padding(0, 12, 0, 0) };
            cuerpo.Controls.Add(estado, 0, 2);
            paginas.Add(seccion, (tabla, estado, actualizar));
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
            pagina.Estado.Text = datos.Rows.Count == 0 ? "No hay registros para esta consulta." : $"{datos.Rows.Count} registros · Actualizado a las {DateTime.Now:HH:mm}";
        }
        catch (OperationCanceledException) { }
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

    private static string[] Secciones(string modulo) => modulo switch
    {
        "Trabajadores" => ["Directorio", "Asistencia", "Actividades", "Préstamos", "Usuarios"],
        "Mi jornada" => ["Mi asistencia", "Mis servicios", "Mis ventas", "Mis préstamos"],
        "Clientes" => ["Clientes", "Historial de servicios"],
        "Inventario" => ["Productos", "Movimientos", "Categorías"],
        "Facturación" => ["Facturas", "Detalle de factura", "Servicios", "Pagos"],
        "Contabilidad" => ["Ingresos y egresos", "Cuadre de caja", "Cierres diarios", "Cajas"],
        _ => []
    };

    private static string[] Operaciones(string modulo, string seccion) => (modulo, seccion) switch
    {
        ("Trabajadores", "Directorio") => ["Empleado"],
        ("Trabajadores", "Usuarios") => ["Clave"],
        ("Trabajadores", "Asistencia") or ("Mi jornada", "Mi asistencia") => ["Asistencia"],
        ("Trabajadores", "Préstamos") or ("Mi jornada", "Mis préstamos") => ["Préstamo"],
        ("Clientes", "Clientes") => ["Cliente"],
        ("Inventario", "Productos") => ["Producto"],
        ("Inventario", "Movimientos") => ["Movimiento"],
        ("Inventario", "Categorías") => ["Categoría"],
        ("Facturación", "Servicios") => ["Servicio"],
        ("Facturación", "Pagos") => ["Pago", "Método de pago"],
        ("Contabilidad", "Ingresos y egresos") => ["Transacción"],
        ("Contabilidad", "Cajas") => ["Caja"],
        ("Contabilidad", "Cuadre de caja") or ("Contabilidad", "Cierres diarios") => ["Cierre"],
        ("Facturación", "Facturas") or ("Facturación", "Detalle de factura") or ("Trabajadores", "Actividades") or ("Clientes", "Historial de servicios") or ("Mi jornada", "Mis servicios") or ("Mi jornada", "Mis ventas") => ["Factura"],
        _ => []
    };

    private static string Descripcion(string seccion) => seccion switch
    {
        "Historial de servicios" => "Corte de pelo, arreglo de barba, pelo y barba, instalación de prótesis y mantenimiento.",
        "Préstamos" => "Solicitudes de trabajadores para revisión y gestión del administrador.",
        "Mis préstamos" => "Consulta tus solicitudes y su estado de aprobación.",
        "Cuadre de caja" => "Compara el saldo registrado con el efectivo contado al finalizar la jornada.",
        "Detalle de factura" => "Cliente, trabajador, servicios, productos, cantidades y precios del comprobante.",
        "Movimientos" => "Entradas, salidas y ajustes del inventario.",
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
        Text = tipo == "Clave" ? "Restablecer contraseña" : "Registrar " + tipo.ToLowerInvariant();
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
                Texto("usuario","Usuario",50); Texto("clave","Contraseña nueva",72,true); Texto("confirmar","Confirmar contraseña",72,true);
                Lista("rol","Rol","Trabajador","Cajera","Cajero","Administrador");
                break;
            case "Clave": Catalogo("usuario","Usuario"); Texto("clave","Contraseña nueva",72,true); Texto("confirmar","Confirmar contraseña",72,true); break;
            case "Cliente": Texto("nombre","Nombre",100); Texto("telefono","Teléfono",20); Texto("correo","Correo",100); break;
            case "Categoría": Texto("nombre","Nombre",80); Texto("descripcion","Descripción",250); break;
            case "Producto": Catalogo("categoria","Categoría"); Texto("nombre","Nombre",100); Numero("precio","Precio de venta",true); Numero("stock","Existencia inicial"); Numero("minimo","Stock mínimo"); break;
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
            "Cierre" => "Se guardará el cuadre con el saldo registrado y la diferencia encontrada.",
            _ => "Completa los campos y pulsa Guardar."
        };
        guardar.Enabled = false;
        Shown += async (_,_) => await CargarCatalogosAsync();
        guardar.Click += async (_,_) => await GuardarAsync();
        FormClosing += (_,e) => { if (guardando) e.Cancel=true; };
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
            if(!IsDisposed) guardar.Enabled=true;
        }
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
            await ConexionBD.GuardarRegistroAsync(sesion,tipo,datos,detalle.Rows.Cast<DataGridViewRow>().Select(f=>(LineaRegistro)f.Tag!).ToArray());
            guardando=false;
            DialogResult=DialogResult.OK;
            Close();
        }
        catch(Exception ex)
        {
            estado.ForeColor=Color.Firebrick;
            estado.Text=ex.Message;
        }
        finally { guardando=false; if(!IsDisposed) guardar.Enabled=true; }
    }
}
