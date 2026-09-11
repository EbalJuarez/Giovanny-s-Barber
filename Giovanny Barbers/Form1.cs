using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Giovanny_Barbers.Modelos;
using Giovanny_Barbers.Datos;

namespace Giovanny_Barbers
{
    public partial class Trabajadores : Form
    {
        public Trabajadores()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CargarTrabajadores();
        }

        private void buttonRegistro_Click(object sender, EventArgs e)
        {
          
        }

        private void CargarTrabajadores()
        {
            dataGridViewTrabajadores.DataSource = TrabajadorDAO.ObtenerTodos();
        }

        private void LimpiarCampos()
        {
            textBoxNombre.Clear();
            textBoxApellido.Clear();
            textBoxTelefono.Clear();
            textBoxDireccion.Clear();
        }

        private void buttonRegistro_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxNombre.Text) || string.IsNullOrWhiteSpace(textBoxApellido.Text))
            {
                MessageBox.Show("Nombre y apellido son obligatorios");
                return;
            }

            Trabajador nuevo = new Trabajador
            {
                Nombres = textBoxNombre.Text.Trim(),
                Apellidos = textBoxApellido.Text.Trim(),
                Telefono = textBoxTelefono.Text.Trim(),
                Direccion = textBoxDireccion.Text.Trim()
            };

            try
            {
                TrabajadorDAO.Insertar(nuevo);

                MessageBox.Show("El trabajador se registro");
                LimpiarCampos();
                CargarTrabajadores();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar: " + ex.Message);
            }
        }
    }
}