using Microsoft.Maui.Animations;

namespace Immons.Tools.Maui.Inspector.Features.Performance;

/// <summary>Flutter-style "slow animations": scales every MAUI animation 5× slower.</summary>
internal static class SlowAnimations
{
    public static bool Enabled { get; private set; }

    public static bool Set(bool on)
    {
        if (MauiInspector.ActiveInspector?.MauiContext?.Services?
                .GetService(typeof(IAnimationManager)) is not IAnimationManager manager)
            return false;

        manager.SpeedModifier = on ? 0.2 : 1.0;
        Enabled = on;
        return true;
    }
}
