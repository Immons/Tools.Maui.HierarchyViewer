using static Immons.Tools.Maui.Inspector.Shared.ValueFormatter;
using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Visibility, colors, shadow, borders, image sources and control-specific looks.</summary>
internal sealed class AppearanceSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Appearance");

        AddEditable(s, el, "IsVisible");
        AddEditable(s, el, "Opacity");
        AddEditable(s, el, "BackgroundColor");

        if (el.BackgroundColor == null && el.Background != null)
            Add(s, "Background", Format(el.Background));

        AddShadowRows(s, el);

        AddEditable(s, el, "ZIndex");
        if (el.Clip != null)
            Add(s, "Clip", el.Clip.GetType().Name);
        if (el.FlowDirection != FlowDirection.MatchParent)
            Add(s, "FlowDirection", el.FlowDirection.ToString());

        AddControlSpecificRows(s, el);

        yield return s;
    }

    static void AddShadowRows(PropertySection s, VisualElement el)
    {
        if (el.Shadow is { } shadow)
        {
            // The instance living in a dictionary renders as its "{StaticResource …}" —
            // valid editor input, so it can be re-pointed at another resource in place.
            var resourceKey = ResourceLookup.KeyOf(el, shadow);
            AddEditable(s, el, "Shadow",
                value: resourceKey != null ? $"{{StaticResource {resourceKey}}}" : null,
                note: ShadowStyleOrigin(el));
            var shadowColor = (shadow.Brush as SolidColorBrush)?.Color;
            s.Rows.Add(new PropertyRow("Shadow.Color", Format(shadowColor), shadowColor,
                new PropertyEditor(EditorKind.Color, null, text =>
                {
                    if (ValueParser.ParseColorValue(text) is not { } color)
                        return false;
                    shadow.Brush = new SolidColorBrush(color);
                    InspectorServices.Current.XamlChanges.Record(shadow, "Brush", color.ToArgbHex(true));
                    RecordElementShadow(el, shadow);
                    return true;
                })));
            AddShadowPart(s, el, shadow, "Radius");
            AddShadowPart(s, el, shadow, "Offset");
            AddShadowPart(s, el, shadow, "Opacity");
            s.Rows.Add(new PropertyRow("", "✕ Remove shadow", Action: () =>
            {
                el.Shadow = null!;
                InspectorServices.Current.XamlChanges.Record(el, "Shadow", XamlChangeLog.RemoveMarker);
            }));
        }
        else
        {
            AddEditable(s, el, "Shadow", value: "");
            s.Rows.Add(new PropertyRow("", "＋ Add shadow", Action: () =>
            {
                el.Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Color.FromArgb("#66000000")),
                    Radius = 8,
                    Offset = new Point(0, 4),
                    Opacity = 0.5f,
                };
                RecordElementShadow(el, el.Shadow);
            }));
        }
    }

    /// <summary>
    /// Sub-editor for one Shadow property: the inner editor patches the Shadow tag when it
    /// exists in the XAML; a runtime-created shadow is instead written whole onto the element
    /// as the converter's attribute form ("offsetX offsetY radius color opacity").
    /// </summary>
    static void AddShadowPart(PropertySection s, VisualElement el, Shadow shadow, string property)
    {
        var inner = EditorFactory.Clr(shadow, property);
        if (inner == null)
            return;

        object? raw = null;
        try { raw = ReflectionLookup.FindInstanceProperty(typeof(Shadow), property)?.GetValue(shadow); }
        catch { /* show the row as empty */ }

        var editor = new PropertyEditor(inner.Kind, inner.Choices, text =>
        {
            if (!inner.Apply(text))
                return false;
            RecordElementShadow(el, shadow);
            return true;
        });
        s.Rows.Add(new PropertyRow("Shadow." + property, FormatValue(raw), raw as Color, editor));
    }

    /// <summary>
    /// Records the whole shadow as the element's Shadow attribute — only for shadows with no
    /// XAML tag of their own. Resource-backed shadows keep their "{StaticResource …}" reference
    /// (the edit mutates the shared instance; its dictionary entry is not literal-patchable).
    /// </summary>
    static void RecordElementShadow(VisualElement el, Shadow shadow)
    {
        if (XamlSource.Describe(shadow) != null || ResourceLookup.KeyOf(el, shadow) != null)
            return;
        if (Structure.ElementCloner.XamlAttributeValue(shadow) is { } text)
            InspectorServices.Current.XamlChanges.Record(el, "Shadow", text);
    }

    /// <summary>"style TitleStyle" when the shadow arrives via a style setter, not a local set.</summary>
    static string? ShadowStyleOrigin(VisualElement el)
    {
        if (el.IsSet(VisualElement.ShadowProperty))
            return null;

        for (var style = el.Style; style != null; style = style.BasedOn)
        {
            if (!style.Setters.Any(setter => setter.Property == VisualElement.ShadowProperty))
                continue;
            var styleKey = ResourceLookup.KeyOf(el, el.Style);
            return styleKey != null ? $"style {styleKey}" : "style";
        }

        // Not local and not the assigned style — a trigger or visual state set it.
        return "style";
    }

    static void AddControlSpecificRows(PropertySection s, VisualElement el)
    {
        switch (el)
        {
            case Border border:
                AddBorderRows(s, border);
                break;
            case Button button:
                AddEditable(s, button, "CornerRadius");
                if (button.BorderWidth > 0)
                {
                    AddEditable(s, button, "BorderWidth");
                    AddEditable(s, button, "BorderColor");
                }
                break;
            case ImageButton imageButton:
                s.Rows.Add(new PropertyRow("Source", ImageSourceSupport.Text(imageButton.Source), null,
                    ImageSourceSupport.CreateEditor(source => imageButton.Source = source)));
                AddEditable(s, imageButton, "Aspect");
                break;
            case Image image:
                s.Rows.Add(new PropertyRow("Source", ImageSourceSupport.Text(image.Source), null,
                    ImageSourceSupport.CreateEditor(source => image.Source = source)));
                AddEditable(s, image, "Aspect");
                break;
            case BoxView:
                AddEditable(s, el, "Color");
                AddEditable(s, el, "CornerRadius");
                break;
        }
    }

    static void AddBorderRows(PropertySection s, Border border)
    {
        s.Rows.Add(new PropertyRow("Stroke", Format(border.Stroke),
            (border.Stroke as SolidColorBrush)?.Color,
            new PropertyEditor(EditorKind.Color, null, text =>
            {
                if (text.Trim().Length == 0)
                {
                    border.Stroke = null;
                    return true;
                }
                if (ValueParser.ParseColorValue(text) is not { } color)
                    return false;
                border.Stroke = new SolidColorBrush(color);
                return true;
            })));
        AddEditable(s, border, "StrokeThickness");
        s.Rows.Add(new PropertyRow("StrokeShape", Format(border.StrokeShape), null,
            new PropertyEditor(EditorKind.Text, null, text =>
            {
                try
                {
                    if (new Microsoft.Maui.Controls.Shapes.StrokeShapeTypeConverter()
                            .ConvertFromInvariantString(text) is IShape shape)
                    {
                        border.StrokeShape = shape;
                        return true;
                    }
                }
                catch
                {
                    // fall through to "invalid input"
                }
                return false;
            })));
    }
}
