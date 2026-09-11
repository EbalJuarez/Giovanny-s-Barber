using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Giovanny_Barbers.Modelos;

namespace Giovanny_Barbers.Datos
{
    public class TrabajadorDAO
    {
        public static void Insertar(Trabajador t)
        {
            using (SqlConnection conn = ConexionBD.ObtenerConexion())
            {
                string query = "INSERT INTO Trabajador (nombres, apellidos, telefono, direccion) VALUES (@nombres, @apellidos, @telefono, @direccion)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nombres", t.Nombres);
                    cmd.Parameters.AddWithValue("@apellidos", t.Apellidos);
                    cmd.Parameters.AddWithValue("@telefono", t.Telefono);
                    cmd.Parameters.AddWithValue("@direccion", t.Direccion);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<Trabajador> ObtenerTodos()
        {
            List<Trabajador> lista = new List<Trabajador>();
            using (SqlConnection conn = ConexionBD.ObtenerConexion())
            {
                string query = "SELECT id_trabajador, id_usuario, nombres, apellidos, telefono, direccion FROM Trabajador";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Trabajador
                            {
                                IdTrabajador = (int)reader["id_trabajador"],
                                IdUsuario = reader["id_usuario"] == DBNull.Value ? (int?)null : (int)reader["id_usuario"],
                                Nombres = reader["nombres"].ToString(),
                                Apellidos = reader["apellidos"].ToString(),
                                Telefono = reader["telefono"].ToString(),
                                Direccion = reader["direccion"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }
    }
}