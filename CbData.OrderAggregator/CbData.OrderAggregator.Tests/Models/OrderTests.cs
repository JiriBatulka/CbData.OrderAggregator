using CbData.OrderAggregator.Core.Models;

namespace CbData.OrderAggregator.Tests.Models;

public sealed class OrderTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties()
    {
        var order = Order.Create("456", 5);

        Assert.Equal("456", order.ProductId);
        Assert.Equal(5L, order.Quantity);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("007")]
    public void Create_WithLeadingZerosOrZero_PreservesProductIdAsIs(string productId)
    {
        var order = Order.Create(productId, 1);

        Assert.Equal(productId, order.ProductId);
    }

    [Fact]
    public void Create_WithMaxQuantity_Succeeds()
    {
        var order = Order.Create("456", long.MaxValue);

        Assert.Equal(long.MaxValue, order.Quantity);
    }

    [Fact]
    public void Create_WithNullProductId_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Order.Create(null!, 1));

        Assert.Equal("productId", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WithEmptyOrWhiteSpaceProductId_ThrowsArgumentException(string productId)
    {
        var exception = Assert.Throws<ArgumentException>(() => Order.Create(productId, 1));

        Assert.Equal("productId", exception.ParamName);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void Create_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(long quantity)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Order.Create("456", quantity));

        Assert.Equal("quantity", exception.ParamName);
    }
}