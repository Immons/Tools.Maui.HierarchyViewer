namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>The pipeline behind <see cref="MauiInspectorHttpHandler"/>: record, rules, breakpoints.</summary>
internal interface INetworkInterceptor
{
    Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken);
}
