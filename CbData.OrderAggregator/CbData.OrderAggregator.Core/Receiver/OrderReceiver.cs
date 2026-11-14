using CbData.OrderAggregator.Core.DataAccess;
using CbData.OrderAggregator.Core.Models;
using Microsoft.Extensions.Logging;

namespace CbData.OrderAggregator.Core.Receiver;

//tady by v rámci YAGNI šlo říct, že se jedná o zbytečnou vrstvu, protože jen provolá další jednu metodu
//ale udělal jsem jí jako ekvivalent k OrderSender
public sealed class OrderReceiver(IOrderDataAccess orderDataAccess, ILogger<OrderReceiver> logger) : IOrderReceiver
{
    public async Task ReceiveAsync(IReadOnlyCollection<Order> orders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orders);
        await orderDataAccess.AddAsync(orders, cancellationToken);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.OrdersReceived(orders.Count, orders.Select(order => order.ProductId));
        }
        return;
    }
}
