using CbData.OrderAggregator.Core.Models;
using System.Collections.Concurrent;

namespace CbData.OrderAggregator.Core.DataAccess;

public sealed class InMemoryOrderDataAccess : IOrderDataAccess
{
    private readonly ConcurrentDictionary<string, long> _orders = [];

    public Task AddAsync(IReadOnlyCollection<Order> orders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orders);
        if (orders.Any(order => order is null))
        {
            throw new ArgumentException("Orders cannot contain null items.", nameof(orders));
        }
        //pokud by se operace zrušila, tak to chceme zjistit dříve, než se přidají objednávky
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var order in orders)
        {
            AddQuantity(order.ProductId, order.Quantity);
        }
        return Task.CompletedTask;
    }

    //pro in-memory máme vlastně TakeBatch a Complete rovnou v metodě TryRemove u slovníku, pro tento případ je to jednodušší
    public Task<OrderBatch?> TakeBatchAsync(CancellationToken cancellationToken = default)
    {
        List<Order> orders = new(_orders.Count);
        foreach (var productId in _orders.Keys)
        {
            if (_orders.TryRemove(productId, out var quantity))
            {
                orders.Add(Order.Create(productId, quantity));
            }
        }
        if (orders.Count == 0)
        {
            return Task.FromResult<OrderBatch?>(null);
        }
        return Task.FromResult<OrderBatch?>(OrderBatch.Create(orders));
    }

    //pro in-memory nemá žádný význam, ale je to příprava na rozšíření pro persistenci v trvalém úložišti pro potvrzení zpracování dávky
    public Task CompleteAsync(OrderBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return Task.CompletedTask;
    }

    //pokud se něco pokazí před odesláním objednávek, tak je chceme vrátit, jinak by se ztratily
    public Task ReturnAsync(OrderBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        foreach (var order in batch.Orders)
        {
            AddQuantity(order.ProductId, order.Quantity);
        }
        return Task.CompletedTask;
    }

    private void AddQuantity(string productId, long quantity)
    {
        //Ta statická lambda zajistí, že se funkce nebude alokovat pokaždé, ale jen se do ní pošlou proměnné jako argument
        //pro stovky objednávek za vteřinu trochu overkill, ale nic to nestojí a do budoucna by to mohlo vliv mít
        _orders.AddOrUpdate(
            productId,
            static (_, quantity) => quantity,
            //aby nepřeteklo číslo, nicméně v DTO máme int a v modelu long, takže by se to stát nemělo (ale záleží na povaze objednávek)
            static (_, current, quantity) => checked(current + quantity),
            quantity);
    }
}