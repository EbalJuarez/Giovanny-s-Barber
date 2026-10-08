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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login));
            panelMarca = new Panel();
            pictureBox1 = new PictureBox();
            panelAcceso = new Panel();
            labelAcceso = new Label();
            Titulo = new Label();
            labelInstrucciones = new Label();
            labelUsuario = new Label();
            maskedTextBoxUsuario = new MaskedTextBox();
            labelContrasena = new Label();
            maskedTextBoxContrasena = new MaskedTextBox();
            buttonInicion = new Button();
            labelPie = new Label();
            panelMarca.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panelAcceso.SuspendLayout();
            SuspendLayout();
            // 
            // panelMarca
            // 
            panelMarca.BackColor = Color.FromArgb(31, 37, 41);
            panelMarca.Controls.Add(pictureBox1);
            panelMarca.Dock = DockStyle.Left;
            panelMarca.Location = new Point(0, 0);
            panelMarca.Name = "panelMarca";
            panelMarca.Size = new Size(359, 560);
            panelMarca.TabIndex = 1;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(26, 143);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(311, 222);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 5;
            pictureBox1.TabStop = false;
            // 
            // panelAcceso
            // 
            panelAcceso.BackColor = Color.FromArgb(250, 250, 248);
            panelAcceso.Controls.Add(labelAcceso);
            panelAcceso.Controls.Add(Titulo);
            panelAcceso.Controls.Add(labelInstrucciones);
            panelAcceso.Controls.Add(labelUsuario);
            panelAcceso.Controls.Add(maskedTextBoxUsuario);
            panelAcceso.Controls.Add(labelContrasena);
            panelAcceso.Controls.Add(maskedTextBoxContrasena);
            panelAcceso.Controls.Add(buttonInicion);
            panelAcceso.Controls.Add(labelPie);
            panelAcceso.Dock = DockStyle.Fill;
            panelAcceso.Location = new Point(359, 0);
            panelAcceso.Name = "panelAcceso";
            panelAcceso.Size = new Size(561, 560);
            panelAcceso.TabIndex = 0;
            // 
            // labelAcceso
            // 
            labelAcceso.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            labelAcceso.ForeColor = Color.FromArgb(128, 101, 34);
            labelAcceso.Location = new Point(64, 49);
            labelAcceso.Name = "labelAcceso";
            labelAcceso.Size = new Size(400, 25);
            labelAcceso.TabIndex = 0;
            labelAcceso.Text = "ACCESO AL SISTEMA";
            // 
            // Titulo
            // 
            Titulo.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            Titulo.ForeColor = Color.FromArgb(31, 37, 41);
            Titulo.Location = new Point(59, 81);
            Titulo.Name = "Titulo";
            Titulo.Size = new Size(460, 52);
            Titulo.TabIndex = 1;
            Titulo.Text = "Bienvenido de nuevo";
            // 
            // labelInstrucciones
            // 
            labelInstrucciones.ForeColor = Color.FromArgb(95, 103, 109);
            labelInstrucciones.Location = new Point(64, 143);
            labelInstrucciones.Name = "labelInstrucciones";
            labelInstrucciones.Size = new Size(440, 28);
            labelInstrucciones.TabIndex = 2;
            labelInstrucciones.Text = "Ingresa tus credenciales para continuar.";
            // 
            // labelUsuario
            // 
            labelUsuario.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            labelUsuario.Location = new Point(64, 199);
            labelUsuario.Name = "labelUsuario";
            labelUsuario.Size = new Size(430, 25);
            labelUsuario.TabIndex = 3;
            labelUsuario.Text = "Usuario";
            // 
            // maskedTextBoxUsuario
            // 
            maskedTextBoxUsuario.AccessibleName = "Usuario";
            maskedTextBoxUsuario.BorderStyle = BorderStyle.FixedSingle;
            maskedTextBoxUsuario.Font = new Font("Segoe UI", 14F);
            maskedTextBoxUsuario.Location = new Point(64, 229);
            maskedTextBoxUsuario.Name = "maskedTextBoxUsuario";
            maskedTextBoxUsuario.Size = new Size(430, 39);
            maskedTextBoxUsuario.TabIndex = 0;
            // 
            // labelContrasena
            // 
            labelContrasena.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            labelContrasena.Location = new Point(64, 286);
            labelContrasena.Name = "labelContrasena";
            labelContrasena.Size = new Size(430, 25);
            labelContrasena.TabIndex = 4;
            labelContrasena.Text = "Contraseña";
            // 
            // maskedTextBoxContrasena
            // 
            maskedTextBoxContrasena.AccessibleName = "Contraseña";
            maskedTextBoxContrasena.BorderStyle = BorderStyle.FixedSingle;
            maskedTextBoxContrasena.Font = new Font("Segoe UI", 14F);
            maskedTextBoxContrasena.Location = new Point(64, 316);
            maskedTextBoxContrasena.Name = "maskedTextBoxContrasena";
            maskedTextBoxContrasena.Size = new Size(430, 39);
            maskedTextBoxContrasena.TabIndex = 1;
            maskedTextBoxContrasena.UseSystemPasswordChar = true;
            // 
            // buttonInicion
            // 
            buttonInicion.BackColor = Color.FromArgb(171, 140, 57);
            buttonInicion.Cursor = Cursors.Hand;
            buttonInicion.FlatAppearance.BorderSize = 0;
            buttonInicion.FlatAppearance.MouseDownBackColor = Color.FromArgb(157, 128, 49);
            buttonInicion.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 162, 77);
            buttonInicion.FlatStyle = FlatStyle.Flat;
            buttonInicion.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            buttonInicion.ForeColor = Color.FromArgb(23, 27, 30);
            buttonInicion.Location = new Point(64, 384);
            buttonInicion.Name = "buttonInicion";
            buttonInicion.Size = new Size(430, 49);
            buttonInicion.TabIndex = 2;
            buttonInicion.Text = "Iniciar sesión";
            buttonInicion.UseVisualStyleBackColor = false;
            buttonInicion.Click += buttonInicion_Click;
            // 
            // labelPie
            // 
            labelPie.Font = new Font("Segoe UI", 8F);
            labelPie.ForeColor = Color.FromArgb(105, 112, 117);
            labelPie.Location = new Point(64, 513);
            labelPie.Name = "labelPie";
            labelPie.Size = new Size(430, 25);
            labelPie.TabIndex = 6;
            labelPie.Text = "GIOVANNY BARBERS  ·  Gestión de barbería";
            labelPie.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Login
            // 
            AcceptButton = buttonInicion;
            AutoScaleDimensions = new SizeF(120F, 120F);
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
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panelAcceso.ResumeLayout(false);
            panelAcceso.PerformLayout();
            ResumeLayout(false);
        }
        #endregion
        private Panel panelMarca;
        private Panel panelAcceso;
        private Label labelAcceso;
        private Label Titulo;
        private Label labelInstrucciones;
        private Button buttonInicion;
        private MaskedTextBox maskedTextBoxUsuario;
        private MaskedTextBox maskedTextBoxContrasena;
        private Label labelUsuario;
        private Label labelContrasena;
        private Label labelPie;
        private PictureBox pictureBox1;
    }
}