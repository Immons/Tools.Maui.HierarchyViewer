using System.Net;
using System.Text.Json.Nodes;
using Immons.Tools.Maui.Inspector.Web.Http;

namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>
/// GET /api/structure/catalog — controls offered by the "Add element" popup and the toolbox.
/// POST /api/structure/add-at — toolbox drop on the mirror (window-dp coordinates).
/// </summary>
internal sealed class StructureEndpoint(
    IMainThreadDispatcher mainThread,
    IElementCatalog catalog,
    IStructureCommands structure,
    IActiveInspectorProvider inspectors,
    IElementRegistry elements) : IHttpEndpoint
{
    string? _cachedJson;

    public async Task<bool> TryHandle(HttpListenerContext context, string method, string path)
    {
        if (method == HttpVerbs.Post && path == ApiRoutes.Structure.AddAt)
        {
            var node = await RequestBody.ReadJson(context).ConfigureAwait(false);
            var x = node?["x"]?.GetValue<double>() ?? 0;
            var y = node?["y"]?.GetValue<double>() ?? 0;
            var type = node?["type"]?.GetValue<string>() ?? "";

            var (id, error) = await mainThread.RunAsync(() => structure.AddAt(new Point(x, y), type)).ConfigureAwait(false);
            var json = new JsonObject { ["ok"] = error == null, ["id"] = id, ["error"] = error }.ToJsonString();
            await HttpResponse.WriteJson(context, json).ConfigureAwait(false);
            return true;
        }

        if (method == HttpVerbs.Post && path == ApiRoutes.Structure.Hit)
        {
            var node = await RequestBody.ReadJson(context).ConfigureAwait(false);
            var x = node?["x"]?.GetValue<double>() ?? 0;
            var y = node?["y"]?.GetValue<double>() ?? 0;

            var id = await mainThread.RunAsync(() =>
                inspectors.Current is { } inspector
                    && VisualTree.HitTester.HitTest(inspector.Roots, new Point(x, y), inspector.BoundsOf) is { } hit
                    ? elements.GetId(hit)
                    : 0).ConfigureAwait(false);
            await HttpResponse.WriteJson(context,
                new JsonObject { ["ok"] = id != 0, ["id"] = id }.ToJsonString()).ConfigureAwait(false);
            return true;
        }

        if (method == HttpVerbs.Post && path == ApiRoutes.Structure.DropTarget)
        {
            var node = await RequestBody.ReadJson(context).ConfigureAwait(false);
            var x = node?["x"]?.GetValue<double>() ?? 0;
            var y = node?["y"]?.GetValue<double>() ?? 0;

            var target = await mainThread.RunAsync(() => structure.DropTargetAt(new Point(x, y))).ConfigureAwait(false);
            var json = target is { } t
                ? new JsonObject
                {
                    ["ok"] = true,
                    ["x"] = t.Bounds.X, ["y"] = t.Bounds.Y,
                    ["w"] = t.Bounds.Width, ["h"] = t.Bounds.Height,
                    ["label"] = t.Label,
                }.ToJsonString()
                : new JsonObject { ["ok"] = false }.ToJsonString();
            await HttpResponse.WriteJson(context, json).ConfigureAwait(false);
            return true;
        }

        if (method != HttpVerbs.Get || path != ApiRoutes.Structure.Catalog)
            return false;

        _cachedJson ??= BuildJson();
        await HttpResponse.WriteJson(context, _cachedJson).ConfigureAwait(false);
        return true;
    }

    string BuildJson()
    {
        var entries = new JsonArray();
        foreach (var entry in catalog.All())
        {
            entries.Add(new JsonObject
            {
                ["name"] = entry.Name,
                ["type"] = entry.TypeName,
                ["description"] = entry.Description,
                ["container"] = entry.IsContainer,
                ["custom"] = entry.IsCustom,
            });
        }

        return new JsonObject { ["controls"] = entries }.ToJsonString();
    }
}
