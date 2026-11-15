using System.Text.Json;
using MySqlConnector;
using BrasaBrio.Desktop.Data;

namespace BrasaBrio.Desktop.Services;

public sealed class PedidoJson
{
    public string SiteOrderId { get; set; } = "";
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Recebido";
    public decimal Total { get; set; }
    public string CustomerEmail { get; set; } = "";
    public List<ItemPedidoJson> Items { get; set; } = [];
}
public sealed class ItemPedidoJson
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public static class PedidoJsonImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static int Importar(string caminho)
    {
        var json = File.ReadAllText(caminho);
        var pedidos = JsonSerializer.Deserialize<List<PedidoJson>>(json, JsonOptions) ?? [];
        var importados = 0;
        using var connection = Conexao.Abrir();
        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var pedido in pedidos)
            {
                if (string.IsNullOrWhiteSpace(pedido.SiteOrderId) || pedido.Items.Count == 0 || pedido.Total < 0)
                    throw new InvalidDataException("O JSON contém um pedido sem identificador, sem itens ou com total inválido.");
                using var exists = new MySqlCommand("SELECT COUNT(*) FROM Pedidos WHERE CodigoSite=@codigo", connection, transaction);
                exists.Parameters.AddWithValue("@codigo", pedido.SiteOrderId);
                if (Convert.ToInt32(exists.ExecuteScalar()) > 0) continue;

                using var insert = new MySqlCommand("INSERT INTO Pedidos (CodigoSite, ClienteEmail, DataPedido, Status, Total) VALUES (@codigo,@email,@data,@status,@total); SELECT LAST_INSERT_ID();", connection, transaction);
                insert.Parameters.AddWithValue("@codigo", pedido.SiteOrderId);
                insert.Parameters.AddWithValue("@email", pedido.CustomerEmail);
                insert.Parameters.AddWithValue("@data", pedido.Date == default ? DateTime.Now : pedido.Date);
                insert.Parameters.AddWithValue("@status", pedido.Status);
                insert.Parameters.AddWithValue("@total", pedido.Total);
                var id = Convert.ToInt32(insert.ExecuteScalar());
                foreach (var item in pedido.Items)
                {
                    using var line = new MySqlCommand("INSERT INTO ItensPedido (PedidoId, ProdutoId, NomeProduto, Quantidade, PrecoUnitario) VALUES (@pedido,@produto,@nome,@quantidade,@preco)", connection, transaction);
                    line.Parameters.AddWithValue("@pedido", id); line.Parameters.AddWithValue("@produto", item.ProductId);
                    line.Parameters.AddWithValue("@nome", item.ProductName); line.Parameters.AddWithValue("@quantidade", item.Quantity);
                    line.Parameters.AddWithValue("@preco", item.UnitPrice); line.ExecuteNonQuery();
                }
                importados++;
            }
            transaction.Commit(); return importados;
        }
        catch { transaction.Rollback(); throw; }
    }
}
