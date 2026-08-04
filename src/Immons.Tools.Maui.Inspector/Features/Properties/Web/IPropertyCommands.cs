namespace Immons.Tools.Maui.Inspector.Features.Properties.Web;

/// <summary>Applies web-client edits to a selected element's property rows.</summary>
internal interface IPropertyCommands
{
    bool Apply(int id, string section, string name, string value);

    bool Clear(int id, string section, string name);

    bool RunAction(int id, string section, string label);

    bool Undo(long seq);
}
