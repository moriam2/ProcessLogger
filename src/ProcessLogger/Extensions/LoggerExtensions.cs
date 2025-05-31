using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProcessLogger.Options;

namespace ProcessLogger.Extensions;

public static class LoggerExtensions
{
    private static readonly ActivitySource ActivitySource = new("ProcessLogger");

    public static Task TrackProcessAsync(
        this ILogger logger,
        string name,
        Func<Task> action,
        object? metadata = null,
        ProcessLoggerOptions? options = null)
    {
        return logger.TrackProcessAsync(
            name,
            _ => action(),
            metadata,
            options,
            CancellationToken.None);
    }

    public static Task TrackProcessAsync(
        this ILogger logger,
        string name,
        Func<CancellationToken, Task> action,
        object? metadata = null,
        ProcessLoggerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= ProcessLoggerOptions.Default;
        return TrackProcessInternalAsync(logger, name, action, metadata, options, cancellationToken);
    }

    private static async Task TrackProcessInternalAsync(
        ILogger logger,
        string name,
        Func<CancellationToken, Task> action,
        object? metadata,
        ProcessLoggerOptions options,
        CancellationToken cancellationToken)
    {
        logger.Log(options.StartLogLevel, "[{Name}] Starting process {Metadata}", name, metadata);

        var start = Stopwatch.GetTimestamp();

        Activity? activity = null;
        if (ActivitySource.HasListeners())
        {
            activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
            if (activity != null && options.ConfigureSpan is not null)
            {
                options.ConfigureSpan(activity);
            }
        }

        try
        {
            await action(cancellationToken);
            var durationMs = GetDurationMs(start);

            logger.Log(options.SuccessLogLevel, "[{Name}] Completed in {Duration}ms {Metadata}", name, durationMs, metadata);
            activity?.SetTag("process.status", "success");
            activity?.SetTag("process.duration_ms", durationMs);
        }
        catch (Exception ex)
        {
            var durationMs = GetDurationMs(start);

            logger.Log(options.FailureLogLevel, ex, "[{Name}] Failed after {Duration}ms {Metadata}", name, durationMs, metadata);
            activity?.SetTag("process.status", "failure");
            activity?.SetTag("process.duration_ms", durationMs);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            activity?.Stop();
        }
    }

    private static double GetDurationMs(long startTimestamp)
    {
        return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }
}
