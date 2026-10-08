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
            AcceptButton = buttonInicion;
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }

        private async void buttonInicion_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(maskedTextBoxUsuario.Text) || string.IsNullOrWhiteSpace(maskedTextBoxContrasena.Text))
            {
                MessageBox.Show("Ingrese usuario y contraseña");
                return;
            }

            SesionUsuario? sesion;
            buttonInicion.Enabled = false;
            UseWaitCursor = true;
            try
            {
                sesion = await ConexionBD.AutenticarAsync(maskedTextBoxUsuario.Text.Trim(), maskedTextBoxContrasena.Text);
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, ex.Message, "No se pudo iniciar sesión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally
            {
                if (!IsDisposed)
                {
                    buttonInicion.Enabled = true;
                    UseWaitCursor = false;
                }
            }
            if (IsDisposed) return;
            if (sesion == null)
            {
                MessageBox.Show("Usuario o contraseña incorrectos");
                maskedTextBoxContrasena.Clear();
                return;
            }
            maskedTextBoxContrasena.Clear();
            Hide();
            try
            {
                using var principal = new Home(sesion);
                principal.ShowDialog();
            }
            finally
            {
                Show();
                maskedTextBoxContrasena.Focus();
            }
        }

        private void labelMarca_Click(object sender, EventArgs e)
        {

        }
    }
}
