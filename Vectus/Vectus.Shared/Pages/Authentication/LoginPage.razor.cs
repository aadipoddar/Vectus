using Microsoft.JSInterop;

using Syncfusion.Blazor.Inputs;

using Vectus.Shared.Components.Dialog;

using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Authentication;

public partial class LoginPage
{
	private UserModel _user = new();

	private bool _isVerifying = false;

	private string _phoneEmail = string.Empty;
	private string _password = string.Empty;

	private string _passwordPlaceholder = "Enter password";

	private bool _isLoginWithCodeEnabled = true;
	private int _maxLoginAttempts;

	private List<UserModel> _users = [];

	private SfTextBox _phoneEmailTextBox;
	private SfTextBox _passwordTextBox;

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

			_users = await CommonData.LoadTableData<UserModel>(OperationNames.User);

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

	private async Task OnPhoneEmailInput(InputEventArgs args)
	{
		_phoneEmail = args.Value;

		var user = _users.FirstOrDefault(u => u.Phone == _phoneEmail || u.Email == _phoneEmail);
		if (user is null)
		{
			_passwordPlaceholder = "Enter password";
			_user = new();
		}

		else
		{
			_user = user;
			_passwordPlaceholder = $"Enter password for {_user.Name}";
			await _passwordTextBox.FocusAsync();
		}

		StateHasChanged();
	}

	private async Task OnPasswordInput(InputEventArgs args)
	{
		_password = args.Value;

		if (_isVerifying)
			return;

		_isVerifying = true;

		_user = _users.FirstOrDefault(u => u.Phone == _phoneEmail || u.Email == _phoneEmail);
		if (_user is not null && _password == _user.Password && _user.Status)
		{
			await UserData.ResetInsertUser(_user);
			await DataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(_user));
			NavigationManager.NavigateTo(PageRouteNames.Dashboard, true);
		}

		_isVerifying = false;
	}

	private async Task OnLoginClick()
	{
		if (_isVerifying)
			return;

		try
		{
			_isVerifying = true;

			_user = _users.FirstOrDefault(u => u.Phone == _phoneEmail || u.Email == _phoneEmail);

			if (_user is null)
			{
				await _phoneEmailTextBox.FocusAsync();
				throw new Exception("No user found with the provided phone number or email.");
			}

			if (!_user.Status)
				throw new Exception("This account is inactive. Please contact support.");

			if (_password != _user.Password)
			{
				_user.FailedAttempts++;

				if (_user.FailedAttempts >= _maxLoginAttempts)
				{
					_user.Status = false;
					await UserData.InsertUser(_user);
					throw new Exception("Your account has been locked due to multiple failed login attempts. Please contact support.");
				}

				await UserData.InsertUser(_user);

				await _passwordTextBox.FocusAsync();
				throw new Exception($"Incorrect password. You have {(_maxLoginAttempts - _user.FailedAttempts)} attempts remaining.");
			}

			await UserData.ResetInsertUser(_user);
			await DataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(_user));
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

		NavigationManager.NavigateTo(PageRouteNames.LoginWithCode, true);
	}
}