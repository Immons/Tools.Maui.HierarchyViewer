namespace Immons.Tools.Maui.Inspector.Web.Dispatch;

/// <summary>Resolves the inspector of the currently active window for remote callers.</summary>
internal interface IActiveInspectorProvider
{
    IWindowInspector? Current { get; }
}
