using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Giovanny_Barbers
{
    public partial class Facturacion : Form
    {
        // Tabla temporal para guardar los productos y servicios
        // antes de registrar la factura en SQL Server.
        private DataTable detalleFactura = new DataTable();

        public Facturacion()
        {
            InitializeComponent();

            // Eventos
            Load += Facturacion_Load;

            cmbTipo.SelectedIndexChanged += cmbTipo_SelectedIndexChanged;
            cmbArticulo.SelectedIndexChanged += cmbArticulo_SelectedIndexChanged;

            btnAgregar.Click += btnAgregar_Click;
            btnNuevaFactura.Click += btnNuevaFactura_Click;
            btnGenerarFactura.Click += btnGenerarFactura_Click;

            dgvDetalle.CellContentClick += dgvDetalle_CellContentClick;
        }


        // ==========================================================
        // CARGA INICIAL
        // ==========================================================

        private void Facturacion_Load(object? sender, EventArgs e)
        {
            ConfigurarDetalle();

            CargarClientes();
            CargarTrabajadores();

            cmbTipo.Items.Clear();
            cmbTipo.Items.Add("Producto");
            cmbTipo.Items.Add("Servicio");

            cmbTipo.SelectedIndex = -1;
            cmbArticulo.DataSource = null;

            txtPrecio.Clear();

            nudCantidad.Minimum = 1;
            nudCantidad.Maximum = 1000;
            nudCantidad.Value = 1;

            ActualizarTotales();
        }


        // ==========================================================
        // CONFIGURAR DATAGRIDVIEW
        // ==========================================================

        private void ConfigurarDetalle()
        {
            detalleFactura = new DataTable();

            detalleFactura.Columns.Add("Tipo", typeof(string));
            detalleFactura.Columns.Add("IdItem", typeof(int));
            detalleFactura.Columns.Add("Articulo", typeof(string));
            detalleFactura.Columns.Add("Cantidad", typeof(int));
            detalleFactura.Columns.Add("Precio", typeof(decimal));
            detalleFactura.Columns.Add("Subtotal", typeof(decimal));

            dgvDetalle.DataSource = detalleFactura;

            dgvDetalle.ReadOnly = true;
            dgvDetalle.AllowUserToAddRows = false;
            dgvDetalle.AllowUserToDeleteRows = false;
            dgvDetalle.MultiSelect = false;
            dgvDetalle.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvDetalle.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            // Ocultamos el ID porque solamente lo usamos internamente.
            if (dgvDetalle.Columns["IdItem"] != null)
            {
                dgvDetalle.Columns["IdItem"].Visible = false;
            }

            // Formato del precio
            if (dgvDetalle.Columns["Precio"] != null)
            {
                dgvDetalle.Columns["Precio"].DefaultCellStyle.Format = "N2";
            }

            if (dgvDetalle.Columns["Subtotal"] != null)
            {
                dgvDetalle.Columns["Subtotal"].DefaultCellStyle.Format = "N2";
            }

            // Botón eliminar
            if (!dgvDetalle.Columns.Contains("Eliminar"))
            {
                DataGridViewButtonColumn botonEliminar =
                    new DataGridViewButtonColumn();

                botonEliminar.Name = "Eliminar";
                botonEliminar.HeaderText = "Acción";
                botonEliminar.Text = "Eliminar";
                botonEliminar.UseColumnTextForButtonValue = true;

                dgvDetalle.Columns.Add(botonEliminar);
            }
        }


        // ==========================================================
        // CARGAR CLIENTES
        // ==========================================================

        private void CargarClientes()
        {
            try
            {
                using SqlConnection conexion =
                    ConexionBD.ObtenerConexion();

                conexion.Open();

                string consulta = @"
                    SELECT
                        id_cliente,
                        nombres
                    FROM Cliente
                    ORDER BY nombres;
                ";

                SqlDataAdapter adaptador =
                    new SqlDataAdapter(consulta, conexion);

                DataTable tabla = new DataTable();

                adaptador.Fill(tabla);

                cmbCliente.DataSource = tabla;
                cmbCliente.DisplayMember = "nombres";
                cmbCliente.ValueMember = "id_cliente";

                cmbCliente.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar los clientes:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ==========================================================
        // CARGAR TRABAJADORES
        // ==========================================================

        private void CargarTrabajadores()
        {
            try
            {
                using SqlConnection conexion =
                    ConexionBD.ObtenerConexion();

                conexion.Open();

                string consulta = @"
                    SELECT
                        id_trabajador,
                        nombres + ' ' + apellidos AS nombre_completo
                    FROM Trabajador
                    ORDER BY nombres, apellidos;
                ";

                SqlDataAdapter adaptador =
                    new SqlDataAdapter(consulta, conexion);

                DataTable tabla = new DataTable();

                adaptador.Fill(tabla);

                cmbTrabajador.DataSource = tabla;
                cmbTrabajador.DisplayMember = "nombre_completo";
                cmbTrabajador.ValueMember = "id_trabajador";

                cmbTrabajador.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar los trabajadores:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ==========================================================
        // CAMBIAR ENTRE PRODUCTO Y SERVICIO
        // ==========================================================

        private void cmbTipo_SelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            txtPrecio.Clear();

            if (cmbTipo.SelectedIndex == -1)
            {
                cmbArticulo.DataSource = null;
                return;
            }

            if (cmbTipo.Text == "Producto")
            {
                CargarProductos();
            }
            else if (cmbTipo.Text == "Servicio")
            {
                CargarServicios();
            }
        }


        // ==========================================================
        // CARGAR PRODUCTOS
        // ==========================================================

        private void CargarProductos()
        {
            try
            {
                using SqlConnection conexion =
                    ConexionBD.ObtenerConexion();

                conexion.Open();

                string consulta = @"
                    SELECT
                        id_producto AS id,
                        nombre,
                        precio_venta AS precio,
                        stock
                    FROM Producto
                    ORDER BY nombre;
                ";

                SqlDataAdapter adaptador =
                    new SqlDataAdapter(consulta, conexion);

                DataTable tabla = new DataTable();

                adaptador.Fill(tabla);

                cmbArticulo.DataSource = tabla;
                cmbArticulo.DisplayMember = "nombre";
                cmbArticulo.ValueMember = "id";

                cmbArticulo.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar los productos:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ==========================================================
        // CARGAR SERVICIOS
        // ==========================================================

        private void CargarServicios()
        {
            try
            {
                using SqlConnection conexion =
                    ConexionBD.ObtenerConexion();

                conexion.Open();

                string consulta = @"
                    SELECT
                        id_servicio AS id,
                        nombre,
                        precio
                    FROM Servicio
                    ORDER BY nombre;
                ";

                SqlDataAdapter adaptador =
                    new SqlDataAdapter(consulta, conexion);

                DataTable tabla = new DataTable();

                adaptador.Fill(tabla);

                cmbArticulo.DataSource = tabla;
                cmbArticulo.DisplayMember = "nombre";
                cmbArticulo.ValueMember = "id";

                cmbArticulo.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar los servicios:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ==========================================================
        // MOSTRAR PRECIO DEL ARTÍCULO SELECCIONADO
        // ==========================================================

        private void cmbArticulo_SelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            if (cmbArticulo.SelectedIndex == -1)
            {
                txtPrecio.Clear();
                return;
            }

            if (cmbArticulo.SelectedItem is DataRowView fila)
            {
                decimal precio =
                    Convert.ToDecimal(fila["precio"]);

                txtPrecio.Text = precio.ToString("0.00");
            }
        }


        // ==========================================================
        // AGREGAR AL DETALLE
        // ==========================================================

        private void btnAgregar_Click(object? sender, EventArgs e)
        {
            if (cmbTipo.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Seleccione si desea agregar un producto o servicio."
                );

                return;
            }

            if (cmbArticulo.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Seleccione un artículo."
                );

                return;
            }

            int cantidad =
                Convert.ToInt32(nudCantidad.Value);

            if (cantidad <= 0)
            {
                MessageBox.Show(
                    "La cantidad debe ser mayor que cero."
                );

                return;
            }

            string tipo = cmbTipo.Text;

            int idItem =
                Convert.ToInt32(cmbArticulo.SelectedValue);

            string nombre =
                cmbArticulo.Text;

            decimal precio =
                Convert.ToDecimal(txtPrecio.Text);

            // Si es producto comprobamos existencia.
            if (tipo == "Producto")
            {
                int cantidadActualDetalle =
                    ObtenerCantidadProductoDetalle(idItem);

                int cantidadTotal =
                    cantidadActualDetalle + cantidad;

                int stockDisponible =
                    ObtenerStockProducto(idItem);

                if (cantidadTotal > stockDisponible)
                {
                    MessageBox.Show(
                        "No existe suficiente stock.\n\n" +
                        "Producto: " + nombre +
                        "\nStock disponible: " + stockDisponible +
                        "\nCantidad solicitada: " + cantidadTotal,
                        "Stock insuficiente",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }
            }


            // Verificamos si ya se agregó el mismo artículo.
            DataRow? filaExistente = null;

            foreach (DataRow fila in detalleFactura.Rows)
            {
                if (fila["Tipo"].ToString() == tipo &&
                    Convert.ToInt32(fila["IdItem"]) == idItem)
                {
                    filaExistente = fila;
                    break;
                }
            }


            if (filaExistente != null)
            {
                int nuevaCantidad =
                    Convert.ToInt32(
                        filaExistente["Cantidad"]
                    ) + cantidad;

                filaExistente["Cantidad"] =
                    nuevaCantidad;

                filaExistente["Subtotal"] =
                    nuevaCantidad * precio;
            }
            else
            {
                decimal subtotal =
                    cantidad * precio;

                detalleFactura.Rows.Add(
                    tipo,
                    idItem,
                    nombre,
                    cantidad,
                    precio,
                    subtotal
                );
            }


            ActualizarTotales();

            cmbTipo.SelectedIndex = -1;
            cmbArticulo.DataSource = null;
            txtPrecio.Clear();
            nudCantidad.Value = 1;
        }


        // ==========================================================
        // OBTENER STOCK REAL
        // ==========================================================

        private int ObtenerStockProducto(int idProducto)
        {
            using SqlConnection conexion =
                ConexionBD.ObtenerConexion();

            conexion.Open();

            string consulta = @"
                SELECT stock
                FROM Producto
                WHERE id_producto = @id_producto;
            ";

            using SqlCommand comando =
                new SqlCommand(consulta, conexion);

            comando.Parameters.AddWithValue(
                "@id_producto",
                idProducto
            );

            object? resultado =
                comando.ExecuteScalar();

            if (resultado == null ||
                resultado == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(resultado);
        }


        // ==========================================================
        // CANTIDAD DEL MISMO PRODUCTO YA AGREGADA
        // ==========================================================

        private int ObtenerCantidadProductoDetalle(int idProducto)
        {
            foreach (DataRow fila in detalleFactura.Rows)
            {
                if (fila["Tipo"].ToString() == "Producto" &&
                    Convert.ToInt32(fila["IdItem"]) == idProducto)
                {
                    return Convert.ToInt32(
                        fila["Cantidad"]
                    );
                }
            }

            return 0;
        }


        // ==========================================================
        // CALCULAR SUBTOTAL Y TOTAL
        // ==========================================================

        private decimal ObtenerTotal()
        {
            decimal total = 0;

            foreach (DataRow fila in detalleFactura.Rows)
            {
                total +=
                    Convert.ToDecimal(
                        fila["Subtotal"]
                    );
            }

            return total;
        }


        private void ActualizarTotales()
        {
            decimal subtotal =
                ObtenerTotal();

            // Actualmente no hay IVA ni descuento separado
            // en el modelo de la factura.
            decimal total = subtotal;

            lblSubtotal.Text =
                "Q " + subtotal.ToString("0.00");

            lblTotal.Text =
                "Q " + total.ToString("0.00");
        }


        // ==========================================================
        // ELIMINAR ARTÍCULO DEL DETALLE
        // ==========================================================

        private void dgvDetalle_CellContentClick(
            object? sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (dgvDetalle.Columns[e.ColumnIndex].Name == "Eliminar")
            {
                DialogResult respuesta =
                    MessageBox.Show(
                        "¿Desea quitar este artículo de la factura?",
                        "Eliminar",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                if (respuesta == DialogResult.Yes)
                {
                    detalleFactura.Rows[e.RowIndex].Delete();

                    ActualizarTotales();
                }
            }
        }


        // ==========================================================
        // NUEVA FACTURA
        // ==========================================================

        private void btnNuevaFactura_Click(
            object? sender,
            EventArgs e)
        {
            NuevaFactura();
        }


        private void NuevaFactura()
        {
            detalleFactura.Clear();

            cmbCliente.SelectedIndex = -1;
            cmbTrabajador.SelectedIndex = -1;

            cmbTipo.SelectedIndex = -1;
            cmbArticulo.DataSource = null;

            txtPrecio.Clear();

            nudCantidad.Value = 1;

            ActualizarTotales();
        }


        // ==========================================================
        // GENERAR FACTURA
        // ==========================================================

        private void btnGenerarFactura_Click(
            object? sender,
            EventArgs e)
        {
            if (cmbCliente.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Seleccione un cliente.",
                    "Facturación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            if (cmbTrabajador.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Seleccione un trabajador.",
                    "Facturación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            if (detalleFactura.Rows.Count == 0)
            {
                MessageBox.Show(
                    "Debe agregar al menos un producto o servicio.",
                    "Facturación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            int idCliente =
                Convert.ToInt32(
                    cmbCliente.SelectedValue
                );

            int idTrabajador =
                Convert.ToInt32(
                    cmbTrabajador.SelectedValue
                );

            decimal subtotal =
                ObtenerTotal();

            decimal total = subtotal;


            using SqlConnection conexion =
                ConexionBD.ObtenerConexion();

            conexion.Open();

            // Todo se guarda junto.
            SqlTransaction transaccion =
                conexion.BeginTransaction();


            try
            {
                // ==================================================
                // INSERTAR FACTURA
                // ==================================================

                string consultaFactura = @"
                    INSERT INTO Factura
                    (
                        id_cliente,
                        id_trabajador,
                        fecha,
                        subtotal,
                        total,
                        estado
                    )
                    OUTPUT INSERTED.id_factura
                    VALUES
                    (
                        @id_cliente,
                        @id_trabajador,
                        SYSDATETIME(),
                        @subtotal,
                        @total,
                        @estado
                    );
                ";


                int idFactura;

                using (
                    SqlCommand comandoFactura =
                    new SqlCommand(
                        consultaFactura,
                        conexion,
                        transaccion
                    )
                )
                {
                    comandoFactura.Parameters.AddWithValue(
                        "@id_cliente",
                        idCliente
                    );

                    comandoFactura.Parameters.AddWithValue(
                        "@id_trabajador",
                        idTrabajador
                    );

                    comandoFactura.Parameters.AddWithValue(
                        "@subtotal",
                        subtotal
                    );

                    comandoFactura.Parameters.AddWithValue(
                        "@total",
                        total
                    );

                    comandoFactura.Parameters.AddWithValue(
                        "@estado",
                        "Emitida"
                    );

                    idFactura =
                        Convert.ToInt32(
                            comandoFactura.ExecuteScalar()
                        );
                }


                // ==================================================
                // GUARDAR DETALLES
                // ==================================================

                foreach (DataRow fila in detalleFactura.Rows)
                {
                    string tipo =
                        fila["Tipo"].ToString()!;

                    int idItem =
                        Convert.ToInt32(
                            fila["IdItem"]
                        );

                    int cantidad =
                        Convert.ToInt32(
                            fila["Cantidad"]
                        );

                    decimal precio =
                        Convert.ToDecimal(
                            fila["Precio"]
                        );

                    decimal subtotalDetalle =
                        Convert.ToDecimal(
                            fila["Subtotal"]
                        );


                    if (tipo == "Producto")
                    {
                        GuardarDetalleProducto(
                            conexion,
                            transaccion,
                            idFactura,
                            idItem,
                            cantidad,
                            precio,
                            subtotalDetalle
                        );
                    }
                    else
                    {
                        GuardarDetalleServicio(
                            conexion,
                            transaccion,
                            idFactura,
                            idItem,
                            cantidad,
                            precio,
                            subtotalDetalle
                        );
                    }
                }


                transaccion.Commit();


                MessageBox.Show(
                    "Factura generada correctamente.\n\n" +
                    "Factura No. " + idFactura +
                    "\nTotal: Q " + total.ToString("0.00"),
                    "Factura generada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );


                NuevaFactura();
            }
            catch (Exception ex)
            {
                try
                {
                    transaccion.Rollback();
                }
                catch
                {
                    // Evita un segundo error si la conexión
                    // ya se cerró.
                }


                MessageBox.Show(
                    "No se pudo generar la factura:\n\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ==========================================================
        // GUARDAR DETALLE DE PRODUCTO
        // ==========================================================

        private void GuardarDetalleProducto(
            SqlConnection conexion,
            SqlTransaction transaccion,
            int idFactura,
            int idProducto,
            int cantidad,
            decimal precio,
            decimal subtotal)
        {
            // Bloqueamos el producto mientras verificamos stock.
            string consultaStock = @"
                SELECT stock
                FROM Producto WITH (UPDLOCK, ROWLOCK)
                WHERE id_producto = @id_producto;
            ";


            int stockActual;

            using (
                SqlCommand comandoStock =
                new SqlCommand(
                    consultaStock,
                    conexion,
                    transaccion
                )
            )
            {
                comandoStock.Parameters.AddWithValue(
                    "@id_producto",
                    idProducto
                );

                object? resultado =
                    comandoStock.ExecuteScalar();

                if (resultado == null ||
                    resultado == DBNull.Value)
                {
                    throw new Exception(
                        "Uno de los productos ya no existe."
                    );
                }

                stockActual =
                    Convert.ToInt32(resultado);
            }


            if (cantidad > stockActual)
            {
                throw new Exception(
                    "No existe suficiente stock para uno " +
                    "de los productos.\n" +
                    "Stock disponible: " + stockActual
                );
            }


            // ------------------------------------------------------
            // GUARDAR DETALLE
            // ------------------------------------------------------

            string consultaDetalle = @"
                INSERT INTO Detalle_Producto
                (
                    id_factura,
                    id_producto,
                    cantidad,
                    precio_unitario,
                    subtotal
                )
                VALUES
                (
                    @id_factura,
                    @id_producto,
                    @cantidad,
                    @precio,
                    @subtotal
                );
            ";


            using (
                SqlCommand comandoDetalle =
                new SqlCommand(
                    consultaDetalle,
                    conexion,
                    transaccion
                )
            )
            {
                comandoDetalle.Parameters.AddWithValue(
                    "@id_factura",
                    idFactura
                );

                comandoDetalle.Parameters.AddWithValue(
                    "@id_producto",
                    idProducto
                );

                comandoDetalle.Parameters.AddWithValue(
                    "@cantidad",
                    cantidad
                );

                comandoDetalle.Parameters.AddWithValue(
                    "@precio",
                    precio
                );

                comandoDetalle.Parameters.AddWithValue(
                    "@subtotal",
                    subtotal
                );

                comandoDetalle.ExecuteNonQuery();
            }


            // ------------------------------------------------------
            // DESCONTAR STOCK
            // ------------------------------------------------------

            string consultaStockUpdate = @"
                UPDATE Producto
                SET stock = stock - @cantidad
                WHERE id_producto = @id_producto;
            ";


            using (
                SqlCommand comandoUpdate =
                new SqlCommand(
                    consultaStockUpdate,
                    conexion,
                    transaccion
                )
            )
            {
                comandoUpdate.Parameters.AddWithValue(
                    "@cantidad",
                    cantidad
                );

                comandoUpdate.Parameters.AddWithValue(
                    "@id_producto",
                    idProducto
                );

                comandoUpdate.ExecuteNonQuery();
            }


            // ------------------------------------------------------
            // REGISTRAR MOVIMIENTO DE INVENTARIO
            // ------------------------------------------------------

            string consultaMovimiento = @"
                INSERT INTO Movimiento_Inventario
                (
                    id_producto,
                    tipo,
                    cantidad,
                    fecha,
                    motivo
                )
                VALUES
                (
                    @id_producto,
                    'Salida',
                    @cantidad,
                    SYSDATETIME(),
                    @motivo
                );
            ";


            using (
                SqlCommand comandoMovimiento =
                new SqlCommand(
                    consultaMovimiento,
                    conexion,
                    transaccion
                )
            )
            {
                comandoMovimiento.Parameters.AddWithValue(
                    "@id_producto",
                    idProducto
                );

                comandoMovimiento.Parameters.AddWithValue(
                    "@cantidad",
                    cantidad
                );

                comandoMovimiento.Parameters.AddWithValue(
                    "@motivo",
                    "Venta - Factura No. " + idFactura
                );

                comandoMovimiento.ExecuteNonQuery();
            }
        }


        // ==========================================================
        // GUARDAR DETALLE DE SERVICIO
        // ==========================================================

        private void GuardarDetalleServicio(
            SqlConnection conexion,
            SqlTransaction transaccion,
            int idFactura,
            int idServicio,
            int cantidad,
            decimal precio,
            decimal subtotal)
        {
            string consulta = @"
                INSERT INTO Detalle_Servicio
                (
                    id_factura,
                    id_servicio,
                    cantidad,
                    precio_unitario,
                    subtotal
                )
                VALUES
                (
                    @id_factura,
                    @id_servicio,
                    @cantidad,
                    @precio,
                    @subtotal
                );
            ";


            using (
                SqlCommand comando =
                new SqlCommand(
                    consulta,
                    conexion,
                    transaccion
                )
            )
            {
                comando.Parameters.AddWithValue(
                    "@id_factura",
                    idFactura
                );

                comando.Parameters.AddWithValue(
                    "@id_servicio",
                    idServicio
                );

                comando.Parameters.AddWithValue(
                    "@cantidad",
                    cantidad
                );

                comando.Parameters.AddWithValue(
                    "@precio",
                    precio
                );

                comando.Parameters.AddWithValue(
                    "@subtotal",
                    subtotal
                );

                comando.ExecuteNonQuery();
            }
        }
        private void label7_Click(object sender, EventArgs e)
        {
        }
    }

}