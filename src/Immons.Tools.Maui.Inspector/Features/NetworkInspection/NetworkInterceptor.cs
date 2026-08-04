using System.Diagnostics;
using System.Net;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>
/// Order per call: request breakpoint → rule (delay/fail/request body) → network or
/// short-circuit → rule response overrides → response breakpoint → record with bodies.
/// </summary>
internal sealed class NetworkInterceptor(
    INetworkLog log,
    IMockRules rules,
    IBreakpointGate breakpoints,
    IScenarioRecorder recorder) : INetworkInterceptor
{
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        var method = request.Method.Method;
        var url = request.RequestUri?.ToString() ?? "";
        var watch = Stopwatch.StartNew();
        string? tag = null;
        var requestBody = await HttpBodyCapture.ReadAsync(request.Content).ConfigureAwait(false);

        try
        {
            // 1. Request breakpoint — the panel may edit the outgoing body or abort.
            if (breakpoints.ShouldCapture(InterceptPhase.Request, url))
            {
                var decision = await breakpoints.WaitAsync(new InterceptedCall
                {
                    Id = breakpoints.NextId(),
                    Phase = InterceptPhase.Request,
                    Method = method,
                    Url = url,
                    Body = requestBody ?? "",
                }, cancellationToken).ConfigureAwait(false);

                if (decision is { Abort: true })
                    throw new HttpRequestException("Aborted from the MauiInspector breakpoint panel");
                if (decision?.Body != null && decision.Body != requestBody)
                {
                    request.Content = HttpBodyCapture.Replace(request.Content, decision.Body);
                    requestBody = decision.Body;
                    tag = "✋ edited";
                }
            }

            // 2. First matching rule: delay / simulated failure / request-body swap.
            var rule = rules.Match(method, url);
            if (rule != null)
            {
                tag = rule.Tag;
                if (rule.DelayMs > 0)
                    await Task.Delay(rule.DelayMs, cancellationToken).ConfigureAwait(false);
                if (rule.FailMode == MockRule.FailTimeout)
                    throw new TaskCanceledException($"Mocked timeout ({rule.Tag})");
                if (rule.FailMode == MockRule.FailError)
                    throw new HttpRequestException($"Mocked network error ({rule.Tag})");
                if (rule.RequestBody != null)
                {
                    request.Content = HttpBodyCapture.Replace(request.Content, rule.RequestBody);
                    requestBody = rule.RequestBody;
                }
            }

            // 3. Network call — or a fully mocked response without touching the server.
            var response = rule is { ShortCircuit: true }
                ? new HttpResponseMessage((HttpStatusCode)(rule.Status ?? 200))
                {
                    Content = HttpBodyCapture.Replace(null, rule.ResponseBody ?? ""),
                    RequestMessage = request,
                }
                : await send(request, cancellationToken).ConfigureAwait(false);

            // 4. Rule overrides on a real response.
            if (rule is { ShortCircuit: false })
            {
                if (rule.Status is { } status)
                    response.StatusCode = (HttpStatusCode)status;
                if (rule.ResponseBody != null)
                    response.Content = HttpBodyCapture.Replace(response.Content, rule.ResponseBody);
            }

            var responseBody = await HttpBodyCapture.ReadAsync(response.Content).ConfigureAwait(false);

            // 5. Response breakpoint — the panel may edit body/status or abort.
            if (breakpoints.ShouldCapture(InterceptPhase.Response, url))
            {
                var decision = await breakpoints.WaitAsync(new InterceptedCall
                {
                    Id = breakpoints.NextId(),
                    Phase = InterceptPhase.Response,
                    Method = method,
                    Url = url,
                    Body = responseBody ?? "",
                    Status = (int)response.StatusCode,
                }, cancellationToken).ConfigureAwait(false);

                if (decision is { Abort: true })
                    throw new HttpRequestException("Aborted from the MauiInspector breakpoint panel");
                if (decision != null)
                {
                    if (decision.Status is { } status)
                        response.StatusCode = (HttpStatusCode)status;
                    if (decision.Body != null && decision.Body != responseBody)
                    {
                        response.Content = HttpBodyCapture.Replace(response.Content, decision.Body);
                        responseBody = decision.Body;
                    }
                    tag = "✋ edited";
                }
            }

            watch.Stop();
            recorder.Capture(method, url, (int)response.StatusCode, responseBody);
            log.Record(method, url, (int)response.StatusCode, watch.Elapsed.TotalMilliseconds,
                response.Content?.Headers.ContentLength, error: null, tag, requestBody, responseBody);
            return response;
        }
        catch (Exception ex)
        {
            watch.Stop();
            log.Record(method, url, 0, watch.Elapsed.TotalMilliseconds, null,
                ex.GetType().Name, tag, requestBody, null);
            throw;
        }
    }
}
