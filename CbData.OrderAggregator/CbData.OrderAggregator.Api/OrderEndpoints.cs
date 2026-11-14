using CbData.OrderAggregator.Api.Models;
using CbData.OrderAggregator.Core.Receiver;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace CbData.OrderAggregator.Api;

public static class OrderEndpoints
{
    internal const int MaxOrdersPerRequest = 1000;
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", ReceiveOrders)
            .WithName("ReceiveOrders")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        return app;
    }

    //MaxLength nedávám do konfigurace, protože je to pouze ochrana před zbytečným zpracováváním nesmyslného requestu, nikoliv business pravidlo
    //reálně by maximální velikost měla být výrazně vyšší než očekávaná velikost v jednom requestu, ale ta v zadání není.
    private static async Task<Results<Accepted, ValidationProblem>> ReceiveOrders(
        [MinLength(1)][MaxLength(MaxOrdersPerRequest)] IReadOnlyList<OrderDto> orders,
        IOrderReceiver orderReceiver,
        CancellationToken cancellationToken)
    {
        //pokud by to spadlo později, tak by se to vrátilo jako 500 kvůli NullReferenceException,
        //nicméně jedná se o validační chybu
        if (orders.Any(order => order is null))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(orders)] = ["Orders cannot contain null items."]
            });
        }
        await orderReceiver.ReceiveAsync([.. orders.Select(order => order.ToOrder())], cancellationToken);
        return TypedResults.Accepted((string?)null);
    }
}