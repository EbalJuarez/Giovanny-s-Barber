using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Giovanny_Barbers.Modelos;

namespace Giovanny_Barbers.Datos
{
    public class ClienteDAO
    {
        public static int InsertarCliente(Cliente c)
        {
            using (SqlConnection conn = ConexionBD.ObtenerConexion())
            {
                string query = @"INSERT INTO Cliente (nombres, telefono, nit) 
                                  OUTPUT INSERTED.id_cliente
                                  VALUES (@nombres, @telefono, @nit)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nombres", c.Nombres);
                    cmd.Parameters.AddWithValue("@telefono", c.Telefono);
                    cmd.Parameters.AddWithValue("@nit", c.Nit);
                    conn.Open();
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public static List<Cliente> ObtenerTodos()
        {
            List<Cliente> lista = new List<Cliente>();
            using (SqlConnection conn = ConexionBD.ObtenerConexion())
            {
                string query = "SELECT id_cliente, nombres, telefono, nit FROM Cliente";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Cliente
                            {
                                IdCliente = (int)reader["id_cliente"],
                                Nombres = reader["nombres"].ToString(),
                                Telefono = reader["telefono"] == DBNull.Value ? "" : reader["telefono"].ToString(),
                                Nit = reader["nit"] == DBNull.Value ? "" : reader["nit"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }
    }
}