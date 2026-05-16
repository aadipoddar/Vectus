using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;
using Vectus.Shared.Services;

using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Accounts.Masters.Exports;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Accounts.Masters;

public partial class GroupPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private GroupModel _group = new();
	private NatureModel _selectedNature;

	private List<GroupModel> _groups = [];
	private List<NatureModel> _natures = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<GroupModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;
	private CustomTextField _sfFirstFocus;

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

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Accounts]);
		await LoadData();
	}

	private async Task LoadData()
	{
		_natures = await CommonData.LoadTableDataByStatus<NatureModel>(AccountNames.Nature);
		_groups = await CommonData.LoadTableData<GroupModel>(AccountNames.Group);

		_natures = [.. _natures.OrderBy(n => n.Name)];
		_selectedNature = _natures.FirstOrDefault(n => n.Id == _group.NatureId);

		if (!_showDeleted)
			_groups = [.. _groups.Where(g => g.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region Change Events
	private void OnNatureChanged(NatureModel value)
	{
		_selectedNature = value;
		_group.NatureId = value?.Id ?? 0;
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

			await _toastNotification.ShowAsync("Processing", "Please wait...", ToastType.Info);

			await GroupData.SaveTransaction(_group, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var group = await CommonData.LoadTableDataById<GroupModel>(AccountNames.Group, _deleteTransactionId)
				?? throw new Exception("Transaction not found.");

			await GroupData.DeleteTransaction(group, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var group = await CommonData.LoadTableDataById<GroupModel>(AccountNames.Group, _recoverTransactionId)
				?? throw new Exception("Transaction not found.");

			await GroupData.RecoverTransaction(group, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var (stream, fileName) = await GroupExport.ExportMaster(_groups, ReportExportType.Excel);
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

			var (stream, fileName) = await GroupExport.ExportMaster(_groups, ReportExportType.PDF);
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

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<GroupModel> args)
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

		_group = await CommonData.LoadTableDataById<GroupModel>(AccountNames.Group, selectedRecords[0].Id);
		if (_group is null)
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);

		_selectedNature = _natures.FirstOrDefault(n => n.Id == _group.NatureId);
		StateHasChanged();

		await _sfFirstFocus.FocusAsync();
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
		NavigationManager.NavigateTo(PageRouteNames.GroupMaster, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);
	#endregion
}
