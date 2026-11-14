using CbData.OrderAggregator.Core.Models;

namespace CbData.OrderAggregator.Api.Models;

internal static class OrderDtoMapping
{
    internal static Order ToOrder(this OrderDto dto) 
        => Order.Create(dto.ProductId, dto.Quantity);
}
