using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProcessLogger.Options;

namespace ProcessLogger.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ILogger"/> to track the execution of asynchronous operations
/// with structured logging and optional telemetry (via <see cref="Activity"/>).
/// </summary>
public static class LoggerExtensions
{
    private static readonly ActivitySource ActivitySource = new("ProcessLogger");

    /// <summary>
    /// Tracks the execution of an asynchronous operation with logging and optional telemetry.
    /// Logs the start and success/failure, and emits a span if listeners are registered.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The name of the process being tracked.</param>
    /// <param name="action">The asynchronous operation to execute.</param>
    /// <param name="metadata">Optional metadata object to include in the logs.</param>
    /// <param name="options">Optional logger behavior configuration. If null, default options are used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Tracks the execution of an asynchronous operation with logging, telemetry, and cancellation support.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The name of the process being tracked.</param>
    /// <param name="action">The asynchronous operation to execute, which supports cancellation.</param>
    /// <param name="metadata">Optional metadata object to include in the logs.</param>
    /// <param name="options">Optional logger behavior configuration. If null, default options are used.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Internal implementation of process tracking with logging, exception handling, and span emission.
    /// </summary>
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

        var source = options.ActivitySourceOverride ?? ProcessLoggerContext.ActivitySource;

        Activity? activity = null;
        if (source.HasListeners())
        {
            activity = source.StartActivity(name, ActivityKind.Internal);
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
            //activity?.SetTag("process.duration_ms", durationMs); //Remove (redundant, spans already include duration)
        }
        catch (Exception ex)
        {
            var durationMs = GetDurationMs(start);

            logger.Log(options.FailureLogLevel, ex, "[{Name}] Failed after {Duration}ms {Metadata}", name, durationMs, metadata);
            activity?.SetTag("process.status", "failure");
            // activity?.SetTag("process.duration_ms", durationMs);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            activity?.Stop();
        }
    }

    /// <summary>
    /// Calculates the duration in milliseconds from a given <see cref="Stopwatch"/> start timestamp.
    /// </summary>
    /// <param name="startTimestamp">The timestamp captured when the operation started.</param>
    /// <returns>The duration in milliseconds.</returns>
    private static double GetDurationMs(long startTimestamp)
    {
        return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }
}
