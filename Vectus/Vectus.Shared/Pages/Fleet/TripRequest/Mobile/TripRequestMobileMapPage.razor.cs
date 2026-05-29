using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Vectus.Shared.Components.Dialog;

using VectusLibrary.Accounts.Masters.Data;
using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Data;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Data;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Models;

using RouteData = VectusLibrary.Fleet.Route.Data.RouteData;

namespace Vectus.Shared.Pages.Fleet.TripRequest.Mobile;

public partial class TripRequestMobileMapPage : IAsyncDisposable
{
	[Parameter] public int RouteId { get; set; }
	[SupplyParameterFromQuery(Name = "company")] private int Company { get; set; }

	private ElementReference _sheetRef;
	private ElementReference _handleRef;
	private IJSObjectReference _sheetModule;
	private bool _sheetInit;

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing;

	private bool _confirmOpen;
	private bool _success;
	private string _txnNo = string.Empty;

	private RouteOverviewModel _route;
	private CompanyModel _company;
	private VehicleLocationModel _selectedVehicle;
	private string _mapHighlightCode;
	private string _vehicleQuery = string.Empty;
	private FinancialYearModel _selectedFinancialYear = new();
	private TripRequestModel _tripRequest = new();

	private List<VehicleLocationModel> _vehicles = [];

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
				await InitializePage();
			}
			catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
			return;
		}

		if (!_isLoading && _route is not null && !_sheetInit)
		{
			_sheetInit = true;
			_sheetModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Vectus.Shared/js/bottomSheet.js");
			await _sheetModule.InvokeVoidAsync("init", _sheetRef, _handleRef);
		}
	}

	private async Task InitializePage()
	{
		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		var routes = await RouteData.LoadRouteOverview();
		_route = routes.FirstOrDefault(r => r.Id == RouteId);
		if (_route is null)
		{
			NavigationManager.NavigateTo(PageRouteNames.TripRequestMobile);
			return;
		}

		var companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_company = companies.FirstOrDefault(c => c.Id == Company) ?? companies.FirstOrDefault();

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_tripRequest = new() { Id = 0, TransactionDateTime = currentDateTime, Status = true };

		var lastTransaction = await CommonData.LoadLastTableData<TripRequestModel>(FleetNames.TripRequest);
		if (lastTransaction is not null)
			_tripRequest.TransactionDateTime = lastTransaction.TransactionDateTime;

		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_tripRequest.TransactionDateTime);
		if (_selectedFinancialYear is not null)
			_tripRequest.FinancialYearId = _selectedFinancialYear.Id;

		_vehicles = await VehicleData.LoadVehicleLocations(_route, _tripRequest.TransactionDateTime);
		_vehicles = [.. _vehicles.Where(v => !v.InGarage)];
		_selectedVehicle = _vehicles.FirstOrDefault();
	}
	#endregion

	#region Change Events
	private List<VehicleLocationModel> FilteredOtherVehicles
	{
		get
		{
			var others = _vehicles.Where(v => v.Id != _selectedVehicle?.Id);

			if (!string.IsNullOrEmpty(_vehicleQuery?.Trim()))
				others = others.Where(v => $"{v.Code} {v.SDR}".Contains(_vehicleQuery?.Trim(), StringComparison.OrdinalIgnoreCase));

			return [.. others];
		}
	}

	private void SelectVehicle(VehicleLocationModel vehicle)
	{
		if (vehicle is null || vehicle.Id == 0)
			return;

		_selectedVehicle = vehicle;
		_mapHighlightCode = vehicle.Code;
	}

	private void ToggleSelectedCard()
	{
		if (_selectedVehicle is null)
			return;

		_mapHighlightCode = _mapHighlightCode == _selectedVehicle.Code ? null : _selectedVehicle.Code;
	}

	private async Task OnMapVehicleSelected(string code)
	{
		var vehicle = _vehicles.FirstOrDefault(v => string.Equals(v.Code, code, StringComparison.OrdinalIgnoreCase));
		SelectVehicle(vehicle);
		StateHasChanged();
	}
	#endregion

	#region Saving
	private void OpenConfirm()
	{
		if (_selectedVehicle is null)
			return;

		_confirmOpen = true;
	}

	private async Task ConfirmTransaction()
	{
		if (_isProcessing || _selectedVehicle is null)
			return;

		try
		{
			_isProcessing = true;
			_confirmOpen = false;
			StateHasChanged();

			_tripRequest.CompanyId = _company?.Id ?? 0;
			_tripRequest.VehicleId = _selectedVehicle.Id;
			_tripRequest.RouteId = _route.Id;

			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_tripRequest.Status = true;
			_tripRequest.TransactionDateTime = DateOnly.FromDateTime(_tripRequest.TransactionDateTime)
				.ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_tripRequest.CreatedBy = _user.Id;
			_tripRequest.LastModifiedBy = _user.Id;
			_tripRequest.CreatedAt = currentDateTime;
			_tripRequest.LastModifiedAt = currentDateTime;
			_tripRequest.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_tripRequest.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await TripRequestData.SaveTransaction(_tripRequest);

			_txnNo = _tripRequest.TransactionNo;
			_success = true;
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
	// Same status vocabulary RouteMap uses for its coloured markers.
	private static string Status(VehicleLocationModel v) =>
		v.Speed > 0 ? "Moving" : v.IgnitionOn ? "Idle" : "Stopped";

	private static string StatusColor(string status) => status switch
	{
		"Moving" => "#00a868",
		"Idle" => "#f5a623",
		_ => "#b5b5b5"
	};

	// Rough "minutes away" estimate from the haversine distance (mirrors the design mock).
	private static int MinAway(decimal? km) => Math.Max(2, (int)Math.Round((km ?? 2) * 2.4m));

	public async ValueTask DisposeAsync()
	{
		if (_sheetModule is not null)
		{
			try { await _sheetModule.DisposeAsync(); }
			catch { /* circuit/WebView already gone */ }
		}

		GC.SuppressFinalize(this);
	}
	#endregion
}
