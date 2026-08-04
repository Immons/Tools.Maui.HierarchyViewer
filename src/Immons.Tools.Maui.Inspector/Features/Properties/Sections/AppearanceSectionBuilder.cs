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
            var shadowColor = (shadow.Brush as SolidColorBrush)?.Color;
            s.Rows.Add(new PropertyRow("Shadow.Color", Format(shadowColor), shadowColor,
                new PropertyEditor(EditorKind.Color, null, text =>
                {
                    if (ValueParser.ParseColorValue(text) is not { } color)
                        return false;
                    shadow.Brush = new SolidColorBrush(color);
                    return true;
                })));
            AddEditable(s, shadow, "Radius", "Shadow.Radius");
            AddEditable(s, shadow, "Offset", "Shadow.Offset");
            AddEditable(s, shadow, "Opacity", "Shadow.Opacity");
            s.Rows.Add(new PropertyRow("", "✕ Remove shadow", Action: () => el.Shadow = null!));
        }
        else
        {
            s.Rows.Add(new PropertyRow("Shadow", "＋ Add shadow", Action: () => el.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#66000000")),
                Radius = 8,
                Offset = new Point(0, 4),
                Opacity = 0.5f,
            }));
        }
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
