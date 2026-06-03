namespace Vectus;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		ConfigurePlatform();
	}

	protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage()) { Title = "Vectus" };

	partial void ConfigurePlatform();
}
