using Microsoft.AspNetCore.Components;

using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;
using Vectus.Shared.Services;

using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Data;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Fleet.TripRequest;

public partial class TripRequestPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany;
	private RouteModel _selectedRoute;
	private VehicleModel _selectedVehicle;
	private TripRequestModel _tripRequest = new();

	private List<CompanyModel> _companies = [];
	private List<RouteModel> _routes = [];
	private List<VehicleModel> _vehicles = [];

	private CustomAutoComplete<CompanyModel> _sfFirstFocus;
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
		catch { NavigateBack(); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await ResolveTransaction();
		await LoadSelections();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_routes = await CommonData.LoadTableDataByStatus<RouteModel>(FleetNames.Route);
		_vehicles = await CommonData.LoadTableDataByStatus<VehicleModel>(FleetNames.Vehicle);

		_companies = [.. _companies.OrderBy(c => c.Name)];
		_routes = [.. _routes.OrderBy(r => r.Code)];
		_vehicles = [.. _vehicles.OrderBy(v => v.Code)];
	}

	private async Task ResolveTransaction()
	{
		try
		{
			if (await LoadExistingTransaction())
				return;

			await CreateNewTransaction();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Transaction Data", ex.Message, ToastType.Error);
			ResetPage();
		}
	}

	private async Task<bool> LoadExistingTransaction()
	{
		if (!Id.HasValue)
			return false;

		_tripRequest = await CommonData.LoadTableDataById<TripRequestModel>(FleetNames.TripRequest, Id.Value);
		if (_tripRequest is null || _tripRequest.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested trip request could not be found.", ToastType.Error);
			ResetPage();
		}

		return true;
	}

	private async Task CreateNewTransaction()
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		_tripRequest = new()
		{
			Id = 0,
			TransactionDateTime = currentDateTime,
			Status = true
		};

		var lastTransaction = await CommonData.LoadLastTableData<TripRequestModel>(FleetNames.TripRequest);
		if (lastTransaction is not null)
			_tripRequest.TransactionDateTime = lastTransaction.TransactionDateTime;
	}

	private async Task LoadSelections()
	{
		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);

		_selectedCompany = _tripRequest.CompanyId > 0
			? _companies.FirstOrDefault(c => c.Id == _tripRequest.CompanyId)
			: _companies.FirstOrDefault(c => c.Id == int.Parse(mainCompanyId.Value));

		_selectedRoute = _tripRequest.RouteId > 0
			? _routes.FirstOrDefault(r => r.Id == _tripRequest.RouteId)
			: _routes.FirstOrDefault();

		_selectedVehicle = _tripRequest.VehicleId > 0
			? _vehicles.FirstOrDefault(v => v.Id == _tripRequest.VehicleId)
			: null;

		_tripRequest.CompanyId = _selectedCompany.Id;
		_tripRequest.RouteId = _selectedRoute.Id;
	}
	#endregion

	#region Change Events
	private Task OnCompanyChanged()
	{
		if (_selectedCompany is null || _selectedCompany.Id <= 0)
			return Task.CompletedTask;

		_tripRequest.CompanyId = _selectedCompany.Id;
		return Task.CompletedTask;
	}

	private Task OnRouteChanged()
	{
		if (_selectedRoute is null || _selectedRoute.Id <= 0)
			return Task.CompletedTask;

		_tripRequest.RouteId = _selectedRoute.Id;
		return Task.CompletedTask;
	}

	private Task OnVehicleChanged()
	{
		if (_selectedVehicle is null || _selectedVehicle.Id <= 0)
			return Task.CompletedTask;

		_tripRequest.VehicleId = _selectedVehicle.Id;
		return Task.CompletedTask;
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
			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_tripRequest.Status = true;
			_tripRequest.TransactionDateTime = DateOnly.FromDateTime(_tripRequest.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_tripRequest.CreatedBy = _user.Id;
			_tripRequest.LastModifiedBy = _user.Id;
			_tripRequest.CreatedAt = currentDateTime;
			_tripRequest.LastModifiedAt = currentDateTime;
			_tripRequest.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_tripRequest.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await TripRequestData.SaveTransaction(_tripRequest);

			await _toastNotification.ShowAsync("Success", $"Trip Request transaction '{_tripRequest.TransactionNo}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.TripRequest, true);
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

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": ResetPage(); break;
			case "SaveTransaction": await SaveTransaction(); break;
		}
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.TripRequest, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.FleetTransactionsDashboard, true);
	#endregion
}
