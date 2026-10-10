namespace KikasEBonitas.Api.Dtos;

public class ClienteRegistoDto
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PalavraPasse { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Morada { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Nif { get; set; }
}

public record ClienteAtualizaDto(
    string Nome, 
    string Email, 
    string? Telefone, 
    string? Morada, 
    string? CodigoPostal, 
    string? Nif
);