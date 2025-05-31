using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProcessLogger.Options;

namespace ProcessLogger.Extensions;

public static class LoggerExtensions
{
    private static readonly ActivitySource ActivitySource = new("ProcessLogger");

    public static async Task TrackProcessAsync(
        this ILogger logger,
        string name,
        Func<Task> action,
        object? metadata = null,
        ProcessLoggerOptions? options = null)
    {
        options ??= ProcessLoggerOptions.Default;

        logger.Log(options.StartLogLevel, "[{Name}] Starting process {Metadata}", name, metadata);

        var start = Stopwatch.GetTimestamp();

        Activity? activity = null;
        if (ActivitySource.HasListeners())
        {
            activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        }

        try
        {
            await action();
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
