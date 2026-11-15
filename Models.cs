using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BrasaBrio.Api;

public sealed class BrasaDb(DbContextOptions<BrasaDb> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Produto>().Property(p => p.Preco).HasPrecision(10, 2);
        modelBuilder.Entity<Pedido>().Property(p => p.Total).HasPrecision(10, 2);
        modelBuilder.Entity<ItemPedido>().Property(i => i.PrecoUnitario).HasPrecision(10, 2);
        modelBuilder.Entity<Pedido>().HasMany(p => p.Itens).WithOne().HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class Usuario
{
    public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    [MaxLength(100), EmailAddress] public string Email { get; set; } = "";
    [MaxLength(15)] public string Telefone { get; set; } = "";
    public string SenhaHash { get; set; } = "";
}

public sealed class Produto
{
    public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    [MaxLength(240)] public string Descricao { get; set; } = "";
    [MaxLength(30)] public string Categoria { get; set; } = "";
    public decimal Preco { get; set; }
    [MaxLength(500)] public string? ImagemUrl { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class Pedido
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    [MaxLength(24)] public string Status { get; set; } = "Recebido";
    public decimal Total { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];
}

public sealed class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public sealed record UsuarioEntrada(string Nome, string Email, string Telefone, string Senha);
public sealed record LoginEntrada(string Email, string Senha);
public sealed record UsuarioAtualizacao(string Nome, string Telefone);
public sealed record UsuarioPublico(int Id, string Nome, string Email, string Telefone);
public sealed record ItemPedidoEntrada(int ProdutoId, int Quantidade);
public sealed record PedidoEntrada(int UsuarioId, List<ItemPedidoEntrada> Itens);
public sealed record StatusEntrada(string Status);
