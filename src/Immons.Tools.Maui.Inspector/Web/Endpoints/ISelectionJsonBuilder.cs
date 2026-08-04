namespace Immons.Tools.Maui.Inspector.Web.Endpoints;

/// <summary>Serializes the current selection + mode flags polled by the web client.</summary>
internal interface ISelectionJsonBuilder
{
    string Build();
}
