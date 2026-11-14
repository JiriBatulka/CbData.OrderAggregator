using System.Net;
using System.Net.Http.Json;
using System.Text;
using CbData.OrderAggregator.Api;
using Microsoft.AspNetCore.Http;

namespace CbData.OrderAggregator.Tests.Api;

public sealed class OrderEndpointsTests(OrderAggregatorFactory factory) : IClassFixture<OrderAggregatorFactory>
{
    private const string Url = "/api/orders";
    private const int MaxOrdersPerRequest = OrderEndpoints.MaxOrdersPerRequest;

    private readonly HttpClient _client = factory.CreateClient();

    public static TheoryData<string> InvalidRequests =>
    [
        "[]",
        CreateOrdersJson(MaxOrdersPerRequest + 1),

        """[{"productId":"456","quantity":0}]""",
        """[{"productId":null,"quantity":5}]""",
        """[{"productId":"abc","quantity":5}]""",
        $$"""[{"productId":"{{new string('1', 51)}}","quantity":5}]""",

        """[{"productId":"456","quantity":5}""",

        "[null]",
    ];

    public static TheoryData<string> BoundaryValidRequests =>
    [
        """[{"productId":"007","quantity":1}]""",

        $$"""[{"productId":"{{new string('1', 50)}}","quantity":1}]""",
        CreateOrdersJson(MaxOrdersPerRequest),
    ];

    [Fact]
    public async Task Post_ValidOrders_ReturnsAccepted()
    {
        using var response = await PostAsync("""[{"productId":"456","quantity":5},{"productId":"789","quantity":42}]""");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(BoundaryValidRequests))]
    public async Task Post_BoundaryValues_ReturnsAccepted(string json)
    {
        using var response = await PostAsync(json);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Post_InvalidRequest_ReturnsBadRequest(string json)
    {
        using var response = await PostAsync(json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidProductId_ReturnsValidationProblemForProductId()
    {
        using var response = await PostAsync("""[{"productId":"abc","quantity":5}]""");

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(problem.Errors.Keys, key => key.Contains("ProductId", StringComparison.OrdinalIgnoreCase));
    }

    private Task<HttpResponseMessage> PostAsync(string json) =>
        _client.PostAsync(
            Url,
            new StringContent(json, Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

    private static string CreateOrdersJson(int count) =>
        $"[{string.Join(',', Enumerable.Repeat("""{"productId":"456","quantity":1}""", count))}]";
}