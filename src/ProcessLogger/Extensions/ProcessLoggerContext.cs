using System.Diagnostics;

namespace ProcessLogger.Extensions;

public static class ProcessLoggerContext
{
    public static readonly ActivitySource ActivitySource = new("ProcessLogger");
}
