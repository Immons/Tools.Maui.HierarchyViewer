using Immons.Tools.Maui.Inspector.Features.Editing.Storage;
using Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;

namespace Immons.Tools.Maui.Inspector.Shared.Storage;

/// <summary>Default backend — no extra dependency, everything in Preferences.</summary>
internal sealed class PreferencesInspectorStorage : IInspectorStorage
{
    public IMockRuleStore MockRules { get; } = new PreferencesMockRuleStore();

    public IBreakpointStore Breakpoints { get; } = new PreferencesBreakpointStore();

    public IExpressionStore Expressions { get; } = new PreferencesExpressionStore();
}
