using System.ComponentModel.DataAnnotations;

namespace KikasEBonitas.Api.Dtos;

// DTO para Criar ou Atualizar um Produto (não inclui o ID, pois o ID é gerado pelo servidor)
public class CriarProdutoDto
{
    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Range(0.01, 10000.00, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Preco { get; set; }

    [Range(0, 1000, ErrorMessage = "O stock não pode ser negativo.")]
    public int Stock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria válida.")]
    public int CategoriaId { get; set; }
    public string? ImagemUrl { get; set; }
}

// DTO para Resposta de Dados da API (o que o cliente lê)
public class ProdutoRespostaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Stock { get; set; }
    public int CategoriaId { get; set; }
    public string NomeCategoria { get; set; } = string.Empty;
    public string? ImagemUrl { get; set; }
}