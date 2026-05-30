using Microsoft.Extensions.Caching.Memory;

using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Fleet.VehicleDocument.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Components.Dashboard;

public partial class DashboardAnalysis
{
	private List<TripRequestOverviewModel> _tripRequests = [];
	private List<TripRequestOverviewModel> _pendingTripRequests = [];
	private List<RepairOverviewModel> _repairs = [];
	private List<VehicleModel> _vehicles = [];
	private List<VehicleDocumentRenewalOverviewModel> _dueDocuments = [];

	private int _warningDays = 30;

	private int _pendingTripRequestsCount = 0;

	private int _thisMonthTripsCount = 0;
	private string _thisMonthTripsTrend = "vs last month";

	private string _thisMonthRepairSpend = "₹0.00";
	private string _thisMonthRepairSpendTrend = "vs last month";

	private int _inGarageVehiclesCount = 0;
	private string _inGarageVehiclesNote = "currently under repair";

	private int _dueDocumentsCount = 0;
	private string _dueDocumentsNote = "within window or expired";

	private int _activeVehiclesCount = 0;
	private string _activeVehiclesNote = "ran a trip this month";

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await LoadData();
	}

	private async Task LoadData()
	{
		if (LoadCached())
			return;

		await LoadFresh();

		var expiry = TimeSpan.FromHours(1);
		MemoryCache.Set(StorageFileNames.TripRequestsOverviewDataFileName, _tripRequests, expiry);
		MemoryCache.Set(StorageFileNames.PendingTripRequestsDataFileName, _pendingTripRequests, expiry);
		MemoryCache.Set(StorageFileNames.RepairsOverviewDataFileName, _repairs, expiry);
		MemoryCache.Set(StorageFileNames.VehiclesDataFileName, _vehicles, expiry);
		MemoryCache.Set(StorageFileNames.DueDocumentsDataFileName, _dueDocuments, expiry);
	}

	private bool LoadCached()
	{
		if (!MemoryCache.TryGetValue(StorageFileNames.TripRequestsOverviewDataFileName, out List<TripRequestOverviewModel> tripRequests))
			return false;

		_tripRequests = tripRequests ?? [];
		_pendingTripRequests = MemoryCache.Get<List<TripRequestOverviewModel>>(StorageFileNames.PendingTripRequestsDataFileName) ?? [];
		_repairs = MemoryCache.Get<List<RepairOverviewModel>>(StorageFileNames.RepairsOverviewDataFileName) ?? [];
		_vehicles = MemoryCache.Get<List<VehicleModel>>(StorageFileNames.VehiclesDataFileName) ?? [];
		_dueDocuments = MemoryCache.Get<List<VehicleDocumentRenewalOverviewModel>>(StorageFileNames.DueDocumentsDataFileName) ?? [];

		ComputeKpis();
		StateHasChanged();
		return true;
	}

	private async Task LoadFresh()
	{
		try
		{
			var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
			var thisMonthEnd = thisMonthStart.AddMonths(1).AddSeconds(-1);
			var lastMonthStart = thisMonthStart.AddMonths(-1);

			var warningSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.ReportWarningDays);
			_warningDays = int.TryParse(warningSetting?.Value, out var days) ? days : 30;

			// Trip requests within current + last month — feeds trip volume KPI and active-vehicle calc.
			_tripRequests = await CommonData.LoadTableDataByDate<TripRequestOverviewModel>(FleetNames.TripRequestOverview, lastMonthStart, thisMonthEnd);

			// All pending requests — independent of date so older still-open requests remain visible.
			_pendingTripRequests = await CommonData.LoadTableData<TripRequestOverviewModel>(FleetNames.TripRequestOverview);

			// All active repairs — used for spend trend KPI and in-garage filter (which can be of any age).
			_repairs = await CommonData.LoadTableDataByStatus<RepairOverviewModel>(FleetNames.RepairOverview);

			_vehicles = await CommonData.LoadTableDataByStatus<VehicleModel>(FleetNames.Vehicle);
			_dueDocuments = await CommonData.LoadTableData<VehicleDocumentRenewalOverviewModel>(FleetNames.VehicleDocumentRenewalOverview);

			_tripRequests = [.. _tripRequests.Where(_ => _.Status).OrderByDescending(_ => _.TransactionDateTime)];
			_pendingTripRequests = [.. _pendingTripRequests.Where(_ => _.Status && _.RequestStatus == nameof(RequestStatus.Requested)).OrderByDescending(_ => _.TransactionDateTime)];
			_repairs = [.. _repairs.Where(_ => _.Status).OrderByDescending(_ => _.TransactionDateTime)];
			_dueDocuments = [.. _dueDocuments.Where(_ => _.DaysRemaining < _warningDays).OrderBy(_ => _.DaysRemaining)];

			ComputeKpis();
		}
		catch { }
		finally { StateHasChanged(); }
	}

	private void ComputeKpis()
	{
		var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
		var lastMonthStart = thisMonthStart.AddMonths(-1);

		_pendingTripRequestsCount = _pendingTripRequests.Count;

		// Trips this month — only accepted requests count as a real trip.
		var lastMonthAccepted = _tripRequests.Count(_ =>
			_.RequestStatus == nameof(RequestStatus.Accepted) &&
			_.TransactionDateTime >= lastMonthStart &&
			_.TransactionDateTime < thisMonthStart);
		_thisMonthTripsCount = _tripRequests.Count(_ =>
			_.RequestStatus == nameof(RequestStatus.Accepted) &&
			_.TransactionDateTime >= thisMonthStart);
		_thisMonthTripsTrend = Helper.FormatMonthlyTrend(_thisMonthTripsCount, lastMonthAccepted);

		// Repair spend = sum of Repair.TotalAmount within the month.
		var thisMonthRepairSpend = _repairs.Where(_ => _.TransactionDateTime >= thisMonthStart).Sum(_ => _.TotalAmount);
		var lastMonthRepairSpend = _repairs.Where(_ => _.TransactionDateTime >= lastMonthStart && _.TransactionDateTime < thisMonthStart).Sum(_ => _.TotalAmount);
		_thisMonthRepairSpend = thisMonthRepairSpend.FormatIndianCurrencyShort();
		_thisMonthRepairSpendTrend = Helper.FormatMonthlyTrend(thisMonthRepairSpend, lastMonthRepairSpend);

		// Vehicles in garage = repairs that have not been closed yet (no GarageOutDateTime).
		_inGarageVehiclesCount = _repairs.Where(_ => _.GarageOutDateTime is null).Select(_ => _.VehicleId).Distinct().Count();
		_inGarageVehiclesNote = "currently under repair";

		// Documents due within the warning window (already filtered).
		_dueDocumentsCount = _dueDocuments.Count;
		_dueDocumentsNote = $"within {_warningDays} days or expired";

		// Active vehicles — fleet inventory + how many actually ran an accepted trip this month.
		_activeVehiclesCount = _vehicles.Count;
		var ranThisMonth = _tripRequests
			.Where(_ => _.RequestStatus == nameof(RequestStatus.Accepted) && _.TransactionDateTime >= thisMonthStart)
			.Select(_ => _.VehicleId)
			.Distinct()
			.Count();
		_activeVehiclesNote = $"{ranThisMonth} ran a trip this month";
	}
}
