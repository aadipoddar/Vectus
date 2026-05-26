using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;
using Vectus.Shared.Services;

using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Garage.Models;
using VectusLibrary.Fleet.Repair.Data;
using VectusLibrary.Fleet.Repair.Exports;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Repair.Reports;

public partial class RepairJobReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel? _selectedCompany = null;
	private VehicleModel? _selectedVehicle = null;
	private GarageModel? _selectedGarage = null;

	private List<CompanyModel> _companies = [];
	private List<VehicleModel> _vehicles = [];
	private List<GarageModel> _garages = [];
	private List<RepairJobOverviewModel> _jobOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportSelectedPdf", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportSelectedExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<RepairJobOverviewModel> _sfGrid;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet, UserRoles.Reports]);
			await InitializePage();
		}
		catch { NavigateBack(); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadJobOverviews();
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
		_vehicles = await CommonData.LoadTableDataByStatus<VehicleModel>(FleetNames.Vehicle);
		_garages = await CommonData.LoadTableDataByStatus<GarageModel>(FleetNames.Garage);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_vehicles = [.. _vehicles.OrderBy(s => s.Code)];
		_garages = [.. _garages.OrderBy(s => s.Name)];
	}

	private async Task LoadJobOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching repair jobs...", ToastType.Info);

			_jobOverviews = await CommonData.LoadTableDataByDate<RepairJobOverviewModel>(
				FleetNames.RepairJobOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			if (!_showDeleted)
				_jobOverviews = [.. _jobOverviews.Where(_ => _.Status)];

			if (_selectedCompany?.Id > 0)
				_jobOverviews = [.. _jobOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedVehicle?.Id > 0)
				_jobOverviews = [.. _jobOverviews.Where(_ => _.VehicleId == _selectedVehicle.Id)];

			if (_selectedGarage?.Id > 0)
				_jobOverviews = [.. _jobOverviews.Where(_ => _.GarageId == _selectedGarage.Id)];

			_jobOverviews = [.. _jobOverviews.OrderBy(_ => _.TransactionDateTime).ThenBy(_ => _.TransactionNo)];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load repair jobs: {ex.Message}", ToastType.Error);
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
		await LoadJobOverviews();
	}

	private async Task OnCompanyChanged(CompanyModel value)
	{
		_selectedCompany = value;
		await LoadJobOverviews();
	}

	private async Task OnVehicleChanged(VehicleModel value)
	{
		_selectedVehicle = value;
		await LoadJobOverviews();
	}

	private async Task OnGarageChanged(GarageModel value)
	{
		_selectedGarage = value;
		await LoadJobOverviews();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadJobOverviews();
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

			var (stream, fileName) = await RepairReportExport.ExportJobReport(
				_jobOverviews,
				ReportExportType.Excel,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedVehicle?.Id > 0 ? _selectedVehicle : null,
				_selectedGarage?.Id > 0 ? _selectedGarage : null
			);
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

			var (stream, fileName) = await RepairReportExport.ExportJobReport(
				_jobOverviews,
				ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedVehicle?.Id > 0 ? _selectedVehicle : null,
				_selectedGarage?.Id > 0 ? _selectedGarage : null
			);
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
			await _toastNotification.ShowAsync("Processing", "Generating the invoice...", ToastType.Info);

			var (stream, fileName) = await RepairInvoiceExport.ExportInvoice(_sfGrid.SelectedRecords.First().MasterId, InvoiceExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The invoice has been downloaded successfully.", ToastType.Success);
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
			await _toastNotification.ShowAsync("Processing", "Generating the invoice...", ToastType.Info);

			var (stream, fileName) = await RepairInvoiceExport.ExportInvoice(_sfGrid.SelectedRecords.First().MasterId, InvoiceExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The invoice has been downloaded successfully.", ToastType.Success);
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

		var selected = _sfGrid.SelectedRecords.First();
		if (!selected.Status)
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

			var repair = await CommonData.LoadTableDataById<RepairModel>(FleetNames.Repair, id)
				?? throw new Exception("Transaction not found.");
			repair.LastModifiedBy = _user.Id;
			repair.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			repair.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await RepairData.DeleteTransaction(repair);

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
			await LoadJobOverviews();
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

			var repair = await CommonData.LoadTableDataById<RepairModel>(FleetNames.Repair, id)
				?? throw new Exception("Transaction not found.");
			repair.LastModifiedBy = _user.Id;
			repair.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			repair.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await RepairData.RecoverTransaction(repair);

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
			await LoadJobOverviews();
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
			case "NewTransaction": await AuthenticationService.NavigateToRoute(PageRouteNames.Repair, FormFactor, JSRuntime, NavigationManager); break;
			case "SummaryReport": await AuthenticationService.NavigateToRoute(PageRouteNames.RepairReport, FormFactor, JSRuntime, NavigationManager); break;
			case "Refresh": await LoadJobOverviews(); break;
			case "ToggleDeleted": await ToggleDeleted(); break;
			case "ToggleDetailsView": await ToggleDetailsView(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ViewSelected": await ViewSelectedTransaction(); break;
			case "ExportSelectedPdf": await ExportSelectedTransactionPdf(); break;
			case "ExportSelectedExcel": await ExportSelectedTransactionExcel(); break;
			case "DeleteRecoverSelected": await DeleteRecoverSelectedTransaction(); break;
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

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<RepairJobOverviewModel> args)
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

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadJobOverviews();
		StateHasChanged();
	}

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.FleetReportsDashboard);

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
				await LoadJobOverviews();
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
