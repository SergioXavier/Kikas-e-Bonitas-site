namespace KikasEBonitas.Web.Dtos;

public class ProdutoDto
{
    public int Id {get; set; }
    public string Nome {get; set; } = string.Empty;
    public decimal Preco {get; set; }
    public int Stock {get; set; }
    public int CategoriaId {get; set; }
    public string NomeCategoria { get; set; } = string.Empty;
    public string? ImagemUrl { get; set; }
}

public class ResultadoPaginadoDto<T>
{
    public List<T> Itens {get; set; } = new();
    public int PaginaAtual {get; set; }
    public int TamanhoPagina { get; set; }
    public int TotalItens {get; set; }
}