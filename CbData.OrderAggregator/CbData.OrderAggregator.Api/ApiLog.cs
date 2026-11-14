namespace CbData.OrderAggregator.Api;

internal static partial class ApiLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing the request.")]
    public static partial void UnhandledException(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request could not be read.")]
    public static partial void BadRequest(this ILogger logger, Exception exception);
}