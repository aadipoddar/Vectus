using Vectus.Shared.Components.Dialog;

using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Operations;

public partial class SettingsPage
{
	#region Fields

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	// Primary Configuration
	private string _primaryCompanyLinkingId = string.Empty;
	private CompanyModel _selectedCompany;
	private List<CompanyModel> _companies = [];

	// Login Settings
	private bool _enableLoginWithCode = true;
	private bool _enableUsersToResetPassword = true;
	private int _maxLoginAttempts = 5;
	private int _codeResendLimit = 3;
	private int _codeExpiryMinutes = 10;

	// Master Code Prefixes
	private string _ledgerCodePrefix = string.Empty;
	private string _employeeCodePrefix = string.Empty;
	private string _sdrCodePrefix = string.Empty;
	private string _vehicleTypeCodePrefix = string.Empty;
	private string _documentTypeCodePrefix = string.Empty;
	private string _driverCodePrefix = string.Empty;
	private string _locationCodePrefix = string.Empty;
	private string _routeCodePrefix = string.Empty;
	private string _garageCodePrefix = string.Empty;
	private string _tyreCompanyCodePrefix = string.Empty;

	// Transaction Prefixes
	private string _financialAccountingTransactionPrefix = string.Empty;
	private string _tripRequestTransactionPrefix = string.Empty;
	private string _repairTransactionPrefix = string.Empty;

	// Ledger Linking
	private string _cashLedgerId = string.Empty;
	private LedgerModel _selectedCashLedger;
	private string _gstLedgerId = string.Empty;
	private LedgerModel _selectedGSTLedger;
	private List<LedgerModel> _ledgers = [];

	// Bank Reconciliation
	private string _bankAccountTypeId = string.Empty;
	private AccountTypeModel _selectedBankAccountType;
	private List<AccountTypeModel> _accountTypes = [];

	// Default Values
	private string _defaultSelectedVoucherId = string.Empty;
	private VoucherModel _selectedDefaultVoucher;
	private List<VoucherModel> _vouchers = [];

	// Fuel & Mileage
	private decimal _truckMileageKmPerLitre = 0;
	private decimal _dieselPricePerLitre = 0;

	// Report Settings
	private int _autoRefreshReportTimer = 5;
	private int _reportWarningDays = 30;
	private int _analysisCacheHours = 12;

	#endregion

	#region Load Data

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Admin]);
			await LoadData();
			_isLoading = false;
			StateHasChanged();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		try
		{
			await LoadAllSettings();
			await LoadCompanies();
			await LoadLedgers();
			await LoadAccountTypes();
			await LoadVouchers();
			MapSelections();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load settings: {ex.Message}", ToastType.Error);
		}
	}

	private async Task LoadAllSettings()
	{
		var map = (await CommonData.LoadTableData<SettingsModel>(OperationNames.Settings) ?? [])
			.ToDictionary(s => s.Key, s => s.Value);

		string Str(string key) => map.TryGetValue(key, out var v) ? v : null;
		int Int(string key, int fallback) => int.TryParse(Str(key), out var v) ? v : fallback;
		bool Bool(string key, bool fallback) => bool.TryParse(Str(key), out var v) ? v : fallback;
		decimal Dec(string key, decimal fallback) => decimal.TryParse(Str(key), out var v) ? v : fallback;

		// Primary Configuration
		_primaryCompanyLinkingId = Str(SettingsKeys.PrimaryCompanyLinkingId) ?? string.Empty;

		// Login Settings
		_enableLoginWithCode = Bool(SettingsKeys.EnableLoginWithCode, true);
		_enableUsersToResetPassword = Bool(SettingsKeys.EnableUsersToResetPassword, true);
		_maxLoginAttempts = Int(SettingsKeys.MaxLoginAttempts, 5);
		_codeResendLimit = Int(SettingsKeys.CodeResendLimit, 3);
		_codeExpiryMinutes = Int(SettingsKeys.CodeExpiryMinutes, 10);

		// Master Code Prefixes
		_ledgerCodePrefix = Str(SettingsKeys.LedgerCodePrefix) ?? string.Empty;
		_employeeCodePrefix = Str(SettingsKeys.EmployeeCodePrefix) ?? string.Empty;
		_sdrCodePrefix = Str(SettingsKeys.SDRCodePrefix) ?? string.Empty;
		_vehicleTypeCodePrefix = Str(SettingsKeys.VehicleTypeCodePrefix) ?? string.Empty;
		_documentTypeCodePrefix = Str(SettingsKeys.DocumentTypeCodePrefix) ?? string.Empty;
		_driverCodePrefix = Str(SettingsKeys.DriverCodePrefix) ?? string.Empty;
		_locationCodePrefix = Str(SettingsKeys.LocationCodePrefix) ?? string.Empty;
		_routeCodePrefix = Str(SettingsKeys.RouteCodePrefix) ?? string.Empty;
		_garageCodePrefix = Str(SettingsKeys.GarageCodePrefix) ?? string.Empty;
		_tyreCompanyCodePrefix = Str(SettingsKeys.TyreCompanyCodePrefix) ?? string.Empty;

		// Transaction Prefixes
		_financialAccountingTransactionPrefix = Str(SettingsKeys.FinancialAccountingTransactionPrefix) ?? string.Empty;
		_tripRequestTransactionPrefix = Str(SettingsKeys.TripRequestTransactionPrefix) ?? string.Empty;
		_repairTransactionPrefix = Str(SettingsKeys.RepairTransactionPrefix) ?? string.Empty;

		// Ledger Linking
		_cashLedgerId = Str(SettingsKeys.CashLedgerId) ?? string.Empty;
		_gstLedgerId = Str(SettingsKeys.GSTLedgerId) ?? string.Empty;

		// Bank Reconciliation
		_bankAccountTypeId = Str(SettingsKeys.BankAccountTypeId) ?? string.Empty;

		// Default Values
		_defaultSelectedVoucherId = Str(SettingsKeys.DefaultSelectedVoucherId) ?? string.Empty;

		// Fuel & Mileage
		_truckMileageKmPerLitre = Dec(SettingsKeys.TruckMileageKmPerLitre, 0);
		_dieselPricePerLitre = Dec(SettingsKeys.DieselPricePerLitre, 0);

		// Report Settings
		_autoRefreshReportTimer = Int(SettingsKeys.AutoRefreshReportTimer, 5);
		_reportWarningDays = Int(SettingsKeys.ReportWarningDays, 30);
		_analysisCacheHours = Int(SettingsKeys.AnalysisCacheHours, 12);
	}

	private async Task LoadCompanies()
	{
		var result = await CommonData.LoadTableData<CompanyModel>(AccountNames.Company);
		_companies = result ?? [];
	}

	private async Task LoadLedgers()
	{
		var result = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
		_ledgers = result ?? [];
	}

	private async Task LoadAccountTypes()
	{
		var result = await CommonData.LoadTableData<AccountTypeModel>(AccountNames.AccountType);
		_accountTypes = result ?? [];
	}

	private async Task LoadVouchers()
	{
		var result = await CommonData.LoadTableData<VoucherModel>(AccountNames.Voucher);
		_vouchers = result ?? [];
	}

	private void MapSelections()
	{
		if (!string.IsNullOrWhiteSpace(_primaryCompanyLinkingId) && int.TryParse(_primaryCompanyLinkingId, out var companyId))
			_selectedCompany = _companies.FirstOrDefault(c => c.Id == companyId);

		if (!string.IsNullOrWhiteSpace(_cashLedgerId) && int.TryParse(_cashLedgerId, out var cashId))
			_selectedCashLedger = _ledgers.FirstOrDefault(l => l.Id == cashId);

		if (!string.IsNullOrWhiteSpace(_gstLedgerId) && int.TryParse(_gstLedgerId, out var gstId))
			_selectedGSTLedger = _ledgers.FirstOrDefault(l => l.Id == gstId);

		if (!string.IsNullOrWhiteSpace(_bankAccountTypeId) && int.TryParse(_bankAccountTypeId, out var bankAccountTypeId))
			_selectedBankAccountType = _accountTypes.FirstOrDefault(a => a.Id == bankAccountTypeId);

		if (!string.IsNullOrWhiteSpace(_defaultSelectedVoucherId) && int.TryParse(_defaultSelectedVoucherId, out var voucherId))
			_selectedDefaultVoucher = _vouchers.FirstOrDefault(v => v.Id == voucherId);
	}

	#endregion

	#region Change Handlers

	private void OnCompanyChange(CompanyModel value)
	{
		_selectedCompany = value;
		_primaryCompanyLinkingId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnCashLedgerChange(LedgerModel value)
	{
		_selectedCashLedger = value;
		_cashLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnGSTLedgerChange(LedgerModel value)
	{
		_selectedGSTLedger = value;
		_gstLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnBankAccountTypeChange(AccountTypeModel value)
	{
		_selectedBankAccountType = value;
		_bankAccountTypeId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnDefaultVoucherChange(VoucherModel value)
	{
		_selectedDefaultVoucher = value;
		_defaultSelectedVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	#endregion

	#region Save Settings

	private async Task SaveSettings()
	{
		if (_isProcessing) return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (string.IsNullOrWhiteSpace(_primaryCompanyLinkingId))
			{
				await _toastNotification.ShowAsync("Validation", "Primary Company is required.", ToastType.Warning);
				return;
			}

			await _toastNotification.ShowAsync("Saving", "Processing settings...", ToastType.Info);

			var settings = await CommonData.LoadTableData<SettingsModel>(OperationNames.Settings);
			string Desc(string key) => settings.FirstOrDefault(s => s.Key == key)?.Description ?? string.Empty;

			// Primary Configuration
			await UpdateSetting(SettingsKeys.PrimaryCompanyLinkingId, _primaryCompanyLinkingId, Desc(SettingsKeys.PrimaryCompanyLinkingId));

			// Login Settings
			await UpdateSetting(SettingsKeys.EnableLoginWithCode, _enableLoginWithCode.ToString().ToLower(), Desc(SettingsKeys.EnableLoginWithCode));
			await UpdateSetting(SettingsKeys.EnableUsersToResetPassword, _enableUsersToResetPassword.ToString().ToLower(), Desc(SettingsKeys.EnableUsersToResetPassword));
			await UpdateSetting(SettingsKeys.MaxLoginAttempts, _maxLoginAttempts.ToString(), Desc(SettingsKeys.MaxLoginAttempts));
			await UpdateSetting(SettingsKeys.CodeResendLimit, _codeResendLimit.ToString(), Desc(SettingsKeys.CodeResendLimit));
			await UpdateSetting(SettingsKeys.CodeExpiryMinutes, _codeExpiryMinutes.ToString(), Desc(SettingsKeys.CodeExpiryMinutes));

			// Master Code Prefixes
			await UpdateSetting(SettingsKeys.LedgerCodePrefix, _ledgerCodePrefix, Desc(SettingsKeys.LedgerCodePrefix));
			await UpdateSetting(SettingsKeys.EmployeeCodePrefix, _employeeCodePrefix, Desc(SettingsKeys.EmployeeCodePrefix));
			await UpdateSetting(SettingsKeys.SDRCodePrefix, _sdrCodePrefix, Desc(SettingsKeys.SDRCodePrefix));
			await UpdateSetting(SettingsKeys.VehicleTypeCodePrefix, _vehicleTypeCodePrefix, Desc(SettingsKeys.VehicleTypeCodePrefix));
			await UpdateSetting(SettingsKeys.DocumentTypeCodePrefix, _documentTypeCodePrefix, Desc(SettingsKeys.DocumentTypeCodePrefix));
			await UpdateSetting(SettingsKeys.DriverCodePrefix, _driverCodePrefix, Desc(SettingsKeys.DriverCodePrefix));
			await UpdateSetting(SettingsKeys.LocationCodePrefix, _locationCodePrefix, Desc(SettingsKeys.LocationCodePrefix));
			await UpdateSetting(SettingsKeys.RouteCodePrefix, _routeCodePrefix, Desc(SettingsKeys.RouteCodePrefix));
			await UpdateSetting(SettingsKeys.GarageCodePrefix, _garageCodePrefix, Desc(SettingsKeys.GarageCodePrefix));
			await UpdateSetting(SettingsKeys.TyreCompanyCodePrefix, _tyreCompanyCodePrefix, Desc(SettingsKeys.TyreCompanyCodePrefix));

			// Transaction Prefixes
			await UpdateSetting(SettingsKeys.FinancialAccountingTransactionPrefix, _financialAccountingTransactionPrefix, Desc(SettingsKeys.FinancialAccountingTransactionPrefix));
			await UpdateSetting(SettingsKeys.TripRequestTransactionPrefix, _tripRequestTransactionPrefix, Desc(SettingsKeys.TripRequestTransactionPrefix));
			await UpdateSetting(SettingsKeys.RepairTransactionPrefix, _repairTransactionPrefix, Desc(SettingsKeys.RepairTransactionPrefix));

			// Ledger Linking
			await UpdateSetting(SettingsKeys.CashLedgerId, _cashLedgerId, Desc(SettingsKeys.CashLedgerId));
			await UpdateSetting(SettingsKeys.GSTLedgerId, _gstLedgerId, Desc(SettingsKeys.GSTLedgerId));

			// Bank Reconciliation
			await UpdateSetting(SettingsKeys.BankAccountTypeId, _bankAccountTypeId, Desc(SettingsKeys.BankAccountTypeId));

			// Default Values
			await UpdateSetting(SettingsKeys.DefaultSelectedVoucherId, _defaultSelectedVoucherId, Desc(SettingsKeys.DefaultSelectedVoucherId));

			// Fuel & Mileage
			await UpdateSetting(SettingsKeys.TruckMileageKmPerLitre, _truckMileageKmPerLitre.ToString(), Desc(SettingsKeys.TruckMileageKmPerLitre));
			await UpdateSetting(SettingsKeys.DieselPricePerLitre, _dieselPricePerLitre.ToString(), Desc(SettingsKeys.DieselPricePerLitre));

			// Report Settings
			await UpdateSetting(SettingsKeys.AutoRefreshReportTimer, _autoRefreshReportTimer.ToString(), Desc(SettingsKeys.AutoRefreshReportTimer));
			await UpdateSetting(SettingsKeys.ReportWarningDays, _reportWarningDays.ToString(), Desc(SettingsKeys.ReportWarningDays));
			await UpdateSetting(SettingsKeys.AnalysisCacheHours, _analysisCacheHours.ToString(), Desc(SettingsKeys.AnalysisCacheHours));

			await _toastNotification.ShowAsync("Saved", "Settings saved successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save settings: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private static async Task UpdateSetting(string key, string value, string description)
	{
		await SettingsData.UpdateSettings(new SettingsModel
		{
			Key = key,
			Value = value ?? string.Empty,
			Description = description
		});
	}

	#endregion

	#region Reset Settings

	private async Task ShowResetConfirmation() =>
		await ShowConfirmation("Reset", "Are you sure you want to restore all settings to their defaults?", ResetSettings);

	private async Task ResetSettings()
	{
		try
		{
			_isProcessing = true;

			await _toastNotification.ShowAsync("Resetting", "Restoring default settings...", ToastType.Info);
			await SettingsData.ResetSettings();
			await LoadData();
			await _toastNotification.ShowAsync("Reset", "Settings restored to defaults.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to reset settings: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ShowConfirmation(string title, string message, Func<Task> action)
	{
		_confirmTitle = title;
		_confirmMessage = message;
		_confirmAction = action;
		StateHasChanged();
		await _confirmationDialog.ShowAsync();
	}

	private async Task OnConfirmed()
	{
		await _confirmationDialog.HideAsync();
		if (_confirmAction is not null)
			await _confirmAction();
		_confirmAction = null;
	}

	private async Task OnCancelled()
	{
		_confirmAction = null;
		await _confirmationDialog.HideAsync();
	}

	#endregion
}
