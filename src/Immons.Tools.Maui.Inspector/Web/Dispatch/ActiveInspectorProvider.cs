namespace Immons.Tools.Maui.Inspector.Web.Dispatch;

/// <summary>Adapts the facade's active-window lookup to the endpoint layer.</summary>
internal sealed class ActiveInspectorProvider : IActiveInspectorProvider
{
    public IWindowInspector? Current => MauiInspector.ActiveInspector;
}
