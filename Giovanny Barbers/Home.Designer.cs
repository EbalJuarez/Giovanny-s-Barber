namespace Giovanny_Barbers
{
    partial class Home
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                cancelacion.Cancel();
                components?.Dispose();
                cancelacion.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Home));
            layoutPrincipal = new TableLayoutPanel();
            panelLateral = new TableLayoutPanel();
            panelMenu = new FlowLayoutPanel();
            buttonInicio = new Button();
            buttonTrabajadores = new Button();
            buttonClientes = new Button();
            buttonInventario = new Button();
            buttonFacturacion = new Button();
            buttonContabilidad = new Button();
            buttonMiJornada = new Button();
            panelSesion = new FlowLayoutPanel();
            labelUsuario = new Label();
            labelRol = new Label();
            buttonCerrarSesion = new Button();
            pictureBox1 = new PictureBox();
            contenido = new Panel();
            panelInicio = new Panel();
            filasInicio = new TableLayoutPanel();
            labelTitulo = new Label();
            labelBienvenida = new Label();
            labelEstadoResumen = new Label();
            buttonActualizar = new Button();
            panelTarjetas = new FlowLayoutPanel();
            tarjeta1 = new Panel();
            valor1 = new Label();
            titulo1 = new Label();
            tarjeta2 = new Panel();
            valor2 = new Label();
            titulo2 = new Label();
            tarjeta3 = new Panel();
            valor3 = new Label();
            titulo3 = new Label();
            tarjeta4 = new Panel();
            valor4 = new Label();
            titulo4 = new Label();
            tarjeta5 = new Panel();
            valor5 = new Label();
            titulo5 = new Label();
            tarjeta6 = new Panel();
            valor6 = new Label();
            titulo6 = new Label();
            tarjeta7 = new Panel();
            valor7 = new Label();
            titulo7 = new Label();
            tarjeta8 = new Panel();
            valor8 = new Label();
            titulo8 = new Label();
            labelAccesos = new Label();
            panelAccesos = new FlowLayoutPanel();
            accesoTrabajadores = new Button();
            accesoClientes = new Button();
            accesoInventario = new Button();
            accesoFacturacion = new Button();
            accesoContabilidad = new Button();
            accesoMiJornada = new Button();
            layoutPrincipal.SuspendLayout();
            panelLateral.SuspendLayout();
            panelMenu.SuspendLayout();
            panelSesion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            contenido.SuspendLayout();
            panelInicio.SuspendLayout();
            filasInicio.SuspendLayout();
            panelTarjetas.SuspendLayout();
            tarjeta1.SuspendLayout();
            tarjeta2.SuspendLayout();
            tarjeta3.SuspendLayout();
            tarjeta4.SuspendLayout();
            tarjeta5.SuspendLayout();
            tarjeta6.SuspendLayout();
            tarjeta7.SuspendLayout();
            tarjeta8.SuspendLayout();
            panelAccesos.SuspendLayout();
            SuspendLayout();
            // 
            // layoutPrincipal
            // 
            layoutPrincipal.ColumnCount = 2;
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235F));
            layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutPrincipal.Controls.Add(panelLateral, 0, 0);
            layoutPrincipal.Controls.Add(contenido, 1, 0);
            layoutPrincipal.Dock = DockStyle.Fill;
            layoutPrincipal.Location = new Point(0, 0);
            layoutPrincipal.Margin = new Padding(0);
            layoutPrincipal.Name = "layoutPrincipal";
            layoutPrincipal.RowCount = 1;
            layoutPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPrincipal.Size = new Size(1264, 781);
            layoutPrincipal.TabIndex = 0;
            // 
            // panelLateral
            // 
            panelLateral.BackColor = Color.FromArgb(31, 37, 41);
            panelLateral.ColumnCount = 1;
            panelLateral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panelLateral.Controls.Add(panelMenu, 0, 1);
            panelLateral.Controls.Add(panelSesion, 0, 2);
            panelLateral.Controls.Add(pictureBox1, 0, 0);
            panelLateral.Dock = DockStyle.Fill;
            panelLateral.Location = new Point(0, 0);
            panelLateral.Margin = new Padding(0);
            panelLateral.Name = "panelLateral";
            panelLateral.Padding = new Padding(16);
            panelLateral.RowCount = 3;
            panelLateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            panelLateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panelLateral.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            panelLateral.Size = new Size(235, 781);
            panelLateral.TabIndex = 0;
            // 
            // panelMenu
            // 
            panelMenu.AutoScroll = true;
            panelMenu.Controls.Add(buttonInicio);
            panelMenu.Controls.Add(buttonTrabajadores);
            panelMenu.Controls.Add(buttonClientes);
            panelMenu.Controls.Add(buttonInventario);
            panelMenu.Controls.Add(buttonFacturacion);
            panelMenu.Controls.Add(buttonContabilidad);
            panelMenu.Controls.Add(buttonMiJornada);
            panelMenu.Dock = DockStyle.Fill;
            panelMenu.FlowDirection = FlowDirection.TopDown;
            panelMenu.Location = new Point(16, 156);
            panelMenu.Margin = new Padding(0);
            panelMenu.Name = "panelMenu";
            panelMenu.Size = new Size(203, 489);
            panelMenu.TabIndex = 1;
            panelMenu.WrapContents = false;
            // 
            // buttonInicio
            // 
            buttonInicio.BackColor = Color.FromArgb(31, 37, 41);
            buttonInicio.Cursor = Cursors.Hand;
            buttonInicio.FlatAppearance.BorderSize = 0;
            buttonInicio.FlatStyle = FlatStyle.Flat;
            buttonInicio.ForeColor = Color.White;
            buttonInicio.Location = new Point(0, 0);
            buttonInicio.Margin = new Padding(0, 0, 0, 8);
            buttonInicio.Name = "buttonInicio";
            buttonInicio.Padding = new Padding(10, 0, 0, 0);
            buttonInicio.Size = new Size(195, 46);
            buttonInicio.TabIndex = 0;
            buttonInicio.Text = "Inicio";
            buttonInicio.TextAlign = ContentAlignment.MiddleLeft;
            buttonInicio.UseVisualStyleBackColor = false;
            buttonInicio.Click += Navegacion_Click;
            // 
            // buttonTrabajadores
            // 
            buttonTrabajadores.BackColor = Color.FromArgb(31, 37, 41);
            buttonTrabajadores.Cursor = Cursors.Hand;
            buttonTrabajadores.FlatAppearance.BorderSize = 0;
            buttonTrabajadores.FlatStyle = FlatStyle.Flat;
            buttonTrabajadores.ForeColor = Color.White;
            buttonTrabajadores.Location = new Point(0, 54);
            buttonTrabajadores.Margin = new Padding(0, 0, 0, 8);
            buttonTrabajadores.Name = "buttonTrabajadores";
            buttonTrabajadores.Padding = new Padding(10, 0, 0, 0);
            buttonTrabajadores.Size = new Size(195, 46);
            buttonTrabajadores.TabIndex = 1;
            buttonTrabajadores.Text = "Trabajadores";
            buttonTrabajadores.TextAlign = ContentAlignment.MiddleLeft;
            buttonTrabajadores.UseVisualStyleBackColor = false;
            buttonTrabajadores.Click += Navegacion_Click;
            // 
            // buttonClientes
            // 
            buttonClientes.BackColor = Color.FromArgb(31, 37, 41);
            buttonClientes.Cursor = Cursors.Hand;
            buttonClientes.FlatAppearance.BorderSize = 0;
            buttonClientes.FlatStyle = FlatStyle.Flat;
            buttonClientes.ForeColor = Color.White;
            buttonClientes.Location = new Point(0, 108);
            buttonClientes.Margin = new Padding(0, 0, 0, 8);
            buttonClientes.Name = "buttonClientes";
            buttonClientes.Padding = new Padding(10, 0, 0, 0);
            buttonClientes.Size = new Size(195, 46);
            buttonClientes.TabIndex = 2;
            buttonClientes.Text = "Clientes";
            buttonClientes.TextAlign = ContentAlignment.MiddleLeft;
            buttonClientes.UseVisualStyleBackColor = false;
            buttonClientes.Click += Navegacion_Click;
            // 
            // buttonInventario
            // 
            buttonInventario.BackColor = Color.FromArgb(31, 37, 41);
            buttonInventario.Cursor = Cursors.Hand;
            buttonInventario.FlatAppearance.BorderSize = 0;
            buttonInventario.FlatStyle = FlatStyle.Flat;
            buttonInventario.ForeColor = Color.White;
            buttonInventario.Location = new Point(0, 162);
            buttonInventario.Margin = new Padding(0, 0, 0, 8);
            buttonInventario.Name = "buttonInventario";
            buttonInventario.Padding = new Padding(10, 0, 0, 0);
            buttonInventario.Size = new Size(195, 46);
            buttonInventario.TabIndex = 3;
            buttonInventario.Text = "Inventario";
            buttonInventario.TextAlign = ContentAlignment.MiddleLeft;
            buttonInventario.UseVisualStyleBackColor = false;
            buttonInventario.Click += Navegacion_Click;
            // 
            // buttonFacturacion
            // 
            buttonFacturacion.BackColor = Color.FromArgb(31, 37, 41);
            buttonFacturacion.Cursor = Cursors.Hand;
            buttonFacturacion.FlatAppearance.BorderSize = 0;
            buttonFacturacion.FlatStyle = FlatStyle.Flat;
            buttonFacturacion.ForeColor = Color.White;
            buttonFacturacion.Location = new Point(0, 216);
            buttonFacturacion.Margin = new Padding(0, 0, 0, 8);
            buttonFacturacion.Name = "buttonFacturacion";
            buttonFacturacion.Padding = new Padding(10, 0, 0, 0);
            buttonFacturacion.Size = new Size(195, 46);
            buttonFacturacion.TabIndex = 4;
            buttonFacturacion.Text = "Facturación";
            buttonFacturacion.TextAlign = ContentAlignment.MiddleLeft;
            buttonFacturacion.UseVisualStyleBackColor = false;
            buttonFacturacion.Click += Navegacion_Click;
            // 
            // buttonContabilidad
            // 
            buttonContabilidad.BackColor = Color.FromArgb(31, 37, 41);
            buttonContabilidad.Cursor = Cursors.Hand;
            buttonContabilidad.FlatAppearance.BorderSize = 0;
            buttonContabilidad.FlatStyle = FlatStyle.Flat;
            buttonContabilidad.ForeColor = Color.White;
            buttonContabilidad.Location = new Point(0, 270);
            buttonContabilidad.Margin = new Padding(0, 0, 0, 8);
            buttonContabilidad.Name = "buttonContabilidad";
            buttonContabilidad.Padding = new Padding(10, 0, 0, 0);
            buttonContabilidad.Size = new Size(195, 46);
            buttonContabilidad.TabIndex = 5;
            buttonContabilidad.Text = "Contabilidad";
            buttonContabilidad.TextAlign = ContentAlignment.MiddleLeft;
            buttonContabilidad.UseVisualStyleBackColor = false;
            buttonContabilidad.Click += Navegacion_Click;
            // 
            // buttonMiJornada
            // 
            buttonMiJornada.BackColor = Color.FromArgb(31, 37, 41);
            buttonMiJornada.Cursor = Cursors.Hand;
            buttonMiJornada.FlatAppearance.BorderSize = 0;
            buttonMiJornada.FlatStyle = FlatStyle.Flat;
            buttonMiJornada.ForeColor = Color.White;
            buttonMiJornada.Location = new Point(0, 324);
            buttonMiJornada.Margin = new Padding(0, 0, 0, 8);
            buttonMiJornada.Name = "buttonMiJornada";
            buttonMiJornada.Padding = new Padding(10, 0, 0, 0);
            buttonMiJornada.Size = new Size(195, 46);
            buttonMiJornada.TabIndex = 6;
            buttonMiJornada.Text = "Mi jornada";
            buttonMiJornada.TextAlign = ContentAlignment.MiddleLeft;
            buttonMiJornada.UseVisualStyleBackColor = false;
            buttonMiJornada.Click += Navegacion_Click;
            // 
            // panelSesion
            // 
            panelSesion.Controls.Add(labelUsuario);
            panelSesion.Controls.Add(labelRol);
            panelSesion.Controls.Add(buttonCerrarSesion);
            panelSesion.Dock = DockStyle.Fill;
            panelSesion.FlowDirection = FlowDirection.TopDown;
            panelSesion.Location = new Point(16, 645);
            panelSesion.Margin = new Padding(0);
            panelSesion.Name = "panelSesion";
            panelSesion.Size = new Size(203, 120);
            panelSesion.TabIndex = 2;
            panelSesion.WrapContents = false;
            // 
            // labelUsuario
            // 
            labelUsuario.AutoEllipsis = true;
            labelUsuario.ForeColor = Color.White;
            labelUsuario.Location = new Point(3, 0);
            labelUsuario.Name = "labelUsuario";
            labelUsuario.Size = new Size(195, 26);
            labelUsuario.TabIndex = 0;
            labelUsuario.Text = "Usuario";
            // 
            // labelRol
            // 
            labelRol.AutoEllipsis = true;
            labelRol.ForeColor = Color.White;
            labelRol.Location = new Point(3, 26);
            labelRol.Name = "labelRol";
            labelRol.Size = new Size(195, 26);
            labelRol.TabIndex = 1;
            labelRol.Text = "Rol";
            // 
            // buttonCerrarSesion
            // 
            buttonCerrarSesion.Cursor = Cursors.Hand;
            buttonCerrarSesion.FlatStyle = FlatStyle.Flat;
            buttonCerrarSesion.ForeColor = Color.White;
            buttonCerrarSesion.Location = new Point(3, 55);
            buttonCerrarSesion.Name = "buttonCerrarSesion";
            buttonCerrarSesion.Size = new Size(195, 40);
            buttonCerrarSesion.TabIndex = 2;
            buttonCerrarSesion.Text = "Cerrar sesión";
            buttonCerrarSesion.Click += CerrarSesion_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(19, 19);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(197, 134);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // contenido
            // 
            contenido.Controls.Add(panelInicio);
            contenido.Dock = DockStyle.Fill;
            contenido.Location = new Point(235, 0);
            contenido.Margin = new Padding(0);
            contenido.Name = "contenido";
            contenido.Padding = new Padding(28);
            contenido.Size = new Size(1029, 781);
            contenido.TabIndex = 1;
            // 
            // panelInicio
            // 
            panelInicio.AutoScroll = true;
            panelInicio.Controls.Add(filasInicio);
            panelInicio.Dock = DockStyle.Fill;
            panelInicio.Location = new Point(28, 28);
            panelInicio.Name = "panelInicio";
            panelInicio.Size = new Size(973, 725);
            panelInicio.TabIndex = 0;
            // 
            // filasInicio
            // 
            filasInicio.AutoSize = true;
            filasInicio.ColumnCount = 1;
            filasInicio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filasInicio.Controls.Add(labelTitulo);
            filasInicio.Controls.Add(labelBienvenida);
            filasInicio.Controls.Add(labelEstadoResumen);
            filasInicio.Controls.Add(buttonActualizar);
            filasInicio.Controls.Add(panelTarjetas);
            filasInicio.Controls.Add(labelAccesos);
            filasInicio.Controls.Add(panelAccesos);
            filasInicio.Dock = DockStyle.Top;
            filasInicio.Location = new Point(0, 0);
            filasInicio.Name = "filasInicio";
            filasInicio.Padding = new Padding(0, 0, 12, 20);
            filasInicio.RowCount = 7;
            filasInicio.RowStyles.Add(new RowStyle());
            filasInicio.RowStyles.Add(new RowStyle());
            filasInicio.RowStyles.Add(new RowStyle());
            filasInicio.RowStyles.Add(new RowStyle());
            filasInicio.RowStyles.Add(new RowStyle());
            filasInicio.RowStyles.Add(new RowStyle());
            filasInicio.RowStyles.Add(new RowStyle());
            filasInicio.Size = new Size(952, 746);
            filasInicio.TabIndex = 0;
            // 
            // labelTitulo
            // 
            labelTitulo.AutoSize = true;
            labelTitulo.Font = new Font("Segoe UI", 27F, FontStyle.Bold);
            labelTitulo.Location = new Point(0, 0);
            labelTitulo.Margin = new Padding(0, 0, 0, 8);
            labelTitulo.Name = "labelTitulo";
            labelTitulo.Size = new Size(334, 61);
            labelTitulo.TabIndex = 0;
            labelTitulo.Text = "Panel de inicio";
            // 
            // labelBienvenida
            // 
            labelBienvenida.AutoSize = true;
            labelBienvenida.Location = new Point(0, 69);
            labelBienvenida.Margin = new Padding(0, 0, 0, 22);
            labelBienvenida.Name = "labelBienvenida";
            labelBienvenida.Size = new Size(291, 23);
            labelBienvenida.TabIndex = 1;
            labelBienvenida.Text = "Bienvenido · Resumen de tu barbería";
            // 
            // labelEstadoResumen
            // 
            labelEstadoResumen.AutoSize = true;
            labelEstadoResumen.ForeColor = Color.DimGray;
            labelEstadoResumen.Location = new Point(0, 114);
            labelEstadoResumen.Margin = new Padding(0, 0, 0, 16);
            labelEstadoResumen.MaximumSize = new Size(800, 0);
            labelEstadoResumen.Name = "labelEstadoResumen";
            labelEstadoResumen.Size = new Size(185, 23);
            labelEstadoResumen.TabIndex = 2;
            labelEstadoResumen.Text = "Resumen de la jornada";
            // 
            // buttonActualizar
            // 
            buttonActualizar.AutoSize = true;
            buttonActualizar.Location = new Point(0, 153);
            buttonActualizar.Margin = new Padding(0, 0, 0, 16);
            buttonActualizar.Name = "buttonActualizar";
            buttonActualizar.Size = new Size(166, 38);
            buttonActualizar.TabIndex = 3;
            buttonActualizar.Text = "Actualizar resumen";
            buttonActualizar.Click += Actualizar_Click;
            // 
            // panelTarjetas
            // 
            panelTarjetas.AutoSize = true;
            panelTarjetas.Controls.Add(tarjeta1);
            panelTarjetas.Controls.Add(tarjeta2);
            panelTarjetas.Controls.Add(tarjeta3);
            panelTarjetas.Controls.Add(tarjeta4);
            panelTarjetas.Controls.Add(tarjeta5);
            panelTarjetas.Controls.Add(tarjeta6);
            panelTarjetas.Controls.Add(tarjeta7);
            panelTarjetas.Controls.Add(tarjeta8);
            panelTarjetas.Dock = DockStyle.Top;
            panelTarjetas.Location = new Point(0, 207);
            panelTarjetas.Margin = new Padding(0);
            panelTarjetas.Name = "panelTarjetas";
            panelTarjetas.Size = new Size(940, 288);
            panelTarjetas.TabIndex = 4;
            // 
            // tarjeta1
            // 
            tarjeta1.BackColor = Color.White;
            tarjeta1.Controls.Add(valor1);
            tarjeta1.Controls.Add(titulo1);
            tarjeta1.Location = new Point(0, 0);
            tarjeta1.Margin = new Padding(0, 0, 14, 14);
            tarjeta1.Name = "tarjeta1";
            tarjeta1.Padding = new Padding(16);
            tarjeta1.Size = new Size(210, 130);
            tarjeta1.TabIndex = 0;
            // 
            // valor1
            // 
            valor1.AutoEllipsis = true;
            valor1.Dock = DockStyle.Bottom;
            valor1.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor1.Location = new Point(16, 66);
            valor1.Name = "valor1";
            valor1.Size = new Size(178, 48);
            valor1.TabIndex = 0;
            valor1.Text = "—";
            // 
            // titulo1
            // 
            titulo1.Dock = DockStyle.Top;
            titulo1.Location = new Point(16, 16);
            titulo1.Name = "titulo1";
            titulo1.Size = new Size(178, 52);
            titulo1.TabIndex = 1;
            titulo1.Text = "Ingresos de hoy";
            // 
            // tarjeta2
            // 
            tarjeta2.BackColor = Color.White;
            tarjeta2.Controls.Add(valor2);
            tarjeta2.Controls.Add(titulo2);
            tarjeta2.Location = new Point(224, 0);
            tarjeta2.Margin = new Padding(0, 0, 14, 14);
            tarjeta2.Name = "tarjeta2";
            tarjeta2.Padding = new Padding(16);
            tarjeta2.Size = new Size(210, 130);
            tarjeta2.TabIndex = 1;
            // 
            // valor2
            // 
            valor2.AutoEllipsis = true;
            valor2.Dock = DockStyle.Bottom;
            valor2.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor2.Location = new Point(16, 66);
            valor2.Name = "valor2";
            valor2.Size = new Size(178, 48);
            valor2.TabIndex = 0;
            valor2.Text = "—";
            // 
            // titulo2
            // 
            titulo2.Dock = DockStyle.Top;
            titulo2.Location = new Point(16, 16);
            titulo2.Name = "titulo2";
            titulo2.Size = new Size(178, 52);
            titulo2.TabIndex = 1;
            titulo2.Text = "Servicios de hoy";
            // 
            // tarjeta3
            // 
            tarjeta3.BackColor = Color.White;
            tarjeta3.Controls.Add(valor3);
            tarjeta3.Controls.Add(titulo3);
            tarjeta3.Location = new Point(448, 0);
            tarjeta3.Margin = new Padding(0, 0, 14, 14);
            tarjeta3.Name = "tarjeta3";
            tarjeta3.Padding = new Padding(16);
            tarjeta3.Size = new Size(210, 130);
            tarjeta3.TabIndex = 2;
            // 
            // valor3
            // 
            valor3.AutoEllipsis = true;
            valor3.Dock = DockStyle.Bottom;
            valor3.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor3.Location = new Point(16, 66);
            valor3.Name = "valor3";
            valor3.Size = new Size(178, 48);
            valor3.TabIndex = 0;
            valor3.Text = "—";
            // 
            // titulo3
            // 
            titulo3.Dock = DockStyle.Top;
            titulo3.Location = new Point(16, 16);
            titulo3.Name = "titulo3";
            titulo3.Size = new Size(178, 52);
            titulo3.TabIndex = 1;
            titulo3.Text = "Productos vendidos";
            // 
            // tarjeta4
            // 
            tarjeta4.BackColor = Color.White;
            tarjeta4.Controls.Add(valor4);
            tarjeta4.Controls.Add(titulo4);
            tarjeta4.Location = new Point(672, 0);
            tarjeta4.Margin = new Padding(0, 0, 14, 14);
            tarjeta4.Name = "tarjeta4";
            tarjeta4.Padding = new Padding(16);
            tarjeta4.Size = new Size(210, 130);
            tarjeta4.TabIndex = 3;
            // 
            // valor4
            // 
            valor4.AutoEllipsis = true;
            valor4.Dock = DockStyle.Bottom;
            valor4.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor4.Location = new Point(16, 66);
            valor4.Name = "valor4";
            valor4.Size = new Size(178, 48);
            valor4.TabIndex = 0;
            valor4.Text = "—";
            // 
            // titulo4
            // 
            titulo4.Dock = DockStyle.Top;
            titulo4.Location = new Point(16, 16);
            titulo4.Name = "titulo4";
            titulo4.Size = new Size(178, 52);
            titulo4.TabIndex = 1;
            titulo4.Text = "Trabajadores activos";
            // 
            // tarjeta5
            // 
            tarjeta5.BackColor = Color.White;
            tarjeta5.Controls.Add(valor5);
            tarjeta5.Controls.Add(titulo5);
            tarjeta5.Location = new Point(0, 144);
            tarjeta5.Margin = new Padding(0, 0, 14, 14);
            tarjeta5.Name = "tarjeta5";
            tarjeta5.Padding = new Padding(16);
            tarjeta5.Size = new Size(210, 130);
            tarjeta5.TabIndex = 4;
            tarjeta5.Visible = false;
            // 
            // valor5
            // 
            valor5.AutoEllipsis = true;
            valor5.Dock = DockStyle.Bottom;
            valor5.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor5.Location = new Point(16, 66);
            valor5.Name = "valor5";
            valor5.Size = new Size(178, 48);
            valor5.TabIndex = 0;
            valor5.Text = "—";
            // 
            // titulo5
            // 
            titulo5.Dock = DockStyle.Top;
            titulo5.Location = new Point(16, 16);
            titulo5.Name = "titulo5";
            titulo5.Size = new Size(178, 52);
            titulo5.TabIndex = 1;
            titulo5.Text = "Indicador";
            // 
            // tarjeta6
            // 
            tarjeta6.BackColor = Color.White;
            tarjeta6.Controls.Add(valor6);
            tarjeta6.Controls.Add(titulo6);
            tarjeta6.Location = new Point(224, 144);
            tarjeta6.Margin = new Padding(0, 0, 14, 14);
            tarjeta6.Name = "tarjeta6";
            tarjeta6.Padding = new Padding(16);
            tarjeta6.Size = new Size(210, 130);
            tarjeta6.TabIndex = 5;
            tarjeta6.Visible = false;
            // 
            // valor6
            // 
            valor6.AutoEllipsis = true;
            valor6.Dock = DockStyle.Bottom;
            valor6.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor6.Location = new Point(16, 66);
            valor6.Name = "valor6";
            valor6.Size = new Size(178, 48);
            valor6.TabIndex = 0;
            valor6.Text = "—";
            // 
            // titulo6
            // 
            titulo6.Dock = DockStyle.Top;
            titulo6.Location = new Point(16, 16);
            titulo6.Name = "titulo6";
            titulo6.Size = new Size(178, 52);
            titulo6.TabIndex = 1;
            titulo6.Text = "Indicador";
            // 
            // tarjeta7
            // 
            tarjeta7.BackColor = Color.White;
            tarjeta7.Controls.Add(valor7);
            tarjeta7.Controls.Add(titulo7);
            tarjeta7.Location = new Point(448, 144);
            tarjeta7.Margin = new Padding(0, 0, 14, 14);
            tarjeta7.Name = "tarjeta7";
            tarjeta7.Padding = new Padding(16);
            tarjeta7.Size = new Size(210, 130);
            tarjeta7.TabIndex = 6;
            tarjeta7.Visible = false;
            // 
            // valor7
            // 
            valor7.AutoEllipsis = true;
            valor7.Dock = DockStyle.Bottom;
            valor7.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor7.Location = new Point(16, 66);
            valor7.Name = "valor7";
            valor7.Size = new Size(178, 48);
            valor7.TabIndex = 0;
            valor7.Text = "—";
            // 
            // titulo7
            // 
            titulo7.Dock = DockStyle.Top;
            titulo7.Location = new Point(16, 16);
            titulo7.Name = "titulo7";
            titulo7.Size = new Size(178, 52);
            titulo7.TabIndex = 1;
            titulo7.Text = "Indicador";
            // 
            // tarjeta8
            // 
            tarjeta8.BackColor = Color.White;
            tarjeta8.Controls.Add(valor8);
            tarjeta8.Controls.Add(titulo8);
            tarjeta8.Location = new Point(672, 144);
            tarjeta8.Margin = new Padding(0, 0, 14, 14);
            tarjeta8.Name = "tarjeta8";
            tarjeta8.Padding = new Padding(16);
            tarjeta8.Size = new Size(210, 130);
            tarjeta8.TabIndex = 7;
            tarjeta8.Visible = false;
            // 
            // valor8
            // 
            valor8.AutoEllipsis = true;
            valor8.Dock = DockStyle.Bottom;
            valor8.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            valor8.Location = new Point(16, 66);
            valor8.Name = "valor8";
            valor8.Size = new Size(178, 48);
            valor8.TabIndex = 0;
            valor8.Text = "—";
            // 
            // titulo8
            // 
            titulo8.Dock = DockStyle.Top;
            titulo8.Location = new Point(16, 16);
            titulo8.Name = "titulo8";
            titulo8.Size = new Size(178, 52);
            titulo8.TabIndex = 1;
            titulo8.Text = "Indicador";
            // 
            // labelAccesos
            // 
            labelAccesos.AutoSize = true;
            labelAccesos.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            labelAccesos.Location = new Point(0, 515);
            labelAccesos.Margin = new Padding(0, 20, 0, 16);
            labelAccesos.Name = "labelAccesos";
            labelAccesos.Size = new Size(303, 37);
            labelAccesos.TabIndex = 5;
            labelAccesos.Text = "Accesos a los módulos";
            // 
            // panelAccesos
            // 
            panelAccesos.AutoSize = true;
            panelAccesos.Controls.Add(accesoTrabajadores);
            panelAccesos.Controls.Add(accesoClientes);
            panelAccesos.Controls.Add(accesoInventario);
            panelAccesos.Controls.Add(accesoFacturacion);
            panelAccesos.Controls.Add(accesoContabilidad);
            panelAccesos.Controls.Add(accesoMiJornada);
            panelAccesos.Dock = DockStyle.Top;
            panelAccesos.Location = new Point(0, 568);
            panelAccesos.Margin = new Padding(0);
            panelAccesos.Name = "panelAccesos";
            panelAccesos.Size = new Size(940, 158);
            panelAccesos.TabIndex = 6;
            // 
            // accesoTrabajadores
            // 
            accesoTrabajadores.BackColor = Color.White;
            accesoTrabajadores.Cursor = Cursors.Hand;
            accesoTrabajadores.FlatAppearance.BorderColor = Color.FromArgb(171, 140, 57);
            accesoTrabajadores.FlatStyle = FlatStyle.Flat;
            accesoTrabajadores.Location = new Point(0, 0);
            accesoTrabajadores.Margin = new Padding(0, 0, 14, 14);
            accesoTrabajadores.Name = "accesoTrabajadores";
            accesoTrabajadores.Size = new Size(210, 65);
            accesoTrabajadores.TabIndex = 0;
            accesoTrabajadores.Text = "Trabajadores";
            accesoTrabajadores.UseVisualStyleBackColor = false;
            accesoTrabajadores.Click += Navegacion_Click;
            // 
            // accesoClientes
            // 
            accesoClientes.BackColor = Color.White;
            accesoClientes.Cursor = Cursors.Hand;
            accesoClientes.FlatAppearance.BorderColor = Color.FromArgb(171, 140, 57);
            accesoClientes.FlatStyle = FlatStyle.Flat;
            accesoClientes.Location = new Point(224, 0);
            accesoClientes.Margin = new Padding(0, 0, 14, 14);
            accesoClientes.Name = "accesoClientes";
            accesoClientes.Size = new Size(210, 65);
            accesoClientes.TabIndex = 1;
            accesoClientes.Text = "Clientes";
            accesoClientes.UseVisualStyleBackColor = false;
            accesoClientes.Click += Navegacion_Click;
            // 
            // accesoInventario
            // 
            accesoInventario.BackColor = Color.White;
            accesoInventario.Cursor = Cursors.Hand;
            accesoInventario.FlatAppearance.BorderColor = Color.FromArgb(171, 140, 57);
            accesoInventario.FlatStyle = FlatStyle.Flat;
            accesoInventario.Location = new Point(448, 0);
            accesoInventario.Margin = new Padding(0, 0, 14, 14);
            accesoInventario.Name = "accesoInventario";
            accesoInventario.Size = new Size(210, 65);
            accesoInventario.TabIndex = 2;
            accesoInventario.Text = "Inventario";
            accesoInventario.UseVisualStyleBackColor = false;
            accesoInventario.Click += Navegacion_Click;
            // 
            // accesoFacturacion
            // 
            accesoFacturacion.BackColor = Color.White;
            accesoFacturacion.Cursor = Cursors.Hand;
            accesoFacturacion.FlatAppearance.BorderColor = Color.FromArgb(171, 140, 57);
            accesoFacturacion.FlatStyle = FlatStyle.Flat;
            accesoFacturacion.Location = new Point(672, 0);
            accesoFacturacion.Margin = new Padding(0, 0, 14, 14);
            accesoFacturacion.Name = "accesoFacturacion";
            accesoFacturacion.Size = new Size(210, 65);
            accesoFacturacion.TabIndex = 3;
            accesoFacturacion.Text = "Facturación";
            accesoFacturacion.UseVisualStyleBackColor = false;
            accesoFacturacion.Click += Navegacion_Click;
            // 
            // accesoContabilidad
            // 
            accesoContabilidad.BackColor = Color.White;
            accesoContabilidad.Cursor = Cursors.Hand;
            accesoContabilidad.FlatAppearance.BorderColor = Color.FromArgb(171, 140, 57);
            accesoContabilidad.FlatStyle = FlatStyle.Flat;
            accesoContabilidad.Location = new Point(0, 79);
            accesoContabilidad.Margin = new Padding(0, 0, 14, 14);
            accesoContabilidad.Name = "accesoContabilidad";
            accesoContabilidad.Size = new Size(210, 65);
            accesoContabilidad.TabIndex = 4;
            accesoContabilidad.Text = "Contabilidad";
            accesoContabilidad.UseVisualStyleBackColor = false;
            accesoContabilidad.Click += Navegacion_Click;
            // 
            // accesoMiJornada
            // 
            accesoMiJornada.BackColor = Color.White;
            accesoMiJornada.Cursor = Cursors.Hand;
            accesoMiJornada.FlatAppearance.BorderColor = Color.FromArgb(171, 140, 57);
            accesoMiJornada.FlatStyle = FlatStyle.Flat;
            accesoMiJornada.Location = new Point(224, 79);
            accesoMiJornada.Margin = new Padding(0, 0, 14, 14);
            accesoMiJornada.Name = "accesoMiJornada";
            accesoMiJornada.Size = new Size(210, 65);
            accesoMiJornada.TabIndex = 5;
            accesoMiJornada.Text = "Mi jornada";
            accesoMiJornada.UseVisualStyleBackColor = false;
            accesoMiJornada.Click += Navegacion_Click;
            // 
            // Home
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(246, 247, 249);
            ClientSize = new Size(1264, 781);
            Controls.Add(layoutPrincipal);
            Font = new Font("Segoe UI", 10F);
            MinimumSize = new Size(1000, 700);
            Name = "Home";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Giovanny Barbers · Gestión de barbería";
            Load += Home_Load;
            layoutPrincipal.ResumeLayout(false);
            panelLateral.ResumeLayout(false);
            panelMenu.ResumeLayout(false);
            panelSesion.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            contenido.ResumeLayout(false);
            panelInicio.ResumeLayout(false);
            panelInicio.PerformLayout();
            filasInicio.ResumeLayout(false);
            filasInicio.PerformLayout();
            panelTarjetas.ResumeLayout(false);
            tarjeta1.ResumeLayout(false);
            tarjeta2.ResumeLayout(false);
            tarjeta3.ResumeLayout(false);
            tarjeta4.ResumeLayout(false);
            tarjeta5.ResumeLayout(false);
            tarjeta6.ResumeLayout(false);
            tarjeta7.ResumeLayout(false);
            tarjeta8.ResumeLayout(false);
            panelAccesos.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion
        private TableLayoutPanel layoutPrincipal;
        private TableLayoutPanel panelLateral;
        private FlowLayoutPanel panelMenu;
        private Button buttonInicio;
        private Button buttonTrabajadores;
        private Button buttonClientes;
        private Button buttonInventario;
        private Button buttonFacturacion;
        private Button buttonContabilidad;
        private Button buttonMiJornada;
        private FlowLayoutPanel panelSesion;
        private Label labelUsuario;
        private Label labelRol;
        private Button buttonCerrarSesion;
        private Panel contenido;
        private Panel panelInicio;
        private TableLayoutPanel filasInicio;
        private Label labelTitulo;
        private Label labelBienvenida;
        private Label labelEstadoResumen;
        private Button buttonActualizar;
        private FlowLayoutPanel panelTarjetas;
        private Panel tarjeta1;
        private Label valor1;
        private Label titulo1;
        private Panel tarjeta2;
        private Label valor2;
        private Label titulo2;
        private Panel tarjeta3;
        private Label valor3;
        private Label titulo3;
        private Panel tarjeta4;
        private Label valor4;
        private Label titulo4;
        private Panel tarjeta5;
        private Label valor5;
        private Label titulo5;
        private Panel tarjeta6;
        private Label valor6;
        private Label titulo6;
        private Panel tarjeta7;
        private Label valor7;
        private Label titulo7;
        private Panel tarjeta8;
        private Label valor8;
        private Label titulo8;
        private Label labelAccesos;
        private FlowLayoutPanel panelAccesos;
        private Button accesoTrabajadores;
        private Button accesoClientes;
        private Button accesoInventario;
        private Button accesoFacturacion;
        private Button accesoContabilidad;
        private Button accesoMiJornada;
        private PictureBox pictureBox1;
    }
}
