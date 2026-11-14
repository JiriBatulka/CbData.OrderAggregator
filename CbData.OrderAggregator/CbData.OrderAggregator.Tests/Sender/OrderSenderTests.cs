using CbData.OrderAggregator.Core.DataAccess;
using CbData.OrderAggregator.Core.Models;
using CbData.OrderAggregator.Core.Sender;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CbData.OrderAggregator.Tests.Sender;

public sealed class OrderSenderTests
{
    private readonly ISendingClient _sendingClient = Substitute.For<ISendingClient>();
    private readonly IOrderDataAccess _dataAccess = Substitute.For<IOrderDataAccess>();
    private readonly OrderSender _sut;

    public OrderSenderTests()
    {
        _sut = new OrderSender(_sendingClient, _dataAccess, NullLogger<OrderSender>.Instance);
    }

    [Fact]
    public async Task SendAsync_WhenNoBatch_DoesNotSendCompleteOrReturn()
    {
        _dataAccess
            .TakeBatchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<OrderBatch?>(null));

        await _sut.SendAsync(TestContext.Current.CancellationToken);

        await _sendingClient.DidNotReceiveWithAnyArgs().SendBatchAsync(default!, TestContext.Current.CancellationToken);
        await _dataAccess.DidNotReceiveWithAnyArgs().CompleteAsync(default!, TestContext.Current.CancellationToken);
        await _dataAccess.DidNotReceiveWithAnyArgs().ReturnAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_PassesTokenToTakeAndSend()
    {
        var batch = CreateBatch();
        using var cts = new CancellationTokenSource();
        SetupTake(batch);

        await _sut.SendAsync(cts.Token);

        await _dataAccess.Received(1).TakeBatchAsync(cts.Token);
        await _sendingClient.Received(1).SendBatchAsync(batch, cts.Token);
    }

    [Fact]
    public async Task SendAsync_WhenSendSucceeds_CompletesBatchAfterSending()
    {
        var batch = CreateBatch();
        SetupTake(batch);

        await _sut.SendAsync(TestContext.Current.CancellationToken);

        Received.InOrder(() =>
        {
            _sendingClient.SendBatchAsync(batch, Arg.Any<CancellationToken>());
            _dataAccess.CompleteAsync(batch, CancellationToken.None);
        });
        await _dataAccess.DidNotReceiveWithAnyArgs().ReturnAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_WhenSendFails_ReturnsBatchAndRethrows()
    {
        var batch = CreateBatch();
        var failure = new InvalidOperationException("Send failed");
        SetupTake(batch);
        _sendingClient
            .SendBatchAsync(Arg.Any<OrderBatch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(failure);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SendAsync(TestContext.Current.CancellationToken));

        Assert.Same(failure, exception);
        await _dataAccess.Received(1).ReturnAsync(batch, CancellationToken.None);
        await _dataAccess.DidNotReceiveWithAnyArgs().CompleteAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_WhenSendIsCancelled_ReturnsBatchWithNonCancelledToken()
    {
        var batch = CreateBatch();
        using var cts = new CancellationTokenSource();
        SetupTake(batch);
        _sendingClient
            .SendBatchAsync(Arg.Any<OrderBatch>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _sut.SendAsync(cts.Token));

        await _dataAccess.Received(1).ReturnAsync(batch, CancellationToken.None);
        await _dataAccess.DidNotReceiveWithAnyArgs().CompleteAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_WhenSendAndReturnFail_RethrowsOriginalSendException()
    {
        var batch = CreateBatch();
        var sendFailure = new InvalidOperationException("Send failed");
        SetupTake(batch);
        _sendingClient
            .SendBatchAsync(Arg.Any<OrderBatch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(sendFailure);
        _dataAccess
            .ReturnAsync(Arg.Any<OrderBatch>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Return failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SendAsync(TestContext.Current.CancellationToken));

        Assert.Same(sendFailure, exception);
        await _dataAccess.DidNotReceiveWithAnyArgs().CompleteAsync(default!, TestContext.Current.CancellationToken);
    }

    private void SetupTake(OrderBatch batch) =>
        _dataAccess
            .TakeBatchAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<OrderBatch?>(batch));

    private static OrderBatch CreateBatch() =>
        OrderBatch.Create([Order.Create("456", 5), Order.Create("789", 42)]);
}