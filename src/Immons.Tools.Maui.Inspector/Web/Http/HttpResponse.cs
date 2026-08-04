using System.Net;
using System.Text;

namespace Immons.Tools.Maui.Inspector.Web.Http;

/// <summary>Response helpers shared by the endpoints.</summary>
internal static class HttpResponse
{
    public static Task WriteText(HttpListenerContext context, int status, string text) =>
        Write(context, status, "text/plain; charset=utf-8", text);

    public static Task WriteJson(HttpListenerContext context, string json, int status = 200) =>
        Write(context, status, "application/json", json);

    /// <summary>The canonical {"ok":bool} result.</summary>
    public static Task WriteOk(HttpListenerContext context, bool ok, int status = 200) =>
        WriteJson(context, ok ? "{\"ok\":true}" : "{\"ok\":false}", status);

    public static async Task Write(HttpListenerContext context, int status, string contentType, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        await WriteBytes(context, contentType, bytes, status).ConfigureAwait(false);
    }

    public static async Task WriteBytes(HttpListenerContext context, string contentType, byte[] bytes, int status = 200)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = contentType;
        context.Response.Headers["Cache-Control"] = "no-store";
        // The panel of one app mirrors edits to sibling apps on other ports — allow it.
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        context.Response.Close();
    }
}
