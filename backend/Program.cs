using BrasaBrio.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BrasaDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Brasa") ?? "Data Source=brasa.db"));
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:5500")
          .AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BrasaDb>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Produtos.AnyAsync())
    {
        db.Produtos.AddRange(
            new Produto { Nome = "Picanha na brasa", Descricao = "Picanha fatiada, farofa da casa e vinagrete fresco.", Categoria = "Cortes", Preco = 49.90m, Ativo = true },
            new Produto { Nome = "Costela fogo de chão", Descricao = "Costela assada lentamente, macia e defumada.", Categoria = "Cortes", Preco = 54.90m, Ativo = true },
            new Produto { Nome = "Combo Churrasco", Descricao = "Picanha, linguiça, pão de alho e acompanhamentos.", Categoria = "Combos", Preco = 89.90m, Ativo = true });
        await db.SaveChangesAsync();
    }
}

app.MapGet("/api/produtos", async (BrasaDb db, string? busca, string? categoria) =>
{
    var query = db.Produtos.AsNoTracking().Where(p => p.Ativo);
    if (!string.IsNullOrWhiteSpace(busca)) query = query.Where(p => p.Nome.Contains(busca) || p.Descricao.Contains(busca));
    if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todos") query = query.Where(p => p.Categoria == categoria);
    return Results.Ok(await query.OrderBy(p => p.Nome).ToListAsync());
});
app.MapGet("/api/produtos/{id:int}", async (int id, BrasaDb db) =>
    await db.Produtos.FindAsync(id) is { } p ? Results.Ok(p) : Results.NotFound());
app.MapPost("/api/produtos", async (Produto entrada, BrasaDb db) =>
{
    if (entrada.Preco < 0 || string.IsNullOrWhiteSpace(entrada.Nome)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["produto"] = ["Nome obrigatório e preço não negativo."] });
    var produto = new Produto { Nome = entrada.Nome.Trim(), Descricao = entrada.Descricao.Trim(), Categoria = entrada.Categoria.Trim(), Preco = entrada.Preco, ImagemUrl = entrada.ImagemUrl, Ativo = true };
    db.Produtos.Add(produto); await db.SaveChangesAsync();
    return Results.Created($"/api/produtos/{produto.Id}", produto);
});
app.MapPut("/api/produtos/{id:int}", async (int id, Produto entrada, BrasaDb db) =>
{
    var produto = await db.Produtos.FindAsync(id); if (produto is null) return Results.NotFound();
    if (entrada.Preco < 0 || string.IsNullOrWhiteSpace(entrada.Nome)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["produto"] = ["Nome obrigatório e preço não negativo."] });
    produto.Nome = entrada.Nome.Trim(); produto.Descricao = entrada.Descricao.Trim(); produto.Categoria = entrada.Categoria.Trim(); produto.Preco = entrada.Preco; produto.ImagemUrl = entrada.ImagemUrl; produto.Ativo = entrada.Ativo;
    await db.SaveChangesAsync(); return Results.Ok(produto);
});
app.MapDelete("/api/produtos/{id:int}", async (int id, BrasaDb db) =>
{
    var produto = await db.Produtos.FindAsync(id); if (produto is null) return Results.NotFound();
    produto.Ativo = false; await db.SaveChangesAsync(); return Results.NoContent();
});

app.MapPost("/api/usuarios", async (UsuarioEntrada entrada, BrasaDb db, IPasswordHasher<Usuario> hasher) =>
{
    if (string.IsNullOrWhiteSpace(entrada.Nome) || entrada.Nome.Length > 80 || entrada.Email.Length > 100 || entrada.Senha.Length < 6 || entrada.Telefone.Length > 15)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["cadastro"] = ["Confira nome, e-mail, senha (mínimo 6 caracteres) e telefone."] });
    var email = entrada.Email.Trim().ToLowerInvariant();
    if (await db.Usuarios.AnyAsync(u => u.Email == email)) return Results.Conflict(new { mensagem = "E-mail já cadastrado." });
    var usuario = new Usuario { Nome = entrada.Nome.Trim(), Email = email, Telefone = entrada.Telefone.Trim() };
    usuario.SenhaHash = hasher.HashPassword(usuario, entrada.Senha);
    db.Usuarios.Add(usuario); await db.SaveChangesAsync();
    return Results.Created($"/api/usuarios/{usuario.Id}", new UsuarioPublico(usuario.Id, usuario.Nome, usuario.Email, usuario.Telefone));
});
app.MapGet("/api/usuarios/{id:int}", async (int id, BrasaDb db) =>
{
    var u = await db.Usuarios.FindAsync(id); return u is null ? Results.NotFound() : Results.Ok(new UsuarioPublico(u.Id, u.Nome, u.Email, u.Telefone));
});
app.MapGet("/api/usuarios", async (BrasaDb db) =>
    Results.Ok(await db.Usuarios.AsNoTracking().OrderBy(u => u.Nome)
        .Select(u => new UsuarioPublico(u.Id, u.Nome, u.Email, u.Telefone)).ToListAsync()));
app.MapPost("/api/sessoes", async (LoginEntrada entrada, BrasaDb db, IPasswordHasher<Usuario> hasher) =>
{
    var email = entrada.Email.Trim().ToLowerInvariant();
    var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email);
    if (usuario is null || hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, entrada.Senha) == PasswordVerificationResult.Failed)
        return Results.Unauthorized();
    return Results.Ok(new UsuarioPublico(usuario.Id, usuario.Nome, usuario.Email, usuario.Telefone));
});
app.MapPut("/api/usuarios/{id:int}", async (int id, UsuarioAtualizacao entrada, BrasaDb db) =>
{
    var u = await db.Usuarios.FindAsync(id); if (u is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(entrada.Nome) || entrada.Nome.Length > 80 || entrada.Telefone.Length > 15) return Results.BadRequest(new { mensagem = "Confira nome e telefone." });
    u.Nome = entrada.Nome.Trim(); u.Telefone = entrada.Telefone.Trim(); await db.SaveChangesAsync();
    return Results.Ok(new UsuarioPublico(u.Id, u.Nome, u.Email, u.Telefone));
});
app.MapDelete("/api/usuarios/{id:int}", async (int id, BrasaDb db) =>
{
    var u = await db.Usuarios.FindAsync(id); if (u is null) return Results.NotFound();
    db.Usuarios.Remove(u); await db.SaveChangesAsync(); return Results.NoContent();
});

app.MapPost("/api/pedidos", async (PedidoEntrada entrada, BrasaDb db) =>
{
    if (entrada.Itens.Count == 0 || entrada.Itens.Any(i => i.Quantidade < 1)) return Results.BadRequest(new { mensagem = "O pedido precisa ter itens e quantidades válidas." });
    if (!await db.Usuarios.AnyAsync(u => u.Id == entrada.UsuarioId)) return Results.BadRequest(new { mensagem = "Usuário não encontrado." });
    var ids = entrada.Itens.Select(i => i.ProdutoId).Distinct().ToArray();
    var produtos = await db.Produtos.Where(p => ids.Contains(p.Id) && p.Ativo).ToDictionaryAsync(p => p.Id);
    if (entrada.Itens.Any(i => !produtos.ContainsKey(i.ProdutoId))) return Results.BadRequest(new { mensagem = "Um ou mais produtos não estão disponíveis." });
    var pedido = new Pedido { UsuarioId = entrada.UsuarioId, Status = "Recebido" };
    pedido.Itens = entrada.Itens.Select(i => new ItemPedido { ProdutoId = i.ProdutoId, Quantidade = i.Quantidade, PrecoUnitario = produtos[i.ProdutoId].Preco }).ToList();
    pedido.Total = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
    db.Pedidos.Add(pedido); await db.SaveChangesAsync();
    return Results.Created($"/api/pedidos/{pedido.Id}", pedido);
});
app.MapGet("/api/pedidos", async (BrasaDb db, int? usuarioId) =>
{
    var query = db.Pedidos.AsNoTracking().Include(p => p.Itens).AsQueryable();
    if (usuarioId.HasValue) query = query.Where(p => p.UsuarioId == usuarioId.Value);
    return Results.Ok(await query.OrderByDescending(p => p.CriadoEm).ToListAsync());
});
app.MapGet("/api/pedidos/{id:int}", async (int id, BrasaDb db) =>
    await db.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id) is { } p ? Results.Ok(p) : Results.NotFound());
app.MapPut("/api/pedidos/{id:int}/status", async (int id, StatusEntrada entrada, BrasaDb db) =>
{
    var permitidos = new[] { "Recebido", "Em preparo", "Saiu para entrega", "Entregue", "Cancelado" };
    var p = await db.Pedidos.FindAsync(id); if (p is null) return Results.NotFound();
    if (!permitidos.Contains(entrada.Status)) return Results.BadRequest(new { mensagem = "Status inválido." });
    p.Status = entrada.Status; await db.SaveChangesAsync(); return Results.Ok(p);
});
app.MapDelete("/api/pedidos/{id:int}", async (int id, BrasaDb db) =>
{
    var p = await db.Pedidos.FindAsync(id); if (p is null) return Results.NotFound();
    db.Pedidos.Remove(p); await db.SaveChangesAsync(); return Results.NoContent();
});

app.MapGet("/api/health", () => Results.Ok(new { status = "Brasa & Brio API no ar" }));
app.Run();
