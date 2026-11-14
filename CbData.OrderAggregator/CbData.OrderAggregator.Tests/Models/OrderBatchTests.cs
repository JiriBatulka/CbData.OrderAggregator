using CbData.OrderAggregator.Core.Models;

namespace CbData.OrderAggregator.Tests.Models;

public sealed class OrderBatchTests
{
    [Fact]
    public void Create_WithOrders_ContainsSameOrdersInSameOrder()
    {
        Order[] orders = [Order.Create("456", 5), Order.Create("789", 42)];

        var batch = OrderBatch.Create(orders);

        Assert.Equal(orders, batch.Orders);
    }

    [Fact]
    public void Create_AssignsVersion7BatchId()
    {
        var batch = OrderBatch.Create([Order.Create("456", 5)]);

        Assert.NotEqual(Guid.Empty, batch.BatchId);
        Assert.Equal(7, batch.BatchId.Version);
    }

    [Fact]
    public void Create_CalledTwice_AssignsDifferentBatchIds()
    {
        Order[] orders = [Order.Create("456", 5)];

        var first = OrderBatch.Create(orders);
        var second = OrderBatch.Create(orders);

        Assert.NotEqual(first.BatchId, second.BatchId);
    }

    [Fact]
    public void Create_WhenSourceCollectionChangesAfterwards_BatchIsNotAffected()
    {
        List<Order> orders = [Order.Create("456", 5)];

        var batch = OrderBatch.Create(orders);
        orders.Add(Order.Create("789", 42));

        Assert.Single(batch.Orders);
    }

    [Fact]
    public void Create_WithNullOrders_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => OrderBatch.Create(null!));

        Assert.Equal("orders", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyOrders_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => OrderBatch.Create([]));

        Assert.Equal("orders", exception.ParamName);
    }
}