using Immons.Tools.Maui.Inspector.Web.Endpoints;

namespace Immons.Tools.Maui.Inspector.Web.Hosting;

/// <summary>Wires the endpoint chain with its dependencies (web-side composition).</summary>
internal static class EndpointFactory
{
    public static IReadOnlyList<IHttpEndpoint> CreateAll()
    {
        IActiveInspectorProvider inspectors = new ActiveInspectorProvider();
        IMainThreadDispatcher mainThread = new MainThreadDispatcher(inspectors);
        var elements = InspectorServices.Current.Elements;
        var history = InspectorServices.Current.History;
        var xamlChanges = InspectorServices.Current.XamlChanges;
        ISyncTracker sync = InspectorServices.Current.Sync;

        ITreeJsonBuilder treeJson = new TreeJsonBuilder(inspectors, elements);
        IElementJsonBuilder elementJson = new ElementJsonBuilder(inspectors, elements, InspectorServices.Current.Properties);
        ISelectionJsonBuilder selectionJson = new SelectionJsonBuilder(inspectors, elements, history, xamlChanges, sync);
        var structure = InspectorServices.Current.Structure;
        IPropertyCommands commands = new PropertyCommands(inspectors, elements, InspectorServices.Current.Properties, history, structure);

        return
        [
            new StaticAssetsEndpoint(),
            new TreeEndpoint(mainThread, inspectors, treeJson),
            new SelectionEndpoint(mainThread, selectionJson),
            new ToggleEndpoint(mainThread, inspectors, xamlChanges),
            new ElementEndpoint(mainThread, inspectors, elements, elementJson, commands, structure),
            new StructureEndpoint(mainThread, InspectorServices.Current.Catalog, structure, inspectors, elements),
            new BroadcastEndpoint(mainThread, inspectors, InspectorServices.Current.Properties),
            new HistoryEndpoint(mainThread, history, commands),
            new NetworkEndpoint(InspectorServices.Current.Network),
            new MockRulesEndpoint(InspectorServices.Current.NetworkRules, InspectorServices.Current.Recorder),
            new InterceptEndpoint(InspectorServices.Current.Breakpoints),
            new LogsEndpoint(InspectorServices.Current.Logs),
            new Features.Editing.Web.ResourcesEndpoint(mainThread, inspectors, xamlChanges),
            new ChangesEndpoint(xamlChanges, sync),
            new MirrorEndpoint(mainThread, inspectors),
            new MeasureEndpoint(mainThread, inspectors, elements),
        ];
    }
}
