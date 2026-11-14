namespace CbData.OrderAggregator.Core.DataAccess;

public sealed class DataAccessOptions
{
    public const string SectionName = "DataAccessOptions";
    public DataAccessProvider Provider { get; set; } = DataAccessProvider.InMemory;
}
