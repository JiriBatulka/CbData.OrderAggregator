using System.ComponentModel.DataAnnotations;

namespace CbData.OrderAggregator.Core.Sender;

public sealed class OrderSenderOptions
{
    public const string SectionName = "OrderSenderOptions";

    [Range(typeof(TimeSpan), "00:00:20", "00:05:00")]
    public TimeSpan SendInterval { get; set; } = TimeSpan.FromSeconds(20);

    [Range(typeof(TimeSpan), "00:00:01", "00:00:25")]
    public TimeSpan ShutdownTokenTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
