namespace CbData.OrderAggregator.Core.DataAccess;

//Redis jako logická volba do budoucna místo InMemory
public enum DataAccessProvider
{
    Unknown = 0,
    InMemory = 1
}
