using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Fleet.Tyre.Data;
using VectusLibrary.Fleet.Tyre.Exports;
using VectusLibrary.Fleet.Tyre.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Tyre;

public partial class TyreCompanyPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private TyreCompanyModel _tyreCompany = new();

	private List<TyreCompanyModel> _tyreCompanies = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<TyreCompanyModel> _sfGrid;
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
		_tyreCompanies = await CommonData.LoadTableData<TyreCompanyModel>(FleetNames.TyreCompany);

		if (!_showDeleted)
			_tyreCompanies = [.. _tyreCompanies.Where(tc => tc.Status)];

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

			await TyreCompanyData.SaveTransaction(_tyreCompany, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

		_tyreCompany = await CommonData.LoadTableDataById<TyreCompanyModel>(FleetNames.TyreCompany, selectedRecords[0].Id);
		if (_tyreCompany is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

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

			var tyreCompany = await CommonData.LoadTableDataById<TyreCompanyModel>(FleetNames.TyreCompany, id)
				?? throw new Exception("Transaction not found.");

			if (isRecover) await TyreCompanyData.RecoverTransaction(tyreCompany, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			else await TyreCompanyData.DeleteTransaction(tyreCompany, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction {tyreCompany.Name} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
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

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} transaction {record.Name}",
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

			var (stream, fileName) = await TyreCompanyExport.ExportMaster(_tyreCompanies, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
			case "ExportExcel": await ExportMaster(true); break;
			case "ExportPdf": await ExportMaster(); break;
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<TyreCompanyModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
