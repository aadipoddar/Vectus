using Vectus.Shared.Components.Dialog;
using Vectus.Shared.Components.Input;
using Vectus.Shared.Services;

using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Data;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Data;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Fleet.TripRequest;

public partial class TripRequestPage
{
	[Microsoft.AspNetCore.Components.Parameter] public int? Id { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany;
	private RouteOverviewModel _selectedRoute;
	private VehicleModel? _selectedVehicle = null;
	private TripRequestModel _tripRequest = new();

	private List<CompanyModel> _companies = [];
	private List<RouteOverviewModel> _routes = [];
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
		_routes = await RouteData.LoadRouteOverview();
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
		_tripRequest.VehicleId = _selectedVehicle?.Id ?? 0;
	}
	#endregion

	#region Change Events
	private async Task OnCompanyChanged(CompanyModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedCompany = value;
		_tripRequest.CompanyId = _selectedCompany.Id;
	}

	private async Task OnRouteChanged(RouteOverviewModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedRoute = value;
		_tripRequest.RouteId = _selectedRoute.Id;

		StateHasChanged();
	}

	private async Task OnVehicleChanged(VehicleModel value)
	{
		if (value is null || value.Id == 0)
		{
			_selectedVehicle = null;
			_tripRequest.VehicleId = 0;
			return;
		}

		_selectedVehicle = value;
		_tripRequest.VehicleId = _selectedVehicle.Id;
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

			_tripRequest.CompanyId = _selectedCompany.Id;
			_tripRequest.VehicleId = _selectedVehicle?.Id ?? 0;
			_tripRequest.RouteId = _selectedRoute.Id;
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
