using Immons.Tools.Maui.Inspector.Inspector;
using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Web.Endpoints;

/// <summary>Selection ids, mode flags, sync state and frame stats for the 1 s poll.</summary>
internal sealed class SelectionJsonBuilder(
    IActiveInspectorProvider inspectors,
    IElementRegistry elements,
    IEditHistory history,
    IXamlChangeLog xamlChanges,
    ISyncTracker sync) : ISelectionJsonBuilder
{
    public string Build()
    {
        var result = new JsonObject
        {
            ["id"] = null,
            ["compare"] = null,
            ["measure"] = false,
            ["select"] = false,
            ["overlay"] = false,
            ["wysiwyg"] = xamlChanges.Enabled,
            ["hseq"] = history.LastSeq,
            ["perf"] = PerfJson(),
        };

        if (inspectors.Current is not { } inspector)
            return result.ToJsonString();

        result["id"] = inspector.SelectedElement is { } selected ? elements.GetId(selected) : null;
        result["compare"] = inspector.CompareElement is { } compare ? elements.GetId(compare) : null;
        result["measure"] = inspector.MeasureMode;
        result["select"] = inspector.RemoteSelectModeActive;
        result["overlay"] = inspector.OverlayShown;
        result["paint"] = inspector.DebugPaintActive;
        result["sync"] = sync.Connected;
        // The server answers while backgrounded, but edits queued on the main thread do not run.
        result["fg"] = AppForegroundState.IsForeground;
        result["slow"] = SlowAnimations.Enabled;
        return result.ToJsonString();
    }

    static JsonObject? PerfJson() => FrameStats.Current is { } f
        ? new JsonObject
        {
            ["fps"] = Math.Round(f.Fps),
            ["avg"] = Math.Round(f.AverageMs, 1),
            ["worst"] = Math.Round(f.WorstMs, 1),
        }
        : null;
}
