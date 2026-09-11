namespace Giovanny_Barbers
{
    partial class Inventario
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
            panel1 = new Panel();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            btnActualizar = new Button();
            btnRegistrar = new Button();
            btnLimpiar = new Button();
            nudStockMinimo = new NumericUpDown();
            nudStock = new NumericUpDown();
            nudPrecio = new NumericUpDown();
            cmbCategoria = new ComboBox();
            txtNombre = new TextBox();
            label1 = new Label();
            panel2 = new Panel();
            label7 = new Label();
            dgvProductos = new DataGridView();
            txtBuscar = new TextBox();
            panel3 = new Panel();
            label10 = new Label();
            label9 = new Label();
            btnEntrada = new Button();
            btnSalida = new Button();
            nudCantidadMovimiento = new NumericUpDown();
            cmbProductoMovimiento = new ComboBox();
            label8 = new Label();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudStockMinimo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudStock).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPrecio).BeginInit();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProductos).BeginInit();
            panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudCantidadMovimiento).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(btnActualizar);
            panel1.Controls.Add(btnRegistrar);
            panel1.Controls.Add(btnLimpiar);
            panel1.Controls.Add(nudStockMinimo);
            panel1.Controls.Add(nudStock);
            panel1.Controls.Add(nudPrecio);
            panel1.Controls.Add(cmbCategoria);
            panel1.Controls.Add(txtNombre);
            panel1.Location = new Point(12, 83);
            panel1.Name = "panel1";
            panel1.Size = new Size(868, 322);
            panel1.TabIndex = 0;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(63, 256);
            label6.Name = "label6";
            label6.Size = new Size(143, 30);
            label6.TabIndex = 12;
            label6.Text = "Stock mínimo:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(63, 201);
            label5.Name = "label5";
            label5.Size = new Size(67, 30);
            label5.TabIndex = 11;
            label5.Text = "Stock:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(63, 147);
            label4.Name = "label4";
            label4.Size = new Size(75, 30);
            label4.TabIndex = 10;
            label4.Text = "Precio:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(63, 95);
            label3.Name = "label3";
            label3.Size = new Size(107, 30);
            label3.TabIndex = 9;
            label3.Text = "Categoría:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(63, 27);
            label2.Name = "label2";
            label2.Size = new Size(94, 30);
            label2.TabIndex = 8;
            label2.Text = "Nombre:";
            // 
            // btnActualizar
            // 
            btnActualizar.Location = new Point(604, 137);
            btnActualizar.Name = "btnActualizar";
            btnActualizar.Size = new Size(131, 40);
            btnActualizar.TabIndex = 7;
            btnActualizar.Text = "Actualizar";
            btnActualizar.UseVisualStyleBackColor = true;
            // 
            // btnRegistrar
            // 
            btnRegistrar.Location = new Point(604, 27);
            btnRegistrar.Name = "btnRegistrar";
            btnRegistrar.Size = new Size(131, 40);
            btnRegistrar.TabIndex = 6;
            btnRegistrar.Text = "Registrar";
            btnRegistrar.UseVisualStyleBackColor = true;
            // 
            // btnLimpiar
            // 
            btnLimpiar.Location = new Point(604, 246);
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Size = new Size(131, 40);
            btnLimpiar.TabIndex = 5;
            btnLimpiar.Text = "Limpiar";
            btnLimpiar.UseVisualStyleBackColor = true;
            // 
            // nudStockMinimo
            // 
            nudStockMinimo.Location = new Point(288, 251);
            nudStockMinimo.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudStockMinimo.Name = "nudStockMinimo";
            nudStockMinimo.Size = new Size(210, 35);
            nudStockMinimo.TabIndex = 4;
            // 
            // nudStock
            // 
            nudStock.Location = new Point(286, 196);
            nudStock.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudStock.Name = "nudStock";
            nudStock.Size = new Size(212, 35);
            nudStock.TabIndex = 3;
            // 
            // nudPrecio
            // 
            nudPrecio.DecimalPlaces = 2;
            nudPrecio.Location = new Point(286, 142);
            nudPrecio.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudPrecio.Name = "nudPrecio";
            nudPrecio.Size = new Size(212, 35);
            nudPrecio.TabIndex = 2;
            // 
            // cmbCategoria
            // 
            cmbCategoria.FormattingEnabled = true;
            cmbCategoria.Location = new Point(286, 87);
            cmbCategoria.Name = "cmbCategoria";
            cmbCategoria.Size = new Size(212, 38);
            cmbCategoria.TabIndex = 1;
            // 
            // txtNombre
            // 
            txtNombre.Location = new Point(286, 22);
            txtNombre.Name = "txtNombre";
            txtNombre.Size = new Size(212, 35);
            txtNombre.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(350, 28);
            label1.Name = "label1";
            label1.Size = new Size(186, 38);
            label1.TabIndex = 0;
            label1.Text = "INVENTARIO";
            // 
            // panel2
            // 
            panel2.Controls.Add(label7);
            panel2.Controls.Add(dgvProductos);
            panel2.Controls.Add(txtBuscar);
            panel2.Location = new Point(12, 411);
            panel2.Name = "panel2";
            panel2.Size = new Size(868, 279);
            panel2.TabIndex = 1;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(63, 32);
            label7.Name = "label7";
            label7.Size = new Size(169, 30);
            label7.TabIndex = 2;
            label7.Text = "Buscar Producto:";
            // 
            // dgvProductos
            // 
            dgvProductos.AllowUserToAddRows = false;
            dgvProductos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProductos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProductos.Location = new Point(63, 94);
            dgvProductos.MultiSelect = false;
            dgvProductos.Name = "dgvProductos";
            dgvProductos.ReadOnly = true;
            dgvProductos.RowHeadersWidth = 72;
            dgvProductos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProductos.Size = new Size(763, 182);
            dgvProductos.TabIndex = 1;
            // 
            // txtBuscar
            // 
            txtBuscar.Location = new Point(286, 27);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.Size = new Size(540, 35);
            txtBuscar.TabIndex = 0;
            // 
            // panel3
            // 
            panel3.Controls.Add(label10);
            panel3.Controls.Add(label9);
            panel3.Controls.Add(btnEntrada);
            panel3.Controls.Add(btnSalida);
            panel3.Controls.Add(nudCantidadMovimiento);
            panel3.Controls.Add(cmbProductoMovimiento);
            panel3.Controls.Add(label8);
            panel3.Location = new Point(12, 696);
            panel3.Name = "panel3";
            panel3.Size = new Size(868, 228);
            panel3.TabIndex = 2;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(79, 162);
            label10.Name = "label10";
            label10.Size = new Size(101, 30);
            label10.TabIndex = 9;
            label10.Text = "Cantidad:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(79, 89);
            label9.Name = "label9";
            label9.Size = new Size(102, 30);
            label9.TabIndex = 8;
            label9.Text = "Producto:";
            // 
            // btnEntrada
            // 
            btnEntrada.Location = new Point(560, 84);
            btnEntrada.Name = "btnEntrada";
            btnEntrada.Size = new Size(266, 40);
            btnEntrada.TabIndex = 7;
            btnEntrada.Text = "Registrar entrada";
            btnEntrada.UseVisualStyleBackColor = true;
            // 
            // btnSalida
            // 
            btnSalida.Location = new Point(560, 157);
            btnSalida.Name = "btnSalida";
            btnSalida.Size = new Size(266, 40);
            btnSalida.TabIndex = 6;
            btnSalida.Text = "Registrar salida";
            btnSalida.UseVisualStyleBackColor = true;
            // 
            // nudCantidadMovimiento
            // 
            nudCantidadMovimiento.Location = new Point(233, 160);
            nudCantidadMovimiento.Name = "nudCantidadMovimiento";
            nudCantidadMovimiento.Size = new Size(265, 35);
            nudCantidadMovimiento.TabIndex = 5;
            // 
            // cmbProductoMovimiento
            // 
            cmbProductoMovimiento.FormattingEnabled = true;
            cmbProductoMovimiento.Location = new Point(231, 89);
            cmbProductoMovimiento.Name = "cmbProductoMovimiento";
            cmbProductoMovimiento.Size = new Size(267, 38);
            cmbProductoMovimiento.TabIndex = 4;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI Semibold", 9.857143F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label8.Location = new Point(257, 16);
            label8.Name = "label8";
            label8.Size = new Size(358, 32);
            label8.TabIndex = 3;
            label8.Text = "MOVIMIENTOS DE INVENTARIO";
            // 
            // Inventario
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(892, 936);
            Controls.Add(label1);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "Inventario";
            Text = "Inventario";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudStockMinimo).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudStock).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPrecio).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProductos).EndInit();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudCantidadMovimiento).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Label label1;
        private Panel panel2;
        private Panel panel3;
        private NumericUpDown nudStock;
        private NumericUpDown nudPrecio;
        private ComboBox cmbCategoria;
        private TextBox txtNombre;
        private Label label3;
        private Label label2;
        private Button btnActualizar;
        private Button btnRegistrar;
        private Button btnLimpiar;
        private NumericUpDown nudStockMinimo;
        private Label label6;
        private Label label5;
        private Label label4;
        private DataGridView dgvProductos;
        private TextBox txtBuscar;
        private Label label7;
        private Label label8;
        private Label label10;
        private Label label9;
        private Button btnEntrada;
        private Button btnSalida;
        private NumericUpDown nudCantidadMovimiento;
        private ComboBox cmbProductoMovimiento;
    }
}