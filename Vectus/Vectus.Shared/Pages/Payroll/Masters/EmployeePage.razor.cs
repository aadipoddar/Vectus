using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Data;
using VectusLibrary.Payroll.Masters.Exports;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Payroll.Masters;

public partial class EmployeePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private EmployeeModel _employee = new() { PaymentMode = "Cash", DateOfJoining = DateTime.Today };

	private DepartmentModel _selectedDepartment;
	private DesignationModel _selectedDesignation;
	private EmployeeLocationModel _selectedLocation;

	private List<EmployeeModel> _employees = [];
	private List<DepartmentModel> _departments = [];
	private List<DesignationModel> _designations = [];
	private List<EmployeeLocationModel> _locations = [];
	private readonly List<string> _paymentModes = ["Cash", "Bank"];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<EmployeeModel> _sfGrid;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Payroll]);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_employees = await CommonData.LoadTableData<EmployeeModel>(PayrollNames.Employee);
		_departments = [.. (await CommonData.LoadTableData<DepartmentModel>(PayrollNames.Department)).Where(d => d.Status).OrderBy(d => d.Name)];
		_designations = [.. (await CommonData.LoadTableData<DesignationModel>(PayrollNames.Designation)).Where(d => d.Status).OrderBy(d => d.Name)];
		_locations = [.. (await CommonData.LoadTableData<EmployeeLocationModel>(PayrollNames.EmployeeLocation)).Where(l => l.Status).OrderBy(l => l.Name)];

		_selectedDepartment = _departments.FirstOrDefault(d => d.Id == _employee.DepartmentId);
		_selectedDesignation = _designations.FirstOrDefault(d => d.Id == _employee.DesignationId);
		_selectedLocation = _locations.FirstOrDefault(l => l.Id == _employee.EmployeeLocationId);

		if (!_showDeleted)
			_employees = [.. _employees.Where(e => e.Status)];

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

			_employee.DepartmentId = _selectedDepartment?.Id ?? 0;
			_employee.DesignationId = _selectedDesignation?.Id ?? 0;
			_employee.EmployeeLocationId = _selectedLocation?.Id ?? 0;

			await EmployeeData.SaveTransaction(_employee, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

		_employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, selectedRecords[0].Id);
		if (_employee is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedDepartment = _departments.FirstOrDefault(d => d.Id == _employee.DepartmentId);
		_selectedDesignation = _designations.FirstOrDefault(d => d.Id == _employee.DesignationId);
		_selectedLocation = _locations.FirstOrDefault(l => l.Id == _employee.EmployeeLocationId);

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

			var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, id)
				?? throw new Exception("Transaction not found.");

			if (isRecover) await EmployeeData.RecoverTransaction(employee, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			else await EmployeeData.DeleteTransaction(employee, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction {employee.Name} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
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

			var (stream, fileName) = await EmployeeExport.ExportMaster(_employees, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<EmployeeModel> args)
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

	private static string MaskAadhaar(string aadhaar) =>
		string.IsNullOrWhiteSpace(aadhaar) ? "—" : $"XXXX XXXX {aadhaar[^4..]}";

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
