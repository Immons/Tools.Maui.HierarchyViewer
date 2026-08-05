using System.Diagnostics;

namespace Immons.Tools.Maui.Inspector;

/// <summary>
/// Optional HTTP logging handler for the web inspector's Network tab.
/// Add it to your HttpClient pipeline:
/// <code>
/// var client = new HttpClient(new MauiInspectorHttpHandler());
/// // or with IHttpClientFactory:
/// services.AddHttpClient("api").AddHttpMessageHandler(MauiInspectorHttpHandler.ForClientFactory);
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

    private MauiInspectorHttpHandler(bool leaveInnerHandlerUnassigned)
    {
    }

    /// <summary>
    /// Creates a handler for <c>IHttpClientFactory</c> pipelines
    /// (<c>AddHttpClient(...).AddHttpMessageHandler(MauiInspectorHttpHandler.ForClientFactory)</c>).
    /// The factory assigns <see cref="DelegatingHandler.InnerHandler"/> itself and throws when it
    /// is already set, so this instance — unlike <see cref="MauiInspectorHttpHandler()"/> — leaves it unassigned.
    /// </summary>
    public static MauiInspectorHttpHandler ForClientFactory() => new(leaveInnerHandlerUnassigned: true);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Inspector.InspectorServices.Interceptor.SendAsync(request, base.SendAsync, cancellationToken);
}
