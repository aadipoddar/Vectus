using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Data;
using VectusLibrary.Payroll.Masters.Exports;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Payroll.Masters;

public partial class SalaryStructurePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private SalaryStructureModel _structure = new() { EffectiveFrom = DateTime.Today };
	private EmployeeModel _selectedEmployee;
	private SalaryComponentModel _selectedComponent;
	private SalaryStructureLineCartModel _selectedCart = new();

	private List<SalaryStructureModel> _structures = [];
	private List<EmployeeModel> _employees = [];
	private List<SalaryComponentModel> _components = [];
	private List<SalaryStructureLineCartModel> _cart = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<SalaryStructureModel> _sfGrid;
	private SfGrid<SalaryStructureLineCartModel> _sfCartGrid;
	private CustomAutoComplete<EmployeeModel> _sfFirstFocus;
	private CustomAutoComplete<SalaryComponentModel> _sfComponentAutoComplete;
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
		_structures = await CommonData.LoadTableData<SalaryStructureModel>(PayrollNames.SalaryStructure);
		_employees = [.. (await CommonData.LoadTableData<EmployeeModel>(PayrollNames.Employee)).Where(e => e.Status).OrderBy(e => e.Name)];
		_components = [.. (await CommonData.LoadTableData<SalaryComponentModel>(PayrollNames.SalaryComponent)).Where(c => c.Status).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Code)];

		_selectedEmployee = _employees.FirstOrDefault(e => e.Id == _structure.EmployeeId);

		if (!_showDeleted)
			_structures = [.. _structures.Where(s => s.Status)];

		_structures = [.. _structures.OrderByDescending(s => s.EffectiveFrom)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region Cart
	private void OnComponentChanged(SalaryComponentModel value)
	{
		if (value is null || value.Id == 0)
		{
			_selectedComponent = null;
			_selectedCart = new();
			return;
		}

		_selectedComponent = value;
		_selectedCart.SalaryComponentId = value.Id;
		_selectedCart.SalaryComponentCode = value.Code;
		_selectedCart.SalaryComponentName = value.Name;
		_selectedCart.ComponentType = value.ComponentType;
		_selectedCart.CalculationType = value.CalculationType;
	}

	private async Task AddItemToCart()
	{
		if (_selectedComponent is null || _selectedComponent.Id == 0)
		{
			await _toastNotification.ShowAsync("Select Component", "Please select a salary component first.", ToastType.Warning);
			return;
		}

		if (_cart.Any(c => c.SalaryComponentId == _selectedComponent.Id))
		{
			await _toastNotification.ShowAsync("Already Added", "This component is already in the structure.", ToastType.Warning);
			return;
		}

		if ((_selectedCart.Amount ?? 0) < 0 || (_selectedCart.OverridePercentage ?? 0) < 0)
		{
			await _toastNotification.ShowAsync("Invalid Details", "Amount and override percentage cannot be negative.", ToastType.Error);
			return;
		}

		if (_selectedComponent.CalculationType is "Fixed" && (_selectedCart.Amount ?? 0) <= 0)
		{
			await _toastNotification.ShowAsync("Amount Required", "A fixed component needs an amount greater than zero.", ToastType.Error);
			return;
		}

		_cart.Add(new()
		{
			SalaryComponentId = _selectedComponent.Id,
			SalaryComponentCode = _selectedComponent.Code,
			SalaryComponentName = _selectedComponent.Name,
			ComponentType = _selectedComponent.ComponentType,
			CalculationType = _selectedComponent.CalculationType,
			Amount = (_selectedCart.Amount ?? 0) == 0 ? null : _selectedCart.Amount,
			OverridePercentage = (_selectedCart.OverridePercentage ?? 0) == 0 ? null : _selectedCart.OverridePercentage
		});

		_selectedComponent = null;
		_selectedCart = new();

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();

		if (_sfComponentAutoComplete is not null)
			await _sfComponentAutoComplete.FocusAsync();
	}

	private async Task EditSelectedCartItem(SalaryStructureLineCartModel cartItem = null)
	{
		if (cartItem is null)
		{
			if (_sfCartGrid?.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
				return;

			await EditSelectedCartItem(_sfCartGrid.SelectedRecords.First());
			return;
		}

		_selectedComponent = _components.FirstOrDefault(c => c.Id == cartItem.SalaryComponentId);
		if (_selectedComponent is null)
			return;

		_selectedCart = new()
		{
			SalaryComponentId = cartItem.SalaryComponentId,
			SalaryComponentCode = cartItem.SalaryComponentCode,
			SalaryComponentName = cartItem.SalaryComponentName,
			ComponentType = cartItem.ComponentType,
			CalculationType = cartItem.CalculationType,
			Amount = cartItem.Amount,
			OverridePercentage = cartItem.OverridePercentage
		};

		await RemoveSelectedCartItem(cartItem);

		if (_sfComponentAutoComplete is not null)
			await _sfComponentAutoComplete.FocusAsync();
	}

	private async Task RemoveSelectedCartItem(SalaryStructureLineCartModel cartItem = null)
	{
		if (cartItem is null)
		{
			if (_sfCartGrid?.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
				return;

			cartItem = _sfCartGrid.SelectedRecords.First();
		}

		_cart.Remove(cartItem);

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();
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

			if (_selectedEmployee is null || _selectedEmployee.Id == 0)
				throw new Exception("Please select an employee for the salary structure.");

			if (_cart.Count == 0)
				throw new Exception("Please add at least one component to the salary structure.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			_structure.EmployeeId = _selectedEmployee.Id;
			var lines = SalaryStructureData.ConvertCartToLines(_cart, _structure.Id);

			await SalaryStructureData.SaveTransaction(_structure, lines, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

		_structure = await CommonData.LoadTableDataById<SalaryStructureModel>(PayrollNames.SalaryStructure, selectedRecords[0].Id);
		if (_structure is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedEmployee = _employees.FirstOrDefault(e => e.Id == _structure.EmployeeId);

		var lines = await CommonData.LoadTableDataByMasterId<SalaryStructureLineModel>(PayrollNames.SalaryStructureLine, _structure.Id);
		_cart = [.. lines.Select(line =>
		{
			var component = _components.FirstOrDefault(c => c.Id == line.SalaryComponentId);
			return new SalaryStructureLineCartModel
			{
				SalaryComponentId = line.SalaryComponentId,
				SalaryComponentCode = component?.Code ?? "N/A",
				SalaryComponentName = component?.Name ?? "N/A",
				ComponentType = component?.ComponentType,
				CalculationType = component?.CalculationType,
				Amount = line.Amount,
				OverridePercentage = line.OverridePercentage
			};
		})];

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();

		StateHasChanged();
		if (_sfFirstFocus is not null)
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

			var structure = await CommonData.LoadTableDataById<SalaryStructureModel>(PayrollNames.SalaryStructure, id)
				?? throw new Exception("Transaction not found.");

			if (isRecover) await SalaryStructureData.RecoverTransaction(structure, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			else await SalaryStructureData.DeleteTransaction(structure, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
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
		var employeeName = _employees.FirstOrDefault(e => e.Id == record.EmployeeId)?.Name ?? "this employee";

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} the structure for {employeeName} effective {record.EffectiveFrom:dd-MM-yyyy}?",
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

			var (stream, fileName) = await SalaryStructureExport.ExportMaster(_structures, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<SalaryStructureModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<SalaryStructureLineCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
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
