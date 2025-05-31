using Microsoft.Extensions.Logging;

namespace ProcessLogger.Options;

public class ProcessLoggerOptions
{
    public LogLevel StartLogLevel { get; set; } = LogLevel.Debug;
    public LogLevel SuccessLogLevel { get; set; } = LogLevel.Information;
    public LogLevel FailureLogLevel { get; set; } = LogLevel.Error;

    public static ProcessLoggerOptions Default => new();
}

