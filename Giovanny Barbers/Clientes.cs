using System;
using System.Windows.Forms;
using Giovanny_Barbers.Modelos;
using Giovanny_Barbers.Datos;

namespace Giovanny_Barbers
{
    public partial class Clientes : Form
    {
        public Clientes()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void label5_Click(object sender, EventArgs e)
        {
        }

        private void Clientes_Load(object sender, EventArgs e)
        {
            CargarServicios();
            CargarClientes();
        }

        private void CargarServicios()
        {
            comboBoxServicios.DataSource = ServicioDAO.ObtenerTodos();
            comboBoxServicios.DisplayMember = "Nombre";
            comboBoxServicios.ValueMember = "IdServicio";
        }

        private void CargarClientes()
        {
            dataGridViewClientes.DataSource = ClienteDAO.ObtenerTodos();
            dataGridViewClientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewClientes.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        private void buttonRegistrar_Click(object sender, EventArgs e)
        {

        }

        private void LimpiarCampos()
        {
            textBoxNombre.Clear();
            textBoxTelefono.Clear();
            textBoxNit.Clear();
        }

        private void Clientes_Load_1(object sender, EventArgs e)
        {
            CargarServicios();
            CargarClientes();
        }

        private void buttonRegistrar_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxNombre.Text))
            {
                MessageBox.Show("El nombre es obligatorio");
                return;
            }

            Cliente nuevoCliente = new Cliente
            {
                Nombres = textBoxNombre.Text.Trim(),
                Telefono = textBoxTelefono.Text.Trim(),
                Nit = textBoxNit.Text.Trim()
            };

            try
            {
                int idClienteNuevo = ClienteDAO.InsertarCliente(nuevoCliente);

                if (comboBoxServicios.SelectedValue != null)
                {
                    int idServicio = (int)comboBoxServicios.SelectedValue;
                    ClienteServicioDAO.Insertar(idClienteNuevo, idServicio);
                }

                MessageBox.Show("Cliente registrado");
                LimpiarCampos();
                CargarClientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar: " + ex.Message);
            }
        }

        private void dataGridViewClientes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}