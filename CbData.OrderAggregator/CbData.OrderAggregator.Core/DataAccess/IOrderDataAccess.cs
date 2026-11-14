using CbData.OrderAggregator.Core.Models;

namespace CbData.OrderAggregator.Core.DataAccess;

public interface IOrderDataAccess
{
    Task AddAsync(IReadOnlyCollection<Order> orders, CancellationToken cancellationToken = default);

    Task<OrderBatch?> TakeBatchAsync(CancellationToken cancellationToken = default);

    Task CompleteAsync(OrderBatch batch, CancellationToken cancellationToken = default);

    Task ReturnAsync(OrderBatch batch, CancellationToken cancellationToken = default);
}