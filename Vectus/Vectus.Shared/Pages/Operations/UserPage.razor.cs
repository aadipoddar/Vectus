using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Services;

using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Exports;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Operations;

public partial class UserPage
{
	private UserModel _loggedInUser;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private UserModel _transactionUser = new();

	private List<UserModel> _users = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<UserModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteTransactionId = 0;
	private string _deleteTransactionName = string.Empty;

	private int _recoverTransactionId = 0;
	private string _recoverTransactionName = string.Empty;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_loggedInUser = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Admin]);
		await LoadData();
	}

	private async Task LoadData()
	{
		_users = await CommonData.LoadTableData<UserModel>(OperationNames.User);

		if (!_showDeleted)
			_users = [.. _users.Where(u => u.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();
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

			if (!_loggedInUser.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			await UserData.SaveTransaction(_transactionUser, _loggedInUser.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var (stream, fileName) = await UserExport.ExportMaster(_users, ReportExportType.Excel);
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

			var (stream, fileName) = await UserExport.ExportMaster(_users, ReportExportType.PDF);
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
	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			if (!_loggedInUser.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, _deleteTransactionId)
				?? throw new Exception("Transaction not found.");

			await UserData.DeleteTransaction(user, _loggedInUser.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			await _recoverConfirmationDialog.HideAsync();

			if (!_loggedInUser.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, _recoverTransactionId)
				?? throw new Exception("Transaction not found.");

			await UserData.RecoverTransaction(user, _loggedInUser.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
			_recoverTransactionId = 0;
			_recoverTransactionName = string.Empty;
		}
	}
	#endregion

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction":
				ResetPage();
				break;
			case "SaveTransaction":
				await SaveTransaction();
				break;
			case "ToggleDeleted":
				await ToggleDeleted();
				break;
			case "ExportExcel":
				await ExportExcel();
				break;
			case "ExportPdf":
				await ExportPdf();
				break;
			case "EditSelectedItem":
				await EditSelectedItem();
				break;
			case "DeleteRecoverSelectedItem":
				await DeleteRecoverSelectedItem();
				break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<UserModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem":
				await EditSelectedItem();
				break;
			case "DeleteRecoverSelectedItem":
				await DeleteRecoverSelectedItem();
				break;
		}
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_transactionUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, selectedRecords[0].Id);
		if (_transactionUser is null)
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);

		StateHasChanged();
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			if (selectedRecords[0].Status)
				await ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
			else
				await ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
		}
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

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverTransactionId = id;
		_recoverTransactionName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverTransactionId = 0;
		_recoverTransactionName = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.User, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.OperationsDashboard);
	#endregion
}
