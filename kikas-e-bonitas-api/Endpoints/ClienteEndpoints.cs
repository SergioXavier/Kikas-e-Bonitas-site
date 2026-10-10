using KikasEBonitas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace KikasEBonitas.Api.Endpoints;

public static class ClienteEndpoints
{
    public static void MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes").WithTags("Clientes");

        // 1. Endpoint para listar todos os clientes registados
        group.MapGet("/", async (AppDbContext db) =>
        {
            var clientes = await db.Clientes
                .Select(c => new 
                {
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Telefone,
                    c.Nif,
                    c.DataRegisto
                })
                .ToListAsync();

            return Results.Ok(clientes);
        });

        // 2. Endpoint para obter detalhes de um cliente específico
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var cliente = await db.Clientes
                .Where(c => c.Id == id)
                .Select(c => new 
                {
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Telefone,
                    c.Morada,
                    c.CodigoPostal,
                    c.Nif,
                    c.DataRegisto,
                    Encomendas = new List<object>() 
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return Results.NotFound(new { mensagem = "Cliente não encontrado." });
            }

            return Results.Ok(cliente);
        });

        // 3. Endpoint para obter o histórico de um cliente específico (Compatível com SQLite)
        group.MapGet("/{id:int}/historico", async (int id, AppDbContext db) =>
        {
            var cliente = await db.Clientes
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return Results.NotFound(new { mensagem = "Cliente não encontrado." });
            }

            // Vai buscar as encomendas do cliente com os respetivos itens e produtos
            var encomendasDb = await db.Encomendas
                .Where(e => e.ClienteId == id)
                .Include(e => e.Itens)
                    .ThenInclude(i => i.Produto)
                .OrderByDescending(e => e.DataCriacao)
                .ToListAsync();

            // Mapeia para o objeto final em memória (evita limitações do SQLite)
            var historicoDto = new
            {
                cliente.Id,
                cliente.Nome,
                cliente.Email,
                cliente.Telefone,
                Encomendas = encomendasDb.Select(e => new 
                {
                    e.Id,
                    Referencia = $"#{e.Id}",
                    Descricao = e.Itens != null && e.Itens.Any() 
                        ? string.Join(", ", e.Itens.Select(i => $"{i.Quantidade}x {i.Produto?.Nome ?? "Produto"}"))
                        : "Sem produtos especificados",
                    Estado = e.Status.ToString(),
                    Data = e.DataCriacao
                }).ToList()
            };

            return Results.Ok(historicoDto);
        });

        // 4. Endpoint para atualizar os dados de um cliente
        group.MapPut("/{id:int}", async (int id, ClienteAtualizaDto dto, AppDbContext db) =>
        {
            var cliente = await db.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return Results.NotFound(new { mensagem = "Cliente não encontrado." });
            }

            cliente.Nome = dto.Nome;
            cliente.Email = dto.Email;
            cliente.Telefone = dto.Telefone;
            cliente.Morada = dto.Morada;
            cliente.CodigoPostal = dto.CodigoPostal;
            cliente.Nif = dto.Nif;

            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = "Cliente atualizado com sucesso!" });
        });

        // 5. Endpoint para eliminar um cliente
        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var cliente = await db.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return Results.NotFound(new { mensagem = "Cliente não encontrado." });
            }

            db.Clientes.Remove(cliente);
            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = "Cliente eliminado com sucesso!" });
        });

        // 6. Endpoint para procurar cliente por telemóvel (usado no checkout)
        group.MapGet("/por-telemovel/{telefone}", async (string telefone, AppDbContext db) =>
        {
            var cliente = await db.Clientes
                .Where(c => c.Telefone == telefone)
                .Select(c => new 
                {
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Telefone,
                    c.Nif
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return Results.NotFound(new { mensagem = "Cliente não registado!" });
            }

            return Results.Ok(cliente);
        });
    }
}

// DTO de suporte para a atualização (sem GifUrl)
public record ClienteAtualizaDto(
    string Nome, 
    string Email, 
    string? Telefone, 
    string? Morada, 
    string? CodigoPostal, 
    string? Nif
);