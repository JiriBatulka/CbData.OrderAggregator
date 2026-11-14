using CbData.OrderAggregator.Core.DataAccess;
using Microsoft.Extensions.Logging;

namespace CbData.OrderAggregator.Core.Sender;

public sealed class OrderSender(
    ISendingClient sendingClient,
    IOrderDataAccess orderDataAccess,
    ILogger<OrderSender> logger) : IOrderSender
{
    public async Task SendAsync(CancellationToken cancellationToken = default)
    {
        var ordersBatch = await orderDataAccess.TakeBatchAsync(cancellationToken);
        if (ordersBatch is null)
        {
            return;
        }
        try
        {
            await sendingClient.SendBatchAsync(ordersBatch, cancellationToken);
        }
        catch
        {
            try
            {
                await orderDataAccess.ReturnAsync(ordersBatch, CancellationToken.None);
                logger.BatchReturned(ordersBatch.BatchId);
            }
            catch (Exception ex)
            {
                //Pokazilo se něco při vrácení batch do úložiště, takže to zalogujeme, ale nechceme přepsat původní výjimku
                logger.BatchReturnFailed(ex, ordersBatch.BatchId, ordersBatch.Orders.Count);
            }
            throw;
        }
        await orderDataAccess.CompleteAsync(ordersBatch, CancellationToken.None);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.BatchSent(ordersBatch.BatchId, ordersBatch.Orders.Count);
        }
    }
}
