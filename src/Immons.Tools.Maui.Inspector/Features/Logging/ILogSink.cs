using Microsoft.Extensions.Logging;

namespace Immons.Tools.Maui.Inspector.Features.Logging;

/// <summary>Sink for log entries shown in the web inspector's Logs tab.</summary>
internal interface ILogSink
{
    void Write(LogLevel level, string category, string message);

    string ToJson();
}
