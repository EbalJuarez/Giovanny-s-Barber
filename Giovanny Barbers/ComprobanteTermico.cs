using System.Drawing.Printing;
using System.Globalization;

namespace Giovanny_Barbers;

// RF9 y CU-16: comprobante para la impresora térmica de 58 mm (SAT Q15 U) por medio de su driver de Windows.
// Es un comprobante interno de la venta, no una factura fiscal (FEL).
public static class ComprobanteTermico
{
    // El papel de 58 mm deja unos 48 mm imprimibles: 32 caracteres por línea con fuente monoespaciada.
    private const int Columnas = 32;
    private const string Fuente = "Consolas";
    private const float TamanoFuente = 7.5f;
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-GT");

    public static List<string> Lineas(Comprobante c, bool reimpresion)
    {
        var config = ConexionBD.Configuracion("Comprobante");
        var lineas = new List<string>();
        void Centrado(string? texto) { foreach (string l in Ajustar(texto, Columnas)) lineas.Add(l.PadLeft((Columnas + l.Length) / 2)); }
        void Separador() => lineas.Add(new string('-', Columnas));
        // Texto a la izquierda e importe alineado a la derecha; si no caben juntos, el importe baja a su propia línea.
        void Par(string izquierda, string derecha)
        {
            if (izquierda.Length + derecha.Length < Columnas) { lineas.Add(izquierda.PadRight(Columnas - derecha.Length) + derecha); return; }
            lineas.AddRange(Ajustar(izquierda, Columnas));
            lineas.Add(derecha.PadLeft(Columnas));
        }
        void Campo(string etiqueta, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;
            const int Sangria = 9;
            var partes = Ajustar(valor, Columnas - Sangria);
            for (int i = 0; i < partes.Count; i++) lineas.Add((i == 0 ? etiqueta : "").PadRight(Sangria) + partes[i]);
        }
        string Moneda(decimal monto) => "Q " + monto.ToString("N2", Cultura);

        Centrado((config["Comercio"] is { Length: > 0 } comercio ? comercio : "Giovannys Barber").ToUpper(Cultura));
        // PENDIENTE: completar la dirección y el teléfono del comercio en appsettings.json (sección "Comprobante").
        // Mientras estén vacíos, esas líneas no se imprimen.
        Centrado(config["Direccion"]);
        Centrado(config["Telefono"] is { Length: > 0 } telefono ? "Tel. " + telefono : null);
        Separador();
        if (string.Equals(c.Estado, "Anulada", StringComparison.OrdinalIgnoreCase)) Centrado("*** FACTURA ANULADA ***");
        Campo("No.:", c.Numero.ToString("D6", Cultura));
        Campo("Fecha:", c.Fecha.ToString("dd/MM/yyyy HH:mm", Cultura));
        Campo("Atendió:", c.Trabajador);
        Campo("Cliente:", c.Cliente);
        Campo("NIT:", c.Nit);
        Separador();
        foreach (var linea in c.Lineas)
        {
            lineas.AddRange(Ajustar(linea.Descripcion, Columnas));
            Par($"  {linea.Cantidad} x {linea.Precio.ToString("N2", Cultura)}", linea.Subtotal.ToString("N2", Cultura));
        }
        Separador();
        Par("TOTAL", Moneda(c.Total));
        if (c.Pagado > 0) Par(c.Metodos is { Length: > 0 } metodos ? $"Pagado ({metodos})" : "Pagado", Moneda(c.Pagado));
        if (c.Total > c.Pagado && !string.Equals(c.Estado, "Anulada", StringComparison.OrdinalIgnoreCase)) Par("Saldo pendiente", Moneda(c.Total - c.Pagado));
        Separador();
        Centrado(config["Mensaje"] is { Length: > 0 } mensaje ? mensaje : "¡Gracias por su visita!");
        Centrado(config["Leyenda"] is { Length: > 0 } leyenda ? leyenda : "Comprobante interno, no válido como factura fiscal");
        if (reimpresion) Centrado("Reimpresión " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura));
        // Espacio final para cortar el papel sin perder la última línea.
        lineas.AddRange(["", "", ""]);
        return lineas;
    }

    private static List<string> Ajustar(string? texto, int ancho)
    {
        var resultado = new List<string>();
        if (string.IsNullOrWhiteSpace(texto)) return resultado;
        string actual = "";
        foreach (string palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string resto = palabra;
            // Una palabra más larga que la línea se corta en trozos.
            while (resto.Length > ancho)
            {
                if (actual.Length > 0) { resultado.Add(actual); actual = ""; }
                resultado.Add(resto[..ancho]);
                resto = resto[ancho..];
            }
            if (actual.Length == 0) actual = resto;
            else if (actual.Length + 1 + resto.Length <= ancho) actual += " " + resto;
            else { resultado.Add(actual); actual = resto; }
        }
        if (actual.Length > 0) resultado.Add(actual);
        return resultado;
    }

    // Usa la impresora de appsettings.json; si no está configurada o no se encuentra, permite elegirla.
    public static bool Imprimir(IReadOnlyList<string> lineas, IWin32Window ventana)
    {
        using var documento = new PrintDocument { DocumentName = "Comprobante Giovannys Barber" };
        string? impresora = ConexionBD.Configuracion("Comprobante")["Impresora"];
        if (!string.IsNullOrWhiteSpace(impresora) && PrinterSettings.InstalledPrinters.Cast<string>().Contains(impresora))
            documento.PrinterSettings.PrinterName = impresora;
        else
        {
            using var dialogo = new PrintDialog { Document = documento, UseEXDialog = true, AllowSomePages = false };
            if (dialogo.ShowDialog(ventana) != DialogResult.OK) return false;
        }
        if (!documento.PrinterSettings.IsValid) throw new InvalidOperationException("La impresora seleccionada no está disponible.");
        using var fuente = new Font(Fuente, TamanoFuente);
        // Con la unidad de página predeterminada (centésimas de pulgada), 58 mm equivalen a 228.
        float altoLinea = fuente.GetHeight(100f);
        documento.DefaultPageSettings.PaperSize = new PaperSize("Ticket 58 mm", 228, (int)Math.Ceiling(lineas.Count * altoLinea) + 20);
        documento.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        documento.PrintPage += (_, e) =>
        {
            float y = 0;
            foreach (string linea in lineas)
            {
                e.Graphics!.DrawString(linea, fuente, Brushes.Black, 0, y, StringFormat.GenericTypographic);
                y += altoLinea;
            }
            e.HasMorePages = false;
        };
        documento.Print();
        return true;
    }
}

// Vista previa del comprobante con opción de imprimir; sirve para la emisión y para la reimpresión.
public sealed class ComprobanteForm : Form
{
    private readonly SesionUsuario sesion;
    private readonly int factura;
    private readonly bool reimpresion;
    private readonly TextBox vista = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 10), BackColor = Color.White, WordWrap = false };
    private readonly Button imprimir = new() { Text = "Imprimir", Width = 130, Height = 40, BackColor = Color.FromArgb(171,140,57), FlatStyle = FlatStyle.Flat, Enabled = false };
    private readonly Label estado = new() { Dock = DockStyle.Fill, ForeColor = Color.DimGray, Padding = new Padding(8) };
    private List<string> lineas = new();
    // Motivo por el que la sesión dejó de ser válida; quien abrió la ventana cierra la sesión.
    public string? SesionFinalizada { get; private set; }

    public ComprobanteForm(SesionUsuario sesion, int factura, bool reimpresion)
    {
        this.sesion = sesion;
        this.factura = factura;
        this.reimpresion = reimpresion;
        Text = "Comprobante de la factura " + factura;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(246,247,249);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96,96);
        ClientSize = new Size(420, 640);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        var estructura = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(14) };
        estructura.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        estructura.RowStyles.Add(new RowStyle(SizeType.Absolute,95));
        Controls.Add(estructura);
        estructura.Controls.Add(vista,0,0);
        var pie = new Panel { Dock = DockStyle.Fill };
        var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.RightToLeft };
        var cerrar = new Button { Text = "Cerrar", Width = 120, Height = 40, DialogResult = DialogResult.Cancel };
        botones.Controls.Add(imprimir);
        botones.Controls.Add(cerrar);
        pie.Controls.Add(estado);
        pie.Controls.Add(botones);
        estructura.Controls.Add(pie,0,1);
        CancelButton = cerrar;
        estado.Text = "Cargando comprobante…";
        Shown += async (_,_) => await CargarAsync();
        imprimir.Click += (_,_) => Imprimir();
    }

    private async Task CargarAsync()
    {
        try
        {
            var comprobante = await ConexionBD.ObtenerComprobanteAsync(sesion, factura);
            if (IsDisposed) return;
            lineas = ComprobanteTermico.Lineas(comprobante, reimpresion);
            vista.Lines = lineas.ToArray();
            estado.Text = reimpresion ? "Reimpresión del comprobante." : "Revisa el comprobante antes de imprimir.";
            imprimir.Enabled = true;
            imprimir.Focus();
        }
        catch (SesionInvalidaException ex) { if (!IsDisposed) { SesionFinalizada = ex.Message; DialogResult = DialogResult.Abort; } }
        catch (Exception ex) { if (!IsDisposed) { estado.ForeColor = Color.Firebrick; estado.Text = "No se pudo cargar el comprobante: " + ex.Message; } }
    }

    private void Imprimir()
    {
        try
        {
            imprimir.Enabled = false;
            if (ComprobanteTermico.Imprimir(lineas, this))
            {
                estado.ForeColor = Color.DimGray;
                estado.Text = "Comprobante enviado a la impresora.";
            }
        }
        catch (Exception ex)
        {
            estado.ForeColor = Color.Firebrick;
            estado.Text = "No se pudo imprimir: " + ex.Message;
        }
        finally { if (!IsDisposed) imprimir.Enabled = true; }
    }
}
