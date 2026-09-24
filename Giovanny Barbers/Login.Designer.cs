namespace Giovanny_Barbers
{
    partial class Login
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            panelMarca = new Panel();
            labelSimbolo = new Label();
            labelMarca = new Label();
            labelMarcaSubtitulo = new Label();
            lineaDorada = new Panel();
            labelLema = new Label();
            panelAcceso = new Panel();
            labelAcceso = new Label();
            Titulo = new Label();
            labelInstrucciones = new Label();
            labelUsuario = new Label();
            maskedTextBoxUsuario = new MaskedTextBox();
            labelContrasena = new Label();
            maskedTextBoxContrasena = new MaskedTextBox();
            buttonInicion = new Button();
            labelAyuda = new Label();
            labelPie = new Label();
            panelMarca.SuspendLayout();
            panelAcceso.SuspendLayout();
            SuspendLayout();
            // panelMarca
            panelMarca.BackColor = Color.FromArgb(31, 37, 41);
            panelMarca.Controls.Add(labelSimbolo);
            panelMarca.Controls.Add(labelMarca);
            panelMarca.Controls.Add(labelMarcaSubtitulo);
            panelMarca.Controls.Add(lineaDorada);
            panelMarca.Controls.Add(labelLema);
            panelMarca.Dock = DockStyle.Left;
            panelMarca.Name = "panelMarca";
            panelMarca.Size = new Size(340, 560);
            panelMarca.TabIndex = 1;
            // labelSimbolo
            labelSimbolo.Text = "GB";
            labelSimbolo.Font = new Font("Segoe UI", 44F, FontStyle.Bold);
            labelSimbolo.ForeColor = Color.FromArgb(194, 162, 77);
            labelSimbolo.Location = new Point(36, 80);
            labelSimbolo.Size = new Size(265, 85);
            labelSimbolo.Name = "labelSimbolo";
            // labelMarca
            labelMarca.Text = "GIOVANNY\r\nBARBERS";
            labelMarca.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
            labelMarca.ForeColor = Color.White;
            labelMarca.Location = new Point(40, 190);
            labelMarca.Size = new Size(270, 105);
            labelMarca.Name = "labelMarca";
            // labelMarcaSubtitulo
            labelMarcaSubtitulo.Text = "Gestión de barbería";
            labelMarcaSubtitulo.Font = new Font("Segoe UI", 11F);
            labelMarcaSubtitulo.ForeColor = Color.FromArgb(199, 204, 207);
            labelMarcaSubtitulo.Location = new Point(44, 305);
            labelMarcaSubtitulo.Size = new Size(260, 30);
            labelMarcaSubtitulo.Name = "labelMarcaSubtitulo";
            // lineaDorada
            lineaDorada.BackColor = Color.FromArgb(171, 140, 57);
            lineaDorada.Location = new Point(44, 367);
            lineaDorada.Size = new Size(54, 3);
            lineaDorada.Name = "lineaDorada";
            // labelLema
            labelLema.Text = "Tu equipo. Tu negocio.\r\nTodo en un solo lugar.";
            labelLema.ForeColor = Color.FromArgb(199, 204, 207);
            labelLema.Font = new Font("Segoe UI", 11F);
            labelLema.Location = new Point(44, 402);
            labelLema.Size = new Size(265, 65);
            labelLema.Name = "labelLema";
            // panelAcceso
            panelAcceso.BackColor = Color.FromArgb(250, 250, 248);
            panelAcceso.Controls.Add(labelAcceso);
            panelAcceso.Controls.Add(Titulo);
            panelAcceso.Controls.Add(labelInstrucciones);
            panelAcceso.Controls.Add(labelUsuario);
            panelAcceso.Controls.Add(maskedTextBoxUsuario);
            panelAcceso.Controls.Add(labelContrasena);
            panelAcceso.Controls.Add(maskedTextBoxContrasena);
            panelAcceso.Controls.Add(buttonInicion);
            panelAcceso.Controls.Add(labelAyuda);
            panelAcceso.Controls.Add(labelPie);
            panelAcceso.Dock = DockStyle.Fill;
            panelAcceso.Name = "panelAcceso";
            panelAcceso.TabIndex = 0;
            // labelAcceso
            labelAcceso.Text = "ACCESO AL SISTEMA";
            labelAcceso.ForeColor = Color.FromArgb(128, 101, 34);
            labelAcceso.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            labelAcceso.Location = new Point(64, 49);
            labelAcceso.Size = new Size(400, 25);
            labelAcceso.Name = "labelAcceso";
            // Titulo
            Titulo.Text = "Bienvenido de nuevo";
            Titulo.ForeColor = Color.FromArgb(31, 37, 41);
            Titulo.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            Titulo.Location = new Point(59, 81);
            Titulo.Size = new Size(460, 52);
            Titulo.Name = "Titulo";
            // labelInstrucciones
            labelInstrucciones.Text = "Ingresa tus credenciales para continuar.";
            labelInstrucciones.ForeColor = Color.FromArgb(95, 103, 109);
            labelInstrucciones.Location = new Point(64, 143);
            labelInstrucciones.Size = new Size(440, 28);
            labelInstrucciones.Name = "labelInstrucciones";
            // labelUsuario
            labelUsuario.Text = "Usuario";
            labelUsuario.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            labelUsuario.Location = new Point(64, 199);
            labelUsuario.Size = new Size(430, 25);
            labelUsuario.Name = "labelUsuario";
            // maskedTextBoxUsuario
            maskedTextBoxUsuario.Font = new Font("Segoe UI", 14F);
            maskedTextBoxUsuario.Location = new Point(64, 229);
            maskedTextBoxUsuario.Size = new Size(430, 32);
            maskedTextBoxUsuario.BorderStyle = BorderStyle.FixedSingle;
            maskedTextBoxUsuario.Name = "maskedTextBoxUsuario";
            maskedTextBoxUsuario.AccessibleName = "Usuario";
            maskedTextBoxUsuario.TabIndex = 0;
            // labelContrasena
            labelContrasena.Text = "Contraseña";
            labelContrasena.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            labelContrasena.Location = new Point(64, 286);
            labelContrasena.Size = new Size(430, 25);
            labelContrasena.Name = "labelContrasena";
            // maskedTextBoxContrasena
            maskedTextBoxContrasena.Font = new Font("Segoe UI", 14F);
            maskedTextBoxContrasena.Location = new Point(64, 316);
            maskedTextBoxContrasena.Size = new Size(430, 32);
            maskedTextBoxContrasena.BorderStyle = BorderStyle.FixedSingle;
            maskedTextBoxContrasena.UseSystemPasswordChar = true;
            maskedTextBoxContrasena.Name = "maskedTextBoxContrasena";
            maskedTextBoxContrasena.AccessibleName = "Contraseña";
            maskedTextBoxContrasena.TabIndex = 1;
            // buttonInicion
            buttonInicion.Text = "Iniciar sesión";
            buttonInicion.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            buttonInicion.BackColor = Color.FromArgb(171, 140, 57);
            buttonInicion.ForeColor = Color.FromArgb(23, 27, 30);
            buttonInicion.FlatStyle = FlatStyle.Flat;
            buttonInicion.FlatAppearance.BorderSize = 0;
            buttonInicion.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 162, 77);
            buttonInicion.FlatAppearance.MouseDownBackColor = Color.FromArgb(157, 128, 49);
            buttonInicion.Cursor = Cursors.Hand;
            buttonInicion.Location = new Point(64, 384);
            buttonInicion.Size = new Size(430, 49);
            buttonInicion.Name = "buttonInicion";
            buttonInicion.TabIndex = 2;
            buttonInicion.UseVisualStyleBackColor = false;
            buttonInicion.Click += buttonInicion_Click;
            // labelAyuda
            labelAyuda.Text = "¿Necesitas acceso? Consulta al administrador.";
            labelAyuda.ForeColor = Color.FromArgb(95, 103, 109);
            labelAyuda.Font = new Font("Segoe UI", 9F);
            labelAyuda.TextAlign = ContentAlignment.MiddleCenter;
            labelAyuda.Location = new Point(64, 447);
            labelAyuda.Size = new Size(430, 30);
            labelAyuda.Name = "labelAyuda";
            // labelPie
            labelPie.Text = "GIOVANNY BARBERS  ·  Gestión de barbería";
            labelPie.Font = new Font("Segoe UI", 8F);
            labelPie.ForeColor = Color.FromArgb(105, 112, 117);
            labelPie.TextAlign = ContentAlignment.MiddleCenter;
            labelPie.Location = new Point(64, 513);
            labelPie.Size = new Size(430, 25);
            labelPie.Name = "labelPie";
            // Login
            AcceptButton = buttonInicion;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(250, 250, 248);
            ClientSize = new Size(920, 560);
            Controls.Add(panelAcceso);
            Controls.Add(panelMarca);
            Font = new Font("Segoe UI", 10F);
            ForeColor = Color.FromArgb(31, 37, 41);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Login";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Giovanny Barbers · Iniciar sesión";
            Load += Form1_Load;
            panelMarca.ResumeLayout(false);
            panelAcceso.ResumeLayout(false);
            panelAcceso.PerformLayout();
            ResumeLayout(false);
        }
        #endregion
        private Panel panelMarca;
        private Label labelSimbolo;
        private Label labelMarca;
        private Label labelMarcaSubtitulo;
        private Panel lineaDorada;
        private Label labelLema;
        private Panel panelAcceso;
        private Label labelAcceso;
        private Label Titulo;
        private Label labelInstrucciones;
        private Button buttonInicion;
        private MaskedTextBox maskedTextBoxUsuario;
        private MaskedTextBox maskedTextBoxContrasena;
        private Label labelUsuario;
        private Label labelContrasena;
        private Label labelAyuda;
        private Label labelPie;
    }
}