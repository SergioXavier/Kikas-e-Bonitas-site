using System.ComponentModel.DataAnnotations;

namespace KikasEBonitas.Api.Dtos;

public class LoginDto
{
    [Required(ErrorMessage = "O utilizador é obrigatório.")]
    public string Usuario {get; set; } = string.Empty;

    [Required(ErrorMessage = "A palavra-passe é obrigatória.")]
    public string Senha {get; set; } = string.Empty;
}

public class RespostaTokenDto
{
    public string Token {get; set; } = string.Empty;
    public DateTime Expiracao {get; set; }
}