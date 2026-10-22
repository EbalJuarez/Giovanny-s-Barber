using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.Runtime.InteropServices;

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
    private static readonly Lazy<Bitmap?> logo = new(CargarLogo);

    // Logo del encabezado (Logo .jpeg incrustado en el programa); null si no se pudo cargar.
    public static Image? Logo => logo.Value;

    // Recorta el margen blanco de la imagen y la deja en blanco y negro puro para que la térmica la imprima nítida.
    private static Bitmap? CargarLogo()
    {
        using var flujo = typeof(ComprobanteTermico).Assembly.GetManifestResourceStream("Logo.jpeg");
        if (flujo == null) return null;
        using var original = new Bitmap(flujo);
        int w = original.Width, h = original.Height;
        var pixeles = new int[w * h];
        var datos = original.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(datos.Scan0, pixeles, 0, pixeles.Length);
        original.UnlockBits(datos);
        static bool Oscuro(int p) => ((p >> 16) & 255) + ((p >> 8) & 255) + (p & 255) < 384;
        int izquierda = w, derecha = -1, arriba = h, abajo = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (Oscuro(pixeles[y * w + x]))
                {
                    izquierda = Math.Min(izquierda, x); derecha = Math.Max(derecha, x);
                    arriba = Math.Min(arriba, y); abajo = Math.Max(abajo, y);
                }
        if (derecha < 0) return null;
        int ancho = derecha - izquierda + 1, alto = abajo - arriba + 1;
        var recorte = new int[ancho * alto];
        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
                recorte[y * ancho + x] = Oscuro(pixeles[(y + arriba) * w + x + izquierda]) ? Color.Black.ToArgb() : Color.White.ToArgb();
        var resultado = new Bitmap(ancho, alto, PixelFormat.Format32bppArgb);
        datos = resultado.LockBits(new Rectangle(0, 0, ancho, alto), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(recorte, 0, datos.Scan0, recorte.Length);
        resultado.UnlockBits(datos);
        return resultado;
    }

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

        // El nombre solo se imprime como texto si falta el logo, que ya lo incluye.
        if (Logo == null) Centrado((config["Comercio"] is { Length: > 0 } comercio ? comercio : "Geovanny's Barber Shop").ToUpper(Cultura));
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
        using var documento = new PrintDocument { DocumentName = "Comprobante Geovanny's Barber Shop" };
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
        // El driver de la SAT Q15 U imprime 48 mm de ancho (189): se pide un papel de ese ancho y del largo del
        // comprobante, porque sus papeles fijos (297 mm o más) avanzan el resto del papel en blanco.
        int ancho = documento.PrinterSettings.PaperSizes.Cast<PaperSize>()
            .Select(p => p.Width)
            .Where(w => w <= 228)
            .DefaultIfEmpty(228)
            .Max();
        // El logo ocupa todo el ancho del papel, seguido de media línea de separación.
        var imagen = Logo;
        float altoLogo = imagen == null ? 0 : (float)ancho * imagen.Height / imagen.Width + altoLinea / 2;
        // Margen extra (unos 2.5 cm): con el alto justo, la impresora cortaba las últimas líneas.
        int alto = (int)Math.Ceiling(altoLogo + lineas.Count * altoLinea) + 100;
        documento.DefaultPageSettings.PaperSize = new PaperSize("Ticket", ancho, alto);
        documento.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        int siguiente = 0;
        bool primeraPagina = true;
        documento.PrintPage += (_, e) =>
        {
            var g = e.Graphics!;
            float y = 0;
            if (primeraPagina && imagen != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(imagen, 0, 0, ancho, altoLogo - altoLinea / 2);
                y = altoLogo;
            }
            primeraPagina = false;
            // Si la página resulta más corta que el comprobante, las líneas restantes siguen en otra página en vez de perderse.
            for (; siguiente < lineas.Count && (y == 0 || y + altoLinea <= e.PageSettings.PrintableArea.Height); siguiente++)
            {
                g.DrawString(lineas[siguiente], fuente, Brushes.Black, 0, y, StringFormat.GenericTypographic);
                y += altoLinea;
            }
            e.HasMorePages = siguiente < lineas.Count;
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
        // El logo se muestra encima del texto, como sale en el papel.
        var contenido = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        contenido.Controls.Add(vista);
        if (ComprobanteTermico.Logo is { } logo)
            contenido.Controls.Add(new PictureBox { Image = logo, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Top, Height = 110, Padding = new Padding(8) });
        vista.BringToFront();
        estructura.Controls.Add(contenido,0,0);
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
            // Una factura vigente se imprime hasta que tiene un pago registrado.
            if (comprobante.Pagado == 0 && !string.Equals(comprobante.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
            {
                estado.Text = "Registra el pago de la factura en la pestaña Pagos para poder imprimir.";
                return;
            }
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
