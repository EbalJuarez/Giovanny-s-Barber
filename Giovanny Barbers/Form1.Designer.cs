namespace Giovanny_Barbers
{
    partial class Trabajadores
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
            dataGridViewTrabajadores = new DataGridView();
            buttonRegistro = new Button();
            label6 = new Label();
            ((System.ComponentModel.ISupportInitialize)dataGridViewTrabajadores).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 15F);
            label1.Location = new Point(296, 132);
            label1.Name = "label1";
            label1.Size = new Size(124, 35);
            label1.TabIndex = 0;
            label1.Text = "Nombres:";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 15F);
            label2.Location = new Point(296, 196);
            label2.Name = "label2";
            label2.Size = new Size(123, 35);
            label2.TabIndex = 1;
            label2.Text = "Apellidos:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 15F);
            label3.Location = new Point(305, 257);
            label3.Name = "label3";
            label3.Size = new Size(115, 35);
            label3.TabIndex = 2;
            label3.Text = "Telefono:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 15F);
            label4.Location = new Point(296, 316);
            label4.Name = "label4";
            label4.Size = new Size(125, 35);
            label4.TabIndex = 3;
            label4.Text = "Direccion:";
            // 
            // textBoxNombre
            // 
            textBoxNombre.Location = new Point(414, 139);
            textBoxNombre.Margin = new Padding(3, 4, 3, 4);
            textBoxNombre.Name = "textBoxNombre";
            textBoxNombre.Size = new Size(239, 27);
            textBoxNombre.TabIndex = 4;
            // 
            // textBoxDireccion
            // 
            textBoxDireccion.Location = new Point(414, 323);
            textBoxDireccion.Margin = new Padding(3, 4, 3, 4);
            textBoxDireccion.Name = "textBoxDireccion";
            textBoxDireccion.Size = new Size(239, 27);
            textBoxDireccion.TabIndex = 5;
            // 
            // textBoxTelefono
            // 
            textBoxTelefono.Location = new Point(414, 264);
            textBoxTelefono.Margin = new Padding(3, 4, 3, 4);
            textBoxTelefono.Name = "textBoxTelefono";
            textBoxTelefono.Size = new Size(239, 27);
            textBoxTelefono.TabIndex = 6;
            // 
            // textBoxApellido
            // 
            textBoxApellido.Location = new Point(414, 203);
            textBoxApellido.Margin = new Padding(3, 4, 3, 4);
            textBoxApellido.Name = "textBoxApellido";
            textBoxApellido.Size = new Size(239, 27);
            textBoxApellido.TabIndex = 7;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 15F);
            label5.Location = new Point(345, 59);
            label5.Name = "label5";
            label5.Size = new Size(293, 35);
            label5.TabIndex = 8;
            label5.Text = "Registro de trabajadores.";
            // 
            // dataGridViewTrabajadores
            // 
            dataGridViewTrabajadores.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewTrabajadores.Location = new Point(32, 552);
            dataGridViewTrabajadores.Margin = new Padding(3, 4, 3, 4);
            dataGridViewTrabajadores.Name = "dataGridViewTrabajadores";
            dataGridViewTrabajadores.RowHeadersWidth = 51;
            dataGridViewTrabajadores.Size = new Size(956, 200);
            dataGridViewTrabajadores.TabIndex = 9;
            // 
            // buttonRegistro
            // 
            buttonRegistro.Font = new Font("Segoe UI", 15F);
            buttonRegistro.Location = new Point(435, 405);
            buttonRegistro.Margin = new Padding(3, 4, 3, 4);
            buttonRegistro.Name = "buttonRegistro";
            buttonRegistro.Size = new Size(150, 56);
            buttonRegistro.TabIndex = 10;
            buttonRegistro.Text = "Registrar";
            buttonRegistro.UseVisualStyleBackColor = true;
            buttonRegistro.Click += buttonRegistro_Click_1;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 15F);
            label6.Location = new Point(368, 511);
            label6.Name = "label6";
            label6.Size = new Size(295, 35);
            label6.TabIndex = 11;
            label6.Text = "Trabajadores registrados:";
            // 
            // Trabajadores
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1011, 793);
            Controls.Add(label6);
            Controls.Add(buttonRegistro);
            Controls.Add(dataGridViewTrabajadores);
            Controls.Add(label5);
            Controls.Add(textBoxApellido);
            Controls.Add(textBoxTelefono);
            Controls.Add(textBoxDireccion);
            Controls.Add(textBoxNombre);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Trabajadores";
            Text = "Trabajadores";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridViewTrabajadores).EndInit();
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
        private DataGridView dataGridViewTrabajadores;
        private Button buttonRegistro;
        private Label label6;
    }
}