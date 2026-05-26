#if WINDOWS
using Vectus.Shared.Services;
#endif

namespace Vectus;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

#if WINDOWS
		// On Windows, open internal routes in a new native window instead of in-place navigation.
		AuthenticationService.OpenRouteInNewWindow = route =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
				Current?.OpenWindow(new Window(new MainPage(route)) { Title = "Vectus" }));
			return true;
		};
#endif
	}

	protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage()) { Title = "Vectus" };
}
