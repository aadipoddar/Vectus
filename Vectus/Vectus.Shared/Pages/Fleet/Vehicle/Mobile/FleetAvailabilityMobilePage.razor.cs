using VectusLibrary.Fleet.Vehicle.Data;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Pages.Fleet.Vehicle.Mobile;

public partial class FleetAvailabilityMobilePage
{
	private UserModel _user;
	private SDRModel _mySDR;
	private bool _isLoading = true;
	private bool _isProcessing;

	private List<VehicleLocationModel> _fleet = [];
	private string _query = string.Empty;

	private bool _toastOn;
	private bool _toastOk;
	private string _toastCode = string.Empty;
	private string _toastText = string.Empty;
	private CancellationTokenSource _toastCts;

	private List<VehicleLocationModel> FilteredFleet
	{
		get
		{
			if (string.IsNullOrWhiteSpace(_query?.Trim()))
				return _fleet;

			return [.. _fleet.Where(v =>
				$"{v.Code} {v.ShortCode} {v.Address}".Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase))];
		}
	}

	private int Total => _fleet.Count;
	private int AvailableCount => _fleet.Count(v => v.AvailableStatus);
	private int Percent => Total == 0 ? 0 : (int)Math.Round((double)AvailableCount / Total * 100);
	private bool AllOn => Total > 0 && AvailableCount == Total;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, VibrationService, [UserRoles.Fleet]);
			await LoadData();

			_isLoading = false;
			StateHasChanged();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		var sdrs = await CommonData.LoadTableDataByStatus<SDRModel>(FleetNames.SDR);
		_mySDR = sdrs.FirstOrDefault(s => s.UserId == _user.Id);

		if (_mySDR is null)
		{
			_fleet = [];
			return;
		}

		var vehicles = await VehicleData.LoadVehicleLocations();
		_fleet = [.. vehicles.Where(v => v.SDRId == _mySDR.Id).OrderBy(v => v.Code)];
	}
	#endregion

	#region Actions
	private async Task ToggleOne(VehicleLocationModel vehicle)
	{
		if (_isProcessing || vehicle is null)
			return;

		var target = !vehicle.AvailableStatus;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await SaveAvailability(vehicle.Id, target);

			vehicle.AvailableStatus = target;
			await Flash(vehicle.Code, target ? "available" : "off duty", target);
		}
		catch (Exception ex)
		{
			await Flash(vehicle.Code, ex.Message, false);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SetAll()
	{
		if (_isProcessing || Total == 0)
			return;

		var target = !AllOn;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			foreach (var vehicle in _fleet.Where(v => v.AvailableStatus != target))
			{
				await SaveAvailability(vehicle.Id, target);
				vehicle.AvailableStatus = target;
			}

			await Flash(string.Empty, target ? "All trucks available" : "All trucks off duty", target);
		}
		catch (Exception ex)
		{
			await Flash(string.Empty, ex.Message, false);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SaveAvailability(int vehicleId, bool available)
	{
		var vehicle = await CommonData.LoadTableDataById<VehicleModel>(FleetNames.Vehicle, vehicleId)
			?? throw new Exception("Vehicle not found.");

		vehicle.AvailableStatus = available;
		await VehicleData.SaveTransaction(vehicle, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
	}
	#endregion

	#region Utilities
	private static string Status(VehicleLocationModel v) =>
		v.Speed > 0 ? "Moving" : v.IgnitionOn ? "Idle" : "Stopped";

	private static string StatusColor(string status) => status switch
	{
		"Moving" => "#00a868",
		"Idle" => "#f5a623",
		_ => "#b5b5b5"
	};

	private async Task Flash(string code, string text, bool ok)
	{
		_toastCts?.Cancel();
		_toastCts = new CancellationTokenSource();
		var token = _toastCts.Token;

		_toastCode = code;
		_toastText = text;
		_toastOk = ok;
		_toastOn = true;
		StateHasChanged();

		try
		{
			await Task.Delay(1600, token);
			_toastOn = false;
			StateHasChanged();
		}
		catch (TaskCanceledException) { /* superseded by a newer toast */ }
	}
	#endregion
}
