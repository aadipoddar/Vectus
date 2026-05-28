using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Fleet.VehicleDocument.Exports;
using VectusLibrary.Fleet.VehicleDocument.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.VehicleDocument.Reports;

public partial class VehicleDocumentRenewalReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;

	private int _warningDays = 30;

	private VehicleModel _selectedVehicle = null;
	private VehicleDocumentTypeModel _selectedDocumentType = null;

	private List<VehicleModel> _vehicles = [];
	private List<VehicleDocumentTypeModel> _documentTypes = [];
	private List<VehicleDocumentRenewalOverviewModel> _transactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Download Document (Alt + D)", Id = "DownloadSelectedDocument", IconCss = "e-icons e-download", Target = ".e-content" }
	];

	private SfGrid<VehicleDocumentRenewalOverviewModel> _sfGrid;
	private CustomAutoComplete<VehicleModel> _sfFirstFocus;
	private ToastNotification _toastNotification;

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
		_vehicles = await CommonData.LoadTableDataByStatus<VehicleModel>(FleetNames.Vehicle);
		_documentTypes = await CommonData.LoadTableDataByStatus<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType);

		_vehicles = [.. _vehicles.OrderBy(s => s.Code)];
		_documentTypes = [.. _documentTypes.OrderBy(s => s.Name)];

		var warningSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.ReportWarningDays);
		_warningDays = int.TryParse(warningSetting?.Value, out var days) ? days : 30;
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

			_transactionOverviews = await CommonData.LoadTableData<VehicleDocumentRenewalOverviewModel>(FleetNames.VehicleDocumentRenewalOverview);

			if (_selectedVehicle?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.VehicleId == _selectedVehicle.Id)];

			if (_selectedDocumentType?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.VehicleDocumentTypeId == _selectedDocumentType.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.RenewalDate)];
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
	private async Task OnVehicleChanged(VehicleModel value)
	{
		_selectedVehicle = value;
		await LoadTransactionOverviews();
	}

	private async Task OnDocumentTypeChanged(VehicleDocumentTypeModel value)
	{
		_selectedDocumentType = value;
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

			var (stream, fileName) = await VehicleDocumentRenewalReportExport.ExportReport(
				_transactionOverviews,
				ReportExportType.Excel,
				_showAllColumns,
				_selectedVehicle?.Id > 0 ? _selectedVehicle : null,
				_selectedDocumentType?.Id > 0 ? _selectedDocumentType : null
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

			var (stream, fileName) = await VehicleDocumentRenewalReportExport.ExportReport(
				_transactionOverviews,
				ReportExportType.PDF,
				_showAllColumns,
				_selectedVehicle?.Id > 0 ? _selectedVehicle : null,
				_selectedDocumentType?.Id > 0 ? _selectedDocumentType : null
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
	private async Task DownloadSelectedDocument()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var documentUrl = _sfGrid.SelectedRecords.First().DocumentUrl;
		if (string.IsNullOrWhiteSpace(documentUrl))
		{
			await _toastNotification.ShowAsync("No Document", "No document is available for the selected transaction.", ToastType.Warning);
			return;
		}

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Downloading the document...", ToastType.Info);

			var (stream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(documentUrl);
			var fileName = documentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Downloaded", "The document has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Downloading", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": await AuthenticationService.NavigateToRoute(PageRouteNames.VehicleDocument, FormFactor, JSRuntime, NavigationManager); break;
			case "Refresh": await LoadTransactionOverviews(); break;
			case "ToggleDetailsView": await ToggleDetailsView(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "VehicleDocument": await AuthenticationService.NavigateToRoute(PageRouteNames.VehicleDocument, FormFactor, JSRuntime, NavigationManager); break;
			case "VehicleDocumentType": await AuthenticationService.NavigateToRoute(PageRouteNames.VehicleDocumentTypeMaster, FormFactor, JSRuntime, NavigationManager); break;
			case "DownloadSelectedDocument": await DownloadSelectedDocument(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<VehicleDocumentRenewalOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "DownloadSelectedDocument": await DownloadSelectedDocument(); break;
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
