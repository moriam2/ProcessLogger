# ProcessLogger

**ProcessLogger** is a lightweight, allocation-friendly .NET library for tracking logical operations with structured log messages and optional OpenTelemetry tracing.

---

## ✅ Features

- 🔁 Logs **start / success / failure** messages for any logical process
- ⏱️ Includes **duration tracking** with zero allocations
- ⚠️ **Exception-safe** — failures are logged and rethrown
- ⚙️ **Configurable log levels** for start/success/failure
- 📡 **Optional OpenTelemetry** integration via `ActivitySource`
- 📦 No external dependencies (uses only `ILogger` and `System.Diagnostics`)
- 🧪 Works with any logging provider (Serilog, NLog, Console, etc.)

---

## 🚀 Quick Start

Install via NuGet:

```bash
dotnet add package ProcessLogger
```

Use the extension method to wrap a logical process:

```csharp
await logger.TrackProcessAsync("ImportFile", new { FileId = 123 }, async () =>
{
    await fileService.DownloadAsync();
    await fileService.ParseAsync();
    await fileService.StoreAsync();
});
```

Produces logs like:

```text
[ImportFile] Started process (FileId=123)
[ImportFile] Completed process in 208ms (FileId=123)
```

On failure:

```text
[ImportFile] Process failed after 89ms (FileId=123): System.Exception: Parse error
```

---

## ⚙️ Configuration

You can configure log levels via `ProcessLoggerOptions`:

```csharp
var options = new ProcessLoggerOptions
{
    StartLogLevel = LogLevel.Debug,
    SuccessLogLevel = LogLevel.Information,
    FailureLogLevel = LogLevel.Error
};

await logger.TrackProcessAsync("DoThing", options, async () => {
    await DoWorkAsync();
});
```

If no options are provided, **sensible defaults are used**.

---

## 🔍 OpenTelemetry Integration (Optional)

If your app uses OpenTelemetry, `ProcessLogger` will automatically:

- Start an `Activity` when tracing is enabled
- Attach duration and exceptions as span attributes

```csharp
// Automatically emits a trace span if listeners are active
await logger.TrackProcessAsync("ProcessOrder", async () => {
    await handler.ExecuteAsync();
});
```

If OpenTelemetry is not configured, it falls back to pure logging — no setup required.

---

## 🧪 Tests

Unit tests are included in the `/tests` directory and can be run with:

```bash
dotnet test
```

---

## 📄 License

MIT License. See [LICENSE](LICENSE) for details.

---

## ✨ Credits

Created and maintained by [Micheál Moriarty](https://github.com/moriam2).
