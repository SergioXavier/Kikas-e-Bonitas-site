namespace KikasEBonitas.Web.Dtos;

public class ResumoFornecedorDto
{
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public string ImagemUrl { get; set; } = string.Empty;
    public int StockAtual { get; set; }
    public int QuantidadeTotalAEncomendar { get; set; }
}