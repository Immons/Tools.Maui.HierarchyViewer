using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Inspector;

/// <summary>
/// MAUI-level tap fallback for platforms without touch injection: triggers the element's
/// own reaction to a tap — gesture recognizers, button clicks, toggle flips. Covers the
/// common interactive controls; complex platform gestures stay out of scope.
/// </summary>
internal static class SemanticTap
{
    public static bool TryInvoke(Element element)
    {
        if (element is View { GestureRecognizers.Count: > 0 } view)
        {
            foreach (var recognizer in view.GestureRecognizers)
            {
                if (recognizer is TapGestureRecognizer tap && SendTapped(tap, view))
                    return true;
            }
        }

        switch (element)
        {
            case InputView input:
                return input.Focus();
            case Button button:
                return Send(button, "SendClicked");
            case ImageButton imageButton:
                return Send(imageButton, "SendClicked");
            case Switch toggle:
                toggle.IsToggled = !toggle.IsToggled;
                return true;
            case CheckBox checkBox:
                checkBox.IsChecked = !checkBox.IsChecked;
                return true;
            case RadioButton radio:
                radio.IsChecked = true;
                return true;
            default:
                return false;
        }
    }

    static bool SendTapped(TapGestureRecognizer tap, View view)
    {
        try
        {
            // internal void SendTapped(View sender, Func<IElement?, Point?>? getPosition)
            var method = typeof(TapGestureRecognizer).GetMethod(
                "SendTapped", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
                return false;
            var arguments = method.GetParameters().Length switch
            {
                1 => new object?[] { view },
                2 => [view, null],
                _ => null,
            };
            if (arguments == null)
                return false;
            method.Invoke(tap, arguments);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static bool Send(VisualElement element, string methodName)
    {
        try
        {
            var method = element.GetType().GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null || method.GetParameters().Length != 0)
                return false;
            method.Invoke(element, null);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
