using CbData.OrderAggregator.Core.DataAccess;
using CbData.OrderAggregator.Core.Models;

namespace CbData.OrderAggregator.Tests.DataAccess;

public abstract class OrderDataAccessContractTests
{
    private IOrderDataAccess? _sut;

    protected IOrderDataAccess Sut => _sut ??= CreateSut();

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract IOrderDataAccess CreateSut();

    [Fact]
    public async Task TakeBatchAsync_WhenEmpty_ReturnsNull()
    {
        var batch = await Sut.TakeBatchAsync(Ct);

        Assert.Null(batch);
    }

    [Fact]
    public async Task AddAsync_SameProductInOneCall_AggregatesQuantity()
    {
        await Sut.AddAsync([Order.Create("456", 5), Order.Create("456", 3)], Ct);

        var quantities = await TakeQuantitiesAsync();

        Assert.Equal(8L, Assert.Single(quantities, q => q.Key == "456").Value);
    }

    [Fact]
    public async Task AddAsync_SameProductInMultipleCalls_AggregatesQuantity()
    {
        await Sut.AddAsync([Order.Create("456", 5)], Ct);
        await Sut.AddAsync([Order.Create("456", 3)], Ct);

        var quantities = await TakeQuantitiesAsync();

        Assert.Equal(new Dictionary<string, long> { ["456"] = 8 }, quantities);
    }

    [Fact]
    public async Task AddAsync_DifferentProducts_KeepsThemSeparate()
    {
        await Sut.AddAsync([Order.Create("456", 5), Order.Create("789", 42)], Ct);

        var quantities = await TakeQuantitiesAsync();

        Assert.Equal(new Dictionary<string, long> { ["456"] = 5, ["789"] = 42 }, quantities);
    }

    [Fact]
    public async Task AddAsync_ProductIdsDifferingOnlyInLeadingZeros_AreDistinct()
    {
        await Sut.AddAsync([Order.Create("7", 1), Order.Create("007", 2)], Ct);

        var quantities = await TakeQuantitiesAsync();

        Assert.Equal(new Dictionary<string, long> { ["7"] = 1, ["007"] = 2 }, quantities);
    }

    [Fact]
    public async Task AddAsync_WhenSumOverflows_ThrowsAndKeepsPreviousQuantity()
    {
        await Sut.AddAsync([Order.Create("456", long.MaxValue)], Ct);

        await Assert.ThrowsAsync<OverflowException>(
            () => Sut.AddAsync([Order.Create("456", 1)], Ct));

        var quantities = await TakeQuantitiesAsync();
        Assert.Equal(long.MaxValue, quantities["456"]);
    }

    [Fact]
    public async Task TakeBatchAsync_RemovesOrders_SecondTakeReturnsNull()
    {
        await Sut.AddAsync([Order.Create("456", 5)], Ct);

        var first = await Sut.TakeBatchAsync(Ct);
        var second = await Sut.TakeBatchAsync(Ct);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task CompleteAsync_AfterTake_NothingRemains()
    {
        await Sut.AddAsync([Order.Create("456", 5)], Ct);
        var batch = await Sut.TakeBatchAsync(Ct);

        await Sut.CompleteAsync(batch!, Ct);

        Assert.Null(await Sut.TakeBatchAsync(Ct));
    }

    [Fact]
    public async Task ReturnAsync_AfterTake_NextTakeContainsSameQuantities()
    {
        await Sut.AddAsync([Order.Create("456", 5), Order.Create("789", 42)], Ct);
        var batch = await Sut.TakeBatchAsync(Ct);

        await Sut.ReturnAsync(batch!, Ct);

        var quantities = await TakeQuantitiesAsync();
        Assert.Equal(new Dictionary<string, long> { ["456"] = 5, ["789"] = 42 }, quantities);
    }

    [Fact]
    public async Task ReturnAsync_MergesWithOrdersAddedAfterTake()
    {
        await Sut.AddAsync([Order.Create("456", 5)], Ct);
        var batch = await Sut.TakeBatchAsync(Ct);
        await Sut.AddAsync([Order.Create("456", 3), Order.Create("789", 1)], Ct);

        await Sut.ReturnAsync(batch!, Ct);

        var quantities = await TakeQuantitiesAsync();
        Assert.Equal(new Dictionary<string, long> { ["456"] = 8, ["789"] = 1 }, quantities);
    }

    [Fact]
    public async Task AddAsync_WithNullOrders_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.AddAsync(null!, Ct));
    }

    [Fact]
    public async Task AddAsync_WithNullItem_ThrowsAndAddsNothing()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Sut.AddAsync([Order.Create("456", 5), null!], Ct));

        Assert.Null(await Sut.TakeBatchAsync(Ct));
    }

    [Fact]
    public async Task AddAsync_WithCancelledToken_ThrowsAndAddsNothing()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Sut.AddAsync([Order.Create("456", 5)], cts.Token));

        Assert.Null(await Sut.TakeBatchAsync(Ct));
    }

    [Fact]
    public async Task CompleteAsync_WithNullBatch_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.CompleteAsync(null!, Ct));
    }

    [Fact]
    public async Task ReturnAsync_WithNullBatch_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.ReturnAsync(null!, Ct));
    }

    [Fact]
    public async Task AddAsync_Concurrently_LosesNoUpdates()
    {
        const int iterations = 10_000;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, iterations),
            Ct,
            async (_, token) => await Sut.AddAsync([Order.Create("456", 1), Order.Create("789", 2)], token));

        var quantities = await TakeQuantitiesAsync();
        Assert.Equal(iterations, quantities["456"]);
        Assert.Equal(2L * iterations, quantities["789"]);
    }

    [Fact]
    public async Task AddAndTake_Concurrently_LosesNoOrders()
    {
        const int producers = 8;
        const int ordersPerProducer = 5_000;
        var taken = new Dictionary<string, long>();

        var producing = Task.WhenAll(Enumerable.Range(0, producers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < ordersPerProducer; i++)
            {
                await Sut.AddAsync([Order.Create("456", 1), Order.Create("789", 2)], Ct);
            }
        }, Ct)));

        while (!producing.IsCompleted)
        {
            await TakeAndCompleteIntoAsync(taken);
            await Task.Yield();
        }

        await producing;
        await TakeAndCompleteIntoAsync(taken);

        Assert.Equal(producers * ordersPerProducer, taken["456"]);
        Assert.Equal(2L * producers * ordersPerProducer, taken["789"]);
    }

    private async Task<Dictionary<string, long>> TakeQuantitiesAsync()
    {
        var batch = await Sut.TakeBatchAsync(Ct);
        Assert.NotNull(batch);

        return batch.Orders.ToDictionary(order => order.ProductId, order => order.Quantity);
    }

    private async Task TakeAndCompleteIntoAsync(Dictionary<string, long> taken)
    {
        var batch = await Sut.TakeBatchAsync(Ct);
        if (batch is null)
        {
            return;
        }

        foreach (var order in batch.Orders)
        {
            taken[order.ProductId] = taken.GetValueOrDefault(order.ProductId) + order.Quantity;
        }

        await Sut.CompleteAsync(batch, Ct);
    }
}