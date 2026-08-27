namespace Giovanny_Barbers
{
    partial class Form1
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
            Titulo.Location = new Point(240, 21);
            Titulo.Name = "Titulo";
            Titulo.Size = new Size(205, 37);
            Titulo.TabIndex = 0;
            Titulo.Text = "Inicio De Sesion";
            // 
            // buttonInicion
            // 
            buttonInicion.Font = new Font("Segoe UI", 15F);
            buttonInicion.Location = new Point(291, 356);
            buttonInicion.Name = "buttonInicion";
            buttonInicion.Size = new Size(159, 38);
            buttonInicion.TabIndex = 1;
            buttonInicion.Text = "Iniciar sesion";
            buttonInicion.UseVisualStyleBackColor = true;
            // 
            // maskedTextBoxUsuario
            // 
            maskedTextBoxUsuario.Location = new Point(291, 120);
            maskedTextBoxUsuario.Name = "maskedTextBoxUsuario";
            maskedTextBoxUsuario.Size = new Size(204, 23);
            maskedTextBoxUsuario.TabIndex = 2;
            // 
            // maskedTextBoxContrasena
            // 
            maskedTextBoxContrasena.Location = new Point(291, 200);
            maskedTextBoxContrasena.Name = "maskedTextBoxContrasena";
            maskedTextBoxContrasena.Size = new Size(204, 23);
            maskedTextBoxContrasena.TabIndex = 3;
            // 
            // labelUsuario
            // 
            labelUsuario.AutoSize = true;
            labelUsuario.Font = new Font("Segoe UI", 15F);
            labelUsuario.Location = new Point(202, 115);
            labelUsuario.Name = "labelUsuario";
            labelUsuario.Size = new Size(83, 28);
            labelUsuario.TabIndex = 4;
            labelUsuario.Text = "Usuario:";
            labelUsuario.Click += label1_Click;
            // 
            // labelContrasena
            // 
            labelContrasena.AutoSize = true;
            labelContrasena.Font = new Font("Segoe UI", 15F);
            labelContrasena.Location = new Point(171, 192);
            labelContrasena.Name = "labelContrasena";
            labelContrasena.Size = new Size(119, 28);
            labelContrasena.TabIndex = 5;
            labelContrasena.Text = "Contraseña: ";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(labelContrasena);
            Controls.Add(labelUsuario);
            Controls.Add(maskedTextBoxContrasena);
            Controls.Add(maskedTextBoxUsuario);
            Controls.Add(buttonInicion);
            Controls.Add(Titulo);
            Name = "Form1";
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
