using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

internal static class EditorFactory
{
    /// <summary>
    /// Builds an editor for a public settable CLR property of the target object
    /// (a VisualElement, Span, …), or returns null when the property type is not supported.
    /// </summary>
    public static PropertyEditor? Clr(object element, string propertyName)
    {
        var pi = ReflectionLookup.FindInstanceProperty(element.GetType(), propertyName);
        if (pi?.SetMethod is not { IsPublic: true })
            return null;

        var type = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
        if (ResolveKind(type) is not var (kind, choices))
            return null;

        // Recursive: an "{OnPlatform …}" entry may itself be a binding, a resource or a literal.
        // top == true only for the user's raw input — it maintains the remembered expression.
        bool Apply(string text, bool top)
        {
            // "{Binding X}" typed into the editor becomes a live binding.
            if (BindingExpressionParser.TryParse(text) is { } bindingExpression)
            {
                if (element is not BindableObject bindable
                    || ReflectionLookup.FindBindableProperty(element.GetType(), propertyName) is not { } bp)
                    return false;
                bindable.SetBinding(bp, bindingExpression);
                if (top)
                    InspectorServices.Current.Expressions.Record(element, propertyName, null);
                return true;
            }

            // "{OnPlatform …}" / "{OnIdiom …}": runtime gets this device's entry;
            // the whole expression is recorded to XAML verbatim and remembered for the panel.
            if (DeviceValueExpressionParser.TryResolve(text, out var deviceValue))
            {
                var ok = deviceValue == null || Apply(deviceValue, top: false);
                if (ok)
                    InspectorServices.Current.Expressions.Record(element, propertyName, text.Trim());
                return ok;
            }

            // Custom markup ("{extensions:Translate Key}"): run the extension when we can,
            // otherwise treat it as a XAML-only edit instead of writing a literal "{…}" string.
            if (MarkupExtensionResolver.IsCustomMarkup(text))
            {
                if (MarkupExtensionResolver.TryResolve(text, element) is { } produced)
                {
                    var coerced = ResourceResolver.Coerce(produced, type);
                    if (coerced != null)
                    {
                        try
                        {
                            pi.SetValue(element, coerced);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
                if (top)
                    InspectorServices.Current.Expressions.Record(element, propertyName, text.Trim());
                return true;
            }

            object? value;
            if (ResourceResolver.IsResourceReference(text, out _))
            {
                // Unknown key must fail loudly instead of landing as literal text.
                if (ResourceResolver.Resolve(element, text) is not { } resource)
                    return false;
                value = ResourceResolver.Coerce(resource, type);
                if (value == null)
                    return false;
            }
            else if (!ValueParser.TryParse(type, kind, text, out value))
            {
                return false;
            }

            try
            {
                pi.SetValue(element, value);
                if (top)
                    InspectorServices.Current.Expressions.Record(element, propertyName, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return new PropertyEditor(kind, choices, text => Apply(text, top: true))
        {
            XamlTarget = element,
            XamlAttribute = propertyName,
            // Markup input ("{Binding X}", "{StaticResource Y}") goes into XAML verbatim.
            // Literal input on a BOUND property stays runtime-only — recording it would
            // replace the "{Binding …}" expression in the source file with a constant.
            XamlValue = text =>
            {
                text = text == XamlChangeLog.RemoveMarker ? text : text.Trim();
                if (text != XamlChangeLog.RemoveMarker && BindingExpressionParser.LooksLikeMarkup(text))
                    return text;
                return BindingDescriptor.Describe(element, propertyName) == null ? text : null;
            },
            ClearAction = MakeClearAction(element, propertyName),
        };
    }

    static (EditorKind Kind, IReadOnlyList<string>? Choices)? ResolveKind(Type type)
    {
        if (type == typeof(bool))
            return (EditorKind.Bool, null);
        if (type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long))
            return (EditorKind.Double, null);
        if (type == typeof(string))
            return (EditorKind.Text, null);
        if (type == typeof(Color))
            return (EditorKind.Color, null);
        if (type == typeof(Thickness))
            return (EditorKind.Thickness, null);
        if (type == typeof(CornerRadius))
            return (EditorKind.CornerRadius, null);
        if (type == typeof(Point))
            return (EditorKind.Point, null);
        if (type == typeof(LayoutOptions))
            return (EditorKind.LayoutOptions, new[] { "Start", "Center", "End", "Fill" });
        if (type.IsEnum)
            return (EditorKind.Enum, Enum.GetNames(type));
        if (typeof(ImageSource).IsAssignableFrom(type))
            return (EditorKind.Image, null);
        return null;
    }

    /// <summary>ClearValue-based reset when a matching BindableProperty exists on the target.</summary>
    static Func<bool>? MakeClearAction(object element, string propertyName)
    {
        if (element is not BindableObject bindable
            || ReflectionLookup.FindBindableProperty(element.GetType(), propertyName) is not { } property)
            return null;

        return () =>
        {
            try
            {
                bindable.ClearValue(property);
                InspectorServices.Current.Expressions.Record(element, propertyName, null);
                return true;
            }
            catch
            {
                return false;
            }
        };
    }

}
