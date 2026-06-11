using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Data;
using VectusLibrary.Payroll.Masters.Exports;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Payroll.Masters;

public partial class StatutoryRulePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private StatutoryRuleModel _rule = new() { RoundingMode = "None" };
	private LedgerModel _selectedLedger;
	private StateUTModel _selectedStateUT;

	private List<StatutoryRuleModel> _rules = [];
	private List<LedgerModel> _ledgers = [];
	private List<StateUTModel> _stateUTs = [];

	// The rate currently being composed + its (stable) working list of slab bands.
	private StatutoryRateCartModel _rateInput = new();
	private StatutorySlabCartModel _slabInput = new();
	private readonly List<StatutorySlabCartModel> _slabs = [];

	// Committed effective-dated rates for this rule.
	private List<StatutoryRateCartModel> _rateCart = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];
	private readonly List<ContextMenuItemModel> _rateGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditRate", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteRate", IconCss = "e-icons e-trash", Target = ".e-content" }
	];
	private readonly List<ContextMenuItemModel> _slabGridContextMenuItems =
	[
		new() { Text = "Delete (Del)", Id = "DeleteSlab", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<StatutoryRuleModel> _sfGrid;
	private SfGrid<StatutoryRateCartModel> _sfRateGrid;
	private SfGrid<StatutorySlabCartModel> _sfSlabGrid;
	private CustomTextField _firstFocus;
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
		_rules = await CommonData.LoadTableData<StatutoryRuleModel>(PayrollNames.StatutoryRule);
		_ledgers = [.. (await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger)).Where(l => l.Status).OrderBy(l => l.Name)];
		_stateUTs = [.. (await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT)).Where(s => s.Status).OrderBy(s => s.Name)];

		_selectedLedger = _ledgers.FirstOrDefault(l => l.Id == _rule.LedgerId);
		_selectedStateUT = _stateUTs.FirstOrDefault(s => s.Id == _rule.StateUTId);

		if (!_showDeleted)
			_rules = [.. _rules.Where(r => r.Status)];

		_rules = [.. _rules.OrderBy(r => r.Code)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}
	#endregion

	#region Rates & Slabs
	private async Task AddRateToCart()
	{
		if (_rateInput.EffectiveFrom == default)
		{
			await _toastNotification.ShowAsync("Date Required", "Please choose an effective-from date for the rate.", ToastType.Warning);
			return;
		}

		if (_rateCart.Any(r => r.EffectiveFrom.Date == _rateInput.EffectiveFrom.Date))
		{
			await _toastNotification.ShowAsync("Duplicate Date", "A rate effective from this date is already added.", ToastType.Warning);
			return;
		}

		if (_slabs.Any(s => _slabs.Count(o => o.FromAmount == s.FromAmount) > 1))
		{
			await _toastNotification.ShowAsync("Invalid Slabs", "Two slab bands share the same 'from' amount.", ToastType.Error);
			return;
		}

		_rateCart.Add(new()
		{
			Id = _rateInput.Id,
			EffectiveFrom = _rateInput.EffectiveFrom,
			EmployeeRate = _rateInput.EmployeeRate,
			EmployerRate = _rateInput.EmployerRate,
			WageCeiling = _rateInput.WageCeiling,
			MaxAmount = _rateInput.MaxAmount,
			MinAmount = _rateInput.MinAmount,
			MinBasePercentOfGross = _rateInput.MinBasePercentOfGross,
			StandardDeduction = _rateInput.StandardDeduction,
			RebateAmount = _rateInput.RebateAmount,
			RebateIncomeLimit = _rateInput.RebateIncomeLimit,
			CessPercent = _rateInput.CessPercent,
			Slabs = [.. _slabs]
		});

		_rateInput = new();
		_slabInput = new();
		_slabs.Clear();

		if (_sfRateGrid is not null)
			await _sfRateGrid.Refresh();
		if (_sfSlabGrid is not null)
			await _sfSlabGrid.Refresh();
	}

	private async Task EditSelectedRate()
	{
		if (_sfRateGrid?.SelectedRecords is null || _sfRateGrid.SelectedRecords.Count == 0)
			return;

		var rate = _sfRateGrid.SelectedRecords.First();

		_rateInput = new()
		{
			Id = rate.Id,
			EffectiveFrom = rate.EffectiveFrom,
			EmployeeRate = rate.EmployeeRate,
			EmployerRate = rate.EmployerRate,
			WageCeiling = rate.WageCeiling,
			MaxAmount = rate.MaxAmount,
			MinAmount = rate.MinAmount,
			MinBasePercentOfGross = rate.MinBasePercentOfGross,
			StandardDeduction = rate.StandardDeduction,
			RebateAmount = rate.RebateAmount,
			RebateIncomeLimit = rate.RebateIncomeLimit,
			CessPercent = rate.CessPercent
		};

		_slabs.Clear();
		_slabs.AddRange(rate.Slabs);

		_rateCart.Remove(rate);

		if (_sfRateGrid is not null)
			await _sfRateGrid.Refresh();
		if (_sfSlabGrid is not null)
			await _sfSlabGrid.Refresh();
	}

	private async Task RemoveSelectedRate()
	{
		if (_sfRateGrid?.SelectedRecords is null || _sfRateGrid.SelectedRecords.Count == 0)
			return;

		_rateCart.Remove(_sfRateGrid.SelectedRecords.First());

		if (_sfRateGrid is not null)
			await _sfRateGrid.Refresh();
	}

	private async Task AddSlabToCart()
	{
		if (_slabInput.ToAmount is not null && _slabInput.ToAmount <= _slabInput.FromAmount)
		{
			await _toastNotification.ShowAsync("Invalid Band", "The 'to' amount must be greater than the 'from' amount.", ToastType.Error);
			return;
		}

		if (_slabs.Any(s => s.FromAmount == _slabInput.FromAmount))
		{
			await _toastNotification.ShowAsync("Duplicate Band", "A slab band with this 'from' amount is already added.", ToastType.Warning);
			return;
		}

		_slabs.Add(new()
		{
			FromAmount = _slabInput.FromAmount,
			ToAmount = _slabInput.ToAmount,
			FixedAmount = _slabInput.FixedAmount,
			Rate = _slabInput.Rate
		});

		_slabInput = new();

		if (_sfSlabGrid is not null)
			await _sfSlabGrid.Refresh();
	}

	private async Task RemoveSelectedSlab()
	{
		if (_sfSlabGrid?.SelectedRecords is null || _sfSlabGrid.SelectedRecords.Count == 0)
			return;

		_slabs.Remove(_sfSlabGrid.SelectedRecords.First());

		if (_sfSlabGrid is not null)
			await _sfSlabGrid.Refresh();
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

			if (_rateCart.Count == 0)
				throw new Exception("Please add at least one effective-dated rate to the rule.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			_rule.LedgerId = _selectedLedger?.Id;
			_rule.StateUTId = _selectedStateUT?.Id;

			await StatutoryRuleData.SaveTransaction(_rule, _rateCart, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

		_rule = await CommonData.LoadTableDataById<StatutoryRuleModel>(PayrollNames.StatutoryRule, selectedRecords[0].Id);
		if (_rule is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedLedger = _ledgers.FirstOrDefault(l => l.Id == _rule.LedgerId);
		_selectedStateUT = _stateUTs.FirstOrDefault(s => s.Id == _rule.StateUTId);

		var rates = await CommonData.LoadTableDataByMasterId<StatutoryRateModel>(PayrollNames.StatutoryRate, _rule.Id);
		_rateCart = [];
		foreach (var rate in rates.OrderBy(r => r.EffectiveFrom))
		{
			var slabs = await CommonData.LoadTableDataByMasterId<StatutorySlabModel>(PayrollNames.StatutorySlab, rate.Id);
			_rateCart.Add(new()
			{
				Id = rate.Id,
				EffectiveFrom = rate.EffectiveFrom,
				EmployeeRate = rate.EmployeeRate,
				EmployerRate = rate.EmployerRate,
				WageCeiling = rate.WageCeiling,
				MaxAmount = rate.MaxAmount,
				MinAmount = rate.MinAmount,
				MinBasePercentOfGross = rate.MinBasePercentOfGross,
				StandardDeduction = rate.StandardDeduction,
				RebateAmount = rate.RebateAmount,
				RebateIncomeLimit = rate.RebateIncomeLimit,
				CessPercent = rate.CessPercent,
				Slabs = [.. slabs.OrderBy(s => s.FromAmount).Select(s => new StatutorySlabCartModel
				{
					FromAmount = s.FromAmount,
					ToAmount = s.ToAmount,
					FixedAmount = s.FixedAmount,
					Rate = s.Rate
				})]
			});
		}

		_rateInput = new();
		_slabInput = new();
		_slabs.Clear();

		if (_sfRateGrid is not null)
			await _sfRateGrid.Refresh();
		if (_sfSlabGrid is not null)
			await _sfSlabGrid.Refresh();

		StateHasChanged();
		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
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

			var rule = await CommonData.LoadTableDataById<StatutoryRuleModel>(PayrollNames.StatutoryRule, id)
				?? throw new Exception("Transaction not found.");

			if (isRecover) await StatutoryRuleData.RecoverTransaction(rule, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			else await StatutoryRuleData.DeleteTransaction(rule, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction {rule.Name} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
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
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} the rule {record.Name}?",
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

			var (stream, fileName) = await StatutoryRuleExport.ExportMaster(_rules, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<StatutoryRuleModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnRateGridContextMenuItemClicked(ContextMenuClickEventArgs<StatutoryRateCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditRate": await EditSelectedRate(); break;
			case "DeleteRate": await RemoveSelectedRate(); break;
		}
	}

	private async Task OnSlabGridContextMenuItemClicked(ContextMenuClickEventArgs<StatutorySlabCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "DeleteSlab": await RemoveSelectedSlab(); break;
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
