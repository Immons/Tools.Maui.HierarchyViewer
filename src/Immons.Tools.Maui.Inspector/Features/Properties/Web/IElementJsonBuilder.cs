namespace Immons.Tools.Maui.Inspector.Features.Properties.Web;

/// <summary>Serializes one element's property sections for the web client.</summary>
internal interface IElementJsonBuilder
{
    /// <summary>Null when the element id is unknown or no window is active.</summary>
    string? Build(int id);
}
