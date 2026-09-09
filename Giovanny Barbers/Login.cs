using System;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Giovanny_Barbers
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }


        private void buttonInicion_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(maskedTextBoxUsuario.Text) || string.IsNullOrWhiteSpace(maskedTextBoxContrasena.Text))
            {
                MessageBox.Show("Ingrese usuario y contraseña");
                return;
            }

            try
            {
                using (SqlConnection conn = ConexionBD.ObtenerConexion())
                {
                    string query = "SELECT rol FROM Usuario WHERE nombre_usuario = @usuario AND contrasena_hash = @contrasena";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@usuario", maskedTextBoxUsuario.Text.Trim());
                        cmd.Parameters.AddWithValue("@contrasena", maskedTextBoxContrasena.Text);

                        conn.Open();
                        var resultado = cmd.ExecuteScalar();

                        if (resultado != null)
                        {
                            string rol = resultado.ToString();
                            MessageBox.Show($"Bienvenido. Rol: {rol}");
                            
                        }
                        else
                        {
                            MessageBox.Show("Usuario o contraseña incorrectos");
                            maskedTextBoxContrasena.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con la base de datos: " + ex.Message);
            }
        }
    }
}