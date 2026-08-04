using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Identity of the element: AutomationId (editable), type, handler, context.</summary>
internal sealed class ElementSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Element");

        // The CLR setter only allows setting AutomationId once — go through the BindableProperty.
        s.Rows.Add(new PropertyRow("AutomationId", el.AutomationId ?? "", null,
            new PropertyEditor(EditorKind.Text, null, text =>
            {
                text = text.Trim();
                if (text.Length == 0)
                    el.ClearValue(Element.AutomationIdProperty);
                else
                    el.SetValue(Element.AutomationIdProperty, text);
                return true;
            })
            {
                XamlTarget = el,
                XamlAttribute = "AutomationId",
                ClearAction = () =>
                {
                    el.ClearValue(Element.AutomationIdProperty);
                    return true;
                },
            }));
        Add(s, "Type", el.GetType().FullName ?? el.GetType().Name);
        if (!string.IsNullOrEmpty(el.StyleId))
            Add(s, "StyleId", el.StyleId);
        Add(s, "Handler", el.Handler?.GetType().Name ?? "–");
        Add(s, "PlatformView", el.Handler?.PlatformView?.GetType().Name ?? "–");
        if (el.BindingContext != null)
            Add(s, "BindingContext", el.BindingContext.GetType().Name);
        if (el.Parent != null)
            Add(s, "Parent", el.Parent.GetType().Name);

        yield return s;
    }
}
