using Syncfusion.Blazor.Inputs;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.MailUtils;

namespace Vectus.Shared.Pages.Authentication;

public partial class LoginWithCodePage
{
	private UserModel _user = new();

	private bool _isVerifying = false;

	private bool _isCodeSent = false;
	private bool _isEmail = false;

	private string _phoneEmail = string.Empty;
	private string _otpCode = string.Empty;
	private DateTime _codeSentTime;

	private string _codePlaceholder = "Enter Code";

	private int _verificationCode;

	private string _newPassword = string.Empty;

	private bool _isLoginWithCodeEnabled = false;
	private bool _isEnabledUsersResetPassword = false;
	private int _maxLoginAttempts;
	private int _codeResendLimit;
	private int _codeExpiryMinutes;

	private List<UserModel> _users = [];

	private CustomTextField _phoneEmailTextBox;
	private CustomTextField _newPasswordTextBox;
	private SfOtpInput _otpInput;

	private ToastNotification _toastNotification;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			await DataStorageService.SecureRemoveAll();
			await _phoneEmailTextBox.FocusAsync();

			_users = await CommonData.LoadTableData<UserModel>(OperationNames.User);

			_isLoginWithCodeEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.EnableLoginWithCode)).Value);

			if (!_isLoginWithCodeEnabled)
				NavigationManager.NavigateTo(PageRouteNames.Login, true);

			_isEnabledUsersResetPassword = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.EnableUsersToResetPassword)).Value);
			_maxLoginAttempts = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.MaxLoginAttempts)).Value);
			_codeResendLimit = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.CodeResendLimit)).Value);
			_codeExpiryMinutes = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.CodeExpiryMinutes)).Value);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Initializing Login Page", ex.Message, ToastType.Error);
		}
	}

	private async Task OnSendCodeClick()
	{
		if (_isVerifying)
			return;

		if (string.IsNullOrWhiteSpace(_phoneEmail))
		{
			await _toastNotification.ShowAsync("Invalid Input", "Please enter a valid phone number or email address.", ToastType.Error);
			return;
		}

		_isEmail = _phoneEmail.Contains('@') && _phoneEmail.Contains('.');

		var user = _users.FirstOrDefault(u => u.Phone == _phoneEmail || u.Email == _phoneEmail);
		if (user is null || user.Status == false)
		{
			await _toastNotification.ShowAsync("No User Found", "No user found with the provided phone number or email.", ToastType.Error);
			_codePlaceholder = "Enter Code";
			return;
		}

		try
		{
			_isVerifying = true;
			_user = user;

			if (_isEmail)
			{
				_verificationCode = new Random().Next(100000, 999999);
				if (_user.CodeResends >= _codeResendLimit)
				{
					_user.Status = false;
					await UserData.InsertUser(_user);
					throw new Exception("You have exceeded the maximum number of code resends. Your account has been locked. Please contact support.");
				}

				var guid = Guid.NewGuid().ToString();
				await DataStorageService.SecureSaveAsync(StorageFileNames.UserDeviceIdDataFileName, guid);

				_user.LastCode = _verificationCode;
				_user.LastCodeDateTime = await CommonData.LoadCurrentDateTime();
				_user.LastCodeDeviceId = guid;
				await UserData.InsertUser(_user);

				var redirectLink = NavigationManager.BaseUri + PageRouteNames.LoginWithCodeRedirect + $"/{_user.Id}/{_verificationCode}";

				await AuthenticationMailing.SendLoginCodeEmail(_user, _verificationCode.ToString(), redirectLink, _codeExpiryMinutes);
				_codeSentTime = await CommonData.LoadCurrentDateTime();
				_codePlaceholder = $"Enter Code sent to {_user.Email} for {_user.Name}. The code is valid till {_codeSentTime.AddMinutes(_codeExpiryMinutes):hh:mm tt}";

				_user.CodeResends++;
			}

			else
			{
				throw new NotImplementedException("Phone code sending not implemented.");
				// _codePlaceholder = $"Enter Code sent to {_user.Phone} for {_user.Name}";
			}

			await _toastNotification.ShowAsync("Code Sent Successfully", _isEmail ? $"A code has been sent to {_user.Email}" : $"A code has been sent to {_user.Phone}", ToastType.Success);

			_otpInput?.FocusAsync();
			_isCodeSent = true;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Code Send Failed", ex.Message, ToastType.Error);
			_codePlaceholder = "Enter Code";
			_isCodeSent = false;
		}
		finally
		{
			_isVerifying = false;
		}
	}

	private async Task OnLoginWithCodeClick()
	{
		if (_isVerifying)
			return;

		try
		{
			_isVerifying = true;

			if (!_isCodeSent)
				throw new Exception("Please send the code before attempting to log in.");

			if (string.IsNullOrWhiteSpace(_otpCode) || _otpCode.Length != _otpInput.Length)
			{
				_otpInput.FocusAsync();
				throw new Exception("Please enter the complete code sent to you.");
			}

			if (_user.Id == 0)
			{
				await _phoneEmailTextBox.FocusAsync();
				throw new Exception("No user selected. Please enter a valid phone number or email address to send the code.");
			}

			if (!_user.Status)
			{
				await _phoneEmailTextBox.FocusAsync();
				throw new Exception("This account is inactive. Please contact support.");
			}

			if (_otpCode != _verificationCode.ToString())
			{
				_user.FailedAttempts++;

				if (_user.FailedAttempts >= _maxLoginAttempts)
				{
					_user.Status = false;
					await UserData.InsertUser(_user);
					throw new Exception("You have exceeded the maximum number of login attempts. Your account has been locked. Please contact support.");
				}

				await UserData.InsertUser(_user);

				_otpInput.FocusAsync();
				throw new Exception("Incorrect code. Please try again.");
			}

			if (_codeSentTime.AddMinutes(_codeExpiryMinutes) < await CommonData.LoadCurrentDateTime())
			{
				_otpInput.FocusAsync();
				throw new Exception("The code has expired. Please request a new code.");
			}

			if (!string.IsNullOrWhiteSpace(_newPassword))
			{
				if (_isEnabledUsersResetPassword)
					throw new Exception("Users are not allowed to set a new password. Please contact support.");

				if (_newPassword.Length < 6)
				{
					await _newPasswordTextBox.FocusAsync();
					throw new Exception("Weak Password. The new password must be at least 6 characters long.");
				}

				_user.Password = _newPassword;
			}

			await UserData.ResetInsertUser(_user);
			await DataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(_user));
			NavigationManager.NavigateTo(PageRouteNames.Dashboard);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Failed to Log In", ex.Message, ToastType.Error);
		}
		finally
		{
			_isVerifying = false;
		}
	}
}