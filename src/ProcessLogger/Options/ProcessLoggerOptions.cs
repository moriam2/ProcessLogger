using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

public class ProcessLoggerOptions
{
    public LogLevel StartLogLevel { get; set; } = LogLevel.Information;
    public LogLevel SuccessLogLevel { get; set; } = LogLevel.Information;
    public LogLevel FailureLogLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Optional delegate to configure the emitted activity span.
    /// </summary>
    public Action<Activity>? ConfigureSpan { get; set; }

    /// <summary>
    /// Optional custom <see cref="ActivitySource"/> to use instead of the default.
    /// If not provided, ProcessLogger will use <see cref="ProcessLoggerContext.ActivitySource"/>.
    /// </summary>
    public ActivitySource? ActivitySourceOverride { get; set; }

    public static ProcessLoggerOptions Default => new();
}
