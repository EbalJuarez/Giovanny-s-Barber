namespace Giovanny_Barbers
{
    partial class Login
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Titulo = new Label();
            buttonInicion = new Button();
            maskedTextBoxUsuario = new MaskedTextBox();
            maskedTextBoxContrasena = new MaskedTextBox();
            labelUsuario = new Label();
            labelContrasena = new Label();
            SuspendLayout();
            // 
            // Titulo
            // 
            Titulo.AutoSize = true;
            Titulo.Font = new Font("Segoe UI", 20F);
            Titulo.Location = new Point(274, 28);
            Titulo.Name = "Titulo";
            Titulo.Size = new Size(257, 46);
            Titulo.TabIndex = 0;
            Titulo.Text = "Inicio De Sesion";
            // 
            // buttonInicion
            // 
            buttonInicion.Font = new Font("Segoe UI", 15F);
            buttonInicion.Location = new Point(333, 475);
            buttonInicion.Margin = new Padding(3, 4, 3, 4);
            buttonInicion.Name = "buttonInicion";
            buttonInicion.Size = new Size(182, 51);
            buttonInicion.TabIndex = 1;
            buttonInicion.Text = "Iniciar sesion";
            buttonInicion.UseVisualStyleBackColor = true;
            buttonInicion.Click += buttonInicion_Click;
            // 
            // maskedTextBoxUsuario
            // 
            maskedTextBoxUsuario.Location = new Point(333, 160);
            maskedTextBoxUsuario.Margin = new Padding(3, 4, 3, 4);
            maskedTextBoxUsuario.Name = "maskedTextBoxUsuario";
            maskedTextBoxUsuario.Size = new Size(233, 27);
            maskedTextBoxUsuario.TabIndex = 2;
            // 
            // maskedTextBoxContrasena
            // 
            maskedTextBoxContrasena.Location = new Point(333, 267);
            maskedTextBoxContrasena.Margin = new Padding(3, 4, 3, 4);
            maskedTextBoxContrasena.Name = "maskedTextBoxContrasena";
            maskedTextBoxContrasena.PasswordChar = '*';
            maskedTextBoxContrasena.Size = new Size(233, 27);
            maskedTextBoxContrasena.TabIndex = 3;
            // 
            // labelUsuario
            // 
            labelUsuario.AutoSize = true;
            labelUsuario.Font = new Font("Segoe UI", 15F);
            labelUsuario.Location = new Point(231, 153);
            labelUsuario.Name = "labelUsuario";
            labelUsuario.Size = new Size(105, 35);
            labelUsuario.TabIndex = 4;
            labelUsuario.Text = "Usuario:";
            labelUsuario.Click += label1_Click;
            // 
            // labelContrasena
            // 
            labelContrasena.AutoSize = true;
            labelContrasena.Font = new Font("Segoe UI", 15F);
            labelContrasena.Location = new Point(175, 259);
            labelContrasena.Name = "labelContrasena";
            labelContrasena.Size = new Size(152, 35);
            labelContrasena.TabIndex = 5;
            labelContrasena.Text = "Contraseña: ";
            // 
            // Login
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(914, 600);
            Controls.Add(labelContrasena);
            Controls.Add(labelUsuario);
            Controls.Add(maskedTextBoxContrasena);
            Controls.Add(maskedTextBoxUsuario);
            Controls.Add(buttonInicion);
            Controls.Add(Titulo);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Login";
            Text = "Inicio de Sesion";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label Titulo;
        private Button buttonInicion;
        private MaskedTextBox maskedTextBoxUsuario;
        private MaskedTextBox maskedTextBoxContrasena;
        private Label labelUsuario;
        private Label labelContrasena;
    }
}
