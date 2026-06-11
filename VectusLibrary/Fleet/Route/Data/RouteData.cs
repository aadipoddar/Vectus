using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Route.Data;

public static class RouteData
{
	public static async Task<int> InsertRoute(RouteModel route, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertRoute, route, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Route.");

	public static async Task<List<RouteOverviewModel>> LoadRouteOverview()
	{
		var routes = await CommonData.LoadTableDataByStatus<RouteModel>(FleetNames.Route);
		var locations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location);

		return [.. routes.Select(r => new RouteOverviewModel
		{
			Id = r.Id,
			FromLocationId = r.FromLocationId,
			FromLocationName = locations.FirstOrDefault(l => l.Id == r.FromLocationId)?.Name ?? string.Empty,
			FromLocationLatitude = locations.FirstOrDefault(l => l.Id == r.FromLocationId)?.Latitude ?? 0,
			FromLocationLongitude = locations.FirstOrDefault(l => l.Id == r.FromLocationId)?.Longitude ?? 0,
			ToLocationId = r.ToLocationId,
			ToLocationName = locations.FirstOrDefault(l => l.Id == r.ToLocationId)?.Name ?? string.Empty,
			ToLocationLatitude = locations.FirstOrDefault(l => l.Id == r.ToLocationId)?.Latitude ?? 0,
			ToLocationLongitude = locations.FirstOrDefault(l => l.Id == r.ToLocationId)?.Longitude ?? 0,
			RouteDisplay = $"{locations.FirstOrDefault(l => l.Id == r.FromLocationId)?.Name ?? r.FromLocationId.ToString()} - {locations.FirstOrDefault(l => l.Id == r.ToLocationId)?.Name ?? r.ToLocationId.ToString()}",
			Code = r.Code,
			EstimatedHours = r.EstimatedHours,
			EstimatedDistance = r.EstimatedDistance,
			EstimatedFuelConsumption = r.EstimatedFuelConsumption,
			EstimatedCost = r.EstimatedCost,
			Remarks = r.Remarks,
			Status = r.Status
		})];
	}

	public static async Task DeleteTransaction(RouteModel route, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			route.Status = false;
			await InsertRoute(route, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.Route,
				RecordNo = route.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(RouteModel route, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			route.Status = true;
			await InsertRoute(route, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.Route,
				RecordNo = route.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(RouteModel item)
	{
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (item.FromLocationId <= 0)
			throw new Exception("From location is required. Please select a valid from location.");

		if (item.ToLocationId <= 0)
			throw new Exception("To location is required. Please select a valid to location.");

		if (item.FromLocationId == item.ToLocationId)
			throw new Exception("From location and to location cannot be the same.");

		if (item.EstimatedHours < 0)
			throw new Exception("Estimated hours must be greater than zero.");

		if (item.EstimatedDistance < 0)
			throw new Exception("Estimated distance must be greater than zero.");

		if (item.EstimatedFuelConsumption < 0)
			throw new Exception("Estimated fuel consumption must be greater than zero.");

		if (item.EstimatedCost < 0)
			throw new Exception("Estimated cost must be greater than zero.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateRouteCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Route code is required. Please enter a valid route code.");

		var allRoutes = await CommonData.LoadTableData<RouteModel>(FleetNames.Route);
		var locations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location);

		var existingByLocationPair = allRoutes.FirstOrDefault(r =>
			r.Id != item.Id &&
			r.FromLocationId == item.FromLocationId &&
			r.ToLocationId == item.ToLocationId);

		if (existingByLocationPair is not null)
		{
			var fromLocationName = locations.FirstOrDefault(rl => rl.Id == item.FromLocationId)?.Name ?? item.FromLocationId.ToString();
			var toLocationName = locations.FirstOrDefault(rl => rl.Id == item.ToLocationId)?.Name ?? item.ToLocationId.ToString();
			throw new Exception($"Route '{fromLocationName} -> {toLocationName}' already exists. Duplicate route entries are not allowed.");
		}

		var existingByCode = allRoutes.FirstOrDefault(r =>
			r.Id != item.Id &&
			r.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));

		if (existingByCode is not null)
			throw new Exception($"Route code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(RouteModel route, int userId, string platform)
	{
		await ValidateTransaction(route);

		var isUpdate = route.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<RouteModel>(FleetNames.Route, route.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertRoute(route, transaction);
			var diff = AuditTrailData.GetDifference(previous, route);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.Route,
				RecordNo = route.Code,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
