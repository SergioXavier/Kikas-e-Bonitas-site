using KikasEBonitas.Api.Data;
using KikasEBonitas.Api.Dtos;
using KikasEBonitas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KikasEBonitas.Api.Endpoints;

public static class EncomendaEndpoints
{
    public static RouteGroupBuilder MapEncomendaEndpoints(
        this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/encomendas")
            .WithTags("Encomendas");

        // POST: /api/encomendas
        group.MapPost("/", async (
            CriarEncomendaDto dto,
            AppDbContext db) =>
        {
            if (dto.Itens == null || dto.Itens.Count == 0)
            {
                return Results.BadRequest(new
                {
                    mensagem = "A encomenda deve conter pelo menos um item."
                });
            }

            using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                // ==========================================
                // 1. OBTER PRODUTOS
                // ==========================================

                var idsProdutos =
                    dto.Itens
                        .Select(i => i.ProdutoId)
                        .Distinct()
                        .ToList();

                var produtosDb =
                    await db.Produtos
                        .Where(p => idsProdutos.Contains(p.Id))
                        .ToDictionaryAsync(p => p.Id);

                // ==========================================
                // 2. VALIDAR ITENS (Sem restrição de stock)
                // ==========================================

                foreach (var item in dto.Itens)
                {
                    if (!produtosDb.TryGetValue(
                            item.ProdutoId,
                            out var produto))
                    {
                        return Results.BadRequest(new
                        {
                            mensagem =
                                $"Produto com ID {item.ProdutoId} não foi encontrado."
                        });
                    }

                    if (item.Quantidade <= 0)
                    {
                        return Results.BadRequest(new
                        {
                            mensagem =
                                $"Quantidade inválida para o produto '{produto.Nome}'."
                        });
                    }
                    // NOTA: A restrição de stock insuficiente foi removida para permitir 
                    // encomendas de produtos sob consulta/sem stock prévio.
                }

                // ==========================================
                // 3. CRIAR ENCOMENDA
                // ==========================================

                var encomenda = new Encomenda
                {
                    NomeCliente = dto.NomeCliente,
                    EmailCliente = dto.Telemovel, 
                    Telemovel = dto.Telemovel,    
                    ClienteId = dto.ClienteId,    
                    DataCriacao = DateTime.UtcNow,
                    Status = StatusEncomenda.Pendente,
                    MetodoPagamento = dto.MetodoPagamento,
                    CustoPortes = dto.CustoPortes,
                    Itens = new List<ItemEncomenda>()
                };

                decimal subtotalProdutos = 0m;

                // ==========================================
                // 4. ADICIONAR ITENS E ATUALIZAR STOCK
                // ==========================================

                foreach (var itemDto in dto.Itens)
                {
                    var produto = produtosDb[itemDto.ProdutoId];
                    var precoUnitario = produto.Preco;

                    subtotalProdutos += precoUnitario * itemDto.Quantidade;
                    
                    // O stock é decrementado. Se for 0, fica negativo (ex: -2), 
                    // indicando a quantidade exata que precisa de ser encomendada ao fornecedor.
                    produto.Stock -= itemDto.Quantidade;

                    encomenda.Itens.Add(
                        new ItemEncomenda
                        {
                            ProdutoId = produto.Id,
                            Quantidade = itemDto.Quantidade,
                            PrecoUnitario = precoUnitario
                        });
                }

                encomenda.ValorTotal = subtotalProdutos + encomenda.CustoPortes;

                // ==========================================
                // 5. GRAVAR BD
                // ==========================================

                db.Encomendas.Add(encomenda);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                // ==========================================
                // 6. PREPARAR RESPOSTA
                // ==========================================

                var respostaDto =
                    new EncomendaRespostaDto
                    {
                        Id = encomenda.Id,
                        DataCriacao = encomenda.DataCriacao,
                        NomeCliente = encomenda.NomeCliente,
                        Telemovel = dto.Telemovel,
                        Status = encomenda.Status.ToString(),
                        MetodoPagamento = encomenda.MetodoPagamento,
                        CustoPortes = encomenda.CustoPortes,
                        ValorTotal = encomenda.ValorTotal,
                        Itens = encomenda.Itens
                            .Select(i => new ItemEncomendaRespostaDto
                            {
                                ProdutoId = i.ProdutoId,
                                NomeProduto = i.Produto != null ? i.Produto.Nome : string.Empty,
                                Quantidade = i.Quantidade,
                                PrecoUnitario = i.PrecoUnitario
                            })
                            .ToList()
                    };

                return Results.Created(
                    $"/api/encomendas/{encomenda.Id}",
                    respostaDto);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        // GET: /api/encomendas
        group.MapGet("/", async (AppDbContext db) =>
        {
            var encomendas =
                await db.Encomendas
                    .Include(e => e.Itens)
                    .ThenInclude(i => i.Produto)
                    .ToListAsync();

            return Results.Ok(encomendas);
        });

        // GET: /api/encomendas/resumo-fornecedor (Lista consolidada para encomenda semanal)
        group.MapGet("/resumo-fornecedor", async (AppDbContext db) =>
        {
            var resumo = await db.ItensEncomenda
                .Include(i => i.Produto)
                .Include(i => i.Encomenda)
                .Where(i => i.Encomenda.Status == StatusEncomenda.Pendente)
                .GroupBy(i => new { i.ProdutoId, i.Produto.Nome, i.Produto.Preco, i.Produto.ImagemUrl, i.Produto.Stock })
                .Select(g => new
                {
                    ProdutoId = g.Key.ProdutoId,
                    NomeProduto = g.Key.Nome,
                    PrecoUnitario = g.Key.Preco,
                    ImagemUrl = g.Key.ImagemUrl,
                    StockAtual = g.Key.Stock,
                    QuantidadeTotalAEncomendar = g.Sum(x => x.Quantidade)
                })
                .ToListAsync();

            return Results.Ok(resumo);
        });

        // GET: /api/encomendas/{id}
        group.MapGet("/{id:int}", async (
            int id,
            AppDbContext db) =>
        {
            var encomenda =
                await db.Encomendas
                    .Include(e => e.Itens)
                    .ThenInclude(i => i.Produto)
                    .FirstOrDefaultAsync(e => e.Id == id);

            if (encomenda == null)
            {
                return Results.NotFound();
            }

            var respostaDto = new EncomendaRespostaDto
            {
                Id = encomenda.Id,
                DataCriacao = encomenda.DataCriacao,
                NomeCliente = encomenda.NomeCliente,
                Telemovel = encomenda.EmailCliente,
                Status = encomenda.Status.ToString(),
                MetodoPagamento = encomenda.MetodoPagamento,
                CustoPortes = encomenda.CustoPortes,
                ValorTotal = encomenda.ValorTotal,
                Itens = encomenda.Itens
                    .Select(i => new ItemEncomendaRespostaDto
                    {
                        ProdutoId = i.ProdutoId,
                        NomeProduto = i.Produto != null ? i.Produto.Nome : string.Empty,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario
                    })
                    .ToList()
            };

            return Results.Ok(respostaDto);
        });

        // PUT: /api/encomendas/{id}/estado
        group.MapPut("/{id:int}/estado", async (
            int id,
            AtualizarEstadoDto dto,
            AppDbContext db) =>
        {
            var encomenda = await db.Encomendas.FindAsync(id);

            if (encomenda == null)
            {
                return Results.NotFound(new
                {
                    mensagem = "Encomenda não encontrada."
                });
            }

            StatusEncomenda novoStatus =
                dto.Status switch
                {
                    "Pendente" => StatusEncomenda.Pendente,
                    "Paga" => StatusEncomenda.Paga,
                    "Enviada" => StatusEncomenda.Enviada,
                    "Cancelada" => StatusEncomenda.Cancelada,
                    _ => StatusEncomenda.Pendente
                };

            encomenda.Status = novoStatus;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                mensagem = "Estado atualizado com sucesso."
            });
        });

        // DELETE: /api/encomendas/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            AppDbContext db) =>
        {
            var encomenda = await db.Encomendas
                .Include(e => e.Itens)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (encomenda == null)
            {
                return Results.NotFound(new { mensagem = "Encomenda não encontrada." });
            }

            foreach (var item in encomenda.Itens)
            {
                var produto = await db.Produtos.FindAsync(item.ProdutoId);
                if (produto != null)
                {
                    produto.Stock += item.Quantidade;
                }
            }

            db.ItensEncomenda.RemoveRange(encomenda.Itens);
            db.Encomendas.Remove(encomenda);
            
            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = "Encomenda eliminada com sucesso." });
        });

        return group;
    }
}