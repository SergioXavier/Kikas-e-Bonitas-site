namespace KikasEBonitas.Api.Models;

public class ItemEncomenda
{
    public int Id {get; set; }
    public int EncomendaId {get; set; }
    public Encomenda Encomenda {get; set; } = null!;
    public int ProdutoId {get; set; }
    public Produto Produto {get; set; } = null!;
    public int Quantidade {get; set; }

    // Preço unitário guardado no momento da criação do pedido
    public decimal PrecoUnitario {get; set; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
}