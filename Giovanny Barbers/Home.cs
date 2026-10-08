using System.Globalization;

namespace Giovanny_Barbers;

public partial class Home : Form
{
    private readonly SesionUsuario? sesion;
    private readonly CancellationTokenSource cancelacion = new();
    private readonly Dictionary<string, Button> botones = new();
    private readonly Dictionary<Button, string> destinos = new();
    private readonly Dictionary<Button, Color> coloresOriginales = new();
    private readonly Dictionary<string, ModuloView> vistas = new();
    private bool cargandoResumen;

    // El diseñador usa este constructor: muestra los controles sin consultar SQL.
    public Home() { InitializeComponent(); }

    public Home(SesionUsuario sesion) : this()
    {
        this.sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        RegistrarDestino("Inicio", buttonInicio);
        RegistrarDestino("Trabajadores", buttonTrabajadores, accesoTrabajadores);
        RegistrarDestino("Clientes", buttonClientes, accesoClientes);
        RegistrarDestino("Inventario", buttonInventario, accesoInventario);
        RegistrarDestino("Facturación", buttonFacturacion, accesoFacturacion);
        RegistrarDestino("Contabilidad", buttonContabilidad, accesoContabilidad);
        RegistrarDestino("Mi jornada", buttonMiJornada, accesoMiJornada);
        labelUsuario.Text = sesion.NombreUsuario;
        labelRol.Text = sesion.Rol;
        labelBienvenida.Text = "Bienvenido, " + sesion.NombreUsuario + " · " +
            DateTime.Today.ToString("D", CultureInfo.GetCultureInfo("es-GT"));
        foreach (var destino in destinos)
        {
            destino.Key.Visible = sesion.PuedeAbrir(destino.Value);
            destino.Key.Enabled = sesion.PuedeAbrir(destino.Value);
        }
        foreach (var tarjeta in Tarjetas()) tarjeta.Panel.Visible = false;
        MostrarModulo("Inicio");
    }

    private void RegistrarDestino(string modulo, Button menu, Button? acceso = null)
    {
        botones.Add(modulo, menu);
        destinos.Add(menu, modulo);
        coloresOriginales.Add(menu, menu.BackColor);
        if (acceso != null) destinos.Add(acceso, modulo);
    }

    private void Navegacion_Click(object? sender, EventArgs e)
    {
        // El texto del botón se puede editar sin cambiar el permiso asociado.
        if (sender is Button boton && destinos.TryGetValue(boton, out string? modulo))
            MostrarModulo(modulo);
    }

    private void MostrarModulo(string nombre)
    {
        if (sesion == null || !sesion.PuedeAbrir(nombre) || !botones.ContainsKey(nombre))
        {
            MessageBox.Show("No tiene permiso para abrir este módulo.", "Acceso denegado",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        contenido.SuspendLayout();
        try
        {
            panelInicio.Visible = nombre == "Inicio";
            foreach (var anterior in vistas.Values) anterior.Visible = false;
            if (nombre == "Inicio") panelInicio.BringToFront();
            else
            {
                if (!vistas.TryGetValue(nombre, out var vista))
                {
                    vista = new ModuloView(nombre, sesion) { Dock = DockStyle.Fill };
                    vistas.Add(nombre, vista);
                    contenido.Controls.Add(vista);
                }
                vista.Visible = true;
                vista.BringToFront();
                _ = vista.ActualizarAsync();
            }
            foreach (var par in botones)
                par.Value.BackColor = par.Key == nombre ? Color.FromArgb(171, 140, 57) : coloresOriginales[par.Value];
        }
        finally { contenido.ResumeLayout(true); }
    }

    private (Panel Panel, Label Titulo, Label Valor)[] Tarjetas() =>
    [
        (tarjeta1, titulo1, valor1),
        (tarjeta2, titulo2, valor2),
        (tarjeta3, titulo3, valor3),
        (tarjeta4, titulo4, valor4),
        (tarjeta5, titulo5, valor5),
        (tarjeta6, titulo6, valor6),
        (tarjeta7, titulo7, valor7),
        (tarjeta8, titulo8, valor8)
    ];

    private async Task ActualizarResumenAsync()
    {
        if (sesion == null || cargandoResumen || IsDisposed) return;
        cargandoResumen = true;
        buttonActualizar.Enabled = false;
        labelEstadoResumen.Text = "Cargando resumen…";
        labelEstadoResumen.ForeColor = Color.DimGray;
        foreach (var tarjeta in Tarjetas()) tarjeta.Panel.Visible = false;
        try
        {
            var resumen = await ConexionBD.ObtenerResumenAsync(sesion, cancelacion.Token);
            if (IsDisposed || Disposing) return;
            var tarjetas = Tarjetas();
            int indice = 0;
            foreach (var indicador in resumen)
            {
                if (indice == tarjetas.Length) break;
                var tarjeta = tarjetas[indice++];
                tarjeta.Titulo.Text = indicador.Key;
                tarjeta.Valor.Text = indicador.Value;
                tarjeta.Panel.Visible = true;
            }
            labelEstadoResumen.Text = resumen.Count == 0
                ? "Selecciona un módulo autorizado. Si no aparece ninguno, solicita permisos al administrador."
                : "Datos de hoy · Consulta el historial completo desde cada módulo.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
            {
                labelEstadoResumen.ForeColor = Color.Firebrick;
                labelEstadoResumen.Text = "No se pudo cargar el resumen: " + ex.Message;
            }
        }
        finally
        {
            cargandoResumen = false;
            if (!IsDisposed && !Disposing) buttonActualizar.Enabled = true;
        }
    }

    private async void Actualizar_Click(object? sender, EventArgs e) => await ActualizarResumenAsync();
    private void CerrarSesion_Click(object? sender, EventArgs e) => Close();

    private async void Home_Load(object sender, EventArgs e)
    {
        // Sin sesión, el diseñador puede abrir Home sin conectarse a la base de datos.
        if (sesion != null) await ActualizarResumenAsync();
    }
}
