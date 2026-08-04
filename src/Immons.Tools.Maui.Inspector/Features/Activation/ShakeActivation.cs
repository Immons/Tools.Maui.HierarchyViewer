namespace Immons.Tools.Maui.Inspector.Features.Activation;

/// <summary>Opens/closes the inspector when the device is shaken (React Native-style).</summary>
internal static class ShakeActivation
{
    static bool _started;

    public static void EnsureStarted()
    {
        if (_started)
            return;

        try
        {
            if (!Accelerometer.Default.IsSupported)
                return;

            Accelerometer.Default.ShakeDetected += (_, _) =>
                Application.Current?.Dispatcher.Dispatch(() => MauiInspector.Toggle());
            Accelerometer.Default.Start(SensorSpeed.Game);
            _started = true;
        }
        catch
        {
            // sensor unavailable (e.g. simulator) — shake activation silently off
        }
    }
}
