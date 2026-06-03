using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Fleet.Tyre.Data;
using VectusLibrary.Fleet.Tyre.Exports;
using VectusLibrary.Fleet.Tyre.Models;
using VectusLibrary.Fleet.Vehicle.Data;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Tyre;

public partial class TyreMountingPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private TyreMountingModel _tyreMounting = new();
	private TyreCompanyModel _selectedTyreCompany;
	private VehicleLocationModel _selectedVehicle;

	private List<TyreMountingModel> _tyreMountings = [];
	private List<TyreCompanyModel> _tyreCompanies = [];
	private List<VehicleLocationModel> _vehicles = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<TyreMountingModel> _sfGrid;
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
		_tyreMountings = await CommonData.LoadTableData<TyreMountingModel>(FleetNames.TyreMounting);
		_tyreCompanies = await CommonData.LoadTableData<TyreCompanyModel>(FleetNames.TyreCompany);
		_vehicles = await VehicleData.LoadVehicleLocations();

		_selectedTyreCompany = _tyreCompanies.FirstOrDefault(tc => tc.Id == _tyreMounting.TyreCompanyId);
		_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _tyreMounting.VehicleId);

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

			_tyreMounting.TyreCompanyId = _selectedTyreCompany?.Id ?? 0;
			_tyreMounting.VehicleId = _selectedVehicle?.Id ?? 0;

			// Dismounting is optional and captured as a pair (date + KM). A missing dismounting date
			// means the tyre is still mounted, so both values are cleared.
			_tyreMounting.DismountingDateTime = _tyreMounting.DismountingDateTime == default ? null : _tyreMounting.DismountingDateTime;
			_tyreMounting.DismountingKM = _tyreMounting.DismountingDateTime == default ? null : _tyreMounting.DismountingKM;

			await TyreMountingData.SaveTransaction(_tyreMounting, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

		_tyreMounting = await CommonData.LoadTableDataById<TyreMountingModel>(FleetNames.TyreMounting, selectedRecords[0].Id);
		if (_tyreMounting is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedTyreCompany = _tyreCompanies.FirstOrDefault(tc => tc.Id == _tyreMounting.TyreCompanyId);
		_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _tyreMounting.VehicleId);
		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
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

			var tyreMounting = await CommonData.LoadTableDataById<TyreMountingModel>(FleetNames.TyreMounting, id)
				?? throw new Exception("Transaction not found.");

			await TyreMountingData.DeleteTransaction(tyreMounting, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction {tyreMounting.TyreNo} has been deleted successfully.", ToastType.Success);
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
		await ShowConfirmation("Delete", $"Are you sure you want to delete transaction {record.TyreNo}", () => DeleteTransaction(record.Id));
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

			var (stream, fileName) = await TyreMountingExport.ExportTransaction(_tyreMountings, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<TyreMountingModel> args)
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
