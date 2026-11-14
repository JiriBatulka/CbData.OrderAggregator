using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CbData.OrderAggregator.Core.Sender;

public sealed class OrderSenderBackgroundService(
    IOrderSender orderSender,
    IOptions<OrderSenderOptions> options,
    TimeProvider timeProvider,
    ILogger<OrderSenderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (true)
            {
                //chceme počkat 20 vteřin po skončení operace, jinak by mohl být porušen požadavek na "ne častěji, než jednou za 20 vteřin"
                //pokud by operace trvala déle
                await Task.Delay(options.Value.SendInterval, timeProvider, stoppingToken);
                await SendSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) 
        {
            //běžné zavření služby, takže to nechceme nechat spadnout na OperationCanceledException
        }
        using var shutdownToken = new CancellationTokenSource(options.Value.ShutdownTokenTimeout, timeProvider);
        try
        {
            await SendSafelyAsync(shutdownToken.Token);
        }
        catch (OperationCanceledException ex) when (shutdownToken.IsCancellationRequested)
        {
            logger.ShutdownSendTimedOut(ex);
        }
    }

    private async Task SendSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await orderSender.SendAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.SendFailed(ex);
        }
    }
}