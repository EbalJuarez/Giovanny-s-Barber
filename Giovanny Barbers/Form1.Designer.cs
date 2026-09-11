namespace Giovanny_Barbers
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            textBoxNombre = new TextBox();
            textBoxDireccion = new TextBox();
            textBoxTelefono = new TextBox();
            textBoxApellido = new TextBox();
            label5 = new Label();
            dataGridView1 = new DataGridView();
            buttonRegistro = new Button();
            label6 = new Label();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 15F);
            label1.Location = new Point(259, 99);
            label1.Name = "label1";
            label1.Size = new Size(97, 28);
            label1.TabIndex = 0;
            label1.Text = "Nombres:";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 15F);
            label2.Location = new Point(259, 147);
            label2.Name = "label2";
            label2.Size = new Size(98, 28);
            label2.TabIndex = 1;
            label2.Text = "Apellidos:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 15F);
            label3.Location = new Point(267, 193);
            label3.Name = "label3";
            label3.Size = new Size(90, 28);
            label3.TabIndex = 2;
            label3.Text = "Telefono:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 15F);
            label4.Location = new Point(259, 237);
            label4.Name = "label4";
            label4.Size = new Size(98, 28);
            label4.TabIndex = 3;
            label4.Text = "Direccion:";
            // 
            // textBoxNombre
            // 
            textBoxNombre.Location = new Point(362, 104);
            textBoxNombre.Name = "textBoxNombre";
            textBoxNombre.Size = new Size(210, 23);
            textBoxNombre.TabIndex = 4;
            // 
            // textBoxDireccion
            // 
            textBoxDireccion.Location = new Point(362, 242);
            textBoxDireccion.Name = "textBoxDireccion";
            textBoxDireccion.Size = new Size(210, 23);
            textBoxDireccion.TabIndex = 5;
            // 
            // textBoxTelefono
            // 
            textBoxTelefono.Location = new Point(362, 198);
            textBoxTelefono.Name = "textBoxTelefono";
            textBoxTelefono.Size = new Size(210, 23);
            textBoxTelefono.TabIndex = 6;
            // 
            // textBoxApellido
            // 
            textBoxApellido.Location = new Point(362, 152);
            textBoxApellido.Name = "textBoxApellido";
            textBoxApellido.Size = new Size(210, 23);
            textBoxApellido.TabIndex = 7;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 15F);
            label5.Location = new Point(302, 44);
            label5.Name = "label5";
            label5.Size = new Size(230, 28);
            label5.TabIndex = 8;
            label5.Text = "Registro de trabajadores.";
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(322, 414);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(230, 150);
            dataGridView1.TabIndex = 9;
            // 
            // buttonRegistro
            // 
            buttonRegistro.Font = new Font("Segoe UI", 15F);
            buttonRegistro.Location = new Point(381, 304);
            buttonRegistro.Name = "buttonRegistro";
            buttonRegistro.Size = new Size(131, 42);
            buttonRegistro.TabIndex = 10;
            buttonRegistro.Text = "Registrar";
            buttonRegistro.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 15F);
            label6.Location = new Point(322, 383);
            label6.Name = "label6";
            label6.Size = new Size(230, 28);
            label6.TabIndex = 11;
            label6.Text = "Trabajadores registrados:";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(885, 595);
            Controls.Add(label6);
            Controls.Add(buttonRegistro);
            Controls.Add(dataGridView1);
            Controls.Add(label5);
            Controls.Add(textBoxApellido);
            Controls.Add(textBoxTelefono);
            Controls.Add(textBoxDireccion);
            Controls.Add(textBoxNombre);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Trabajadores";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private TextBox textBoxNombre;
        private TextBox textBoxDireccion;
        private TextBox textBoxTelefono;
        private TextBox textBoxApellido;
        private Label label5;
        private DataGridView dataGridView1;
        private Button buttonRegistro;
        private Label label6;
    }
}