using Microsoft.Data.SqlClient;

namespace Giovanny_Barbers.Datos
{
    public class ClienteServicioDAO
    {
        public static void Insertar(int idCliente, int idServicio)
        {
            using (SqlConnection conn = ConexionBD.ObtenerConexion())
            {
                string query = "INSERT INTO Cliente_Servicio (id_cliente, id_servicio) VALUES (@idCliente, @idServicio)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idCliente", idCliente);
                    cmd.Parameters.AddWithValue("@idServicio", idServicio);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}