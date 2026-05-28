using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;

using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Garage.Models;
using VectusLibrary.Fleet.Repair.Data;
using VectusLibrary.Fleet.Repair.Exports;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.Vehicle.Data;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Utils.ExportUtils;

namespace Vectus.Shared.Pages.Fleet.Repair;

public partial class RepairPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private VehicleLocationModel _selectedVehicle = null;
	private GarageModel _selectedGarage = null;
	private FinancialYearModel _selectedFinancialYear = new();
	private RepairJobCartModel _selectedCart = new();
	private RepairModel _repair = new();
	private DateTime _garageOutDate;

	private List<CompanyModel> _companies = [];
	private List<VehicleLocationModel> _vehicles = [];
	private List<GarageModel> _garages = [];
	private List<RepairJobCartModel> _cart = [];

	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _sfFirstFocus;
	private CustomTextField _sfJobFocus;
	private SfGrid<RepairJobCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
			await InitializePage();
		}
		catch { await ResetPage(); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await ResolveTransaction();
		await LoadSelections();
		await LoadCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_garages = await CommonData.LoadTableDataByStatus<GarageModel>(FleetNames.Garage);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_garages = [.. _garages.OrderBy(s => s.Name)];

		await LoadVehicles();
	}

	private async Task LoadVehicles()
	{
		var startDateTime = _repair.GarageInDateTime == default ? (DateTime?)null : _repair.GarageInDateTime;
		var endDateTime = _garageOutDate == default ? (DateTime?)null : _garageOutDate;

		_vehicles = await VehicleData.LoadVehicleLocations(null, startDateTime, endDateTime);
		_vehicles = [.. _vehicles.Where(s => !s.InGarage)];

		if (_selectedVehicle is not null)
			_selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == _selectedVehicle.Id);
	}

	private async Task ResolveTransaction()
	{
		try
		{
			if (await LoadExistingTransaction())
				return;

			if (await TryRestoreFromLocalStorage())
				return;

			await CreateNewTransaction();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Transaction Data", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingTransaction()
	{
		if (!Id.HasValue)
			return false;

		_repair = await CommonData.LoadTableDataById<RepairModel>(FleetNames.Repair, Id.Value);
		if (_repair is null || _repair.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.RepairDataFileName))
			return false;

		try
		{
			_repair = System.Text.Json.JsonSerializer.Deserialize<RepairModel>(await DataStorageService.LocalGetAsync(StorageFileNames.RepairDataFileName));
			if (_repair is null)
				return false;

			return true;
		}
		catch
		{
			await DeleteLocalFiles();
			return false;
		}
	}

	private async Task CreateNewTransaction()
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(currentDateTime);

		_repair = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _selectedCompany.Id,
			TransactionDateTime = currentDateTime,
			FinancialYearId = financialYear is null ? 0 : financialYear.Id,
			GarageId = 0,
			VehicleId = 0,
			CurrentKM = null,
			GarageInDateTime = currentDateTime,
			GarageOutDateTime = null,
			TotalItems = 0,
			TotalQuantity = 0,
			TotalAmount = 0,
			Remarks = string.Empty,
			CreatedBy = _user.Id,
			CreatedAt = DateTime.Now,
			CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
			Status = true,
			LastModifiedAt = null,
			LastModifiedBy = null,
			LastModifiedFromPlatform = null
		};

		await DeleteLocalFiles();
	}

	private async Task LoadSelections()
	{
		if (_repair.CompanyId > 0)
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _repair.CompanyId) ?? _companies.FirstOrDefault() ?? new();
		else
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? _companies.FirstOrDefault() ?? new();
		}
		_repair.CompanyId = _selectedCompany.Id;

		if (_repair.VehicleId > 0)
			_selectedVehicle = _vehicles.FirstOrDefault(s => s.Id == _repair.VehicleId);
		else
			_selectedVehicle = null;

		if (_repair.GarageId > 0)
			_selectedGarage = _garages.FirstOrDefault(s => s.Id == _repair.GarageId);
		else
			_selectedGarage = null;

		_garageOutDate = _repair.GarageOutDateTime ?? default;

		if (_repair.FinancialYearId > 0)
			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _repair.FinancialYearId);

		if (_selectedFinancialYear is null || _selectedFinancialYear.Id <= 0)
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_repair.TransactionDateTime);

		if (_selectedFinancialYear is not null)
			_repair.FinancialYearId = _selectedFinancialYear.Id;

		if (_repair.Id == 0)
		{
			var lastTransaction = await CommonData.LoadLastTableData<RepairModel>(FleetNames.Repair);
			if (lastTransaction is not null)
				_repair.TransactionDateTime = lastTransaction.TransactionDateTime;
		}
	}

	private async Task LoadCart()
	{
		try
		{
			_cart.Clear();

			if (_repair.Id > 0)
			{
				var existingJobs = await CommonData.LoadTableDataByMasterId<RepairJobModel>(FleetNames.RepairJob, _repair.Id);

				foreach (var item in existingJobs)
					_cart.Add(new()
					{
						Job = item.Job,
						Quantity = item.Quantity,
						Rate = item.Rate,
						Total = item.Total,
						Remarks = item.Remarks
					});
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.RepairJobCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<RepairJobCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.RepairJobCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
			await ResetPage();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnCompanyChanged(CompanyModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedCompany = value;
		_repair.CompanyId = value.Id;

		await SaveTransactionFile();
	}

	private async Task OnVehicleChanged(VehicleLocationModel value)
	{
		if (value is null || value.Id == 0)
		{
			_selectedVehicle = null;
			_repair.VehicleId = 0;
			return;
		}

		_selectedVehicle = value;
		_repair.VehicleId = value.Id;

		if (value.OdometerKM > 0)
			_repair.CurrentKM = value.OdometerKM;

		await SaveTransactionFile();
	}

	private async Task OnGarageChanged(GarageModel value)
	{
		if (value is null || value.Id == 0)
		{
			_selectedGarage = null;
			_repair.GarageId = 0;
			return;
		}

		_selectedGarage = value;
		_repair.GarageId = value.Id;

		await SaveTransactionFile();
	}

	private async Task OnGarageInDateChanged(DateTime value)
	{
		_repair.GarageInDateTime = value;
		await LoadVehicles();
		await SaveTransactionFile();
	}

	private async Task OnGarageOutDateChanged(DateTime value)
	{
		_garageOutDate = value;
		await LoadVehicles();
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private void OnCartQuantityChanged(decimal value)
	{
		_selectedCart.Quantity = value;
		_selectedCart.Total = decimal.Round(_selectedCart.Quantity * _selectedCart.Rate, 2);
	}

	private void OnCartRateChanged(decimal value)
	{
		_selectedCart.Rate = value;
		_selectedCart.Total = decimal.Round(_selectedCart.Quantity * _selectedCart.Rate, 2);
	}

	private async Task AddJobToCart()
	{
		if (string.IsNullOrWhiteSpace(_selectedCart.Job) ||
			_selectedCart.Quantity <= 0 ||
			_selectedCart.Rate < 0 ||
			_selectedCart.Total < 0)
		{
			await _toastNotification.ShowAsync("Invalid Job Details", "Please ensure the job description, quantity and rate are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		_cart.Add(new()
		{
			Job = _selectedCart.Job.Trim(),
			Quantity = _selectedCart.Quantity,
			Rate = _selectedCart.Rate,
			Total = decimal.Round(_selectedCart.Quantity * _selectedCart.Rate, 2),
			Remarks = _selectedCart.Remarks
		});

		_selectedCart = new();

		if (_sfJobFocus is not null)
			await _sfJobFocus.FocusAsync();

		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem(RepairJobCartModel cartItem = null)
	{
		if (cartItem is null)
		{
			if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
				return;

			await EditSelectedCartItem(_sfCartGrid.SelectedRecords.First());
			return;
		}

		_selectedCart = new()
		{
			Job = cartItem.Job,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			Total = cartItem.Total,
			Remarks = cartItem.Remarks
		};

		if (_sfJobFocus is not null)
			await _sfJobFocus.FocusAsync();

		await RemoveSelectedCartItem(cartItem);
	}

	private async Task RemoveSelectedCartItem(RepairJobCartModel cartItem = null)
	{
		if (cartItem is null)
		{
			if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
				return;

			cartItem = _sfCartGrid.SelectedRecords.First();
		}

		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart.ToList())
		{
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
				continue;
			}

			if (item.Rate < 0 || item.Total < 0)
			{
				_cart.Remove(item);
				continue;
			}

			item.Total = decimal.Round(item.Quantity * item.Rate, 2);
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}

		_repair.TotalItems = _cart.Count;
		_repair.TotalQuantity = _cart.Sum(x => x.Quantity);
		_repair.TotalAmount = _cart.Sum(x => x.Total);

		_repair.CompanyId = _selectedCompany.Id;
		_repair.VehicleId = _selectedVehicle?.Id ?? 0;
		_repair.GarageId = _selectedGarage?.Id ?? 0;
		_repair.GarageOutDateTime = _garageOutDate == default ? null : _garageOutDate;

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_repair.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_repair.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
		#endregion

		if (Id is null)
			_repair.TransactionNo = await GenerateCodes.GenerateRepairTransactionNo(_repair);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var currentTime = new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);

		_repair.Status = true;
		_repair.TransactionDateTime = DateOnly.FromDateTime(_repair.TransactionDateTime).ToDateTime(currentTime);
		if (_repair.GarageOutDateTime.HasValue)
			_repair.GarageOutDateTime = DateOnly.FromDateTime(_repair.GarageOutDateTime.Value).ToDateTime(currentTime);

		_repair.CreatedAt = currentDateTime;
		_repair.LastModifiedAt = currentDateTime;
		_repair.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_repair.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_repair.CreatedBy = _user.Id;
		_repair.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			if (_cart.Count == 0 || _repair.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.RepairDataFileName, System.Text.Json.JsonSerializer.Serialize(_repair));
			await DataStorageService.LocalSaveAsync(StorageFileNames.RepairJobCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Saving Transaction Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SaveTransaction(bool savePDF = false, bool saveExcel = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			await SaveTransactionFile();
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			var jobs = RepairData.ConvertCartToJobs(_cart, _repair.Id);
			_repair.Id = await RepairData.SaveTransaction(_repair, jobs);

			if (savePDF)
			{
				var (pdfStream, pdfFileName) = await RepairInvoiceExport.ExportInvoice(_repair.Id, InvoiceExportType.PDF);
				await SaveAndViewService.SaveAndView(pdfFileName, pdfStream);
			}

			if (saveExcel)
			{
				var (excelStream, excelFileName) = await RepairInvoiceExport.ExportInvoice(_repair.Id, InvoiceExportType.Excel);
				await SaveAndViewService.SaveAndView(excelFileName, excelStream);
			}

			await ResetPage();
			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportPdfInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("Nothing to Export", "There is nothing to export.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await RepairInvoiceExport.ExportInvoice(Id.Value, InvoiceExportType.PDF);
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
		}
	}

	private async Task ExportExcelInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("Nothing to Export", "There is nothing to export.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await RepairInvoiceExport.ExportInvoice(Id.Value, InvoiceExportType.Excel);
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
		}
	}
	#endregion

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": await ResetPage(); break;
			case "SaveTransaction": await SaveTransaction(); break;
			case "SavePdfInvoice": await SaveTransaction(savePDF: true); break;
			case "SaveExcelInvoice": await SaveTransaction(saveExcel: true); break;
			case "ExportPdfInvoice": await ExportPdfInvoice(); break;
			case "ExportExcelInvoice": await ExportExcelInvoice(); break;
			case "RepairReport": await AuthenticationService.NavigateToRoute(PageRouteNames.RepairReport, FormFactor, JSRuntime, NavigationManager); break;
			case "RepairJobReport": await AuthenticationService.NavigateToRoute(PageRouteNames.RepairJobReport, FormFactor, JSRuntime, NavigationManager); break;
		}
	}

	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<RepairJobCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.RepairDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.RepairJobCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}
