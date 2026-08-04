using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Ring buffer of HTTP requests shown in the web inspector's Network tab.</summary>
internal sealed class NetworkLog : INetworkLog
{
    internal sealed record Entry(
        long Seq,
        string Time,
        string Method,
        string Url,
        int Status,
        double Ms,
        long? Bytes,
        string? Error,
        string? Tag,
        string? RequestBody,
        string? ResponseBody);

    readonly RingLog<Entry> _log = new(limit: 200);

    public void Record(string method, string url, int status, double ms, long? bytes,
        string? error = null, string? tag = null, string? requestBody = null, string? responseBody = null) =>
        _log.Add(seq => new Entry(seq, DateTime.Now.ToString("HH:mm:ss"),
            method, url, status, ms, bytes, error, tag, requestBody, responseBody));

    public void Clear() => _log.Clear();

    public string ToJson()
    {
        var array = new JsonArray();
        foreach (var e in _log.NewestFirst())
        {
            array.Add(new JsonObject
            {
                ["seq"] = e.Seq,
                ["time"] = e.Time,
                ["method"] = e.Method,
                ["url"] = e.Url,
                ["status"] = e.Status,
                ["ms"] = Math.Round(e.Ms, 1),
                ["bytes"] = e.Bytes,
                ["error"] = e.Error,
                ["tag"] = e.Tag,
                ["hasBody"] = e.RequestBody != null || e.ResponseBody != null,
            });
        }
        return new JsonObject { ["entries"] = array }.ToJsonString();
    }

    public string? BodiesJson(long seq)
    {
        if (_log.Find(e => e.Seq == seq) is not { } entry)
            return null;

        return new JsonObject
        {
            ["seq"] = entry.Seq,
            ["method"] = entry.Method,
            ["url"] = entry.Url,
            ["status"] = entry.Status,
            ["request"] = entry.RequestBody,
            ["response"] = entry.ResponseBody,
        }.ToJsonString();
    }
}
