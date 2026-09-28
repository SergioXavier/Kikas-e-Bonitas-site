using KikasEBonitas.Web.Dtos;

namespace KikasEBonitas.Web.Services;

public class CartItem
{
    public int ProdutoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Quantidade { get; set; }
}

public class CartService
{
    private readonly List<CartItem> _items = new();

    public event Action? OnChange;

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public void AdicionarProduto(int produtoId, string nome, decimal preco, int quantidade = 1)
    {
        var itemExistente = _items.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (itemExistente != null)
        {
            itemExistente.Quantidade += quantidade;
        }
        else
        {
            _items.Add(new CartItem
            {
                ProdutoId = produtoId,
                Nome = nome,
                Preco = preco,
                Quantidade = quantidade
            });
        }
        NotifyStateChanged();
    }

    public void RemoverProduto(int produtoId)
    {
        var item = _items.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (item != null)
        {
            _items.Remove(item);
            NotifyStateChanged();
        }
    }

    public void LimparCarrinho()
    {
        _items.Clear();
        NotifyStateChanged();
    }

    public decimal ObterTotal()
    {
        return _items.Sum(i => i.Preco * i.Quantidade);
    }

    public int ObterQuantidadeTotal()
    {
        return _items.Sum(i => i.Quantidade);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}