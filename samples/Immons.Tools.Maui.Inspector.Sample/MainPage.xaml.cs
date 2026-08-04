using Microsoft.Extensions.Logging;

namespace SampleApp;

public partial class MainPage : ContentPage
{
	int _count;
	bool _autoShown;

	public MainPage()
	{
		InitializeComponent();
		BindingContext = new DemoViewModel();

		var tap = new TapGestureRecognizer();
		tap.Tapped += async (_, _) =>
		{
			if (Immons.Tools.Maui.Inspector.MauiInspector.WebServerUrl is not { } url)
				return;
			try
			{
				await Launcher.Default.OpenAsync(url);
			}
			catch
			{
				// no browser available on this device/simulator
			}
		};
		WebServerLabel.GestureRecognizers.Add(tap);
	}

	static readonly HttpClient DemoClient = new(new Immons.Tools.Maui.Inspector.MauiInspectorHttpHandler());

	async void OnCounterClicked(object? sender, EventArgs e)
	{
		_count++;
		CounterBtn.Text = _count == 1 ? "Clicked 1 time" : $"Clicked {_count} times";

		// Demo traffic for the inspector's Network and Logs tabs.
		Handler?.MauiContext?.Services
			.GetService<Microsoft.Extensions.Logging.ILogger<MainPage>>()
			?.LogInformation("Counter clicked {Count} time(s)", _count);
		try
		{
			DemoClient.DefaultRequestHeaders.UserAgent.ParseAdd("Inspector.Sample");
			await DemoClient.GetStringAsync("https://raw.githubusercontent.com/dotnet/maui/main/README.md");
		}
		catch
		{
			// offline is fine — the failed request still shows up in the Network tab
		}
	}

	abstract class DemoViewModelBase
	{
		public string? NavigationParameter { get; set; }
	}

	/// <summary>Tiny view model so the inspector's ViewModel section has something to show.
	/// Hides a base property on purpose — regression test for AmbiguousMatchException.</summary>
	sealed class DemoViewModel : DemoViewModelBase, System.ComponentModel.INotifyPropertyChanged
	{
		public new string NavigationParameter { get; set; } = "hidden-property-test";
		string _greeting = "Hello from the view model";
		int _counter = 7;
		bool _isBusy;

		public string Greeting
		{
			get => _greeting;
			set { _greeting = value; Raise(nameof(Greeting)); }
		}

		public int Counter
		{
			get => _counter;
			set { _counter = value; Raise(nameof(Counter)); }
		}

		public bool IsBusy
		{
			get => _isBusy;
			set { _isBusy = value; Raise(nameof(IsBusy)); }
		}

		public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

		void Raise(string name) =>
			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
	}

	void UpdateWebServerLabel()
	{
		WebServerLabel.Text = Immons.Tools.Maui.Inspector.MauiInspector.WebServerUrl is { } url
			? $"Web inspector: {url}"
			: Immons.Tools.Maui.Inspector.MauiInspector.WebServerStartError is { } error
				? $"Web inspector failed: {error}"
				: "Web inspector: starting…";
	}

	void OnOpenInspectorClicked(object? sender, EventArgs e)
	{
		Immons.Tools.Maui.Inspector.MauiInspector.Show();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		UpdateWebServerLabel();
		Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(1), UpdateWebServerLabel);

		// Test hook: HV_AUTOSHOW=1 opens the inspector automatically (used by simulator smoke tests).
		if (!_autoShown && Environment.GetEnvironmentVariable("HV_AUTOSHOW") == "1")
		{
			_autoShown = true;
			Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(2), () =>
				Immons.Tools.Maui.Inspector.MauiInspector.Inspect(CardBorder));
		}
	}
}
