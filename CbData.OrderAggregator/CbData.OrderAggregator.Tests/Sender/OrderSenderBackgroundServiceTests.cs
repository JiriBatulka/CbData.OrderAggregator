using CbData.OrderAggregator.Core.Sender;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CbData.OrderAggregator.Tests.Sender;

public sealed class OrderSenderBackgroundServiceTests : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private readonly TimerTrackingTimeProvider _time = new();
    private readonly IOrderSender _sender = Substitute.For<IOrderSender>();
    private readonly SemaphoreSlim _sendCalled = new(0);
    private readonly OrderSenderBackgroundService _sut;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public OrderSenderBackgroundServiceTests()
    {
        _sender.SendAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _sendCalled.Release();
            return Task.CompletedTask;
        });

        _sut = new OrderSenderBackgroundService(
            _sender,
            Options.Create(new OrderSenderOptions { SendInterval = Interval, ShutdownTokenTimeout = TimeSpan.FromSeconds(5) }),
            _time,
            NullLogger<OrderSenderBackgroundService>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_SendsOnlyAfterInterval()
    {
        await StartAsync();

        _time.Advance(Interval - TimeSpan.FromSeconds(1));
        Assert.False(await _sendCalled.WaitAsync(TimeSpan.FromMilliseconds(100), Ct));

        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.True(await _sendCalled.WaitAsync(WaitTimeout, Ct));

        await _sut.StopAsync(Ct);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSendFails_KeepsRunning()
    {
        var callCount = 0;
        _sender.SendAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _sendCalled.Release();
            return Interlocked.Increment(ref callCount) == 1
                ? Task.FromException(new InvalidOperationException())
                : Task.CompletedTask;
        });
        await StartAsync();

        _time.Advance(Interval);
        Assert.True(await _sendCalled.WaitAsync(WaitTimeout, Ct));

        // Po chybě si služba zaregistruje další čekání – smyčka běží dál.
        await _time.WaitForTimerAsync();
        _time.Advance(Interval);
        Assert.True(await _sendCalled.WaitAsync(WaitTimeout, Ct));

        await _sut.StopAsync(Ct);
    }

    [Fact]
    public async Task StopAsync_SendsPendingOrdersWithNonCancelledToken()
    {
        var tokenWasCancelled = true;
        _sender.SendAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            tokenWasCancelled = call.Arg<CancellationToken>().IsCancellationRequested;
            return Task.CompletedTask;
        });
        await StartAsync();

        await _sut.StopAsync(Ct);

        await _sender.Received(1).SendAsync(Arg.Any<CancellationToken>());
        Assert.False(tokenWasCancelled);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _sendCalled.Dispose();
    }

    // Od .NET 10 běží celé ExecuteAsync na pozadí – čas se smí posouvat až ve chvíli, kdy služba čeká na timer.
    private async Task StartAsync()
    {
        await _sut.StartAsync(Ct);
        await _time.WaitForTimerAsync();
    }

    // Hlásí vytvoření timeru (Task.Delay i CancellationTokenSource s timeoutem ho vytváří přes TimeProvider).
    private sealed class TimerTrackingTimeProvider : FakeTimeProvider
    {
        private readonly SemaphoreSlim _timerCreated = new(0);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            _timerCreated.Release();
            return timer;
        }

        public async Task WaitForTimerAsync() =>
            Assert.True(await _timerCreated.WaitAsync(WaitTimeout), "The service did not start waiting for the timer.");
    }
}