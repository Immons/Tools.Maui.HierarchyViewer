using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Features.XamlSync;

/// <summary>
/// Records successful property edits together with the XAML source location of the edited
/// object, for the XAML Updater tool to write back into the source files.
/// Only the latest value per (object, attribute) is kept.
/// </summary>
internal sealed class XamlChangeLog : IXamlChangeLog
{
    /// <summary>Sentinel returned by an editor's XamlValue to request attribute removal.</summary>
    public const string RemoveMarker = " remove-attribute ";

    internal sealed record Change(
        long Seq,
        string SourceUri,
        int Line,
        int Column,
        string ElementType,
        string Attribute,
        string Value,
        bool Remove);

    readonly object _gate = new();
    readonly Dictionary<string, Change> _latest = [];
    long _seq;
    volatile bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void Record(object target, string attribute, string value)
    {
        if (!_enabled)
            return;

        SourceInfo? info;
        try
        {
            info = Microsoft.Maui.VisualDiagnostics.GetSourceInfo(target);
        }
        catch
        {
            return;
        }
        if (info?.SourceUri == null)
            return;

        var remove = value == RemoveMarker;
        var change = new Change(
            0,
            info.SourceUri.ToString(),
            info.LineNumber,
            info.LinePosition,
            target.GetType().Name,
            attribute,
            remove ? "" : value,
            remove);

        lock (_gate)
        {
            _seq++;
            _latest[$"{change.SourceUri}:{change.Line}:{change.Column}|{attribute}"] = change with { Seq = _seq };
        }
    }

    public string ToJson(long since)
    {
        lock (_gate)
        {
            var changes = new JsonArray();
            foreach (var change in _latest.Values.Where(c => c.Seq > since).OrderBy(c => c.Seq))
            {
                changes.Add(new JsonObject
                {
                    ["seq"] = change.Seq,
                    ["source"] = change.SourceUri,
                    ["line"] = change.Line,
                    ["column"] = change.Column,
                    ["element"] = change.ElementType,
                    ["attribute"] = change.Attribute,
                    ["value"] = change.Value,
                    ["remove"] = change.Remove,
                });
            }

            return new JsonObject
            {
                ["seq"] = _seq,
                ["changes"] = changes,
            }.ToJsonString();
        }
    }
}
