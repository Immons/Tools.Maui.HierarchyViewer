using System.Net;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Web;

/// <summary>/api/element/{id} (GET) and /api/element/{id}/select|property|action (POST).</summary>
internal sealed class ElementEndpoint(
    IMainThreadDispatcher mainThread,
    IActiveInspectorProvider inspectors,
    IElementRegistry elements,
    IElementJsonBuilder elementJson,
    IPropertyCommands commands) : IHttpEndpoint
{
    public async Task<bool> TryHandle(HttpListenerContext context, string method, string path)
    {
        var parts = path.Trim('/').Split('/');
        if (parts.Length < 3 || parts[0] != "api" || parts[1] != "element" || !int.TryParse(parts[2], out var id))
            return false;

        var verb = parts.Length > 3 ? parts[3] : null;

        if (method == HttpVerbs.Get && verb == null)
        {
            var json = await mainThread.RunAsync(() => elementJson.Build(id)).ConfigureAwait(false);
            if (json == null)
                await HttpResponse.WriteText(context, 404, "element not found").ConfigureAwait(false);
            else
                await HttpResponse.WriteJson(context, json).ConfigureAwait(false);
            return true;
        }

        if (method == HttpVerbs.Post && verb == "select")
        {
            var ok = await mainThread.RunAsync(() => Select(id)).ConfigureAwait(false);
            await HttpResponse.WriteOk(context, ok, ok ? 200 : 404).ConfigureAwait(false);
            return true;
        }

        if (method == HttpVerbs.Post && verb is "property" or "action")
        {
            var node = await RequestBody.ReadJson(context).ConfigureAwait(false);
            var section = node?["section"]?.GetValue<string>() ?? "";
            var name = node?["name"]?.GetValue<string>() ?? "";
            var value = node?["value"]?.GetValue<string>() ?? "";
            var clear = node?["clear"]?.GetValue<bool>() ?? false;

            var ok = await mainThread.RunAsync(() => verb == "property"
                ? (clear ? commands.Clear(id, section, name) : commands.Apply(id, section, name, value))
                : commands.RunAction(id, section, name)).ConfigureAwait(false);

            await HttpResponse.WriteOk(context, ok).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    bool Select(int id)
    {
        if (inspectors.Current is not { } inspector || elements.Find(id) is not { } element)
            return false;
        inspector.RemoteSelect(element);
        return true;
    }
}
