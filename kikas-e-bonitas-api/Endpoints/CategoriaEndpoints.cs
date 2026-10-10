using KikasEBonitas.Api.Data;
using KikasEBonitas.Api.Dtos;
using KikasEBonitas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KikasEBonitas.Api.Endpoints;

public static class CatagoriaEndpoints
{
    public static void MapCategoriasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categorias")
                         .WithTags("Categorias");

        // GET: Listar todas as categorias com a contagem de produtos associados
        group.MapGet("/", async (AppDbContext db) =>
        {
            var categorias = await db.Categorias
                .Select (c => new CategoriaRespostaDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    TotalProdutos = c.Produtos.Count
                })
                .ToListAsync();
            
            return Results.Ok(categorias);
        });

        // GET POR ID
        group.MapGet("/{id:int}", async(int id, AppDbContext db) =>
        {
            var categoria = await db.Categorias
                .Where(c => c.Id == id)
                .Select(c => new CategoriaRespostaDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    TotalProdutos = c.Produtos.Count
                })
                .FirstOrDefaultAsync();

            return categoria is not null
                ? Results.Ok(categoria)
                : Results.NotFound(new { Mensagem = $"Categoria com ID {id} não foi encontrada." });
        });
        
        // POST: Criar nova Categoria
        group.MapPost("/", async(CriarCategoriaDto dto, AppDbContext db) =>
        {
           // Evita criar categorias duplicadas com o mesmo nome
           var jaExiste = await db.Categorias.AnyAsync(c => c.Nome.ToLower() == dto.Nome.ToLower());
           if (jaExiste)
            {
                return Results.BadRequest(new { Mensagem = $"Já existe uma categoria com o nome '{dto.Nome}'." });
            }

            var novaCategoria = new Categoria
            {
                Nome = dto.Nome
            };

            db.Categorias.Add(novaCategoria);
            await db.SaveChangesAsync();

            var resposta = new CategoriaRespostaDto
            {
                Id = novaCategoria.Id,
                Nome = novaCategoria.Nome,
                TotalProdutos = 0
            };

            return Results.Created($"/api/categorias/{novaCategoria.Id}", resposta);
        })
        .WithParameterValidation();

        // PUT: Atualizar Categoria
        group.MapPut("/{id:int}", async(int id, CriarCategoriaDto dto, AppDbContext db) =>
        {
            var categoria = await db.Categorias.FindAsync(id);
            if(categoria is null)
            {
                return Results.NotFound(new {Mensagem = $"Categoria com ID {id} não foi encontrada."});
            }
            
            categoria.Nome = dto.Nome;
            await db.SaveChangesAsync();
            
            return Results.Ok(new CategoriaRespostaDto
            {
               Id = categoria.Id,
               Nome = categoria.Nome,
               TotalProdutos = await db.Produtos.CountAsync(p => p.CategoriaId == id)
            });
        })
        .WithParameterValidation();

        // DELETE: Apagar Categoria
        group.MapDelete("/{id:int}", async(int id, AppDbContext db) =>
        {
           var categoria = await db.Categorias
                .Include(c => c.Produtos)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (categoria is null)
            {
                return Results.NotFound(new { Mensagem = $"Categoria com ID {id} não foi encontrada." });
            }

            // Impede apagar categorias que ainda tenham produtos associados
            if (categoria.Produtos.Any())
            {
                return Results.BadRequest(new { Mensagem = $"Não é possível apagar uma categoria que contém produtos associados." });
            }

            db.Categorias.Remove(categoria);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}