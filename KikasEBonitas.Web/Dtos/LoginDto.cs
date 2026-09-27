namespace KikasEBonitas.Web.Dtos;

public class LoginDto
{
    public string Usuario {get; set; } = string.Empty;
    public string Senha {get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string Token {get; set; } = string.Empty;
}