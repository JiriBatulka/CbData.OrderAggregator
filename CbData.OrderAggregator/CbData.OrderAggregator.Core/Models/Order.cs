namespace CbData.OrderAggregator.Core.Models;

public sealed class Order
{
    private Order(string productId, long quantity) 
    { 
        ProductId = productId;
        Quantity = quantity;
    }

    public string ProductId { get; }
    public long Quantity { get; }

    //statický Create u modelů jsem si zvykl používat na minulém projektu, zajistí se, že se objekt nevytvoří s nevalidními hodnotami
    public static Order Create(string productId, long quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return new Order(productId, quantity);
    }
}
