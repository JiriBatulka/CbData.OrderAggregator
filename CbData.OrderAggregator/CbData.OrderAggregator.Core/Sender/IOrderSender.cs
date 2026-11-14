namespace CbData.OrderAggregator.Core.Sender;

public interface IOrderSender
{
    Task SendAsync(CancellationToken cancellationToken = default);
}
