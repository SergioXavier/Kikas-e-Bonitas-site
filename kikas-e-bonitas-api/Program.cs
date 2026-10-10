using System.Text;
using KikasEBonitas.Api.Data;
using KikasEBonitas.Api.Endpoints;
using KikasEBonitas.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// 1. REGISTO DE SERVIÇOS
builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// Regista a geração da documentação OpenAPI
builder.Services.AddOpenApi();

// REGISTO DO GLOBAL EXCEPTION HANDLER
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// CONFIGURAÇÃO DO JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5289","https://kikasbonitas-web.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthorization();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Se também usares controllers tradicionais:
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

// CRIAR A BASE DE DADOS E POPULAR CATEGORIAS E PRODUTOS AUTOMATICAMENTE
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // 1. Garantir que as categorias existem na BD com os IDs correspondentes ao Enum/JSON
    if (!db.Categorias.Any())
    {
        db.Categorias.AddRange(
            new KikasEBonitas.Api.Models.Categoria { Id = 0, Nome = "Capilares" },
            new KikasEBonitas.Api.Models.Categoria { Id = 1, Nome = "Faciais" },
            new KikasEBonitas.Api.Models.Categoria { Id = 2, Nome = "Corpo" },
            new KikasEBonitas.Api.Models.Categoria { Id = 3, Nome = "Higiene" },
            new KikasEBonitas.Api.Models.Categoria { Id = 4, Nome = "Maquiagem" },
            new KikasEBonitas.Api.Models.Categoria { Id = 5, Nome = "Perfumes" },
            new KikasEBonitas.Api.Models.Categoria { Id = 6, Nome = "Saúde" },
            new KikasEBonitas.Api.Models.Categoria { Id = 7, Nome = "PET" },
            new KikasEBonitas.Api.Models.Categoria { Id = 8, Nome = "Casa" },
            new KikasEBonitas.Api.Models.Categoria { Id = 9, Nome = "Auto" },
            new KikasEBonitas.Api.Models.Categoria { Id = 10, Nome = "Solares" }
        );
        await db.SaveChangesAsync();
    }
}

app.UseCors("FrontendPolicy");
app.UseStaticFiles();

// ATIVAR AUTENTICAÇÃO E AUTORIZAÇÃO NO PIPELINE
app.UseAuthentication();
app.UseAuthorization();

// ATIVAR O MIDDLEWARE DE TRATAMENTO DE ERROS (Deve vir no início do pipeline HTTP!)
app.UseExceptionHandler();

// 2. PIPELINE HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); 
}

app.MapGet("/", () => "Bem-vindo à API da Kikas e Bonitas!");

// 3. REGISTO DAS ROTAS MODULARES
app.MapAuthEndpoints(builder.Configuration);
app.MapProdutosEndpoints();
app.MapCategoriasEndpoints();
app.MapEncomendaEndpoints();
app.MapClienteEndpoints();

// ENDPOINT DE IMPORTAÇÃO EM MASSA DO PRODUTOS.JSON
app.MapGet("/api/produtos/importar-json", async (AppDbContext db) =>
{
    try
    {
        var caminhoJson = Path.Combine(Directory.GetCurrentDirectory(), "produtos.json");
        if (!File.Exists(caminhoJson))
        {
            return Results.NotFound("O ficheiro produtos.json não foi encontrado na raiz do projeto.");
        }

        var jsonContent = await File.ReadAllTextAsync(caminhoJson);
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var listaDto = System.Text.Json.JsonSerializer.Deserialize<List<ProdutoImportDto>>(jsonContent, options);

        if (listaDto == null || !listaDto.Any())
        {
            return Results.BadRequest("O ficheiro JSON está vazio ou inválido.");
        }

        // 1. Mapeamento das categorias do enum para garantir que os IDs exatos são criados na BD
        var categoriasEnum = new Dictionary<int, string>
        {
            { 0, "Capilares" },
            { 1, "Faciais" },
            { 2, "Corpo" },
            { 3, "Higiene" },
            { 4, "Maquiagem" },
            { 5, "Perfumes" },
            { 6, "Saúde" },
            { 7, "PET" },
            { 8, "Casa" },
            { 9, "Auto" },
            { 10, "Solares" }
        };

        // 2. Inserir as categorias via SQL direto no SQLite para respeitar os IDs exatos (0, 1, 2, 6...)
        foreach (var cat in categoriasEnum)
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT OR IGNORE INTO Categorias (Id, Nome) VALUES ({0}, {1})", 
                cat.Key, cat.Value);
        }

        int importados = 0;
        foreach (var dto in listaDto)
        {
            var existe = await db.Produtos.FindAsync(dto.Id);
            
            var caminhoImagem = dto.Imagem.Replace("imagens/produtos/", "imagens/");
            if (!caminhoImagem.StartsWith("/"))
            {
                caminhoImagem = $"/{caminhoImagem}";
            }

            if (existe == null)
            {
                db.Produtos.Add(new KikasEBonitas.Api.Models.Produto
                {
                    Id = dto.Id,
                    Nome = dto.Nome,
                    Preco = dto.Preco,
                    Stock = 10,
                    CategoriaId = dto.Categoria,
                    ImagemUrl = caminhoImagem
                });
                importados++;
            }
            else
            {
                existe.Nome = dto.Nome;
                existe.Preco = dto.Preco;
                existe.CategoriaId = dto.Categoria;
                existe.ImagemUrl = caminhoImagem;
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok($"Importação concluída com sucesso! {importados} produtos novos adicionados.");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.InnerException?.Message ?? ex.Message, title: "Erro na Importação");
    }
});

// 4. INICIAR O SERVIDOR
app.Run();

// Classe de suporte para a leitura do JSON
public class ProdutoImportDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Imagem { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Categoria { get; set; }
}