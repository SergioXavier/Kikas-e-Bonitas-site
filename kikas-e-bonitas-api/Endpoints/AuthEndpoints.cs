using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KikasEBonitas.Api.Data;
using KikasEBonitas.Api.Dtos;
using KikasEBonitas.Api.Models; // Onde tens a entidade Cliente
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KikasEBonitas.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, IConfiguration config)
    {
        var group = app.MapGroup("/api/auth").WithTags("Autenticação");

        // 1. ENDPOINT DE LOGIN (Admin)
        group.MapPost("/login", (LoginDto dto) =>
        {
            if (dto.Usuario != "admin" || dto.Senha != "Admin123!")
            {
                return Results.Unauthorized();
            }

            var jwtSettings = config.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, dto.Usuario),
                    new Claim(ClaimTypes.Role, "Admin")    
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiracaoEmMinutos"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Results.Ok(new RespostaTokenDto
            {
                Token = tokenString,
                Expiracao = tokenDescriptor.Expires.Value
            });
        });

        // 2. ENDPOINT DE REGISTO DE CLIENTES
        group.MapPost("/registo", async (ClienteRegistoDto dto, AppDbContext db) =>
        {
            // Verificar se o email já está registado
            var emailExiste = await db.Clientes.AnyAsync(c => c.Email == dto.Email);
            if (emailExiste)
            {
                return Results.BadRequest(new { mensagem = "Já existe uma conta associada a este email." });
            }

            // Criar hash da palavra-passe de forma segura
            var passwordHasher = new PasswordHasher<Cliente>();
            var novoCliente = new Cliente
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Telefone = dto.Telefone,
                Morada = dto.Morada,
                CodigoPostal = dto.CodigoPostal,
                Nif = dto.Nif, 
                DataRegisto = DateTime.UtcNow
            };
            novoCliente.PalavraPasseHash = passwordHasher.HashPassword(novoCliente, dto.PalavraPasse);

            db.Clientes.Add(novoCliente);
            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = "Registo efetuado com sucesso!" });
        });
    }
}