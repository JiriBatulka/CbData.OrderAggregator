using CbData.OrderAggregator.Core.Sender;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CbData.OrderAggregator.Tests.Api;

public sealed class OrderAggregatorFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider FakeTime { get; } = new();

    public ISendingClient SendingClient { get; } = Substitute.For<ISendingClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(FakeTime);
            services.AddSingleton(SendingClient);
        });
    }
}