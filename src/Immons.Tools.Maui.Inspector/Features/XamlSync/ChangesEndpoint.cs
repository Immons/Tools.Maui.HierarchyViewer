using System.Net;

namespace Immons.Tools.Maui.Inspector.Features.XamlSync;

/// <summary>GET /api/changes?since= — polled by the XAML Updater tool.</summary>
internal sealed class ChangesEndpoint(IXamlChangeLog xamlChanges, ISyncTracker sync) : IHttpEndpoint
{
    public async Task<bool> TryHandle(HttpListenerContext context, string method, string path)
    {
        if (method != HttpVerbs.Get || path != ApiRoutes.Changes.List)
            return false;

        sync.MarkPolled();
        long since = 0;
        if (long.TryParse(context.Request.QueryString["since"], out var parsed))
            since = parsed;
        var includeStructural = (context.Request.QueryString["caps"] ?? "").Contains("el", StringComparison.Ordinal);
        await HttpResponse.WriteJson(context, xamlChanges.ToJson(since, includeStructural)).ConfigureAwait(false);
        return true;
    }
}
