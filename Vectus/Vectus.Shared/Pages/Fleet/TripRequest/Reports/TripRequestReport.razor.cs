using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Route.Data;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Data;
using VectusLibrary.Fleet.TripRequest.Exports;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.TripRequest.Reports;

public partial class TripRequestReport : IAsyncDisposable
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
	private RouteOverviewModel? _selectedRoute = null;
	private SDRModel? _selectedSDR = null;
	private string _selectedRequestStatus = null;

	private List<CompanyModel> _companies = [];
	private List<VehicleModel> _vehicles = [];
	private List<RouteOverviewModel> _routes = [];
	private List<SDRModel> _sdrs = [];
	private List<string> _requestStatuses = [.. Enum.GetNames<RequestStatus>()];
	private List<TripRequestOverviewModel> _transactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Accept (Alt + A)", Id = "Accept", IconCss = "e-icons e-check", Target = ".e-content" },
		new() { Text = "Reject (Alt + J)", Id = "Reject", IconCss = "e-icons e-close", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<TripRequestOverviewModel> _sfGrid;
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
		_vehicles = await CommonData.LoadTableDataByStatus<VehicleModel>(FleetNames.Vehicle);
		_routes = await RouteData.LoadRouteOverview();
		_sdrs = await CommonData.LoadTableDataByStatus<SDRModel>(FleetNames.SDR);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_vehicles = [.. _vehicles.OrderBy(s => s.Code)];
		_routes = [.. _routes.OrderBy(s => s.Code)];
		_sdrs = [.. _sdrs.OrderBy(s => s.Name)];
	}

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			_transactionOverviews = await CommonData.LoadTableDataByDate<TripRequestOverviewModel>(
				FleetNames.TripRequestOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			if (!_showDeleted)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedVehicle?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.VehicleId == _selectedVehicle.Id)];

			if (_selectedRoute?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.RouteId == _selectedRoute.Id)];

			if (_selectedSDR?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.SDRId == _selectedSDR.Id)];

			if (!string.IsNullOrWhiteSpace(_selectedRequestStatus))
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.RequestStatus == _selectedRequestStatus)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
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

	private async Task OnVehicleChanged(VehicleModel value)
	{
		_selectedVehicle = value;
		await LoadTransactionOverviews();
	}

	private async Task OnRouteChanged(RouteOverviewModel value)
	{
		_selectedRoute = value;
		await LoadTransactionOverviews();
	}

	private async Task OnSDRChanged(SDRModel value)
	{
		_selectedSDR = value;
		await LoadTransactionOverviews();
	}

	private async Task OnRequestStatusChanged(string value)
	{
		_selectedRequestStatus = value;
		await LoadTransactionOverviews();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadTransactionOverviews();
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

			var (stream, fileName) = await TripRequestReportExport.ExportReport(
				_transactionOverviews,
				ReportExportType.Excel,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedVehicle?.Id > 0 ? _selectedVehicle : null,
				_selectedRoute?.Id > 0 ? _selectedRoute : null,
				_selectedSDR?.Id > 0 ? _selectedSDR : null,
				_selectedRequestStatus
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

			var (stream, fileName) = await TripRequestReportExport.ExportReport(
				_transactionOverviews,
				ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedVehicle?.Id > 0 ? _selectedVehicle : null,
				_selectedRoute?.Id > 0 ? _selectedRoute : null,
				_selectedSDR?.Id > 0 ? _selectedSDR : null,
				_selectedRequestStatus
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

			var tripRequest = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, id)
				?? throw new Exception("Transaction not found.");
			tripRequest.LastModifiedBy = _user.Id;
			tripRequest.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			tripRequest.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await TripRequestData.DeleteTransaction(tripRequest);

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

			var tripRequest = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, id)
				?? throw new Exception("Transaction not found.");
			tripRequest.LastModifiedBy = _user.Id;
			tripRequest.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			tripRequest.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await TripRequestData.RecoverTransaction(tripRequest);

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

	private async Task AcceptTransaction(int id, string transactionNo)
	{
		if (_isProcessing || id == 0)
			return;

		try
		{
			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to accept this transaction.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Accepting transaction...", ToastType.Info);

			var tripRequest = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, id)
				?? throw new Exception("Transaction not found.");

			if (!tripRequest.Status)
				throw new InvalidOperationException("Cannot accept a deleted transaction. Please recover it first.");

			if (string.Equals(tripRequest.RequestStatus, RequestStatus.Accepted.ToString(), StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Transaction is already accepted.");

			tripRequest.RequestStatus = RequestStatus.Accepted.ToString();
			tripRequest.LastModifiedBy = _user.Id;
			tripRequest.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			tripRequest.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await TripRequestData.SaveTransaction(tripRequest);

			await _toastNotification.ShowAsync("Success", $"Transaction {transactionNo} has been accepted successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while accepting transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task RejectTransaction(int id, string transactionNo)
	{
		if (_isProcessing || id == 0)
			return;

		try
		{
			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to reject this transaction.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Rejecting transaction...", ToastType.Info);

			var tripRequest = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, id)
				?? throw new Exception("Transaction not found.");

			if (!tripRequest.Status)
				throw new InvalidOperationException("Cannot reject a deleted transaction. Please recover it first.");

			if (string.Equals(tripRequest.RequestStatus, RequestStatus.Accepted.ToString(), StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Cannot reject an accepted transaction. Please accept it first before rejecting.");

			tripRequest.RequestStatus = RequestStatus.Rejected.ToString();
			tripRequest.LastModifiedBy = _user.Id;
			tripRequest.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			tripRequest.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			await TripRequestData.SaveTransaction(tripRequest);

			await _toastNotification.ShowAsync("Success", $"Transaction {transactionNo} has been rejected successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while rejecting transaction: {ex.Message}", ToastType.Error);
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
			await ShowConfirmation("Delete", $"Are you sure you want to delete transaction {record.TransactionNo}", () => DeleteTransaction(record.Id, record.TransactionNo));
		else
			await ShowConfirmation("Recover", $"Are you sure you want to recover transaction {record.TransactionNo}", () => RecoverTransaction(record.Id, record.TransactionNo));
	}

	private async Task AcceptSelectedTransaction()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();
		await ShowConfirmation("Accept", $"Are you sure you want to accept transaction {record.TransactionNo}", () => AcceptTransaction(record.Id, record.TransactionNo));
	}

	private async Task RejectSelectedTransaction()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();
		await ShowConfirmation("Reject", $"Are you sure you want to reject transaction {record.TransactionNo}", () => RejectTransaction(record.Id, record.TransactionNo));
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
			case "NewTransaction": await AuthenticationService.NavigateToRoute(PageRouteNames.TripRequest, FormFactor, JSRuntime, NavigationManager); break;
			case "Refresh": await LoadTransactionOverviews(); break;
			case "ToggleDeleted": await ToggleDeleted(); break;
			case "ToggleDetailsView": await ToggleDetailsView(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ViewSelected": await ViewSelectedTransaction(); break;
			case "DeleteRecoverSelected": await DeleteRecoverSelectedTransaction(); break;
			case "AcceptSelected": await AcceptSelectedTransaction(); break;
			case "RejectSelected": await RejectSelectedTransaction(); break;
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

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<TripRequestOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "DeleteRecover": await DeleteRecoverSelectedTransaction(); break;
			case "Accept": await AcceptSelectedTransaction(); break;
			case "Reject": await RejectSelectedTransaction(); break;
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
		await LoadTransactionOverviews();
		StateHasChanged();
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
				await LoadTransactionOverviews();
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
