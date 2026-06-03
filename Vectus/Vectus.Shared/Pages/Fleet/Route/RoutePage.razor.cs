using Microsoft.JSInterop;

using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.APIService;
using VectusLibrary.Fleet.Route.Data;
using VectusLibrary.Fleet.Route.Exports;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Route;

public partial class RoutePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private RouteModel _route = new();
	private LocationModel _selectedFromLocation;
	private LocationModel _selectedToLocation;

	private List<RouteModel> _routes = [];
	private List<LocationModel> _locations = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" },
		new() { Text = "Open in Maps (Ctrl + M)", Id = "OpenInMaps", IconCss = "e-icons e-location", Target = ".e-content" }
	];

	private SfGrid<RouteModel> _sfGrid;
	private CustomAutoComplete<LocationModel> _sfFirstFocus;
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
		_routes = await CommonData.LoadTableData<RouteModel>(FleetNames.Route);
		_locations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location);
		_selectedFromLocation = _locations.FirstOrDefault(rl => rl.Id == _route.FromLocationId);
		_selectedToLocation = _locations.FirstOrDefault(rl => rl.Id == _route.ToLocationId);

		if (!_showDeleted)
			_routes = [.. _routes.Where(vr => vr.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region API
	private async Task GetRouteDetails()
	{
		if (_isProcessing || _isLoading)
			return;

		if (_selectedFromLocation is null || _selectedToLocation is null)
		{
			await _toastNotification.ShowAsync("Select Locations", "Please select both From and To locations first.", ToastType.Warning);
			return;
		}

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Fetching", "Getting route details from the map...", ToastType.Info);

			RouteModel estimate;
			try
			{
				estimate = await GoogleMapsApiService.GetRouteEstimate(
					_selectedFromLocation.Latitude, _selectedFromLocation.Longitude,
					_selectedToLocation.Latitude, _selectedToLocation.Longitude);
			}
			catch
			{
				estimate = await OpenRouteApiService.GetRouteEstimate(
					_selectedFromLocation.Latitude, _selectedFromLocation.Longitude,
					_selectedToLocation.Latitude, _selectedToLocation.Longitude);
			}

			_route.EstimatedDistance = estimate.EstimatedDistance;
			_route.EstimatedHours = estimate.EstimatedHours;
			_route.EstimatedFuelConsumption = estimate.EstimatedFuelConsumption;
			_route.EstimatedCost = estimate.EstimatedCost;

			await _toastNotification.ShowAsync("Fetched", "Route details have been fetched successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Fetching", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
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

			_route.FromLocationId = _selectedFromLocation?.Id ?? 0;
			_route.ToLocationId = _selectedToLocation?.Id ?? 0;

			await RouteData.SaveTransaction(_route, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

		_route = await CommonData.LoadTableDataById<RouteModel>(FleetNames.Route, selectedRecords[0].Id);
		if (_route is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedFromLocation = _locations.FirstOrDefault(rl => rl.Id == _route.FromLocationId);
		_selectedToLocation = _locations.FirstOrDefault(rl => rl.Id == _route.ToLocationId);
		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverTransaction(int id, bool isRecover)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var route = await CommonData.LoadTableDataById<RouteModel>(FleetNames.Route, id)
				?? throw new Exception("Transaction not found.");

			if (isRecover) await RouteData.RecoverTransaction(route, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			else await RouteData.DeleteTransaction(route, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			var from = _locations.FirstOrDefault(l => l.Id == route.FromLocationId)?.Name;
			var to = _locations.FirstOrDefault(l => l.Id == route.ToLocationId)?.Name;
			var identifier = $"{from} to {to}";

			await _toastNotification.ShowAsync("Success", $"Transaction {identifier} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while {(isRecover ? "recovering" : "deleting")} transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];
		var from = _locations.FirstOrDefault(l => l.Id == record.FromLocationId)?.Name;
		var to = _locations.FirstOrDefault(l => l.Id == record.ToLocationId)?.Name;
		var identifier = $"{from} to {to}";

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} transaction {identifier}",
			() => DeleteRecoverTransaction(record.Id, !record.Status));
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

			var (stream, fileName) = await RouteExport.ExportMaster(_routes, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<RouteModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
			case "OpenInMaps": await OpenInMaps(); break;
		}
	}

	private async Task OpenInMaps()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var route = selectedRecords[0];
		var from = _locations.FirstOrDefault(l => l.Id == route.FromLocationId);
		var to = _locations.FirstOrDefault(l => l.Id == route.ToLocationId);
		if (from is null || to is null)
			return;

		await JSRuntime.InvokeVoidAsync("open",
			$"https://www.google.com/maps/dir/?api=1&origin={from.Latitude},{from.Longitude}&destination={to.Latitude},{to.Longitude}",
			"_blank");
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
