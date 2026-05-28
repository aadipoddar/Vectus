using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Vehicle.Data;
using VectusLibrary.Fleet.Vehicle.Exports;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Vehicle;

public partial class VehiclePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private VehicleModel _vehicle = new() { PurchaseDate = DateTime.Today };
	private VehicleTypeModel _selectedVehicleType;
	private CompanyModel _selectedCompany;
	private SDRModel? _selectedSDR;

	private List<VehicleModel> _vehicles = [];
	private List<VehicleTypeModel> _vehicleTypes = [];
	private List<CompanyModel> _companies = [];
	private List<SDRModel> _sdrs = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<VehicleModel> _sfGrid;
	private CustomTextField _sfFirstFocus;
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
		_vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);
		_vehicleTypes = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType);
		_companies = await CommonData.LoadTableData<CompanyModel>(AccountNames.Company);
		_sdrs = await CommonData.LoadTableData<SDRModel>(FleetNames.SDR);

		_vehicleTypes = [.. _vehicleTypes.OrderBy(vt => vt.Name)];
		_companies = [.. _companies.OrderBy(c => c.Name)];
		_sdrs = [.. _sdrs.OrderBy(o => o.Name)];

		_selectedVehicleType = _vehicleTypes.FirstOrDefault(vt => vt.Id == _vehicle.VehicleTypeId);
		_selectedCompany = _companies.FirstOrDefault(c => c.Id == _vehicle.CompanyId);
		_selectedSDR = null;

		if (!_showDeleted)
			_vehicles = [.. _vehicles.Where(v => v.Status)];

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

			_vehicle.VehicleTypeId = _selectedVehicleType?.Id ?? 0;
			_vehicle.CompanyId = _selectedCompany?.Id ?? 0;
			_vehicle.SDRId = _selectedSDR?.Id;

			await VehicleData.SaveTransaction(_vehicle, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
	private async Task DeleteTransaction(int id)
	{
		try
		{
			_isProcessing = true;

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var vehicle = await CommonData.LoadTableDataById<VehicleModel>(FleetNames.Vehicle, id)
				?? throw new Exception("Transaction not found.");

			await VehicleData.DeleteTransaction(vehicle, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
		}
	}

	private async Task RecoverTransaction(int id)
	{
		try
		{
			_isProcessing = true;

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var vehicle = await CommonData.LoadTableDataById<VehicleModel>(FleetNames.Vehicle, id)
				?? throw new Exception("Transaction not found.");

			await VehicleData.RecoverTransaction(vehicle, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Recovered", "Transaction has been recovered successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Recovering", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
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

			var (stream, fileName) = await VehicleExport.ExportMaster(_vehicles, ReportExportType.Excel);
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

			var (stream, fileName) = await VehicleExport.ExportMaster(_vehicles, ReportExportType.PDF);
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
			case "ToggleDeleted": await ToggleDeleted(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<VehicleModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_vehicle = await CommonData.LoadTableDataById<VehicleModel>(FleetNames.Vehicle, selectedRecords[0].Id);
		if (_vehicle is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedVehicleType = _vehicleTypes.FirstOrDefault(vt => vt.Id == _vehicle.VehicleTypeId);
		_selectedCompany = _companies.FirstOrDefault(c => c.Id == _vehicle.CompanyId);
		_selectedSDR = _vehicle.SDRId.HasValue ? _sdrs.FirstOrDefault(o => o.Id == _vehicle.SDRId) : null;
		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		if (record.Status)
			await ShowConfirmation("Delete", $"Are you sure you want to delete {record.Code}", () => DeleteTransaction(record.Id));
		else
			await ShowConfirmation("Recover", $"Are you sure you want to recover {record.Code}", () => RecoverTransaction(record.Id));
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

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
