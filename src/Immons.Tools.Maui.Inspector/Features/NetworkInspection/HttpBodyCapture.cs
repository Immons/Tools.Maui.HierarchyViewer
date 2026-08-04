using System.Text;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Safe reading and replacing of HTTP bodies (textual content only, capped size).</summary>
internal static class HttpBodyCapture
{
    static readonly string[] TextualHints = ["json", "text", "xml", "x-www-form-urlencoded", "javascript", "graphql"];

    static int MaxBytes => MauiInspector.Options.MaxCapturedBodyBytes;

    /// <summary>Buffered body as string; null when absent, binary or over the cap. Content stays resendable.</summary>
    public static async Task<string?> ReadAsync(HttpContent? content)
    {
        if (content == null)
            return null;

        try
        {
            var mediaType = content.Headers.ContentType?.MediaType ?? "";
            if (mediaType.Length > 0 && !TextualHints.Any(mediaType.Contains))
                return null;

            await content.LoadIntoBufferAsync().ConfigureAwait(false);
            // Chunked responses report no ContentLength — fall back to what actually got buffered,
            // so the cap holds either way.
            var length = content.Headers.ContentLength
                ?? (await content.ReadAsByteArrayAsync().ConfigureAwait(false)).LongLength;
            if (length > MaxBytes)
                return null;

            return await content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            return null; // unreadable body must never break the actual call
        }
    }

    /// <summary>New content with the given body, preserving the original media type (default JSON).</summary>
    public static StringContent Replace(HttpContent? original, string body)
    {
        var mediaType = original?.Headers.ContentType?.MediaType ?? "application/json";
        return new StringContent(body, Encoding.UTF8, mediaType);
    }
}
