using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Accounts.FinancialAccounting.Data;
using VectusLibrary.Accounts.FinancialAccounting.Exports;
using VectusLibrary.Accounts.FinancialAccounting.Models;
using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Accounts.Reports;

public partial class BankReconciliationPage : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel? _selectedCompany = null;
	private AccountTypeModel? _selectedAccountType = null;
	private LedgerModel? _selectedLedger = null;
	private int _reconciledFilter = YesNoFilterOptions.All;
	private YesNoFilterOption _selectedReconciledFilter;

	private List<CompanyModel> _companies = [];
	private List<AccountTypeModel> _accountTypes = [];
	private List<LedgerModel> _ledgers = [];

	private List<FinancialAccountingLedgerOverviewModel> _transactionOverviews = [];
	private readonly HashSet<int> _dirtyLineIds = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportSelectedPdf", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportSelectedExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<FinancialAccountingLedgerOverviewModel> _sfGrid;
	private CustomDateRangePicker _sfFirstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Accounts, UserRoles.Reports]);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadTransactionOverviews();
		await StartAutoRefresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_fromDate = await CommonData.LoadCurrentDateTime();
		_toDate = _fromDate;

		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_accountTypes = await CommonData.LoadTableDataByStatus<AccountTypeModel>(AccountNames.AccountType);
		_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_accountTypes = [.. _accountTypes.OrderBy(s => s.Name)];
		_ledgers = [.. _ledgers.OrderBy(s => s.Name)];

		// Default the Type filter to the configured bank account type (soft).
		var bankTypeSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.BankAccountTypeId);
		if (int.TryParse(bankTypeSetting?.Value, out var bankTypeId) && bankTypeId > 0)
			_selectedAccountType = _accountTypes.FirstOrDefault(a => a.Id == bankTypeId);
	}

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching ledger lines...", ToastType.Info);

			_transactionOverviews = await CommonData.LoadTableDataByDate<FinancialAccountingLedgerOverviewModel>(
				AccountNames.FinancialAccountingLedgerOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(l => l.CompanyId == _selectedCompany.Id)];

			if (_selectedAccountType?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(l => l.AccountTypeId == _selectedAccountType.Id)];

			if (_selectedLedger?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(l => l.LedgerId == _selectedLedger.Id)];

			if (_reconciledFilter == YesNoFilterOptions.Yes)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.ClearingDate is not null)];
			else if (_reconciledFilter == YesNoFilterOptions.No)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.ClearingDate is null)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			_dirtyLineIds.Clear();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load lines: {ex.Message}", ToastType.Error);
		}
		finally
		{
			if (_sfGrid is not null)
				await _sfGrid.Refresh();
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(MudBlazor.DateRange range)
	{
		_fromDate = range?.Start ?? _fromDate;
		_toDate = range?.End ?? _toDate;
		await LoadTransactionOverviews();
	}

	private async Task OnCompanyChanged(CompanyModel value)
	{
		_selectedCompany = value;
		await LoadTransactionOverviews();
	}

	private async Task OnAccountTypeChanged(AccountTypeModel value)
	{
		_selectedAccountType = value;
		await LoadTransactionOverviews();
	}

	private async Task OnLedgerChanged(LedgerModel value)
	{
		_selectedLedger = value;
		await LoadTransactionOverviews();
	}

	private async Task OnReconciledFilterChanged(YesNoFilterOption value)
	{
		_selectedReconciledFilter = value;
		_reconciledFilter = value?.Id ?? YesNoFilterOptions.All;
		await LoadTransactionOverviews();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadTransactionOverviews();
	}
	#endregion

	#region Reconcile Editing
	private void OnClearingDateChanged(FinancialAccountingLedgerOverviewModel line, DateTime? date)
	{
		if (line is null)
			return;

		line.ClearingDate = date;
		_dirtyLineIds.Add(line.Id);
		StateHasChanged();
	}

	private async Task SaveReconciliation()
	{
		if (_isProcessing)
			return;

		if (_dirtyLineIds.Count == 0)
		{
			await _toastNotification.ShowAsync("Nothing to Save", "No clearing dates have changed.", ToastType.Warning);
			return;
		}

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Saving", "Updating clearing dates...", ToastType.Info);

			var changed = _transactionOverviews
				.Where(l => _dirtyLineIds.Contains(l.Id))
				.Select(l => new FinancialAccountingLedgerModel { Id = l.Id, ClearingDate = l.ClearingDate })
				.ToList();

			await FinancialAccountingData.SaveBRSDates(changed, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Saved", $"{changed.Count} line(s) updated successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save reconciliation: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await FinancialAccountingReportExport.ExportLedgerReport(
				_transactionOverviews,
				ReportExportType.Excel,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				false,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedLedger?.Id > 0 ? _selectedLedger : null);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await FinancialAccountingReportExport.ExportLedgerReport(
				_transactionOverviews,
				ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				false,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedLedger?.Id > 0 ? _selectedLedger : null);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportSelectedTransactionPdf()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, true, false);
			await SaveAndViewService.SaveAndView(decodeTransactionNo.PDFStream.fileName, decodeTransactionNo.PDFStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportSelectedTransactionExcel()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, true);
			await SaveAndViewService.SaveAndView(decodeTransactionNo.ExcelStream.fileName, decodeTransactionNo.ExcelStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Actions
	private async Task ViewSelectedTransaction()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		if (!_sfGrid.SelectedRecords.First().Status)
		{
			await _toastNotification.ShowAsync("Cannot View", "The selected transaction is deleted. Please recover it first.", ToastType.Warning);
			return;
		}

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false);
		await AuthenticationService.NavigateToRoute(decodedTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}

	private async Task DeleteTransaction(int id, string transactionNo)
	{
		if (_isProcessing || id == 0)
			return;

		try
		{
			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			var accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, id)
				?? throw new Exception("Transaction not found.");
			accounting.LastModifiedBy = _user.Id;
			accounting.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			accounting.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await FinancialAccountingData.DeleteTransaction(accounting);

			await _toastNotification.ShowAsync("Success", $"Transaction {transactionNo} has been deleted successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while deleting transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task RecoverTransaction(int id, string transactionNo)
	{
		if (_isProcessing || id == 0)
			return;

		try
		{
			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

			var accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, id)
				?? throw new Exception("Transaction not found.");
			accounting.LastModifiedBy = _user.Id;
			accounting.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			accounting.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await FinancialAccountingData.RecoverTransaction(accounting);

			await _toastNotification.ShowAsync("Success", $"Transaction {transactionNo} has been recovered successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while recovering transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task DeleteRecoverSelectedTransaction()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();

		if (record.Status)
			await ShowConfirmation("Delete", $"Are you sure you want to delete transaction {record.TransactionNo}", () => DeleteTransaction(record.MasterId, record.TransactionNo));
		else
			await ShowConfirmation("Recover", $"Are you sure you want to recover transaction {record.TransactionNo}", () => RecoverTransaction(record.MasterId, record.TransactionNo));
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

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccounting, FormFactor, JSRuntime, NavigationManager); break;
			case "Save": await SaveReconciliation(); break;
			case "Refresh": await LoadTransactionOverviews(); break;
			case "ToggleDetailsView": await ToggleDetailsView(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ViewSelected": await ViewSelectedTransaction(); break;
			case "DownloadSelectedPdf": await ExportSelectedTransactionPdf(); break;
			case "DownloadSelectedExcel": await ExportSelectedTransactionExcel(); break;
			case "DeleteRecoverSelected": await DeleteRecoverSelectedTransaction(); break;
			case "AccountingLedger": await AuthenticationService.NavigateToRoute(PageRouteNames.AccountingLedgerReport, FormFactor, JSRuntime, NavigationManager); break;
			case "AccountingReport": await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccountingReport, FormFactor, JSRuntime, NavigationManager); break;
			case "TrialBalance": await AuthenticationService.NavigateToRoute(PageRouteNames.TrialBalanceReport, FormFactor, JSRuntime, NavigationManager); break;
			case "ProfitLoss": await AuthenticationService.NavigateToRoute(PageRouteNames.ProfitAndLossReport, FormFactor, JSRuntime, NavigationManager); break;
			case "BalanceSheet": await AuthenticationService.NavigateToRoute(PageRouteNames.BalanceSheetReport, FormFactor, JSRuntime, NavigationManager); break;
			case "PeriodToday": await HandleDatesChanged(DateRangeType.Today); break;
			case "PeriodPreviousDay": await HandleDatesChanged(DateRangeType.Yesterday); break;
			case "PeriodNextDay": await HandleDatesChanged(DateRangeType.NextDay); break;
			case "PeriodCurrentMonth": await HandleDatesChanged(DateRangeType.CurrentMonth); break;
			case "PeriodPreviousMonth": await HandleDatesChanged(DateRangeType.PreviousMonth); break;
			case "PeriodNextMonth": await HandleDatesChanged(DateRangeType.NextMonth); break;
			case "PeriodCurrentFinancialYear": await HandleDatesChanged(DateRangeType.CurrentFinancialYear); break;
			case "PeriodPreviousFinancialYear": await HandleDatesChanged(DateRangeType.PreviousFinancialYear); break;
			case "PeriodNextFinancialYear": await HandleDatesChanged(DateRangeType.NextFinancialYear); break;
			case "PeriodAllTime": await HandleDatesChanged(DateRangeType.AllTime); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialAccountingLedgerOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "ExportSelectedPdf": await ExportSelectedTransactionPdf(); break;
			case "ExportSelectedExcel": await ExportSelectedTransactionExcel(); break;
			case "DeleteRecover": await DeleteRecoverSelectedTransaction(); break;
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}

	private async Task StartAutoRefresh()
	{
		var timerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
		var refreshMinutes = int.TryParse(timerSetting?.Value, out var minutes) ? minutes : 5;

		_autoRefreshCts = new CancellationTokenSource();
		_autoRefreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));
		_ = AutoRefreshLoop(_autoRefreshCts.Token);
	}

	private async Task AutoRefreshLoop(CancellationToken cancellationToken)
	{
		try
		{
			while (await _autoRefreshTimer.WaitForNextTickAsync(cancellationToken))
			{
				// Never clobber in-progress reconciliation edits.
				if (_dirtyLineIds.Count == 0)
					await LoadTransactionOverviews();
			}
		}
		catch (OperationCanceledException)
		{
			// Timer was cancelled, expected on dispose
		}
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if (_autoRefreshCts is not null)
		{
			await _autoRefreshCts.CancelAsync();
			_autoRefreshCts.Dispose();
		}

		_autoRefreshTimer?.Dispose();
		GC.SuppressFinalize(this);
	}
	#endregion
}
