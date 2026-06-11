using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

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
	private CustomAutoComplete<VehicleModel> _firstFocus;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
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

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
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
		await _firstFocus.FocusAsync();
	}

	private async Task DeleteTransaction(int id)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			var vehicleDriver = await CommonData.LoadTableDataById<VehicleDriverModel>(FleetNames.VehicleDriver, id)
				?? throw new Exception("Transaction not found.");

			await VehicleDriverData.DeleteTransaction(vehicleDriver, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleDriver.VehicleId)?.Code ?? vehicleDriver.VehicleId.ToString();
			var driver = _drivers.FirstOrDefault(d => d.Id == vehicleDriver.DriverId)?.Name ?? vehicleDriver.DriverId.ToString();
			var identifier = $"{vehicle} - {driver}";

			await _toastNotification.ShowAsync("Success", $"Transaction {identifier} has been deleted successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while deleting transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];
		var vehicle = _vehicles.FirstOrDefault(v => v.Id == record.VehicleId)?.Code ?? record.VehicleId.ToString();
		var driver = _drivers.FirstOrDefault(d => d.Id == record.DriverId)?.Name ?? record.DriverId.ToString();
		var identifier = $"{vehicle} - {driver}";

		await ShowConfirmation("Delete", $"Are you sure you want to delete {identifier}", () => DeleteTransaction(record.Id));
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

	#region Exporting
	private async Task ExportMaster(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await VehicleDriverExport.ExportMaster(_vehicleDrivers, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<VehicleDriverModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteSelectedItem": await DeleteSelectedItem(); break;
		}
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
