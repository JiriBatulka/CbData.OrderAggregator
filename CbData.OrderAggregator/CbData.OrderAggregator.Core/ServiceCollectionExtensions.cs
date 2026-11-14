using CbData.OrderAggregator.Core.DataAccess;
using CbData.OrderAggregator.Core.Receiver;
using CbData.OrderAggregator.Core.Sender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CbData.OrderAggregator.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderAggregator(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<OrderSenderOptions>()
            .BindConfiguration(OrderSenderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IOrderReceiver, OrderReceiver>();
        services.AddSingleton<IOrderSender, OrderSender>();
        services.AddSingleton<ISendingClient>(_ => new ConsoleSendingClient(Console.Out));
        services.AddHostedService<OrderSenderBackgroundService>();
        var dataAccessOptions = configuration
            .GetSection(DataAccessOptions.SectionName)
            .Get<DataAccessOptions>() ?? new DataAccessOptions();
        services.AddOrderDataAccess(dataAccessOptions.Provider);
        return services;
    }

    private static void AddOrderDataAccess(this IServiceCollection services, DataAccessProvider provider)
    {
        switch (provider)
        {
            case DataAccessProvider.InMemory:
                services.AddSingleton<IOrderDataAccess, InMemoryOrderDataAccess>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported data access provider '{provider}'. " +
                    $"Configure '{DataAccessOptions.SectionName}:{nameof(DataAccessOptions.Provider)}'.");
        }
    }
}