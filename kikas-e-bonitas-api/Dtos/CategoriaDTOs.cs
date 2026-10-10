using System.ComponentModel.DataAnnotations;

namespace KikasEBonitas.Api.Dtos;

// DTO para Criar ou Atualizar Categoria
public class CriarCategoriaDto
{
    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome da categoria deve ter entre 3 e 50 caracteres.")]
    public string Nome {get; set; } = string.Empty;    
}

// DTO de Resposta da Categoria
public class CategoriaRespostaDto
{
    public int Id {get; set; }
    public string Nome {get; set; } = string.Empty;
    public int TotalProdutos {get; set; }
}