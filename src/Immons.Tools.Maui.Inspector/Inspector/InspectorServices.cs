namespace Immons.Tools.Maui.Inspector.Inspector;

/// <summary>
/// Composition root of the inspector. The library is activated from a window-handler mapping
/// (outside the app's DI container), so the object graph is wired here once and consumed
/// through the interfaces only.
/// </summary>
internal static class InspectorServices
{
    public static IElementRegistry Elements { get; } = new ElementRegistry();

    public static IXamlChangeLog XamlChanges { get; } = new XamlChangeLog();

    public static IEditHistory History { get; } = new EditHistory(Elements);

    public static INetworkLog Network { get; } = new NetworkLog();

    public static ILogSink Logs { get; } = new LogSink();

    public static IMockRules NetworkRules { get; } = new MockRules();

    public static IBreakpointGate Breakpoints { get; } = new BreakpointGate();

    public static IScenarioRecorder Recorder { get; } = new ScenarioRecorder(NetworkRules);

    public static INetworkInterceptor Interceptor { get; } = new NetworkInterceptor(Network, NetworkRules, Breakpoints, Recorder);

    public static ISyncTracker Sync { get; } = new SyncTracker();

    public static IAppliedExpressions Expressions { get; } = new PersistentAppliedExpressions();

    public static IPropertyCollector Properties { get; } = new PropertyCollector(XamlChanges);
}
