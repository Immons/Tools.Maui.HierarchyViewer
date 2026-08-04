namespace Immons.Tools.Maui.Inspector.Web.Http;

/// <summary>All routes served by the embedded server — the single source of truth for paths.</summary>
internal static class ApiRoutes
{
    public static class Assets
    {
        public const string Root = "/";
        public const string Css = "/app.css";
        public const string JsPrefix = "/js/";
        public const string JsSuffix = ".js";
    }

    public static class Tree
    {
        public const string List = "/api/tree";
    }

    public static class Dump
    {
        public const string Text = "/api/dump";
    }

    public static class Selection
    {
        public const string State = "/api/selection";
    }

    public static class Toggles
    {
        public const string MeasureMode = "/api/measure-mode";
        public const string SelectMode = "/api/select-mode";
        public const string Overlay = "/api/overlay";
        public const string DebugPaint = "/api/debug-paint";
        public const string Perf = "/api/perf";
        public const string SlowAnimations = "/api/slow-animations";
        public const string Wysiwyg = "/api/wysiwyg";
    }

    public static class Elements
    {
        public const string Prefix = "/api/element/";
        public const string SelectVerb = "select";
        public const string PropertyVerb = "property";
        public const string ActionVerb = "action";
    }

    public static class History
    {
        public const string List = "/api/history";
        public const string Undo = "/api/history/undo";
    }

    public static class Network
    {
        public const string List = "/api/network";
        public const string Body = "/api/network/body";
        public const string Clear = "/api/network/clear";
    }

    public static class MockRules
    {
        public const string List = "/api/mock/rules";
        public const string Save = "/api/mock/rules/save";
        public const string Delete = "/api/mock/rules/delete";
        public const string Enable = "/api/mock/rules/enable";
        public const string Import = "/api/mock/rules/import";
        public const string Mocking = "/api/mock/rules/mocking";
        public const string Scenario = "/api/mock/rules/scenario";
        public const string ScenarioAdd = "/api/mock/rules/scenario/add";
        public const string ScenarioRemove = "/api/mock/rules/scenario/remove";
        public const string RecordStart = "/api/mock/record/start";
        public const string RecordStop = "/api/mock/record/stop";
        public const string RecordCancel = "/api/mock/record/cancel";
    }

    public static class Intercept
    {
        public const string State = "/api/intercept";
        public const string Prefix = "/api/intercept/";
        public const string Config = "/api/intercept/config";
        public const string Resume = "/api/intercept/resume";
        public const string Abort = "/api/intercept/abort";
    }

    public static class Logs
    {
        public const string List = "/api/logs";
    }

    public static class Changes
    {
        public const string List = "/api/changes";
    }

    public static class Mirror
    {
        public const string Screenshot = "/api/screenshot";
        public const string SelectAt = "/api/select-at";
    }

    public static class Broadcast
    {
        public const string Ping = "/api/ping";
        public const string Property = "/api/broadcast/property";
        public const string Action = "/api/broadcast/action";
    }

    public static class Measure
    {
        public const string Compute = "/api/measure";
        public const string Clear = "/api/clear";
    }
}
