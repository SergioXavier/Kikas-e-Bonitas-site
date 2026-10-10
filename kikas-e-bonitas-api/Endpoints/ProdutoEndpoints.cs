using KikasEBonitas.Api.Data;
using KikasEBonitas.Api.Dtos;
using KikasEBonitas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KikasEBonitas.Api.Endpoints;

public static class ProdutoEndpoints
{
    public static void MapProdutosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/produtos")
                       .WithTags("Produtos");

        // GET: Listar produtos com Filtros de Pesquisa e Paginação
        group.MapGet("/", async (
            string? nome,
            int? categoriaId,
            int pagina = 1,
            int tamanhoPagina = 10,
            AppDbContext db = null!) =>
        {
            // Garantir limites razoáveis para a paginação
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 10;
            if (tamanhoPagina > 50) tamanhoPagina = 50; // Limite máximo para evitar sobrecarga

            // 1. Iniciar a query base (IQueryable permite construir a consulta SQL antes de executar)
            var query = db.Produtos.AsQueryable();

            // 2. Aplicar Filtro de Nome (se fornecido)
            if (!string.IsNullOrWhiteSpace(nome))
            {
                query = query.Where(p => p.Nome.ToLower().Contains(nome.ToLower()));
            }

            // 3. Aplicar Filtro de Categoria (se fornecido)
            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            // 4. Contar o total de itens que correspondem aos filtros
            var totalItens = await query.CountAsync();

            // 5. Aplicar Paginação (Skip e Take) e projetar para o DTO
            var produtos = await query
                .Include(p => p.Categoria)
                .OrderBy(p => p.Id)
                .Skip((pagina - 1) * tamanhoPagina) // Salta os registos das páginas anteriores
                .Take(tamanhoPagina)               // Pega apenas a quantidade solicitada
                .Select(p => new ProdutoRespostaDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Preco = p.Preco,
                    Stock = p.Stock,
                    CategoriaId = p.CategoriaId,
                    NomeCategoria = p.Categoria != null ? p.Categoria.Nome : "Sem Categoria",
                    ImagemUrl = p.ImagemUrl
                })
                .ToListAsync();

            // 6. Retornar a resposta envelopada
            var resultado = new ResultadoPaginadoDto<ProdutoRespostaDto>
            {
                Itens = produtos,
                PaginaAtual = pagina,
                TamanhoPagina = tamanhoPagina,
                TotalItens = totalItens
            };

            return Results.Ok(resultado);
        });

        // GET POR ID
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var produto = await db.Produtos
                .Include(p => p.Categoria)
                .Where(p => p.Id == id)
                .Select(p => new ProdutoRespostaDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Preco = p.Preco,
                    Stock = p.Stock,
                    CategoriaId = p.CategoriaId,
                    NomeCategoria = p.Categoria != null ? p.Categoria.Nome : "Sem Categoria",
                    ImagemUrl = p.ImagemUrl
                })
                .FirstOrDefaultAsync();

            return produto is not null 
                ? Results.Ok(produto) 
                : Results.NotFound(new { Mensagem = $"Produto com ID {id} não foi encontrado." });
        });

        // POST: Criar Produto com Upload de Imagem (usando AntiForgery/DisableValidation para Multipart ou formulário direto)
        group.MapPost("/", async (HttpRequest request, AppDbContext db) =>
        {
            // Ler dados do formulário multipart
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { Mensagem = "O formato do pedido deve ser multipart/form-data." });
            }

            var form = await request.ReadFormAsync();

            var nome = form["nome"].ToString();
            var precoStr = form["preco"].ToString();
            var stockStr = form["stock"].ToString();
            var categoriaIdStr = form["categoriaId"].ToString();
            var file = form.Files["imagem"];

            if (string.IsNullOrWhiteSpace(nome) || !decimal.TryParse(precoStr, out var preco) || 
                !int.TryParse(stockStr, out var stock) || !int.TryParse(categoriaIdStr, out var categoriaId))
            {
                return Results.BadRequest(new { Mensagem = "Dados do produto inválidos ou incompletos." });
            }

            var categoriaExiste = await db.Categorias.AnyAsync(c => c.Id == categoriaId);
            if (!categoriaExiste)
            {
                return Results.BadRequest(new { Mensagem = $"A categoria com o ID {categoriaId} não existe." });
            }

            string? imagemUrl = null;

            // Processar o upload do ficheiro se existir
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "produtos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                // Substitui espaços por underscores e remove caracteres especiais
                var cleanFileName = System.Text.RegularExpressions.Regex.Replace(originalFileName, @"\s+", "_");
                var extension = Path.GetExtension(file.FileName);

                var uniqueFileName = $"{Guid.NewGuid()}_{cleanFileName}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Caminho relativo que será guardado na BD e acedido pelo frontend
                imagemUrl = $"/images/produtos/{uniqueFileName}";
            }

            var novoProduto = new Produto
            {
                Nome = nome,
                Preco = preco,
                Stock = stock,
                CategoriaId = categoriaId,
                ImagemUrl = imagemUrl
            };

            db.Produtos.Add(novoProduto);
            await db.SaveChangesAsync();

            return Results.Created($"/api/produtos/{novoProduto.Id}", novoProduto);
        })
        .RequireAuthorization();

        // PUT: Atualizar Produto com Upload opcional de nova Imagem
        group.MapPut("/{id:int}", async (int id, HttpRequest request, AppDbContext db) =>
        {
            var produtoExistente = await db.Produtos.FindAsync(id);
            if (produtoExistente is null)
            {
                return Results.NotFound(new { Mensagem = $"Produto com ID {id} não foi encontrado." });
            }

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { Mensagem = "O formato do pedido deve ser multipart/form-data." });
            }

            var form = await request.ReadFormAsync();

            var nome = form["nome"].ToString();
            var precoStr = form["preco"].ToString();
            var stockStr = form["stock"].ToString();
            var categoriaIdStr = form["categoriaId"].ToString();
            var file = form.Files["imagem"];

            if (string.IsNullOrWhiteSpace(nome) || !decimal.TryParse(precoStr, out var preco) || 
                !int.TryParse(stockStr, out var stock) || !int.TryParse(categoriaIdStr, out var categoriaId))
            {
                return Results.BadRequest(new { Mensagem = "Dados do produto inválidos ou incompletos." });
            }

            var categoriaExiste = await db.Categorias.AnyAsync(c => c.Id == categoriaId);
            if (!categoriaExiste)
            {
                return Results.BadRequest(new { Mensagem = $"A categoria com o ID {categoriaId} não existe." });
            }

            // Se for enviada uma nova imagem, guardamos e substituímos a antiga
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "produtos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                // Substitui espaços por underscores e remove caracteres especiais
                var cleanFileName = System.Text.RegularExpressions.Regex.Replace(originalFileName, @"\s+", "_");
                var extension = Path.GetExtension(file.FileName);

                var uniqueFileName = $"{Guid.NewGuid()}_{cleanFileName}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                produtoExistente.ImagemUrl = $"/images/produtos/{uniqueFileName}";
            }

            produtoExistente.Nome = nome;
            produtoExistente.Preco = preco;
            produtoExistente.Stock = stock;
            produtoExistente.CategoriaId = categoriaId;

            await db.SaveChangesAsync();

            return Results.Ok(produtoExistente);
        })
        .RequireAuthorization();

        // DELETE: Apagar Produto
        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var produto = await db.Produtos.FindAsync(id);

            if (produto is null)
            {
                return Results.NotFound(new { Mensagem = $"Produto com ID {id} não foi encontrado." });
            }

            db.Produtos.Remove(produto);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();;
    }
}