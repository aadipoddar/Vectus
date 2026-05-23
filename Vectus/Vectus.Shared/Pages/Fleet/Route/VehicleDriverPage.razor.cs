using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;
using Vectus.Shared.Services;

using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Data;
using VectusLibrary.Fleet.Route.Exports;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Route;

public partial class VehicleDriverPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private VehicleDriverModel _vehicleDriver = new();
	private VehicleModel _selectedVehicle;
	private DriverModel _selectedDriver;
	private DateTime _endDateTime;

	private List<VehicleDriverModel> _vehicleDrivers = [];
	private List<VehicleModel> _vehicles = [];
	private List<DriverModel> _drivers = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<VehicleDriverModel> _sfGrid;
	private CustomAutoComplete<VehicleModel> _sfFirstFocus;
	private ToastNotification _toastNotification;
	private DeleteConfirmationDialog _deleteConfirmationDialog;

	private int _deleteTransactionId = 0;
	private string _deleteTransactionName = string.Empty;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
			await LoadData();
		}
		catch { NavigateBack(); }
	}

	private async Task LoadData()
	{
		_vehicleDrivers = await CommonData.LoadTableData<VehicleDriverModel>(FleetNames.VehicleDriver);
		_vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);
		_drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);

		_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _vehicleDriver.VehicleId);
		_selectedDriver = _drivers.FirstOrDefault(d => d.Id == _vehicleDriver.DriverId);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region Saving
	private async Task SaveTransaction()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			_vehicleDriver.VehicleId = _selectedVehicle?.Id ?? 0;
			_vehicleDriver.DriverId = _selectedDriver?.Id ?? 0;
			_vehicleDriver.EndDateTime = _endDateTime == default ? null : _endDateTime;

			await VehicleDriverData.SaveTransaction(_vehicleDriver, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Saved", "Transaction has been saved successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Actions
	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var vehicleDriver = await CommonData.LoadTableDataById<VehicleDriverModel>(FleetNames.VehicleDriver, _deleteTransactionId)
				?? throw new Exception("Transaction not found.");

			await VehicleDriverData.DeleteTransaction(vehicleDriver, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Deleted", "Transaction has been deleted successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Deleting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteTransactionId = 0;
			_deleteTransactionName = string.Empty;
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

			var (stream, fileName) = await VehicleDriverExport.ExportMaster(_vehicleDrivers, ReportExportType.Excel);
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

			var (stream, fileName) = await VehicleDriverExport.ExportMaster(_vehicleDrivers, ReportExportType.PDF);
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

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": ResetPage(); break;
			case "SaveTransaction": await SaveTransaction(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteSelectedItem": await DeleteSelectedItem(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<VehicleDriverModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteSelectedItem": await DeleteSelectedItem(); break;
		}
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_vehicleDriver = await CommonData.LoadTableDataById<VehicleDriverModel>(FleetNames.VehicleDriver, selectedRecords[0].Id);
		if (_vehicleDriver is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _vehicleDriver.VehicleId);
		_selectedDriver = _drivers.FirstOrDefault(d => d.Id == _vehicleDriver.DriverId);
		_endDateTime = _vehicleDriver.EndDateTime ?? default;
		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var vehicleDriver = selectedRecords[0];
		var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleDriver.VehicleId)?.Code ?? vehicleDriver.VehicleId.ToString();
		var driver = _drivers.FirstOrDefault(d => d.Id == vehicleDriver.DriverId)?.Name ?? vehicleDriver.DriverId.ToString();
		await ShowDeleteConfirmation(vehicleDriver.Id, $"{vehicle} - {driver}");
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteTransactionId = id;
		_deleteTransactionName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteTransactionId = 0;
		_deleteTransactionName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private void ResetPage() => PageRefresh.Request();
	private void NavigateBack() => NavigationManager.NavigateTo(PageRouteNames.FleetMastersDashboard);
	#endregion
}
