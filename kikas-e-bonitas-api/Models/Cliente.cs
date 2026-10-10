namespace KikasEBonitas.Api.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PalavraPasseHash { get; set; } = string.Empty;

    // Campos opcionais de contacto e faturação
    public string? Telefone { get; set; }
    public string? Morada { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Nif { get; set; } 
    public DateTime DataRegisto { get; set; } = DateTime.UtcNow;

    // Relação: Um cliente tem várias encomendas
    public List<Encomenda> Encomendas { get; set; } = new();
}