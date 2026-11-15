# Brasa & Brio Desktop (C#)

Aplicativo Windows Forms para administração do delivery. Inclui login, consulta e atualização de pedidos importados, manutenção do catálogo existente e CRUD completo para clientes e entregadores.

## Preparar o banco MySQL

1. Abra `sql/DB_Delivery.sql` no MySQL Workbench e execute o script.
2. O script cria o banco `DB_Delivery`, as tabelas e oito produtos iniciais com IDs compatíveis com o cardápio do site.
3. O acesso de demonstração é `admin@brasabrio.com` / `Brasa@123`. Altere a senha do administrador antes de usar com dados reais.
4. Por padrão a conexão usa `localhost`, usuário `root` sem senha. Para outro ambiente, defina a variável de ambiente `BRASA_DB_CONNECTION` com a connection string correta antes de iniciar o programa.

## Abrir o projeto

Requer Windows, .NET 8 SDK (ou superior compatível com `net8.0-windows`) e MySQL Server. Abra `BrasaBrio.Desktop.csproj` no Visual Studio ou rode `dotnet run --project BrasaBrio.Desktop.csproj` no PowerShell. Na primeira execução o NuGet baixa o driver MySqlConnector.

## Fluxo site → desktop

1. No site Brasa & Brio, faça pedidos normalmente e abra a seção **Seus pedidos**.
2. Clique em **Exportar pedidos para o desktop** e salve `brasa-pedidos.json`.
3. No aplicativo desktop abra **Pedidos e importação JSON** e escolha o arquivo.
4. O importador reconhece `siteOrderId`, data, status, total e itens; usa transação e ignora códigos do site já importados.
5. No desktop é possível consultar, atualizar status e excluir/cancelar os pedidos. Não existe operação para criar pedido manualmente.

O botão exporta os pedidos armazenados no navegador do usuário atual. Em execução conectada à API, o site busca primeiro os pedidos daquele usuário.

## Limites do catálogo

Os produtos iniciais são cadastrados pelo SQL/Workbench. O desktop permite consultar, editar, ativar/desativar e excluir produtos; não há botão para cadastrar novo produto, conforme o enunciado. Dois módulos adicionais implementam inserir, consultar, atualizar e excluir: **Clientes** e **Entregadores**.

## Segurança e organização

As consultas de valores usam parâmetros MySQL. As senhas de acesso não são armazenadas em texto puro: o exemplo de aula compara SHA-256 em hexadecimal e cria um usuário inicial via `SHA2` do MySQL. Em um sistema real, use hash de senha com salt e fator de trabalho (por exemplo Argon2id) e credenciais secretas fora do código. O desktop conecta diretamente ao banco local conforme o escopo acadêmico; não exponha MySQL à internet.

## JSON esperado

```json
[
  {
    "siteOrderId": "12345",
    "date": "2026-11-15T18:30:00Z",
    "status": "Recebido",
    "total": 69.9,
    "customerEmail": "cliente@email.com",
    "items": [
      { "productId": 1, "productName": "Picanha na Brasa", "quantity": 1, "unitPrice": 69.9 }
    ]
  }
]
```
