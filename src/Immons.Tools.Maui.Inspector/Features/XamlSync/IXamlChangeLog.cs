namespace Immons.Tools.Maui.Inspector.Features.XamlSync;

/// <summary>Registry of applied edits destined for the XAML Updater tool.</summary>
internal interface IXamlChangeLog
{
    /// <summary>Runtime switch (toggled from the web client). Off by default — edits stay in-memory only.</summary>
    bool Enabled { get; set; }

    /// <summary>Records an applied edit; no-op when disabled or the object has no XAML source info.</summary>
    void Record(object target, string attribute, string value);

    string ToJson(long since);
}
