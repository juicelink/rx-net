using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;

namespace Tests.Core;

public class MyLogFormatter : ILogFormatter
{
    public string Format(
        int scopeLevel,
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        string message,
        Exception exception)
    {
        var level = GetLogLevelString(logLevel);

        var exceptionStr = exception == null
            ? string.Empty
            : $"\n{exception}";

        return $"{DateTime.Now:HH:mm:ss.fff} {level} {message}{exceptionStr}";
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRA",
            LogLevel.Debug => "DEB",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WAR",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRI",
            _ => "UNK"
        };
    }
}