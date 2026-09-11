using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Giovanny_Barbers.Modelos;

namespace Giovanny_Barbers.Datos
{
    public class ServicioDAO
    {
        public static List<Servicio> ObtenerTodos()
        {
            List<Servicio> lista = new List<Servicio>();
            using (SqlConnection conn = ConexionBD.ObtenerConexion())
            {
                string query = "SELECT id_servicio, nombre, precio FROM Servicio";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Servicio
                            {
                                IdServicio = (int)reader["id_servicio"],
                                Nombre = reader["nombre"].ToString(),
                                Precio = (decimal)reader["precio"]
                            });
                        }
                    }
                }
            }
            return lista;
        }
    }
}