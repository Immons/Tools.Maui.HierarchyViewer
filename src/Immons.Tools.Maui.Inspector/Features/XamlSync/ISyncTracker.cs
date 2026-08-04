namespace Immons.Tools.Maui.Inspector.Features.XamlSync;

/// <summary>Tracks whether the XAML Updater tool is actively polling for changes.</summary>
internal interface ISyncTracker
{
    void MarkPolled();

    bool Connected { get; }
}
