using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Services;

public static class AuthenticationService
{
	public static async Task<UserModel> ValidateUser(IDataStorageService dataStorageService, NavigationManager navigationManager, IVibrationService vibrationService, List<UserRoles> userRoles = null)
	{
		var userData = await dataStorageService.SecureGetAsync(StorageFileNames.UserDataFileName);
		if (string.IsNullOrWhiteSpace(userData))
			await Logout(dataStorageService, navigationManager, vibrationService);

		var user = System.Text.Json.JsonSerializer.Deserialize<UserModel>(userData);
		if (user is null)
			await Logout(dataStorageService, navigationManager, vibrationService);

		var serverUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, user.Id);
		if (serverUser is null)
			await Logout(dataStorageService, navigationManager, vibrationService);

		user = serverUser;
		await dataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(user));

		if (!serverUser.Status)
			await Logout(dataStorageService, navigationManager, vibrationService);

		if (userRoles is not null)
		{
			var hasPermission = userRoles.All(role => role switch
			{
				UserRoles.Accounts => user.Accounts,
				UserRoles.Fleet => user.Fleet,
				UserRoles.Reports => user.Reports,
				UserRoles.Admin => user.Admin,
				_ => false
			});

			if (!hasPermission)
				await Logout(dataStorageService, navigationManager, vibrationService);
		}

		await dataStorageService.SecureRemove(StorageFileNames.UserDeviceIdDataFileName);
		return user;
	}

	public static async Task Logout(IDataStorageService dataStorageService, NavigationManager navigationManager, IVibrationService vibrationService)
	{
		await dataStorageService.SecureRemoveAll();
		vibrationService.VibrateWithTime(500);
		navigationManager.NavigateTo(PageRouteNames.Login);
	}

	public static Func<string, bool> OpenRouteInNewWindow { get; set; }
	public static async Task NavigateToRoute(string route, IFormFactor FormFactor, IJSRuntime JSRuntime, NavigationManager NavigationManager)
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", route, "_blank");
		else if (OpenRouteInNewWindow is not null && OpenRouteInNewWindow(route))
			return;
		else
			NavigationManager.NavigateTo(route);
	}
}