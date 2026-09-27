namespace KikasEBonitas.Web.Dtos;

public class CategoriaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int TotalProdutos { get; set; }
}

public class CriarCategoriaDto
{
    public string Nome { get; set; } = string.Empty;
}