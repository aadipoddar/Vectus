using Microsoft.Extensions.Caching.Memory;

using Syncfusion.Blazor.Grids;

using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Fleet.VehicleDocument.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace Vectus.Shared.Components.Dashboard;

public partial class DashboardAttention
{
	private int _warningDays = 30;

	private List<VehicleDocumentRenewalOverviewModel> _dueDocuments = [];
	private List<TripRequestOverviewModel> _pendingTripRequests = [];
	private List<RepairOverviewModel> _inGarageRepairs = [];
	private List<VehicleModel> _idleVehicles = [];
	private List<CompanyModel> _companies = [];
	private List<VehicleTypeModel> _vehicleTypes = [];

	private SfGrid<VehicleDocumentRenewalOverviewModel> _documentsGrid;
	private SfGrid<TripRequestOverviewModel> _pendingGrid;
	private SfGrid<RepairOverviewModel> _garageGrid;
	private SfGrid<VehicleModel> _idleGrid;

	private readonly List<ContextMenuItemModel> _documentContextMenuItems =
	[
		new() { Text = "Renew", Id = "Renew", IconCss = "e-icons e-refresh", Target = ".e-content" }
	];

	private readonly List<ContextMenuItemModel> _pendingContextMenuItems =
	[
		new() { Text = "View Request", Id = "ViewRequest", IconCss = "e-icons e-eye", Target = ".e-content" }
	];

	private readonly List<ContextMenuItemModel> _garageContextMenuItems =
	[
		new() { Text = "View Repair", Id = "ViewRepair", IconCss = "e-icons e-eye", Target = ".e-content" }
	];

	private readonly List<ContextMenuItemModel> _idleContextMenuItems =
	[
		new() { Text = "View Vehicle", Id = "ViewVehicle", IconCss = "e-icons e-eye", Target = ".e-content" }
	];

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await LoadData();
	}

	private async Task LoadData()
	{
		if (LoadCachedAttentions())
			return;

		await LoadNewAttentions();

		var expiry = TimeSpan.FromHours(1);
		MemoryCache.Set(StorageFileNames.DueDocumentsDataFileName, _dueDocuments, expiry);
		MemoryCache.Set(StorageFileNames.PendingTripRequestsDataFileName, _pendingTripRequests, expiry);
		MemoryCache.Set(StorageFileNames.InGarageRepairsDataFileName, _inGarageRepairs, expiry);
		MemoryCache.Set(StorageFileNames.IdleVehiclesDataFileName, _idleVehicles, expiry);
		MemoryCache.Set(StorageFileNames.CompaniesDataFileName, _companies, expiry);
		MemoryCache.Set(StorageFileNames.VehicleTypesDataFileName, _vehicleTypes, expiry);
	}

	private bool LoadCachedAttentions()
	{
		if (!MemoryCache.TryGetValue(StorageFileNames.IdleVehiclesDataFileName, out List<VehicleModel> idleVehicles))
			return false;

		_idleVehicles = idleVehicles ?? [];
		_dueDocuments = MemoryCache.Get<List<VehicleDocumentRenewalOverviewModel>>(StorageFileNames.DueDocumentsDataFileName) ?? [];
		_pendingTripRequests = MemoryCache.Get<List<TripRequestOverviewModel>>(StorageFileNames.PendingTripRequestsDataFileName) ?? [];
		_inGarageRepairs = MemoryCache.Get<List<RepairOverviewModel>>(StorageFileNames.InGarageRepairsDataFileName) ?? [];
		_companies = MemoryCache.Get<List<CompanyModel>>(StorageFileNames.CompaniesDataFileName) ?? [];
		_vehicleTypes = MemoryCache.Get<List<VehicleTypeModel>>(StorageFileNames.VehicleTypesDataFileName) ?? [];
		StateHasChanged();
		return true;
	}

	private async Task LoadNewAttentions()
	{
		try
		{
			var warningSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.ReportWarningDays);
			_warningDays = int.TryParse(warningSetting?.Value, out var days) ? days : 30;
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			var warningWindowStart = currentDateTime.AddDays(-_warningDays);

			// Documents due within the warning window or already expired.
			_dueDocuments = await CommonData.LoadTableData<VehicleDocumentRenewalOverviewModel>(FleetNames.VehicleDocumentRenewalOverview);
			_dueDocuments = [.. _dueDocuments.Where(_ => _.DaysRemaining < _warningDays).OrderBy(_ => _.DaysRemaining)];

			// All pending trip requests — independent of date so older still-open ones remain visible.
			_pendingTripRequests = await CommonData.LoadTableData<TripRequestOverviewModel>(FleetNames.TripRequestOverview);
			_pendingTripRequests = [.. _pendingTripRequests
				.Where(_ => _.Status && _.RequestStatus == nameof(RequestStatus.Requested))
				.OrderByDescending(_ => _.TransactionDateTime)];

			// All active repairs — for "in garage" filter (vehicles whose repair is not yet closed) and idle calc.
			var allActiveRepairs = await CommonData.LoadTableDataByStatus<RepairOverviewModel>(FleetNames.RepairOverview);
			allActiveRepairs = [.. allActiveRepairs.Where(_ => _.Status)];
			_inGarageRepairs = [.. allActiveRepairs.Where(_ => _.GarageOutDateTime is null).OrderBy(_ => _.GarageInDateTime)];
			var inGarageVehicleIds = _inGarageRepairs.Select(_ => _.VehicleId).ToHashSet();

			// Accepted trip requests within the warning window — for idle calc.
			var windowTripRequests = await CommonData.LoadTableDataByDate<TripRequestOverviewModel>(FleetNames.TripRequestOverview, warningWindowStart, currentDateTime);
			windowTripRequests = [.. windowTripRequests.Where(_ => _.Status && _.RequestStatus == nameof(RequestStatus.Accepted))];
			var activeVehicleIds = windowTripRequests.Select(_ => _.VehicleId).ToHashSet();

			// Idle = active vehicles that ran no accepted trip in window AND are not currently in garage.
			var vehicles = await CommonData.LoadTableDataByStatus<VehicleModel>(FleetNames.Vehicle);
			_idleVehicles = [.. vehicles
				.Where(_ => !activeVehicleIds.Contains(_.Id) && !inGarageVehicleIds.Contains(_.Id))
				.OrderBy(_ => _.Code)];

			_companies = await CommonData.LoadTableData<CompanyModel>(AccountNames.Company);
			_vehicleTypes = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType);
		}
		catch { }
		finally { StateHasChanged(); }
	}

	private void OnDocumentContextMenuItemClicked(ContextMenuClickEventArgs<VehicleDocumentRenewalOverviewModel> args)
	{
		if (args.Item.Id == "Renew")
			NavigationManager.NavigateTo(PageRouteNames.VehicleDocument);
	}

	private void OnPendingContextMenuItemClicked(ContextMenuClickEventArgs<TripRequestOverviewModel> args)
	{
		if (args.Item.Id == "ViewRequest")
			NavigationManager.NavigateTo(PageRouteNames.TripRequest);
	}

	private void OnGarageContextMenuItemClicked(ContextMenuClickEventArgs<RepairOverviewModel> args)
	{
		if (args.Item.Id == "ViewRepair")
			NavigationManager.NavigateTo(PageRouteNames.Repair);
	}

	private void OnIdleContextMenuItemClicked(ContextMenuClickEventArgs<VehicleModel> args)
	{
		if (args.Item.Id == "ViewVehicle")
			NavigationManager.NavigateTo(PageRouteNames.VehicleMaster);
	}
}
