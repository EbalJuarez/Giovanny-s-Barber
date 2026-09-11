using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Giovanny_Barbers
{
    public partial class Inventario : Form
    {
        // Guarda el ID del producto seleccionado en el DataGridView.
        // Si vale 0 significa que no hay ningún producto seleccionado.
        private int idProductoSeleccionado = 0;

        public Inventario()
        {
            InitializeComponent();

            // Eventos del formulario
            Load += Inventario_Load;

            // Eventos de botones
            btnRegistrar.Click += btnRegistrar_Click;
            btnActualizar.Click += btnActualizar_Click;
            btnLimpiar.Click += btnLimpiar_Click;

            btnEntrada.Click += btnEntrada_Click;
            btnSalida.Click += btnSalida_Click;

            // Eventos de búsqueda y tabla
            txtBuscar.TextChanged += txtBuscar_TextChanged;
            dgvProductos.CellClick += dgvProductos_CellClick;
        }


        // =========================================================
        // CARGA INICIAL
        // =========================================================

        private void Inventario_Load(object? sender, EventArgs e)
        {
            ConfigurarTabla();

            CargarCategorias();
            CargarProductos();
            CargarProductosMovimiento();
        }


        // =========================================================
        // CONFIGURACIÓN DEL DATAGRIDVIEW
        // =========================================================

        private void ConfigurarTabla()
        {
            dgvProductos.ReadOnly = true;
            dgvProductos.AllowUserToAddRows = false;
            dgvProductos.AllowUserToDeleteRows = false;
            dgvProductos.MultiSelect = false;
            dgvProductos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProductos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }


        // =========================================================
        // CARGAR CATEGORÍAS
        // =========================================================

        private void CargarCategorias()
        {
            try
            {
                using (SqlConnection conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        SELECT id_categoria, nombre
                        FROM categoria
                        ORDER BY nombre;
                    ";

                    SqlDataAdapter adaptador =
                        new SqlDataAdapter(consulta, conexion);

                    DataTable tabla = new DataTable();

                    adaptador.Fill(tabla);

                    cmbCategoria.DataSource = tabla;
                    cmbCategoria.DisplayMember = "nombre";
                    cmbCategoria.ValueMember = "id_categoria";

                    cmbCategoria.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar las categorías:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // CARGAR PRODUCTOS EN LA TABLA
        // =========================================================

        private void CargarProductos()
        {
            try
            {
                using (SqlConnection conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        SELECT
                            p.id_producto AS ID,
                            p.nombre AS Producto,
                            c.nombre AS Categoria,
                            p.precio_venta AS Precio,
                            p.stock AS Stock,
                            p.stock_minimo AS StockMinimo,
                            p.id_categoria
                        FROM producto AS p
                        INNER JOIN categoria AS c
                            ON p.id_categoria = c.id_categoria
                        ORDER BY p.nombre;
                    ";

                    SqlDataAdapter adaptador =
                        new SqlDataAdapter(consulta, conexion);

                    DataTable tabla = new DataTable();

                    adaptador.Fill(tabla);

                    dgvProductos.DataSource = tabla;

                    // Ocultamos el id de categoría porque solamente
                    // lo usamos internamente al seleccionar un producto.
                    if (dgvProductos.Columns["id_categoria"] != null)
                    {
                        dgvProductos.Columns["id_categoria"].Visible = false;
                    }

                    dgvProductos.ClearSelection();
                }
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


        // =========================================================
        // CARGAR PRODUCTOS PARA ENTRADAS Y SALIDAS
        // =========================================================

        private void CargarProductosMovimiento()
        {
            try
            {
                using (SqlConnection conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        SELECT id_producto, nombre
                        FROM producto
                        ORDER BY nombre;
                    ";

                    SqlDataAdapter adaptador =
                        new SqlDataAdapter(consulta, conexion);

                    DataTable tabla = new DataTable();

                    adaptador.Fill(tabla);

                    cmbProductoMovimiento.DataSource = tabla;
                    cmbProductoMovimiento.DisplayMember = "nombre";
                    cmbProductoMovimiento.ValueMember = "id_producto";

                    cmbProductoMovimiento.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar los productos para movimientos:\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // REGISTRAR PRODUCTO
        // =========================================================

        private void btnRegistrar_Click(object? sender, EventArgs e)
        {
            if (!ValidarProducto())
            {
                return;
            }

            try
            {
                using (SqlConnection conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        INSERT INTO producto
                        (
                            id_categoria,
                            nombre,
                            precio_venta,
                            stock,
                            stock_minimo
                        )
                        VALUES
                        (
                            @id_categoria,
                            @nombre,
                            @precio,
                            @stock,
                            @stock_minimo
                        );
                    ";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@id_categoria",
                            Convert.ToInt32(cmbCategoria.SelectedValue)
                        );

                        comando.Parameters.AddWithValue(
                            "@nombre",
                            txtNombre.Text.Trim()
                        );

                        comando.Parameters.AddWithValue(
                            "@precio",
                            nudPrecio.Value
                        );

                        comando.Parameters.AddWithValue(
                            "@stock",
                            Convert.ToInt32(nudStock.Value)
                        );

                        comando.Parameters.AddWithValue(
                            "@stock_minimo",
                            Convert.ToInt32(nudStockMinimo.Value)
                        );

                        comando.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "Producto registrado correctamente.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                LimpiarCampos();

                CargarProductos();
                CargarProductosMovimiento();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al registrar el producto:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // VALIDAR DATOS DEL PRODUCTO
        // =========================================================

        private bool ValidarProducto()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show(
                    "Ingrese el nombre del producto.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                txtNombre.Focus();

                return false;
            }

            if (cmbCategoria.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Seleccione una categoría.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            if (nudPrecio.Value <= 0)
            {
                MessageBox.Show(
                    "El precio debe ser mayor que cero.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            if (nudStock.Value < 0)
            {
                MessageBox.Show(
                    "El stock no puede ser negativo.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            if (nudStockMinimo.Value < 0)
            {
                MessageBox.Show(
                    "El stock mínimo no puede ser negativo.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            return true;
        }


        // =========================================================
        // SELECCIONAR PRODUCTO DE LA TABLA
        // =========================================================

        private void dgvProductos_CellClick(
            object? sender,
            DataGridViewCellEventArgs e)
        {
            // Si se hace clic en el encabezado, no hacemos nada.
            if (e.RowIndex < 0)
            {
                return;
            }

            DataGridViewRow fila =
                dgvProductos.Rows[e.RowIndex];

            idProductoSeleccionado =
                Convert.ToInt32(fila.Cells["ID"].Value);

            txtNombre.Text =
                fila.Cells["Producto"].Value?.ToString() ?? "";

            nudPrecio.Value =
                Convert.ToDecimal(fila.Cells["Precio"].Value);

            nudStock.Value =
                Convert.ToDecimal(fila.Cells["Stock"].Value);

            nudStockMinimo.Value =
                Convert.ToDecimal(fila.Cells["StockMinimo"].Value);

            cmbCategoria.SelectedValue =
                Convert.ToInt32(fila.Cells["id_categoria"].Value);
        }


        // =========================================================
        // ACTUALIZAR PRODUCTO
        // =========================================================

        private void btnActualizar_Click(object? sender, EventArgs e)
        {
            if (idProductoSeleccionado == 0)
            {
                MessageBox.Show(
                    "Seleccione primero un producto de la tabla.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            if (!ValidarProducto())
            {
                return;
            }

            try
            {
                using (SqlConnection conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        UPDATE producto
                        SET
                            id_categoria = @id_categoria,
                            nombre = @nombre,
                            precio_venta = @precio,
                            stock = @stock,
                            stock_minimo = @stock_minimo
                        WHERE id_producto = @id_producto;
                    ";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@id_categoria",
                            Convert.ToInt32(cmbCategoria.SelectedValue)
                        );

                        comando.Parameters.AddWithValue(
                            "@nombre",
                            txtNombre.Text.Trim()
                        );

                        comando.Parameters.AddWithValue(
                            "@precio",
                            nudPrecio.Value
                        );

                        comando.Parameters.AddWithValue(
                            "@stock",
                            Convert.ToInt32(nudStock.Value)
                        );

                        comando.Parameters.AddWithValue(
                            "@stock_minimo",
                            Convert.ToInt32(nudStockMinimo.Value)
                        );

                        comando.Parameters.AddWithValue(
                            "@id_producto",
                            idProductoSeleccionado
                        );

                        int filasAfectadas =
                            comando.ExecuteNonQuery();

                        if (filasAfectadas == 0)
                        {
                            MessageBox.Show(
                                "No se encontró el producto.",
                                "Inventario",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );

                            return;
                        }
                    }
                }

                MessageBox.Show(
                    "Producto actualizado correctamente.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                LimpiarCampos();

                CargarProductos();
                CargarProductosMovimiento();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al actualizar el producto:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // LIMPIAR CAMPOS
        // =========================================================

        private void btnLimpiar_Click(object? sender, EventArgs e)
        {
            LimpiarCampos();
        }


        private void LimpiarCampos()
        {
            idProductoSeleccionado = 0;

            txtNombre.Clear();

            cmbCategoria.SelectedIndex = -1;

            nudPrecio.Value = nudPrecio.Minimum;
            nudStock.Value = nudStock.Minimum;
            nudStockMinimo.Value = nudStockMinimo.Minimum;

            dgvProductos.ClearSelection();

            txtNombre.Focus();
        }


        // =========================================================
        // BUSCAR PRODUCTOS
        // =========================================================

        private void txtBuscar_TextChanged(object? sender, EventArgs e)
        {
            BuscarProductos();
        }


        private void BuscarProductos()
        {
            try
            {
                using (SqlConnection conexion = ConexionBD.ObtenerConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        SELECT
                            p.id_producto AS ID,
                            p.nombre AS Producto,
                            c.nombre AS Categoria,
                            p.precio_venta AS Precio,
                            p.stock AS Stock,
                            p.stock_minimo AS StockMinimo,
                            p.id_categoria
                        FROM producto AS p
                        INNER JOIN categoria AS c
                            ON p.id_categoria = c.id_categoria
                        WHERE
                            p.nombre LIKE @buscar
                            OR c.nombre LIKE @buscar
                        ORDER BY p.nombre;
                    ";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@buscar",
                            "%" + txtBuscar.Text.Trim() + "%"
                        );

                        SqlDataAdapter adaptador =
                            new SqlDataAdapter(comando);

                        DataTable tabla = new DataTable();

                        adaptador.Fill(tabla);

                        dgvProductos.DataSource = tabla;

                        if (dgvProductos.Columns["id_categoria"] != null)
                        {
                            dgvProductos.Columns["id_categoria"].Visible = false;
                        }

                        dgvProductos.ClearSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al buscar productos:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // REGISTRAR ENTRADA
        // =========================================================

        private void btnEntrada_Click(object? sender, EventArgs e)
        {
            RegistrarMovimiento("ENTRADA");
        }


        // =========================================================
        // REGISTRAR SALIDA
        // =========================================================

        private void btnSalida_Click(object? sender, EventArgs e)
        {
            RegistrarMovimiento("SALIDA");
        }


        // =========================================================
        // MÉTODO PARA ENTRADAS Y SALIDAS
        // =========================================================

        private void RegistrarMovimiento(string tipo)
        {
            if (cmbProductoMovimiento.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Seleccione un producto.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            int cantidad =
                Convert.ToInt32(nudCantidadMovimiento.Value);

            if (cantidad <= 0)
            {
                MessageBox.Show(
                    "La cantidad debe ser mayor que cero.",
                    "Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            int idProducto =
                Convert.ToInt32(
                    cmbProductoMovimiento.SelectedValue
                );

            using (SqlConnection conexion =
                ConexionBD.ObtenerConexion())
            {
                conexion.Open();

                // Usamos una transacción para que tanto el stock
                // como el movimiento se guarden juntos.
                SqlTransaction transaccion =
                    conexion.BeginTransaction();

                try
                {
                    // ---------------------------------------------
                    // Obtener stock actual
                    // ---------------------------------------------

                    string consultaStock = @"
                        SELECT stock
                        FROM producto
                        WHERE id_producto = @id_producto;
                    ";

                    int stockActual;

                    using (SqlCommand comandoStock =
                        new SqlCommand(
                            consultaStock,
                            conexion,
                            transaccion))
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
                            transaccion.Rollback();

                            MessageBox.Show(
                                "El producto seleccionado no existe.",
                                "Inventario",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );

                            return;
                        }

                        stockActual =
                            Convert.ToInt32(resultado);
                    }


                    // ---------------------------------------------
                    // Validar salida
                    // ---------------------------------------------

                    if (tipo == "SALIDA" &&
                        cantidad > stockActual)
                    {
                        transaccion.Rollback();

                        MessageBox.Show(
                            "No existe suficiente stock.\n\n" +
                            "Stock disponible: " + stockActual +
                            "\nCantidad solicitada: " + cantidad,
                            "Stock insuficiente",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        return;
                    }


                    // ---------------------------------------------
                    // Actualizar stock
                    // ---------------------------------------------

                    string consultaActualizar;

                    if (tipo == "ENTRADA")
                    {
                        consultaActualizar = @"
                            UPDATE producto
                            SET stock = stock + @cantidad
                            WHERE id_producto = @id_producto;
                        ";
                    }
                    else
                    {
                        consultaActualizar = @"
                            UPDATE producto
                            SET stock = stock - @cantidad
                            WHERE id_producto = @id_producto;
                        ";
                    }


                    using (SqlCommand comandoActualizar =
                        new SqlCommand(
                            consultaActualizar,
                            conexion,
                            transaccion))
                    {
                        comandoActualizar.Parameters.AddWithValue(
                            "@cantidad",
                            cantidad
                        );

                        comandoActualizar.Parameters.AddWithValue(
                            "@id_producto",
                            idProducto
                        );

                        comandoActualizar.ExecuteNonQuery();
                    }


                    // ---------------------------------------------
                    // Registrar historial del movimiento
                    // ---------------------------------------------

                    string consultaMovimiento = @"
                        INSERT INTO movimiento_inventario
                        (
                            id_producto,
                            tipo,
                            cantidad,
                            fecha
                        )
                        VALUES
                        (
                            @id_producto,
                            @tipo,
                            @cantidad,
                            CURRENT_TIMESTAMP
                        );
                    ";


                    using (SqlCommand comandoMovimiento =
                        new SqlCommand(
                            consultaMovimiento,
                            conexion,
                            transaccion))
                    {
                        comandoMovimiento.Parameters.AddWithValue(
                            "@id_producto",
                            idProducto
                        );

                        comandoMovimiento.Parameters.AddWithValue(
                            "@tipo",
                            tipo
                        );

                        comandoMovimiento.Parameters.AddWithValue(
                            "@cantidad",
                            cantidad
                        );

                        comandoMovimiento.ExecuteNonQuery();
                    }


                    // Si todo salió bien confirmamos ambas operaciones.
                    transaccion.Commit();


                    MessageBox.Show(
                        tipo == "ENTRADA"
                            ? "Entrada registrada correctamente."
                            : "Salida registrada correctamente.",
                        "Inventario",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );


                    nudCantidadMovimiento.Value =
                        nudCantidadMovimiento.Minimum;

                    cmbProductoMovimiento.SelectedIndex = -1;

                    CargarProductos();
                    CargarProductosMovimiento();
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaccion.Rollback();
                    }
                    catch
                    {
                        // Si la conexión ya se cerró,
                        // no hacemos nada adicional.
                    }

                    MessageBox.Show(
                        "Error al registrar el movimiento:\n" +
                        ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }
    }
}