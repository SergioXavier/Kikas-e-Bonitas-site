using KikasEBonitas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KikasEBonitas.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Representa a tabela "Produtos" na nossa base de dados
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Encomenda> Encomendas => Set<Encomenda>();
    public DbSet<ItemEncomenda> ItensEncomenda => Set<ItemEncomenda>();
    public DbSet<Cliente> Clientes { get; set; }

    // Popula a base de dados com categorias padrão no momento do OnModelCreating
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração explícita do relacionamento entre Encomenda e ItemEncomenda
        modelBuilder.Entity<Encomenda>()
            .HasMany(e => e.Itens)
            .WithOne(i => i.Encomenda)
            .HasForeignKey(i => i.EncomendaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuração do relacionamento entre ItemEncomenda e Produto
        modelBuilder.Entity<ItemEncomenda>()
            .HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId);

        // Configuração adicional se necessária para precisão decimal
        modelBuilder.Entity<Produto>()
            .Property(p => p.Preco)
            .HasConversion<double>();

        modelBuilder.Entity<ItemEncomenda>()
            .Property(i => i.PrecoUnitario)
            .HasConversion<double>();

        modelBuilder.Entity<Encomenda>()
            .Property(e => e.ValorTotal)
            .HasConversion<double>();

        // Seed Data para Categorias
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nome = "Cosmética" },
            new Categoria { Id = 2, Nome = "Utilitários" },
            new Categoria { Id = 3, Nome = "Naturais" }
        );
    }
}