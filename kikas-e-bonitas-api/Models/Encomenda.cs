namespace KikasEBonitas.Api.Models;

public class Encomenda
{
    public int Id { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public string NomeCliente { get; set; } = string.Empty;
    public string EmailCliente { get; set; } = string.Empty;
    public string Telemovel { get; set; } = string.Empty; // Necessário para associar o contacto do checkout
    public decimal ValorTotal { get; set; }

    // Novos campos para métodos de pagamento e portes de envio
    public string MetodoPagamento { get; set; } = string.Empty;
    public decimal CustoPortes { get; set; } = 0m;

    public StatusEncomenda Status { get; set; } = StatusEncomenda.Pendente;

    // Chave estrangeira e navegação para o cliente registado (opcional)
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public List<ItemEncomenda> Itens { get; set; } = new();
}

public enum StatusEncomenda
{
    Pendente,
    Paga,
    Enviada,
    Cancelada
}