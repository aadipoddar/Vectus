#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using MudBlazor.Services;

using Syncfusion.Blazor;

using Vectus.Services;
using Vectus.Shared.Services;

using VectusLibrary.DataAccess;

namespace Vectus;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		SqlDataAccess.SetupConfiguration();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		// Add device-specific services used by the Vectus.Shared project
		builder.Services.AddSingleton<IFormFactor, FormFactor>();
		builder.Services.AddSingleton<ISaveAndViewService, SaveAndViewService>();
		builder.Services.AddSingleton<IUpdateService, UpdateService>();
		builder.Services.AddSingleton<IDataStorageService, DataStorageService>();
		builder.Services.AddSingleton<IVibrationService, VibrationService>();
		builder.Services.AddSingleton<ISoundService, SoundService>();
		builder.Services.AddScoped<INotificationService, NotificationService>();

		builder.Services
			.AddSyncfusionBlazor()
			.AddMudServices()
			.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
