using CbData.OrderAggregator.Core.Models;

namespace CbData.OrderAggregator.Core.Sender;

//tady tohle by schovalo reálný odesílač objednávek, nejspíš http client
public interface ISendingClient
{
    Task SendBatchAsync(OrderBatch batch, CancellationToken cancellationToken = default);
}
