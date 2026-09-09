using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

public class ConexionBD
{
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .Build();

    public static SqlConnection ObtenerConexion()
    {
        string cadena = Config.GetConnectionString("GeovannysBarberDB");
        return new SqlConnection(cadena);
    }
}