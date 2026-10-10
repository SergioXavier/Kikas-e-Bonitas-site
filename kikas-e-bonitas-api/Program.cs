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

// CRIAR A BASE DE DADOS E TABELAS AUTOMATICAMENTE SE NÃO EXISTIREM
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
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

// 4. INICIAR O SERVIDOR
app.Run();
