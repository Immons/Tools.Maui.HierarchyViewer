using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Margins, size requests, container-specific knobs and attached Grid position.</summary>
internal sealed class LayoutSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Layout");

        if (el is View)
        {
            AddEditable(s, el, "Margin");
            AddEditable(s, el, "HorizontalOptions");
            AddEditable(s, el, "VerticalOptions");
        }

        if (ElementInfo.GetPadding(el) != null)
            AddEditable(s, el, "Padding");

        AddEditable(s, el, "WidthRequest");
        AddEditable(s, el, "HeightRequest");
        if (el.MinimumWidthRequest >= 0)
            AddEditable(s, el, "MinimumWidthRequest");
        if (el.MinimumHeightRequest >= 0)
            AddEditable(s, el, "MinimumHeightRequest");
        if (!double.IsPositiveInfinity(el.MaximumWidthRequest) && el.MaximumWidthRequest >= 0)
            AddEditable(s, el, "MaximumWidthRequest");
        if (!double.IsPositiveInfinity(el.MaximumHeightRequest) && el.MaximumHeightRequest >= 0)
            AddEditable(s, el, "MaximumHeightRequest");

        AddContainerRows(s, el);

        if (el.Parent is Grid && el is View gridChild)
        {
            AddAttachedInt(s, "Grid.Row", () => Grid.GetRow(gridChild), v => Grid.SetRow(gridChild, v), min: 0, gridChild);
            AddAttachedInt(s, "Grid.Column", () => Grid.GetColumn(gridChild), v => Grid.SetColumn(gridChild, v), min: 0, gridChild);
            AddAttachedInt(s, "Grid.RowSpan", () => Grid.GetRowSpan(gridChild), v => Grid.SetRowSpan(gridChild, v), min: 1, gridChild);
            AddAttachedInt(s, "Grid.ColumnSpan", () => Grid.GetColumnSpan(gridChild), v => Grid.SetColumnSpan(gridChild, v), min: 1, gridChild);
        }

        yield return s;
    }

    static void AddContainerRows(PropertySection s, VisualElement el)
    {
        switch (el)
        {
            case StackBase:
                AddEditable(s, el, "Spacing");
                if (el is StackLayout)
                    AddEditable(s, el, "Orientation");
                break;
            case Grid grid:
                AddEditable(s, el, "RowSpacing");
                AddEditable(s, el, "ColumnSpacing");
                s.Rows.Add(new PropertyRow(
                    "Definitions",
                    $"{grid.RowDefinitions.Count} row(s), {grid.ColumnDefinitions.Count} column(s)",
                    TogglesGroup: "griddefs"));
                break;
            case ScrollView:
                AddEditable(s, el, "Orientation");
                break;
            case FlexLayout flex:
                Add(s, "Direction", flex.Direction.ToString());
                Add(s, "Wrap", flex.Wrap.ToString());
                Add(s, "JustifyContent", flex.JustifyContent.ToString());
                Add(s, "AlignItems", flex.AlignItems.ToString());
                break;
        }
    }
}
