using Microsoft.Extensions.Logging;
using ProcessLogger.Extensions;

namespace ProcessLogger.Tests;

public class LoggerExtensionsTests
{
    [Fact]
    public async Task TrackProcessAsync_LogsSuccess()
    {
        var logs = new List<string>();
        var logger = new TestLogger(msg => logs.Add(msg));

        await logger.TrackProcessAsync("TestOp", async () =>
        {
            await Task.Delay(10);
        });

        Assert.Contains(logs, l => l.Contains("Starting process"));
        Assert.Contains(logs, l => l.Contains("Completed in"));
    }

    private class TestLogger(Action<string> log) : ILogger
    {
        private readonly Action<string> _log = log;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _log(formatter(state, exception));
        }
    }
}
