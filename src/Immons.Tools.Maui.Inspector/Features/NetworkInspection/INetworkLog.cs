namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Sink for HTTP requests captured by <see cref="MauiInspectorHttpHandler"/>.</summary>
internal interface INetworkLog
{
    void Record(string method, string url, int status, double ms, long? bytes,
        string? error = null, string? tag = null, string? requestBody = null, string? responseBody = null);

    string ToJson();

    /// <summary>Drops all recorded entries (the panel's "clear list" button).</summary>
    void Clear();

    /// <summary>Captured bodies of one entry as {"request":…,"response":…}; null when unknown seq.</summary>
    string? BodiesJson(long seq);
}
