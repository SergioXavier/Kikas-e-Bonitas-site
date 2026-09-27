namespace KikasEBonitas.Web.Dtos;

public class CriarEncomendaDto
{
    public string NomeCliente { get; set; } = string.Empty;
    public string EmailCliente { get; set; } = string.Empty;
    public List<CriarItemEncomendaDto> Itens { get; set; } = new();
}

public class CriarItemEncomendaDto
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}

public class EncomendaRespostaDto
{
    public int Id { get; set; }
    public DateTime DataCriacao { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public string EmailCliente { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public List<ItemEncomendaRespostaDto> Itens { get; set; } = new();
}

public class ItemEncomendaRespostaDto
{
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}