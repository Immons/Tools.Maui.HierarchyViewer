namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

internal enum InterceptPhase
{
    Request,
    Response,
}

/// <summary>What the panel decided for a paused call.</summary>
internal sealed record InterceptDecision(bool Abort, string? Body, int? Status);

/// <summary>A call paused at a breakpoint, waiting for the panel.</summary>
internal sealed class InterceptedCall
{
    public required long Id { get; init; }

    public required InterceptPhase Phase { get; init; }

    public required string Method { get; init; }

    public required string Url { get; init; }

    /// <summary>Captured body ("" when absent/binary).</summary>
    public required string Body { get; init; }

    /// <summary>Response status; 0 for the request phase.</summary>
    public int Status { get; init; }

    public string Time { get; } = DateTime.Now.ToString("HH:mm:ss");

    internal TaskCompletionSource<InterceptDecision> Decision { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
