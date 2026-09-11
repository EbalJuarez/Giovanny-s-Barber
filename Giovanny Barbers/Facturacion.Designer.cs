namespace Giovanny_Barbers
{
    partial class Facturacion
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
            btnAgregar = new Button();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            dgvDetalle = new DataGridView();
            txtPrecio = new TextBox();
            nudCantidad = new NumericUpDown();
            cmbArticulo = new ComboBox();
            cmbTipo = new ComboBox();
            cmbTrabajador = new ComboBox();
            cmbCliente = new ComboBox();
            panel2 = new Panel();
            label8 = new Label();
            label7 = new Label();
            lblTotal = new Label();
            lblSubtotal = new Label();
            btnGenerarFactura = new Button();
            btnNuevaFactura = new Button();
            FACTUACIÓN = new Label();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDetalle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudCantidad).BeginInit();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(btnAgregar);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(dgvDetalle);
            panel1.Controls.Add(txtPrecio);
            panel1.Controls.Add(nudCantidad);
            panel1.Controls.Add(cmbArticulo);
            panel1.Controls.Add(cmbTipo);
            panel1.Controls.Add(cmbTrabajador);
            panel1.Controls.Add(cmbCliente);
            panel1.Location = new Point(12, 50);
            panel1.Name = "panel1";
            panel1.Size = new Size(776, 534);
            panel1.TabIndex = 0;
            // 
            // btnAgregar
            // 
            btnAgregar.Location = new Point(316, 255);
            btnAgregar.Name = "btnAgregar";
            btnAgregar.Size = new Size(152, 40);
            btnAgregar.TabIndex = 13;
            btnAgregar.Text = "Agregar";
            btnAgregar.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(437, 165);
            label6.Name = "label6";
            label6.Size = new Size(102, 30);
            label6.TabIndex = 12;
            label6.Text = "Precio: Q.";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(22, 167);
            label5.Name = "label5";
            label5.Size = new Size(101, 30);
            label5.TabIndex = 11;
            label5.Text = "Cantidad:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(449, 99);
            label4.Name = "label4";
            label4.Size = new Size(90, 30);
            label4.TabIndex = 10;
            label4.Text = "Articulo:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(65, 99);
            label3.Name = "label3";
            label3.Size = new Size(58, 30);
            label3.TabIndex = 9;
            label3.Text = "Tipo:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(424, 13);
            label2.Name = "label2";
            label2.Size = new Size(115, 30);
            label2.TabIndex = 8;
            label2.Text = "Trabajador:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(41, 13);
            label1.Name = "label1";
            label1.Size = new Size(82, 30);
            label1.TabIndex = 7;
            label1.Text = "Cliente:";
            // 
            // dgvDetalle
            // 
            dgvDetalle.AllowUserToAddRows = false;
            dgvDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDetalle.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDetalle.Location = new Point(30, 314);
            dgvDetalle.MultiSelect = false;
            dgvDetalle.Name = "dgvDetalle";
            dgvDetalle.ReadOnly = true;
            dgvDetalle.RowHeadersWidth = 72;
            dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDetalle.Size = new Size(722, 206);
            dgvDetalle.TabIndex = 6;
            // 
            // txtPrecio
            // 
            txtPrecio.Location = new Point(554, 162);
            txtPrecio.Name = "txtPrecio";
            txtPrecio.ReadOnly = true;
            txtPrecio.Size = new Size(205, 35);
            txtPrecio.TabIndex = 5;
            // 
            // nudCantidad
            // 
            nudCantidad.Location = new Point(144, 165);
            nudCantidad.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudCantidad.Name = "nudCantidad";
            nudCantidad.Size = new Size(205, 35);
            nudCantidad.TabIndex = 4;
            nudCantidad.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // cmbArticulo
            // 
            cmbArticulo.FormattingEnabled = true;
            cmbArticulo.Location = new Point(554, 96);
            cmbArticulo.Name = "cmbArticulo";
            cmbArticulo.Size = new Size(205, 38);
            cmbArticulo.TabIndex = 3;
            // 
            // cmbTipo
            // 
            cmbTipo.FormattingEnabled = true;
            cmbTipo.Items.AddRange(new object[] { "Producto", "Servicio" });
            cmbTipo.Location = new Point(144, 93);
            cmbTipo.Name = "cmbTipo";
            cmbTipo.Size = new Size(205, 38);
            cmbTipo.TabIndex = 2;
            // 
            // cmbTrabajador
            // 
            cmbTrabajador.FormattingEnabled = true;
            cmbTrabajador.Location = new Point(554, 10);
            cmbTrabajador.Name = "cmbTrabajador";
            cmbTrabajador.Size = new Size(205, 38);
            cmbTrabajador.TabIndex = 1;
            // 
            // cmbCliente
            // 
            cmbCliente.FormattingEnabled = true;
            cmbCliente.Location = new Point(144, 10);
            cmbCliente.Name = "cmbCliente";
            cmbCliente.Size = new Size(205, 38);
            cmbCliente.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.Controls.Add(label8);
            panel2.Controls.Add(label7);
            panel2.Controls.Add(lblTotal);
            panel2.Controls.Add(lblSubtotal);
            panel2.Controls.Add(btnGenerarFactura);
            panel2.Controls.Add(btnNuevaFactura);
            panel2.Location = new Point(12, 590);
            panel2.Name = "panel2";
            panel2.Size = new Size(776, 123);
            panel2.TabIndex = 1;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(170, 72);
            label8.Name = "label8";
            label8.Size = new Size(62, 30);
            label8.TabIndex = 5;
            label8.Text = "Total:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(170, 13);
            label7.Name = "label7";
            label7.Size = new Size(95, 30);
            label7.TabIndex = 4;
            label7.Text = "Subtotal:";
            label7.Click += label7_Click;
            // 
            // lblTotal
            // 
            lblTotal.AutoSize = true;
            lblTotal.Location = new Point(271, 72);
            lblTotal.Name = "lblTotal";
            lblTotal.Size = new Size(73, 30);
            lblTotal.TabIndex = 3;
            lblTotal.Text = "Q 0.00";
            // 
            // lblSubtotal
            // 
            lblSubtotal.AutoSize = true;
            lblSubtotal.Location = new Point(271, 13);
            lblSubtotal.Name = "lblSubtotal";
            lblSubtotal.Size = new Size(73, 30);
            lblSubtotal.TabIndex = 2;
            lblSubtotal.Text = "Q 0.00";
            // 
            // btnGenerarFactura
            // 
            btnGenerarFactura.Location = new Point(386, 67);
            btnGenerarFactura.Name = "btnGenerarFactura";
            btnGenerarFactura.Size = new Size(261, 40);
            btnGenerarFactura.TabIndex = 1;
            btnGenerarFactura.Text = "Generar factura";
            btnGenerarFactura.UseVisualStyleBackColor = true;
            // 
            // btnNuevaFactura
            // 
            btnNuevaFactura.Location = new Point(386, 8);
            btnNuevaFactura.Name = "btnNuevaFactura";
            btnNuevaFactura.Size = new Size(261, 40);
            btnNuevaFactura.TabIndex = 0;
            btnNuevaFactura.Text = "Nueva factura";
            btnNuevaFactura.UseVisualStyleBackColor = true;
            // 
            // FACTUACIÓN
            // 
            FACTUACIÓN.AutoSize = true;
            FACTUACIÓN.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            FACTUACIÓN.Location = new Point(283, 9);
            FACTUACIÓN.Name = "FACTUACIÓN";
            FACTUACIÓN.Size = new Size(210, 38);
            FACTUACIÓN.TabIndex = 0;
            FACTUACIÓN.Text = "FACTURACIÓN";
            // 
            // Facturacion
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 729);
            Controls.Add(FACTUACIÓN);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "Facturacion";
            Text = "Facturacion";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDetalle).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudCantidad).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Label FACTUACIÓN;
        private Panel panel2;
        private Label label2;
        private Label label1;
        private DataGridView dgvDetalle;
        private TextBox txtPrecio;
        private NumericUpDown nudCantidad;
        private ComboBox cmbArticulo;
        private ComboBox cmbTipo;
        private ComboBox cmbTrabajador;
        private ComboBox cmbCliente;
        private Button btnAgregar;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label8;
        private Label label7;
        private Label lblTotal;
        private Label lblSubtotal;
        private Button btnGenerarFactura;
        private Button btnNuevaFactura;
    }
}