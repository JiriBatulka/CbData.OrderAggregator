using CbData.OrderAggregator.Core.DataAccess;
using CbData.OrderAggregator.Core.Models;
using CbData.OrderAggregator.Core.Receiver;
using CbData.OrderAggregator.Core.Sender;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;

namespace CbData.OrderAggregator.Tests.Api;

public sealed class OrderAggregationEndToEndTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Post_MultipleRequests_AggregatesQuantitiesPerProduct()
    {
        await using var factory = new OrderAggregatorFactory();
        using var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/orders",
            new[] { new { productId = "456", quantity = 5 }, new { productId = "789", quantity = 42 } }, Ct))
            .EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/orders",
            new[] { new { productId = "456", quantity = 3 } }, Ct))
            .EnsureSuccessStatusCode();

        var dataAccess = factory.Services.GetRequiredService<IOrderDataAccess>();
        var batch = await dataAccess.TakeBatchAsync(Ct);

        Assert.NotNull(batch);
        Assert.Equal(
            new Dictionary<string, long> { ["456"] = 8, ["789"] = 42 },
            batch.Orders.ToDictionary(order => order.ProductId, order => order.Quantity));
    }

    [Fact]
    public async Task Startup_WithSendIntervalBelowMinimum_Fails()
    {
        await using var baseFactory = new OrderAggregatorFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.UseSetting(
            $"{OrderSenderOptions.SectionName}:{nameof(OrderSenderOptions.SendInterval)}",
            "00:00:19"));

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task Post_WhenUnexpectedErrorOccurs_ReturnsProblemDetailsWithoutInternalDetails()
    {
        const string internalDetail = "Sensitive internal detail";
        var receiver = Substitute.For<IOrderReceiver>();
        receiver.ReceiveAsync(Arg.Any<IReadOnlyCollection<Order>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException(internalDetail)));

        await using var baseFactory = new OrderAggregatorFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton(receiver)));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/orders", new[] { new { productId = "456", quantity = 5 } }, Ct);
        var body = await response.Content.ReadAsStringAsync(Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(internalDetail, body);
    }
}