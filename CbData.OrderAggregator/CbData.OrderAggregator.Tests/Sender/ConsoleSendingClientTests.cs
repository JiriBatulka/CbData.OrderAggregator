using System.Text.Json;
using CbData.OrderAggregator.Core.Models;
using CbData.OrderAggregator.Core.Sender;

namespace CbData.OrderAggregator.Tests.Sender;

public sealed class ConsoleSendingClientTests : IDisposable
{
    private readonly StringWriter _output = new();
    private readonly ConsoleSendingClient _sut;

    public ConsoleSendingClientTests()
    {
        _sut = new ConsoleSendingClient(_output);
    }

    [Fact]
    public async Task SendBatchAsync_WritesBatchAsCamelCaseJson()
    {
        var batch = OrderBatch.Create([Order.Create("007", 5), Order.Create("789", 42)]);

        await _sut.SendBatchAsync(batch, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(_output.ToString());
        var root = json.RootElement;
        Assert.Equal(batch.BatchId, root.GetProperty("batchId").GetGuid());

        var orders = root.GetProperty("orders").EnumerateArray().ToArray();
        Assert.Equal(2, orders.Length);
        Assert.Equal("007", orders[0].GetProperty("productId").GetString());
        Assert.Equal(5L, orders[0].GetProperty("quantity").GetInt64());
        Assert.Equal("789", orders[1].GetProperty("productId").GetString());
        Assert.Equal(42L, orders[1].GetProperty("quantity").GetInt64());
    }

    [Fact]
    public async Task SendBatchAsync_CalledTwice_WritesEachBatchOnSeparateLine()
    {
        var first = OrderBatch.Create([Order.Create("456", 5)]);
        var second = OrderBatch.Create([Order.Create("789", 42)]);

        await _sut.SendBatchAsync(first, TestContext.Current.CancellationToken);
        await _sut.SendBatchAsync(second, TestContext.Current.CancellationToken);

        var lines = _output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal(first.BatchId, ParseBatchId(lines[0]));
        Assert.Equal(second.BatchId, ParseBatchId(lines[1]));
    }

    [Fact]
    public async Task SendBatchAsync_WithNullBatch_ThrowsArgumentNullException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.SendBatchAsync(null!, TestContext.Current.CancellationToken));

        Assert.Equal("batch", exception.ParamName);
    }

    [Fact]
    public async Task SendBatchAsync_WithCancelledToken_ThrowsAndWritesNothing()
    {
        var batch = OrderBatch.Create([Order.Create("456", 5)]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.SendBatchAsync(batch, cts.Token));

        Assert.Empty(_output.ToString());
    }

    public void Dispose() => _output.Dispose();

    private static Guid ParseBatchId(string line)
    {
        using var json = JsonDocument.Parse(line);
        return json.RootElement.GetProperty("batchId").GetGuid();
    }
}