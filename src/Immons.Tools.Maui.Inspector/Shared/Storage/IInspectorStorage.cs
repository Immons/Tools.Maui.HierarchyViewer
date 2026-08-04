using Immons.Tools.Maui.Inspector.Features.Editing.Storage;
using Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;

namespace Immons.Tools.Maui.Inspector.Shared.Storage;

/// <summary>
/// Everything the inspector persists, in one place. Implemented by the default Preferences backend
/// and by optional packages (see Immons.Tools.Maui.Inspector.Persistency for SQLite).
/// </summary>
internal interface IInspectorStorage
{
    IMockRuleStore MockRules { get; }

    IBreakpointStore Breakpoints { get; }

    IExpressionStore Expressions { get; }
}
