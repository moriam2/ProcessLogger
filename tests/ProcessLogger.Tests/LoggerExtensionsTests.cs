using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessLogger.Extensions;
using ProcessLogger.Options;

namespace ProcessLogger.Tests;

[Collection("NonParallelTests")] //ActivitySource.AddActivityListener(listener); listeners persist globally across tests and can interfere with each other.
public partial class LoggerExtensionsTests
{

    [Fact]
    public async Task TrackProcessAsync_LogsStartAndSuccess()
    {
        var logs = new List<string>();
        var logger = new TestLogger(msg => logs.Add(msg));

        await logger.TrackProcessAsync("ImportFile", async () =>
        {
            await Task.Delay(10); // Simulated work
        });

        Assert.Contains(logs, l => l.Contains("Starting process"));
        Assert.Contains(logs, l => l.Contains("Completed in"));
    }
    [Fact]
    public async Task TrackProcessAsync_LogsMetadataCorrectly()
    {
        var logger = new TestLogger();

        await logger.TrackProcessAsync("Meta", async () =>
        {
            await Task.Delay(10);
        }, new { Id = 42, Status = "OK" });

        Assert.Contains(logger.Entries, e => e.Message.Contains("42") && e.Message.Contains("OK"));
    }


    [Fact]
    public async Task TrackProcessAsync_WorksWithoutMetadata()
    {
        var logger = new TestLogger();
        var options = new ProcessLoggerOptions
        {
            StartLogLevel = LogLevel.Information,
            SuccessLogLevel = LogLevel.Information,
            FailureLogLevel = LogLevel.Error
        };

        await logger.TrackProcessAsync("NoMetadataProcess", async () =>
        {
            await Task.Delay(10);
        }, metadata: null, options: options);

        var startLogged = logger.Entries.Any(e => e.Level == LogLevel.Information && e.Message.Contains("Starting process"));
        var completeLogged = logger.Entries.Any(e => e.Level == LogLevel.Information && e.Message.Contains("Completed in"));

        Assert.True(startLogged, "Expected start log was not found.");
        Assert.True(completeLogged, "Expected completion log was not found.");
    }


    [Fact]
    public async Task TrackProcessAsync_LogsFailureAndRethrows()
    {
        var logs = new List<string>();
        var logger = new TestLogger(msg => logs.Add(msg));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await logger.TrackProcessAsync("FailingProcess", async () =>
            {
                await Task.Delay(5);
                throw new InvalidOperationException("Boom");
            });
        });

        Assert.Equal("Boom", ex.Message);
        Assert.Contains(logs, l => l.Contains("Starting process"));
        Assert.Contains(logs, l => l.Contains("Failed after"));
        Assert.DoesNotContain(logs, l => l.Contains("Completed")); // Should not say success
    }

    [Fact]
    public async Task TrackProcessAsync_WithCancellationToken_LogsFailure_WhenCancelled()
    {
        var logs = new List<string>();
        var logger = new TestLogger(msg => logs.Add(msg));

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var ex = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await logger.TrackProcessAsync("CancellableProcess",
                async token =>
                {
                    await Task.Delay(50, token);
                },
                cancellationToken: cts.Token);
        });

        Assert.Contains(logs, l => l.Contains("Starting process"));
        Assert.Contains(logs, l => l.Contains("Failed after"));
        Assert.DoesNotContain(logs, l => l.Contains("Completed"));
    }

    [Fact]
    public async Task TrackProcessAsync_UsesConfiguredLogLevels()
    {
        var logger = new TestLogger();
        var options = new ProcessLoggerOptions
        {
            StartLogLevel = LogLevel.Warning,
            SuccessLogLevel = LogLevel.Critical,
            FailureLogLevel = LogLevel.Debug
        };

        await logger.TrackProcessAsync("Example", async () =>
        {
            await Task.Delay(10);
        }, options: options);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Starting process"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Critical && e.Message.Contains("Completed"));
    }

    [Fact]
    public async Task TrackProcessAsync_LogsAt_Debug_Info_Error_Levels()
    {
        var logger = new TestLogger();
        var options = new ProcessLoggerOptions
        {
            StartLogLevel = LogLevel.Debug,
            SuccessLogLevel = LogLevel.Information,
            FailureLogLevel = LogLevel.Error
        };

        await logger.TrackProcessAsync("HappyPath", async () =>
        {
            await Task.Delay(10);
        }, options: options);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("Starting process"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Completed"));
    }

    [Fact]
    public async Task TrackProcessAsync_LogsAt_Warning_Critical()
    {
        var logger = new TestLogger();
        var options = new ProcessLoggerOptions
        {
            StartLogLevel = LogLevel.Warning,
            SuccessLogLevel = LogLevel.Critical,
            FailureLogLevel = LogLevel.Trace
        };

        await logger.TrackProcessAsync("AnotherSuccess", async () =>
        {
            await Task.Delay(5);
        }, options: options);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Starting process"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Critical && e.Message.Contains("Completed"));
    }

    [Fact]
    public async Task TrackProcessAsync_LogsFailure_AtConfiguredLevel()
    {
        var logger = new TestLogger();
        var options = new ProcessLoggerOptions
        {
            StartLogLevel = LogLevel.Debug,
            SuccessLogLevel = LogLevel.Information,
            FailureLogLevel = LogLevel.Warning
        };

        var ex = await Assert.ThrowsAsync<ApplicationException>(async () =>
        {
            await logger.TrackProcessAsync("FailsWithWarning", async () =>
            {
                throw new ApplicationException("Whoops");
            }, options: options);
        });

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("Starting process"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Failed after"));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("Completed"));
    }

    [Fact]
    public async Task TrackProcessAsync_LogsFailure_AsCritical()
    {
        var logger = new TestLogger();
        var options = new ProcessLoggerOptions
        {
            StartLogLevel = LogLevel.Information,
            SuccessLogLevel = LogLevel.Debug,
            FailureLogLevel = LogLevel.Critical
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await logger.TrackProcessAsync("CriticalFailure", async () =>
            {
                await Task.Delay(5);
                throw new InvalidOperationException("Boom");
            }, options: options);
        });

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Starting process"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Critical && e.Message.Contains("Failed after"));
    }



    [Fact]
    public async Task TrackProcessAsync_AllowsLoggerMessageAttributeUsage()
    {
        var logs = new List<string>();
        var logger = new TestLogger(msg => logs.Add(msg));

        await logger.TrackProcessAsync("TestProcess", async () =>
        {
            LogHello(logger, "world");
            await Task.Delay(10);
        });

        Assert.Contains(logs, l => l.Contains("Starting process"));
        Assert.Contains(logs, l => l.Contains("Hello world"));
        Assert.Contains(logs, l => l.Contains("Completed in"));
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Hello {Name}")]
    private static partial void LogHello(ILogger logger, string name);






    [Fact]
    public async Task TrackProcessAsync_EmitsSpan_WithSuccessTags()
    {
        var logger = new TestLogger();
        var capturedActivities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ProcessLogger",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => { },
            ActivityStopped = activity => capturedActivities.Add(activity)
        };

        ActivitySource.AddActivityListener(listener);

        await logger.TrackProcessAsync("TelemetrySuccess", async () =>
        {
            await Task.Delay(10);
        });

        Assert.Single(capturedActivities);
        var activity = capturedActivities[0];

        Assert.Equal("TelemetrySuccess", activity.DisplayName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.Equal("success", activity.Tags.FirstOrDefault(t => t.Key == "process.status").Value);
        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }


    [Fact]
    public async Task TrackProcessAsync_EmitsSpan_WithErrorStatusOnFailure()
    {
        var logger = new TestLogger();
        var capturedActivities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ProcessLogger",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => { },
            ActivityStopped = activity => capturedActivities.Add(activity)
        };

        ActivitySource.AddActivityListener(listener);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await logger.TrackProcessAsync("TelemetryFailure", async () =>
            {
                throw new InvalidOperationException("Expected failure");
            });
        });

        Assert.Single(capturedActivities);
        var activity = capturedActivities[0];

        Assert.Equal("TelemetryFailure", activity.DisplayName);
        Assert.Equal("failure", activity.Tags.FirstOrDefault(t => t.Key == "process.status").Value);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Contains("Expected failure", activity.StatusDescription);
    }


    [Fact]
    public async Task ConfigureSpan_IsCalledAndAddsTags()
    {
        var logger = new TestLogger();
        List<Activity> _capturedSpans = new();
        var wasCalled = false;

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "ProcessLogger",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = act => _capturedSpans.Add(act)
        };
        ActivitySource.AddActivityListener(listener);

        var options = new ProcessLoggerOptions
        {
            ConfigureSpan = span =>
            {
                wasCalled = true;
                span?.SetTag("custom.tag", "test123");
            }
        };

        await logger.TrackProcessAsync("ConfigureSpanTest", async () => await Task.Delay(10), options: options);

        Assert.True(wasCalled);
        var activity = Assert.Single(_capturedSpans);
        Assert.Equal("test123", activity.Tags.FirstOrDefault(t => t.Key == "custom.tag").Value);
    }

    [Fact]
    public async Task ConfigureSpan_IsNotCalled_IfNoActivityListener()
    {
        var logger = new TestLogger();
        var wasCalled = false;

        var options = new ProcessLoggerOptions
        {
            ConfigureSpan = _ => wasCalled = true
        };

        await logger.TrackProcessAsync("NoTelemetry", async () => await Task.Delay(5), options: options);

        Assert.False(wasCalled);
    }

    [Fact]
    public async Task ConfigureSpan_Null_DoesNotBreakLogging()
    {
        var logger = new TestLogger();

        var options = new ProcessLoggerOptions
        {
            ConfigureSpan = null,
            StartLogLevel = LogLevel.Information,
            SuccessLogLevel = LogLevel.Information
        };

        await logger.TrackProcessAsync("NullConfigureSpan", async () => await Task.Delay(5), options: options);

        Assert.Contains(logger.Entries, e => e.Message.Contains("Starting process"));
        Assert.Contains(logger.Entries, e => e.Message.Contains("Completed in"));
    }

    [Fact]
    public async Task TrackProcessAsync_EmitsSpan_WhenLoggingIsDisabled()
    {
        var capturedActivities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ProcessLogger",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var logger = new SilentLogger(); // suppresses logging

        await logger.TrackProcessAsync("SilentSpan", async () =>
        {
            await Task.Delay(10);
        });

        Assert.Single(capturedActivities);
        var span = capturedActivities[0];

        Assert.Equal("SilentSpan", span.DisplayName);
        Assert.Equal("success", span.Tags.FirstOrDefault(t => t.Key == "process.status").Value);
    }


    [Fact]
    public async Task ConfigureSpan_CanMutateSpanProperties()
    {
        var logger = new TestLogger();
        List<Activity> _capturedSpans = new();

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "ProcessLogger",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStopped = act => _capturedSpans.Add(act)
        };
        ActivitySource.AddActivityListener(listener);

        var options = new ProcessLoggerOptions
        {
            ConfigureSpan = span =>
            {
                span.DisplayName = "RenamedSpan";
                span.SetStatus(ActivityStatusCode.Error, "custom description");
            }
        };

        await logger.TrackProcessAsync("OriginalName", async () => await Task.Delay(5), options: options);

        var span = Assert.Single(_capturedSpans);
        Assert.Equal("RenamedSpan", span.DisplayName);
        Assert.Equal("custom description", span.StatusDescription);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
    }


    [Fact]
    public async Task TrackProcessAsync_EmitsSpan_EvenWhenLoggingIsDisabled()
    {
        var capturedActivities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ProcessLogger",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        // Logger that disables all log levels
        var logger = new DisabledLogger();

        await logger.TrackProcessAsync("SilentTrace", async () => await Task.Delay(5));

        var span = Assert.Single(capturedActivities);
        Assert.Equal("SilentTrace", span.DisplayName);
        Assert.Equal("success", span.Tags.FirstOrDefault(t => t.Key == "process.status").Value);
    }

[Fact]
public async Task TrackProcessAsync_Uses_Custom_ActivitySource_WhenProvided()
{
    var logger = new TestLogger();
    var customSpans = new List<Activity>();
    var customSource = new ActivitySource("CustomSource");

    using var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "CustomSource",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        ActivityStarted = _ => { },
        ActivityStopped = activity => customSpans.Add(activity)
    };

    ActivitySource.AddActivityListener(listener);

    var options = new ProcessLoggerOptions
    {
        ActivitySourceOverride = customSource
    };

    await logger.TrackProcessAsync("CustomSpanProcess", async () =>
    {
        await Task.Delay(5);
    }, options: options);

    var span = Assert.Single(customSpans);
    Assert.Equal("CustomSpanProcess", span.DisplayName);
    Assert.Equal("success", span.Tags.FirstOrDefault(t => t.Key == "process.status").Value);
}



    [Fact]
    public async Task TrackProcessAsync_AllowsConcurrentUsage()
    {
        var logger = new TestLogger();
        const int concurrentTasks = 100;

        var tasks = Enumerable.Range(0, concurrentTasks).Select(i =>
            logger.TrackProcessAsync($"Process{i}", async () =>
            {
                await Task.Delay(20);
            }));

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentTasks, logger.Entries.Count(e => e.Message.Contains("Completed")));
    }

    [Fact]
    public async Task TrackProcessAsync_AlwaysRethrows_OnFailure()
    {
        var logger = new TestLogger();

        await Assert.ThrowsAsync<DivideByZeroException>(() =>
            logger.TrackProcessAsync("Explodes", async () =>
            {
                int a = 1;
                int b = 0;
                _ = a / b; // Runtime exception
            }));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Starting process"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Failed after"));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("Completed"));
    }






    // Minimal test logger
    private class TestLogger : ILogger
    {
        private readonly ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> _entries = new();
        private readonly Action<string>? _simpleLog;

        public TestLogger(Action<string>? simpleLog = null)
        {
            _simpleLog = simpleLog;
        }

        public IReadOnlyCollection<(LogLevel Level, string Message, Exception? Exception)> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _entries.Enqueue((logLevel, message, exception));
            _simpleLog?.Invoke(message);
        }
    }


    private class DisabledLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;
        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Intentionally does nothing
        }
    }

    private class SilentLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;
        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // No-op
        }
    }



}
