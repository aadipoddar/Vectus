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

public partial class LedgerPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private LedgerModel _ledger = new();
	private GroupModel _selectedGroup;
	private AccountTypeModel _selectedAccountType;
	private StateUTModel _selectedStateUT;

	private List<LedgerModel> _ledgers = [];
	private List<GroupModel> _groups = [];
	private List<AccountTypeModel> _accountTypes = [];
	private List<StateUTModel> _stateUTs = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<LedgerModel> _sfGrid;
	private CustomTextField _sfFirstFocus;
	private ToastNotification _toastNotification;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteTransactionId = 0;
	private string _deleteTransactionName = string.Empty;
	private int _recoverTransactionId = 0;
	private string _recoverTransactionName = string.Empty;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Accounts]);
			await LoadData();
		}
		catch { NavigateBack(); }
	}

	private async Task LoadData()
	{
		_ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
		_groups = await CommonData.LoadTableData<GroupModel>(AccountNames.Group);
		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(AccountNames.AccountType);
		_stateUTs = await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT);

		_groups = [.. _groups.OrderBy(g => g.Name)];
		_accountTypes = [.. _accountTypes.OrderBy(a => a.Name)];
		_stateUTs = [.. _stateUTs.OrderBy(s => s.Name)];

		SyncSelections();

		if (!_showDeleted)
			_ledgers = [.. _ledgers.Where(l => l.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region Changed Events
	private void SyncSelections()
	{
		_selectedGroup = _groups.FirstOrDefault(g => g.Id == _ledger.GroupId);
		_selectedAccountType = _accountTypes.FirstOrDefault(a => a.Id == _ledger.AccountTypeId);
		_selectedStateUT = _stateUTs.FirstOrDefault(s => s.Id == _ledger.StateUTId);
	}

	private void OnGroupChanged(GroupModel value)
	{
		_selectedGroup = value;
		_ledger.GroupId = value?.Id ?? 0;
	}

	private void OnAccountTypeChanged(AccountTypeModel value)
	{
		_selectedAccountType = value;
		_ledger.AccountTypeId = value?.Id ?? 0;
	}

	private void OnStateUTChanged(StateUTModel value)
	{
		_selectedStateUT = value;
		_ledger.StateUTId = value?.Id ?? 0;
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

			await LedgerData.SaveTransaction(_ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var ledger = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, _deleteTransactionId)
				?? throw new Exception("Transaction not found.");

			await LedgerData.DeleteTransaction(ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var ledger = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, _recoverTransactionId)
				?? throw new Exception("Transaction not found.");

			await LedgerData.RecoverTransaction(ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var (stream, fileName) = await LedgerExport.ExportMaster(_ledgers, ReportExportType.Excel);
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

			var (stream, fileName) = await LedgerExport.ExportMaster(_ledgers, ReportExportType.PDF);
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

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<LedgerModel> args)
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

		_ledger = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, selectedRecords[0].Id);
		if (_ledger is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		SyncSelections();
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

	private void ResetPage() => PageRefresh.Request();
	private void NavigateBack() => NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);
	#endregion
}
