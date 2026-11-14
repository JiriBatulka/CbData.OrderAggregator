namespace CbData.OrderAggregator.Core.Models;

//pro in-memory implementaci je tohle funkčně zbytečné, protože máme jen jeden slovník v jedné instanci
//nicméně pokud bych měl např. více instancí v Redisu, tak už potřebuji mít možnost jednotlivá zpracování identifikovat
public sealed class OrderBatch
{
    public Guid BatchId { get; }
    public IReadOnlyCollection<Order> Orders { get; }

    private OrderBatch(Guid batchId, IReadOnlyCollection<Order> orders)
    {
        BatchId = batchId;
        Orders = orders;
    }

    public static OrderBatch Create(IReadOnlyCollection<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        if (orders.Count == 0)
        {
            throw new ArgumentException("Orders parameter cannot be empty while creating a batch.", nameof(orders));
        }
        if (orders.Any(order => order is null))
        {
            throw new ArgumentException("Orders parameter cannot contain null items while creating a batch.", nameof(orders));
        }

        return new OrderBatch(Guid.CreateVersion7(), [.. orders]);
    }
}