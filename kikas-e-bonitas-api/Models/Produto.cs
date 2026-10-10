using System.ComponentModel.DataAnnotations;

namespace KikasEBonitas.Api.Models;

public class Produto
{
    public int Id {get; set; }
    [Required(ErrorMessage = "O campo do produto é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome {get; set; } = string.Empty;

    [Range(0.01, 10000.00, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Preco {get; set; }

    [Range(0, 1000, ErrorMessage = "O stock não pode ser negativo.")]
    public int Stock {get; set; }

    // Chave Estrangeira para a Categoria
    public int CategoriaId { get; set; }

    // Propriedade de Navegação
    public Categoria? Categoria { get; set; }
    public string? ImagemUrl { get; set; }
}