using VectusLibrary.APIService;
using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Vehicle.Data;

public static class VehicleData
{
	private static async Task<int> InsertVehicle(VehicleModel vehicle, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertVehicle, vehicle, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Vehicle.");

	public static async Task<List<VehicleLocationModel>> LoadVehicleLocations()
	{
		var vehicleLocations = await CommonData.LoadTableData<VehicleLocationModel>(FleetNames.Vehicle);

		List<VamosysVehicleModel> vamosysVehicles = [];
		try { vamosysVehicles = await VamosysApiService.GetLiveVehicles(); }
		catch { /* tracker unreachable — still show the fleet, just without live data */ }

		foreach (var vehicle in vehicleLocations)
		{
			var gps = vamosysVehicles.FirstOrDefault(v => string.Equals(v.VehicleId, vehicle.Code, StringComparison.OrdinalIgnoreCase));
			if (gps is null)
				continue;

			vehicle.VehicleId = gps.VehicleId;
			vehicle.RegNo = gps.RegNo;
			vehicle.ShortName = gps.ShortName;
			vehicle.VehicleType = gps.VehicleType;

			vehicle.Latitude = gps.Latitude;
			vehicle.Longitude = gps.Longitude;
			vehicle.Address = gps.Address;

			vehicle.Speed = gps.Speed;
			vehicle.TopSpeed = gps.TopSpeed;
			vehicle.AverageSpeed = gps.AverageSpeed;
			vehicle.VehicleMode = gps.VehicleMode;
			vehicle.IgnitionOn = gps.IgnitionOn;

			vehicle.LastUpdate = gps.LastUpdate;

			vehicle.FuelLitre = gps.FuelLitre;
			vehicle.TankSize = gps.TankSize;
			vehicle.ExpectedFuelMileage = gps.ExpectedFuelMileage;

			vehicle.OdometerKM = gps.OdometerKM;
			vehicle.DistanceCovered = gps.DistanceCovered;

			vehicle.IsOverSpeed = gps.IsOverSpeed;
			vehicle.InsideGeoFence = gps.InsideGeoFence;
			vehicle.HasAlert = gps.HasAlert;

			vehicle.GpsExpiryDate = gps.GpsExpiryDate;
			vehicle.GpsExpiryDays = gps.GpsExpiryDays;
		}

		return vehicleLocations;
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
