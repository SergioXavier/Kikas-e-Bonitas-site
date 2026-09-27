namespace KikasEBonitas.Web.Dtos;

public class CriarProdutoDto
{
    public string Nome {get; set; } = string.Empty;
    public decimal Preco {get; set; }
    public int Stock {get; set; }
    public int CategoriaId {get; set; }
}