using System.Globalization;

namespace Giovanny_Barbers;

public partial class Home : Form
{
    private readonly string usuario = "";
    private readonly bool administrador;
    private readonly SesionUsuario? sesion;
    private readonly CancellationTokenSource cancelacion = new();
    private readonly Panel contenido = new() { Dock = DockStyle.Fill, Padding = new Padding(28), AutoScroll = true };
    private readonly Dictionary<string, Button> botones = new();
    private readonly Dictionary<string, UserControl> vistas = new();
    private readonly Color oscuro = Color.FromArgb(31, 37, 41);
    private readonly Color dorado = Color.FromArgb(171, 140, 57);

    // Constructor exclusivo del diseñador; no construye el menú de la aplicación.
    public Home() { InitializeComponent(); }

    public Home(SesionUsuario sesion)
    {
        this.sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        InitializeComponent();
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        usuario = sesion.NombreUsuario;
        administrador = sesion.EsAdministrador;
        Controls.Remove(label1);
        label1.Dispose();
        Text = "Giovanny Barbers · Gestión de barbería";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 700);
        Size = new Size(1280, 820);
        BackColor = Color.FromArgb(246, 247, 249);
        Font = new Font("Segoe UI", 10);

        var estructura = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235));
        estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(estructura);
        var lateral = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = oscuro, RowCount = 3, ColumnCount = 1, Padding = new Padding(16), Margin = Padding.Empty };
        lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 155));
        lateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        estructura.Controls.Add(lateral, 0, 0);
        estructura.Controls.Add(contenido, 1, 0);
        lateral.Controls.Add(new Label { Text = "GIOVANNY\nBARBERS\nGestión de barbería", Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold) }, 0, 0);
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        lateral.Controls.Add(menu, 0, 1);
        string[] modulos = ["Inicio", "Trabajadores", "Clientes", "Inventario", "Facturación", "Contabilidad", "Mi jornada"];
        foreach (string modulo in modulos.Where(sesion.PuedeAbrir))
        {
            var boton = new Button { Text = modulo, Width = 190, Height = 48, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = oscuro, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 8) };
            boton.FlatAppearance.BorderSize = 0;
            boton.Click += (_, _) => MostrarModulo(modulo);
            botones.Add(modulo, boton);
            menu.Controls.Add(boton);
        }
        var panelSesion = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        panelSesion.Controls.Add(new Label { Text = usuario + "\n" + this.sesion.Rol, ForeColor = Color.White, Width = 190, Height = 54, AutoEllipsis = true });
        var salir = new Button { Text = "Cerrar sesión", Width = 190, Height = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
        salir.Click += (_, _) => Close();
        panelSesion.Controls.Add(salir);
        lateral.Controls.Add(panelSesion, 0, 2);
        MostrarModulo("Inicio");
        FormClosed += (_, _) => { cancelacion.Cancel(); foreach (var vista in vistas.Values) vista.Dispose(); };
    }

    private void MostrarModulo(string nombre)
    {
        // Validar siempre: otro código podría intentar abrir el módulo sin usar el menú.
        if (sesion == null || !sesion.PuedeAbrir(nombre) || !botones.ContainsKey(nombre))
        {
            MessageBox.Show("No tiene permiso para abrir este módulo.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!vistas.TryGetValue(nombre, out var vista))
        {
            vista = nombre == "Inicio" ? CrearInicio() : new ModuloView(nombre, sesion);
            vista.Dock = DockStyle.Fill;
            vistas.Add(nombre, vista);
        }
        contenido.SuspendLayout();
        contenido.Controls.Clear();
        contenido.Controls.Add(vista);
        foreach (var par in botones) par.Value.BackColor = par.Key == nombre ? dorado : oscuro;
        contenido.ResumeLayout();
        if (vista is ModuloView moduloVista) _ = moduloVista.ActualizarAsync();
    }

    private UserControl CrearInicio()
    {
        var vista = new UserControl { AutoScroll = true, BackColor = BackColor, AutoScaleMode = AutoScaleMode.None, Font = Font };
        var filas = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(0, 0, 12, 20) };
        filas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        vista.Controls.Add(filas);
        filas.Controls.Add(new Label { Text = "Panel de inicio", Font = new Font("Segoe UI", 27, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 8) });
        filas.Controls.Add(new Label { Text = "Bienvenido, " + usuario + " · " + DateTime.Today.ToString("D", CultureInfo.GetCultureInfo("es-GT")), AutoSize = true, Margin = new Padding(0, 0, 0, 22) });
        var estado = new Label { Text = "Resumen de la jornada", AutoSize = true, MaximumSize = new Size(800, 0), ForeColor = Color.DimGray, Margin = new Padding(0, 0, 0, 20) };
        filas.Controls.Add(estado);
        var actualizar = new Button { Text = "Actualizar resumen", AutoSize = true, Height = 38, Margin = new Padding(0, 0, 0, 16) };
        filas.Controls.Add(actualizar);
        var tarjetas = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        bool cargando = false;
        async Task ActualizarResumenAsync()
        {
            if (sesion == null || cargando || vista.IsDisposed) return;
            cargando = true;
            actualizar.Enabled = false;
            estado.Text = "Cargando resumen…";
            estado.ForeColor = Color.DimGray;
            try
            {
                var resumen = await ConexionBD.ObtenerResumenAsync(sesion, cancelacion.Token);
                if (vista.IsDisposed) return;
                foreach (Control tarjetaAnterior in tarjetas.Controls.Cast<Control>().ToArray()) tarjetaAnterior.Dispose();
                tarjetas.Controls.Clear();
                foreach (var indicador in resumen)
                {
                    var tarjeta = new Panel { Width = 210, Height = 130, BackColor = Color.White, Margin = new Padding(0, 0, 14, 14), Padding = new Padding(16) };
                    tarjeta.Controls.Add(new Label { Text = indicador.Value, Font = new Font("Segoe UI", 20, FontStyle.Bold), Dock = DockStyle.Bottom, Height = 48, AutoEllipsis = true });
                    tarjeta.Controls.Add(new Label { Text = indicador.Key, Dock = DockStyle.Top, Height = 52 });
                    tarjetas.Controls.Add(tarjeta);
                }
                estado.Text = resumen.Count == 0 ? "Selecciona un módulo autorizado. Si no aparece ninguno, solicita permisos al administrador." : "Datos de hoy · Consulta el historial completo desde cada módulo.";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!vista.IsDisposed)
                {
                    estado.ForeColor = Color.Firebrick;
                    estado.Text = "No se pudo cargar el resumen: " + ex.Message;
                }
            }
            finally
            {
                cargando = false;
                if (!vista.IsDisposed) actualizar.Enabled = true;
            }
        }
        vista.Load += async (_, _) => await ActualizarResumenAsync();
        actualizar.Click += async (_, _) => await ActualizarResumenAsync();
        filas.Controls.Add(tarjetas);
        filas.Controls.Add(new Label { Text = "Accesos a los módulos", AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), Margin = new Padding(0, 20, 0, 16) });
        var accesos = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        foreach (string nombre in botones.Keys.Where(n => n != "Inicio"))
        {
            var boton = new Button { Text = nombre, Size = new Size(210, 65), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 14, 14) };
            boton.FlatAppearance.BorderColor = dorado;
            boton.Click += (_, _) => MostrarModulo(nombre);
            accesos.Controls.Add(boton);
        }
        filas.Controls.Add(accesos);
        return vista;
    }
}
