using CbData.OrderAggregator.Core.DataAccess;
using CbData.OrderAggregator.Core.Models;
using CbData.OrderAggregator.Core.Receiver;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CbData.OrderAggregator.Tests.Receiver;

public sealed class OrderReceiverTests
{
    private readonly IOrderDataAccess _dataAccess = Substitute.For<IOrderDataAccess>();
    private readonly OrderReceiver _sut;

    public OrderReceiverTests()
    {
        _sut = new OrderReceiver(_dataAccess, NullLogger<OrderReceiver>.Instance);
    }

    [Fact]
    public async Task ReceiveAsync_PassesOrdersAndTokenToDataAccess()
    {
        Order[] orders = [Order.Create("456", 5), Order.Create("789", 42)];
        using var cts = new CancellationTokenSource();

        await _sut.ReceiveAsync(orders, cts.Token);

        await _dataAccess.Received(1).AddAsync(orders, cts.Token);
    }

    [Fact]
    public async Task ReceiveAsync_WithNullOrders_ThrowsAndDoesNotCallDataAccess()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ReceiveAsync(null!, TestContext.Current.CancellationToken));

        Assert.Equal("orders", exception.ParamName);
        await _dataAccess.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReceiveAsync_WhenDataAccessFails_PropagatesException()
    {
        _dataAccess
            .AddAsync(Arg.Any<IReadOnlyCollection<Order>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Storage failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ReceiveAsync([Order.Create("456", 5)], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReceiveAsync_DoesNotCompleteBeforeDataAccessCompletes()
    {
        var addCompletion = new TaskCompletionSource();
        _dataAccess
            .AddAsync(Arg.Any<IReadOnlyCollection<Order>>(), Arg.Any<CancellationToken>())
            .Returns(addCompletion.Task);

        var receiveTask = _sut.ReceiveAsync([Order.Create("456", 5)], TestContext.Current.CancellationToken);

        Assert.False(receiveTask.IsCompleted);

        addCompletion.SetResult();
        await receiveTask;
    }
}