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

public partial class FinancialYearPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private FinancialYearModel _financialYear = new();

	private List<FinancialYearModel> _financialYears = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<FinancialYearModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;
	private CustomDatePicker _sfFirstFocus;

	private DateTime StartDateTime => _financialYear.StartDate == default ? default : _financialYear.StartDate.ToDateTime(TimeOnly.MinValue);
	private DateTime EndDateTime => _financialYear.EndDate == default ? default : _financialYear.EndDate.ToDateTime(TimeOnly.MinValue);

	private void OnStartDateChanged(DateTime value) => _financialYear.StartDate = DateOnly.FromDateTime(value);
	private void OnEndDateChanged(DateTime value) => _financialYear.EndDate = DateOnly.FromDateTime(value);

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

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Accounts]);
			await LoadData();
		}
		catch
		{
			ResetPage();
		}
	}

	private async Task LoadData()
	{
		_financialYears = await CommonData.LoadTableData<FinancialYearModel>(AccountNames.FinancialYear);

		if (!_showDeleted)
			_financialYears = [.. _financialYears.Where(fy => fy.Status)];

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

			await FinancialYearData.SaveTransaction(_financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
	private void AutoGenerateNextYear()
	{
		if (_financialYears.Count == 0)
		{
			_financialYear.StartDate = new DateOnly(DateTime.Now.Year, 4, 1);
			_financialYear.EndDate = new DateOnly(DateTime.Now.Year + 1, 3, 31);
			_financialYear.YearNo = 1;
		}
		else
		{
			var latestYear = _financialYears
				.Where(fy => fy.Status)
				.OrderByDescending(fy => fy.EndDate)
				.FirstOrDefault();

			if (latestYear != null)
			{
				_financialYear.StartDate = latestYear.EndDate.AddDays(1);
				_financialYear.EndDate = latestYear.EndDate.AddYears(1);
				_financialYear.YearNo = latestYear.YearNo + 1;
			}
			else
			{
				_financialYear.StartDate = new DateOnly(DateTime.Now.Year, 4, 1);
				_financialYear.EndDate = new DateOnly(DateTime.Now.Year + 1, 3, 31);
				_financialYear.YearNo = 1;
			}
		}

		_financialYear.Id = 0;
		_financialYear.Remarks = $"Auto-generated based on {(_financialYears.Count == 0 ? "current date" : "latest financial year")}";
		_financialYear.Locked = false;
		_financialYear.Status = true;

		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _deleteTransactionId)
				?? throw new Exception("Transaction not found.");

			await FinancialYearData.DeleteTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _recoverTransactionId)
				?? throw new Exception("Transaction not found.");

			await FinancialYearData.RecoverTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var (stream, fileName) = await FinancialYearExport.ExportMaster(_financialYears, ReportExportType.Excel);
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

			var (stream, fileName) = await FinancialYearExport.ExportMaster(_financialYears, ReportExportType.PDF);
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
			case "AutoGenerateNextYear":
				AutoGenerateNextYear();
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

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialYearModel> args)
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

		_financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, selectedRecords[0].Id);
		if (_financialYear is null)
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);

		StateHasChanged();

		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			var fy = selectedRecords[0];
			if (fy.Status)
				await ShowDeleteConfirmation(fy.Id, $"{fy.StartDate:dd-MMM-yyyy} to {fy.EndDate:dd-MMM-yyyy}");
			else
				await ShowRecoverConfirmation(fy.Id, $"{fy.StartDate:dd-MMM-yyyy} to {fy.EndDate:dd-MMM-yyyy}");
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
		NavigationManager.NavigateTo(PageRouteNames.FinancialYearMaster, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);
	#endregion
}
