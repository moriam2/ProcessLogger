using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ProcessLogger.Options;

public class ProcessLoggerOptions
{
    public LogLevel StartLogLevel { get; set; }
    public LogLevel SuccessLogLevel { get; set; } 
    public LogLevel FailureLogLevel { get; set; }
    /// <summary>
    /// Optional action to customize the Activity (OpenTelemetry span) when one is created.
    /// This is only called if tracing is enabled via ActivitySource listeners.
    /// </summary>
    public Action<Activity>? ConfigureSpan { get; set; }


    public static readonly ProcessLoggerOptions Default = new()
    {
        StartLogLevel = LogLevel.Information,
        SuccessLogLevel = LogLevel.Information,
        FailureLogLevel = LogLevel.Error,
        ConfigureSpan = null 
    };
}

