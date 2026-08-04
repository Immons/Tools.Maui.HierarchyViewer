using Microsoft.Extensions.Logging;
using Immons.Tools.Maui.Inspector;
using Immons.Tools.Maui.Inspector.Persistency;

namespace SampleApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder
			.UseMauiInspector(options =>
			{
				options.LongPressDuration = TimeSpan.FromMilliseconds(800);
				options.EnableWebServer = true;
				options.ShakeToOpen = true;
				options.SeedRulesAsset = "inspector-rules.json";
			})
			.UseMauiInspectorPersistency()   // mock rules and scenarios in SQLite instead of Preferences
			.Logging.AddDebug();
		builder.Logging.AddMauiInspector();
#endif

		return builder.Build();
	}
}
