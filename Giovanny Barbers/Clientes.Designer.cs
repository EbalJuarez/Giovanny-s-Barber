namespace Giovanny_Barbers
{
    partial class Clientes
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
            textBoxNombre = new TextBox();
            textBoxTelefono = new TextBox();
            textBoxNit = new TextBox();
            dataGridViewClientes = new DataGridView();
            comboBoxServicios = new ComboBox();
            label4 = new Label();
            label5 = new Label();
            buttonRegistrar = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridViewClientes).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F);
            label1.Location = new Point(157, 97);
            label1.Name = "label1";
            label1.Size = new Size(89, 28);
            label1.TabIndex = 0;
            label1.Text = "Nombre:";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 12F);
            label2.Location = new Point(157, 169);
            label2.Name = "label2";
            label2.Size = new Size(90, 28);
            label2.TabIndex = 1;
            label2.Text = "Telefono:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 12F);
            label3.Location = new Point(200, 134);
            label3.Name = "label3";
            label3.Size = new Size(46, 28);
            label3.TabIndex = 2;
            label3.Text = "NIT:";
            // 
            // textBoxNombre
            // 
            textBoxNombre.Location = new Point(267, 101);
            textBoxNombre.Name = "textBoxNombre";
            textBoxNombre.Size = new Size(230, 27);
            textBoxNombre.TabIndex = 3;
            // 
            // textBoxTelefono
            // 
            textBoxTelefono.Location = new Point(267, 173);
            textBoxTelefono.Name = "textBoxTelefono";
            textBoxTelefono.Size = new Size(230, 27);
            textBoxTelefono.TabIndex = 4;
            // 
            // textBoxNit
            // 
            textBoxNit.Location = new Point(267, 134);
            textBoxNit.Name = "textBoxNit";
            textBoxNit.Size = new Size(230, 27);
            textBoxNit.TabIndex = 5;
            // 
            // dataGridViewClientes
            // 
            dataGridViewClientes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewClientes.Location = new Point(83, 295);
            dataGridViewClientes.Name = "dataGridViewClientes";
            dataGridViewClientes.RowHeadersWidth = 51;
            dataGridViewClientes.Size = new Size(680, 188);
            dataGridViewClientes.TabIndex = 6;
            // 
            // comboBoxServicios
            // 
            comboBoxServicios.FormattingEnabled = true;
            comboBoxServicios.Location = new Point(267, 218);
            comboBoxServicios.Name = "comboBoxServicios";
            comboBoxServicios.Size = new Size(230, 28);
            comboBoxServicios.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 12F);
            label4.Location = new Point(156, 218);
            label4.Name = "label4";
            label4.Size = new Size(85, 28);
            label4.TabIndex = 8;
            label4.Text = "Servicio:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 20F);
            label5.Location = new Point(314, 32);
            label5.Name = "label5";
            label5.Size = new Size(138, 46);
            label5.TabIndex = 9;
            label5.Text = "Clientes";
            label5.Click += label5_Click;
            // 
            // buttonRegistrar
            // 
            buttonRegistrar.Location = new Point(330, 260);
            buttonRegistrar.Name = "buttonRegistrar";
            buttonRegistrar.Size = new Size(94, 29);
            buttonRegistrar.TabIndex = 10;
            buttonRegistrar.Text = "Registrar";
            buttonRegistrar.UseVisualStyleBackColor = true;
            // 
            // Clientes
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(848, 502);
            Controls.Add(buttonRegistrar);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(comboBoxServicios);
            Controls.Add(dataGridViewClientes);
            Controls.Add(textBoxNit);
            Controls.Add(textBoxTelefono);
            Controls.Add(textBoxNombre);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "Clientes";
            Text = "Clientes";
            ((System.ComponentModel.ISupportInitialize)dataGridViewClientes).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private TextBox textBoxNombre;
        private TextBox textBoxTelefono;
        private TextBox textBoxNit;
        private DataGridView dataGridViewClientes;
        private ComboBox comboBoxServicios;
        private Label label4;
        private Label label5;
        private Button buttonRegistrar;
    }
}