using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Immons.Tools.Maui.Inspector.Features.Logging;

/// <summary>Ring buffer of log entries shown in the web inspector's Logs tab.</summary>
internal sealed class LogSink : ILogSink
{
    internal sealed record Entry(long Seq, string Time, string Level, string Category, string Message);

    readonly RingLog<Entry> _log = new(limit: 500);

    public void Write(LogLevel level, string category, string message) =>
        _log.Add(seq => new Entry(seq, DateTime.Now.ToString("HH:mm:ss.fff"), level.ToString(), category, message));

    public string ToJson()
    {
        var array = new JsonArray();
        foreach (var e in _log.NewestFirst())
        {
            array.Add(new JsonObject
            {
                ["time"] = e.Time,
                ["level"] = e.Level,
                ["category"] = e.Category,
                ["message"] = e.Message,
            });
        }
        return new JsonObject { ["entries"] = array }.ToJsonString();
    }
}
