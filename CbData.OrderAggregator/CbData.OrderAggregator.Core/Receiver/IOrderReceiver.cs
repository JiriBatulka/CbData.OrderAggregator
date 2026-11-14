using CbData.OrderAggregator.Core.Models;

namespace CbData.OrderAggregator.Core.Receiver;

public interface IOrderReceiver
{
    Task ReceiveAsync(IReadOnlyCollection<Order> orders, CancellationToken cancellationToken = default);
}