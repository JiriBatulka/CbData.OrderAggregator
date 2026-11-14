using CbData.OrderAggregator.Core.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace CbData.OrderAggregator.Tests.DI;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddOrderAggregator_BuildsValidServiceProvider()
    {
        using var provider = TestServiceProvider.Build();

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddOrderAggregator_WithoutDataAccessSection_UsesInMemory()
    {
        using var provider = TestServiceProvider.Build();

        Assert.IsType<InMemoryOrderDataAccess>(provider.GetRequiredService<IOrderDataAccess>());
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("5")]
    public void AddOrderAggregator_WithUnsupportedProvider_Throws(string provider)
    {
        Assert.Throws<InvalidOperationException>(() => TestServiceProvider.Build(new()
        {
            [$"{DataAccessOptions.SectionName}:{nameof(DataAccessOptions.Provider)}"] = provider
        }));
    }
}