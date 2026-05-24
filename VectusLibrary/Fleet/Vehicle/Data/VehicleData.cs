using VectusLibrary.APIService;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Repair.Data;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Vehicle.Data;

public static class VehicleData
{
	private static async Task<int> InsertVehicle(VehicleModel vehicle, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertVehicle, vehicle, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Vehicle.");

	public static async Task<List<VehicleLocationModel>> LoadVehicleLocations(RouteOverviewModel route = null, DateTime? startDateTime = null, DateTime? endDateTime = null, bool onlyActive = true)
	{
		var vehicleLocations = await CommonData.LoadTableData<VehicleLocationModel>(FleetNames.Vehicle);
		var sdr = await CommonData.LoadTableData<SDRModel>(FleetNames.SDR);
		var garageVehicles = await RepairData.LoadGarageVehiclesByDate(startDateTime ?? DateTime.Now, endDateTime);

		List<VamosysVehicleModel> vamosysVehicles = [];
		try { vamosysVehicles = await VamosysApiService.GetLiveVehicles(); }
		catch { /* tracker unreachable — still show the fleet, just without live data */ }

		foreach (var vehicle in vehicleLocations)
		{
			vehicle.SDR = sdr.FirstOrDefault(s => s.Id == vehicle.SDRId)?.Name;
			vehicle.InGarage = garageVehicles.Any(g => g.VehicleId == vehicle.Id);

			var vamosysVehicle = vamosysVehicles.FirstOrDefault(v => string.Equals(v.VehicleId, vehicle.Code.RemoveSpace().Replace("-", ""), StringComparison.OrdinalIgnoreCase));
			if (vamosysVehicle is null)
				continue;

			vehicle.VehicleId = vamosysVehicle.VehicleId;
			vehicle.RegNo = vamosysVehicle.RegNo;
			vehicle.ShortName = vamosysVehicle.ShortName;
			vehicle.VehicleType = vamosysVehicle.VehicleType;

			vehicle.Latitude = vamosysVehicle.Latitude;
			vehicle.Longitude = vamosysVehicle.Longitude;
			vehicle.Address = vamosysVehicle.Address;

			vehicle.HaversineDistance = HaversineDistance(vehicle, route);

			vehicle.Speed = vamosysVehicle.Speed;
			vehicle.TopSpeed = vamosysVehicle.TopSpeed;
			vehicle.AverageSpeed = vamosysVehicle.AverageSpeed;
			vehicle.VehicleMode = vamosysVehicle.VehicleMode;
			vehicle.IgnitionOn = vamosysVehicle.IgnitionOn;

			vehicle.LastUpdate = vamosysVehicle.LastUpdate;

			vehicle.FuelLitre = vamosysVehicle.FuelLitre;
			vehicle.TankSize = vamosysVehicle.TankSize;
			vehicle.ExpectedFuelMileage = vamosysVehicle.ExpectedFuelMileage;

			vehicle.OdometerKM = vamosysVehicle.OdometerKM;
			vehicle.DistanceCovered = vamosysVehicle.DistanceCovered;

			vehicle.IsOverSpeed = vamosysVehicle.IsOverSpeed;
			vehicle.InsideGeoFence = vamosysVehicle.InsideGeoFence;
			vehicle.HasAlert = vamosysVehicle.HasAlert;

			vehicle.GpsExpiryDate = vamosysVehicle.GpsExpiryDate;
			vehicle.GpsExpiryDays = vamosysVehicle.GpsExpiryDays;
		}

		if (onlyActive)
			vehicleLocations = [.. vehicleLocations.Where(v => v.Status)];

		if (route is null)
			return vehicleLocations;
		else
			return [.. vehicleLocations.Where(v => v.HaversineDistance.HasValue).OrderBy(v => v.HaversineDistance)];
	}

	public static decimal? HaversineDistance(VehicleLocationModel vehicle, RouteOverviewModel route)
	{
		if (route is null || (route.FromLocationLatitude == 0 && route.FromLocationLongitude == 0) || !vehicle.HasValidPosition)
			return null;

		var locationLat = route.FromLocationLatitude;
		var locationLon = route.FromLocationLongitude;
		var vehicleLat = vehicle.Latitude;
		var vehicleLon = vehicle.Longitude;

		static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

		var deltaLat = ToRadians((double)(locationLat - vehicleLat));
		var deltaLon = ToRadians((double)(locationLon - vehicleLon));

		var a = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2))
					+ (Math.Cos(ToRadians((double)locationLat))
					* Math.Cos(ToRadians((double)vehicleLat))
					* Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2));

		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

		var earthRadiusKm = 6371.0;
		// Round to 3 decimals (≈1 m) so trucks near the origin don't collapse
		// to 0.0; the grid formats this adaptively (metres under 1 km).
		return (decimal)Math.Round(earthRadiusKm * c, 3);
	}

	public static async Task DeleteTransaction(VehicleModel vehicle, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			vehicle.Status = false;
			await InsertVehicle(vehicle, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.Vehicle,
				RecordNo = vehicle.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(VehicleModel vehicle, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			vehicle.Status = true;
			await InsertVehicle(vehicle, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.Vehicle,
				RecordNo = vehicle.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(VehicleModel item)
	{
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.ShortCode = item.ShortCode?.Trim().ToUpper() ?? string.Empty;
		item.ChasisCode = string.IsNullOrWhiteSpace(item.ChasisCode) ? null : item.ChasisCode.Trim();
		item.EngineCode = string.IsNullOrWhiteSpace(item.EngineCode) ? null : item.EngineCode.Trim();
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Vehicle code is required. Please enter a valid vehicle code.");

		if (string.IsNullOrWhiteSpace(item.ShortCode))
			throw new Exception("Vehicle short code is required. Please enter a valid short code.");

		if (item.VehicleTypeId <= 0)
			throw new Exception("Vehicle Type is required. Please select a valid vehicle type.");

		if (item.CompanyId <= 0)
			throw new Exception("Company is required. Please select a valid company.");

		if (item.OpeningKM < 0)
			throw new Exception("Opening KM cannot be negative.");

		var allVehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);

		var existingByCode = allVehicles.FirstOrDefault(v => v.Id != item.Id && v.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Vehicle code '{item.Code}' already exists. Please choose a different code.");

		if (!string.IsNullOrWhiteSpace(item.ChasisCode))
		{
			var existingByChasis = allVehicles.FirstOrDefault(v => v.Id != item.Id && !string.IsNullOrWhiteSpace(v.ChasisCode) && v.ChasisCode.Equals(item.ChasisCode, StringComparison.OrdinalIgnoreCase));
			if (existingByChasis is not null)
				throw new Exception($"Chasis code '{item.ChasisCode}' already exists. Please choose a different chasis code.");
		}

		if (!string.IsNullOrWhiteSpace(item.EngineCode))
		{
			var existingByEngine = allVehicles.FirstOrDefault(v => v.Id != item.Id && !string.IsNullOrWhiteSpace(v.EngineCode) && v.EngineCode.Equals(item.EngineCode, StringComparison.OrdinalIgnoreCase));
			if (existingByEngine is not null)
				throw new Exception($"Engine code '{item.EngineCode}' already exists. Please choose a different engine code.");
		}
	}

	public static async Task<int> SaveTransaction(VehicleModel vehicle, int userId, string platform)
	{
		await ValidateTransaction(vehicle);

		var isUpdate = vehicle.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<VehicleModel>(FleetNames.Vehicle, vehicle.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertVehicle(vehicle, transaction);
			var diff = AuditTrailData.GetDifference(previous, vehicle);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.Vehicle,
				RecordNo = vehicle.Code,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
