using System.Collections;
using static Immons.Tools.Maui.Inspector.Shared.ValueFormatter;
using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Control-specific state: scroll position, items, values, ranges…</summary>
internal sealed class ControlSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Control");
        switch (el)
        {
            case ScrollView scroll:
                Add(s, "Scroll position", $"{F(scroll.ScrollX)}, {F(scroll.ScrollY)}");
                Add(s, "ContentSize", Format(scroll.ContentSize));
                break;
            case ItemsView items:
                Add(s, "ItemsSource", DescribeItemsSource(items.ItemsSource));
                if (items is StructuredItemsView structured)
                    Add(s, "ItemsLayout", structured.ItemsLayout?.GetType().Name ?? "–");
                break;
            case ContentPage page:
                if (!string.IsNullOrEmpty(page.Title))
                    AddEditable(s, el, "Title");
                break;
            case Slider slider:
                AddEditable(s, el, "Value");
                Add(s, "Range", $"{F(slider.Minimum)} – {F(slider.Maximum)}");
                break;
            case Stepper stepper:
                AddEditable(s, el, "Value");
                Add(s, "Range", $"{F(stepper.Minimum)} – {F(stepper.Maximum)}, step {F(stepper.Increment)}");
                break;
            case Switch:
                AddEditable(s, el, "IsToggled");
                break;
            case CheckBox:
                AddEditable(s, el, "IsChecked");
                break;
            case ProgressBar:
                AddEditable(s, el, "Progress");
                break;
            case Picker picker:
                Add(s, "SelectedIndex", picker.SelectedIndex.ToString());
                Add(s, "Items", picker.Items.Count.ToString());
                break;
            case DatePicker date:
                Add(s, "Date", $"{date.Date:yyyy-MM-dd}");
                break;
            case TimePicker time:
                Add(s, "Time", $"{time.Time}");
                break;
            case WebView web:
                Add(s, "Source", web.Source?.ToString() ?? "–");
                break;
        }

        if (el is Layout container)
            Add(s, "Children", container.Children.Count.ToString());
        else if (el is ContentView { Content: { } content })
            Add(s, "Content", content.GetType().Name);

        yield return s;
    }

    static string DescribeItemsSource(IEnumerable? source)
    {
        if (source == null)
            return "–";
        var count = source is ICollection c ? c.Count.ToString() : "?";
        return $"{source.GetType().Name} ({count} items)";
    }
}
