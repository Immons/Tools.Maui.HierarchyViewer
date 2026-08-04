using Immons.Tools.Maui.Inspector.Web.Endpoints;

namespace Immons.Tools.Maui.Inspector.Web.Hosting;

/// <summary>Wires the endpoint chain with its dependencies (web-side composition).</summary>
internal static class EndpointFactory
{
    public static IReadOnlyList<IHttpEndpoint> CreateAll()
    {
        IActiveInspectorProvider inspectors = new ActiveInspectorProvider();
        IMainThreadDispatcher mainThread = new MainThreadDispatcher(inspectors);
        var elements = InspectorServices.Elements;
        var history = InspectorServices.History;
        var xamlChanges = InspectorServices.XamlChanges;
        ISyncTracker sync = InspectorServices.Sync;

        ITreeJsonBuilder treeJson = new TreeJsonBuilder(inspectors, elements);
        IElementJsonBuilder elementJson = new ElementJsonBuilder(inspectors, elements, InspectorServices.Properties);
        ISelectionJsonBuilder selectionJson = new SelectionJsonBuilder(inspectors, elements, history, xamlChanges, sync);
        IPropertyCommands commands = new PropertyCommands(inspectors, elements, InspectorServices.Properties, history);

        return
        [
            new StaticAssetsEndpoint(),
            new TreeEndpoint(mainThread, inspectors, treeJson),
            new SelectionEndpoint(mainThread, selectionJson),
            new ToggleEndpoint(mainThread, inspectors, xamlChanges),
            new ElementEndpoint(mainThread, inspectors, elements, elementJson, commands),
            new BroadcastEndpoint(mainThread, inspectors, InspectorServices.Properties),
            new HistoryEndpoint(mainThread, history, commands),
            new NetworkEndpoint(InspectorServices.Network),
            new MockRulesEndpoint(InspectorServices.NetworkRules, InspectorServices.Recorder),
            new InterceptEndpoint(InspectorServices.Breakpoints),
            new LogsEndpoint(InspectorServices.Logs),
            new ChangesEndpoint(xamlChanges, sync),
            new MirrorEndpoint(mainThread, inspectors),
            new MeasureEndpoint(mainThread, inspectors, elements),
        ];
    }
}
