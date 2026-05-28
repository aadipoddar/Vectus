using Microsoft.JSInterop;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Authentication;

public partial class LoginPage
{
	private bool _isVerifying = false;

	private string _phoneEmail = string.Empty;
	private string _password = string.Empty;

	private bool _isLoginWithCodeEnabled = true;
	private int _maxLoginAttempts;

	private CustomTextField _phoneEmailTextBox;
	private CustomTextField _passwordTextBox;

	private ToastNotification _toastNotification;

	private bool _yetiInitialized = false;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			await DataStorageService.SecureRemoveAll();

			_maxLoginAttempts = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.MaxLoginAttempts)).Value);
			_isLoginWithCodeEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.EnableLoginWithCode)).Value);

			// Initialize Yeti animation after a short delay to ensure DOM is ready
			await Task.Delay(100);
			await InitializeYetiAnimation();

			await _phoneEmailTextBox.FocusAsync();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Initializing Login Page", ex.Message, ToastType.Error);
		}
	}

	private async Task InitializeYetiAnimation()
	{
		if (_yetiInitialized) return;

		try
		{
			await JSRuntime.InvokeVoidAsync("YetiAnimation.init", "phoneEmailInput", "passwordInput");
			_yetiInitialized = true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to initialize Yeti animation: {ex.Message}");
		}
	}

	private async Task OnLoginClick()
	{
		if (_isVerifying)
			return;

		try
		{
			_isVerifying = true;

			var user = await UserData.LoadUserByPhoneEmail(_phoneEmail);
			if (user is null)
			{
				await _phoneEmailTextBox.FocusAsync();
				throw new Exception("No user found with the provided phone number or email.");
			}

			if (!user.Status)
				throw new Exception("This account is inactive. Please contact support.");

			if (_password != user.Password)
			{
				user.FailedAttempts++;

				if (user.FailedAttempts >= _maxLoginAttempts)
				{
					user.Status = false;
					await UserData.InsertUser(user);
					throw new Exception("Your account has been locked due to multiple failed login attempts. Please contact support.");
				}

				await UserData.InsertUser(user);

				await _passwordTextBox.FocusAsync();
				throw new Exception($"Incorrect password. You have {(_maxLoginAttempts - user.FailedAttempts)} attempts remaining.");
			}

			await UserData.ResetInsertUser(user);
			await DataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(user));
			VibrationService.VibrateWithTime(500);
			NavigationManager.NavigateTo(PageRouteNames.Dashboard);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Login Failed", ex.Message, ToastType.Error);
		}
		finally
		{
			_isVerifying = false;
		}
	}

	private async Task OnForgotPasswordClick()
	{
		if (!_isLoginWithCodeEnabled)
			return;

		NavigationManager.NavigateTo(PageRouteNames.LoginWithCode);
	}
}