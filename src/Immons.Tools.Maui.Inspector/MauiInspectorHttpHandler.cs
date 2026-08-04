using System.Diagnostics;

namespace Immons.Tools.Maui.Inspector;

/// <summary>
/// Optional HTTP logging handler for the web inspector's Network tab.
/// Add it to your HttpClient pipeline:
/// <code>
/// var client = new HttpClient(new MauiInspectorHttpHandler());
/// // or with DI: services.AddHttpClient("api").AddHttpMessageHandler(() => new MauiInspectorHttpHandler());
/// </code>
/// </summary>
public sealed class MauiInspectorHttpHandler : DelegatingHandler
{
    public MauiInspectorHttpHandler()
        : base(new HttpClientHandler())
    {
    }

    public MauiInspectorHttpHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Inspector.InspectorServices.Interceptor.SendAsync(request, base.SendAsync, cancellationToken);
}
