using CbData.OrderAggregator.Core.Models;
using System.Text.Json;

namespace CbData.OrderAggregator.Core.Sender;

//injektován TextWriter kvůli testovatelnosti
public sealed class ConsoleSendingClient(TextWriter output) : ISendingClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task SendBatchAsync(OrderBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        string message = JsonSerializer.Serialize(batch, SerializerOptions);
        await output.WriteLineAsync(message.AsMemory(), cancellationToken);
    }
}
