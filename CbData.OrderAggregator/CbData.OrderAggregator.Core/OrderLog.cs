using Microsoft.Extensions.Logging;

namespace CbData.OrderAggregator.Core;

internal static partial class OrderLog
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Order batch {BatchId} with {OrderCount} orders sent.")]
    public static partial void BatchSent(this ILogger logger, Guid batchId, int orderCount);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Received {OrderCount} orders for products {ProductIds}.")]
    public static partial void OrdersReceived(
        this ILogger logger, int orderCount, IEnumerable<string> productIds);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Order batch {BatchId} was not sent and has been returned to the store.")]
    public static partial void BatchReturned(this ILogger logger, Guid batchId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to return batch {BatchId} to storage; {OrderCount} orders may be lost.")]
    public static partial void BatchReturnFailed(this ILogger logger, Exception exception, Guid batchId, int orderCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order send failed in background service, but it will be retried.")]
    public static partial void SendFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending orders before shutdown failed.")]
    public static partial void ShutdownSendTimedOut(this ILogger logger, Exception exception);
}