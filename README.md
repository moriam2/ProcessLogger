# ProcessLogger

**ProcessLogger** is a lightweight .NET library for tracking logical operations with structured log messages and optional OpenTelemetry tracing.

---

## ✅ Features

- 🔁 Logs **start / success / failure** messages for any logical process  
- ⏱️ Includes **duration tracking**  
- ⚠️ **Exception-safe** — failures are logged and rethrown  
- ⚙️ **Configurable log levels** for start, success, and failure  
- 📡 **Optional OpenTelemetry** span generation via `ActivitySource`  
- 🔧 Optional hook to customize each span via `ConfigureSpan`  
- 🌐 Optional override of `ActivitySource` to integrate with existing OTEL pipelines  
- 📦 No external dependencies (uses only `ILogger` and `System.Diagnostics`)  
- 🧪 Compatible with all logging providers (Serilog, NLog, Console, etc.)

---

## 🚀 Quick Start

Install via NuGet:

```bash
dotnet add package ProcessLogger
```

Use the extension method to wrap a logical process:

```csharp
await logger.TrackProcessAsync("ImportFile", async () =>
{
    await fileService.DownloadAsync();
    await fileService.ParseAsync();
    await fileService.StoreAsync();
}, metadata: new { FileId = 123 });
```

Produces logs like:

```text
[ImportFile] Starting process (FileId=123)
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

await logger.TrackProcessAsync("DoThing", async () =>
{
    await DoWorkAsync();
}, options: options);
```

If no options are provided, **sensible defaults are used**.

---

## 📡 OpenTelemetry Integration (Optional)

If your app uses OpenTelemetry, `ProcessLogger` will automatically:

- Start an `Activity` when OpenTelemetry listeners are present
- Attach tags like:
  - `process.status` (`success` or `failure`)
  - Any thrown exception (as error status)

You can customize the span using `ConfigureSpan`:

```csharp
var options = new ProcessLoggerOptions
{
    ConfigureSpan = span =>
    {
        span?.SetTag("custom.tag", "abc123");
        span?.SetStatus(ActivityStatusCode.Error, "manual failure");
    }
};
```

You can also override the `ActivitySource` if you're using a shared source:

```csharp
var options = new ProcessLoggerOptions
{
    ActivitySourceOverride = myCustomSource
};
```

### 🔒 Not Using OpenTelemetry? No Problem!

If you're not using OpenTelemetry, you don't need to configure or think about it.  
**ProcessLogger works perfectly with just logs.**  
OpenTelemetry features are completely optional and safe to ignore.

---

## 🧪 Tests

Unit tests are included in the `/tests` directory and can be run with:

```bash
dotnet test
```

Test coverage includes:
- Logging behavior with/without metadata
- Failure propagation
- Log level customization
- OpenTelemetry span emission and tagging
- Custom `ActivitySource` usage

---

## 📄 License

MIT License. See [LICENSE](LICENSE) for details.

---

## ✨ Credits

Created and maintained by [Micheál Moriarty](https://github.com/moriam2).