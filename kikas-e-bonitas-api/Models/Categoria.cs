using System.ComponentModel.DataAnnotations;

namespace KikasEBonitas.Api.Models;

public class Categoria
{
    public int Id {get; set; }

    [Required (ErrorMessage = "O nome da categoria é obrigatório.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome da categoria deve ter entre 3 e 50 caracteres.")]
    public string Nome {get; set; } = string.Empty;

    // Propriedade de Navegação: 1 Categoria tem muitos Produtos
    public List<Produto> Produtos {get; set; } = new();
}