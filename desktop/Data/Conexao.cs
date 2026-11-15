using MySqlConnector;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace BrasaBrio.Desktop.Data;

public static class Conexao
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("BRASA_DB_CONNECTION")
        ?? "Server=localhost;Port=3306;Database=DB_Delivery;User ID=root;Password=;SslMode=None;";

    public static MySqlConnection Abrir()
    {
        var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public static bool ValidarLogin(string email, string senha)
    {
        using var connection = Abrir();
        using var command = new MySqlCommand("SELECT SenhaHash FROM UsuariosAcesso WHERE Email=@email AND Ativo=1", connection);
        command.Parameters.AddWithValue("@email", email.Trim());
        var hash = command.ExecuteScalar() as string;
        return hash is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senha)))));
    }

    public static DataTable Consultar(string sql, params MySqlParameter[] parametros)
    {
        using var connection = Abrir();
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddRange(parametros);
        using var adapter = new MySqlDataAdapter(command);
        var table = new DataTable(); adapter.Fill(table); return table;
    }

    public static int Executar(string sql, params MySqlParameter[] parametros)
    {
        using var connection = Abrir();
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddRange(parametros);
        return command.ExecuteNonQuery();
    }
}
