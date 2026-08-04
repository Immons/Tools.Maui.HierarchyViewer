using Microsoft.Extensions.Logging;

namespace Immons.Tools.Maui.Inspector;

/// <summary>Routes ILogger output into the web inspector's Logs tab.</summary>
public static class MauiInspectorLoggingExtensions
{
    /// <summary>Adds the MauiInspector log sink: <c>builder.Logging.AddMauiInspector();</c></summary>
    public static ILoggingBuilder AddMauiInspector(this ILoggingBuilder builder, LogLevel minimumLevel = LogLevel.Debug)
    {
        builder.AddProvider(new MauiInspectorLoggerProvider(minimumLevel));
        return builder;
    }
}

sealed class MauiInspectorLoggerProvider(LogLevel minimumLevel) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new SinkLogger(categoryName, minimumLevel);

    public void Dispose()
    {
    }

    sealed class SinkLogger(string category, LogLevel minimumLevel) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var message = formatter(state, exception);
            if (exception != null)
                message += $" — {exception.GetType().Name}: {exception.Message}";
            Inspector.InspectorServices.Logs.Write(logLevel, category, message);
        }
    }
}
