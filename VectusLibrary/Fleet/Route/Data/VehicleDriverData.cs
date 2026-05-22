using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Route.Data;

public static class VehicleDriverData
{
	private static async Task<int> InsertVehicleDriver(VehicleDriverModel vehicleDriver, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertVehicleDriver, vehicleDriver, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Vehicle Driver.");

	private static async Task<int> DeleteVehicleDriver(int Id, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.DeleteVehicleDriver, new { Id }, transaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Vehicle Driver.");

	public static async Task<List<VehicleDriverOverviewModel>> LoadVehicleDriverOverview()
	{
		var vehicleDrivers = await CommonData.LoadTableData<VehicleDriverModel>(FleetNames.VehicleDriver);
		var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);
		var vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);

		return [.. vehicleDrivers.Select(vd => new VehicleDriverOverviewModel
		{
			Id = vd.Id,
			DriverId = vd.DriverId,
			DriverName = drivers.FirstOrDefault(d => d.Id == vd.DriverId)?.Name ?? string.Empty,
			VehicleId = vd.VehicleId,
			VehicleCode = vehicles.FirstOrDefault(v => v.Id == vd.VehicleId)?.Code ?? string.Empty,
			StartDateTime = vd.StartDateTime,
			EndDateTime = vd.EndDateTime,
			Remarks = vd.Remarks
		})];
	}

	public static async Task DeleteTransaction(VehicleDriverModel vehicleDriver, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			await DeleteVehicleDriver(vehicleDriver.Id, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.VehicleDriver,
				RecordNo = vehicleDriver.Id.ToString(),
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(VehicleDriverModel item)
	{
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (item.DriverId <= 0)
			throw new Exception("Driver is required. Please select a valid driver.");

		if (item.VehicleId <= 0)
			throw new Exception("Vehicle is required. Please select a valid vehicle.");

		if (item.StartDateTime == default)
			throw new Exception("Start date is required. Please select a valid date.");

		if (item.EndDateTime.HasValue && item.EndDateTime.Value <= item.StartDateTime)
			throw new Exception("End date must be greater than start date. Please select valid dates.");

		var allItems = await CommonData.LoadTableData<VehicleDriverModel>(FleetNames.VehicleDriver);

		// Two periods overlap when each starts before the other ends. A null end date is open-ended
		// ("until further notice"), so treat it as DateTime.MaxValue.
		var itemEnd = item.EndDateTime ?? DateTime.MaxValue;
		bool Overlaps(VehicleDriverModel vd) =>
			vd.Id != item.Id &&
			item.StartDateTime < (vd.EndDateTime ?? DateTime.MaxValue) &&
			vd.StartDateTime < itemEnd;

		// The same driver cannot be assigned to two vehicles over the same period.
		var driverConflict = allItems.FirstOrDefault(vd => vd.DriverId == item.DriverId && Overlaps(vd));
		if (driverConflict is not null)
			throw new Exception($"The selected driver is already assigned to a vehicle during the specified period (Vehicle ID: {driverConflict.VehicleId}, Start: {driverConflict.StartDateTime}, End: {(driverConflict.EndDateTime.HasValue ? driverConflict.EndDateTime.Value.ToString() : "Ongoing")}). Please select a different driver or adjust the dates.");

		// The same vehicle cannot be assigned to two drivers over the same period.
		var vehicleConflict = allItems.FirstOrDefault(vd => vd.VehicleId == item.VehicleId && Overlaps(vd));
		if (vehicleConflict is not null)
			throw new Exception($"The selected vehicle is already assigned to a driver during the specified period (Driver ID: {vehicleConflict.DriverId}, Start: {vehicleConflict.StartDateTime}, End: {(vehicleConflict.EndDateTime.HasValue ? vehicleConflict.EndDateTime.Value.ToString() : "Ongoing")}). Please select a different vehicle or adjust the dates.");
	}

	public static async Task<int> SaveTransaction(VehicleDriverModel vehicleDriver, int userId, string platform)
	{
		await ValidateTransaction(vehicleDriver);

		var isUpdate = vehicleDriver.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<VehicleDriverModel>(FleetNames.VehicleDriver, vehicleDriver.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertVehicleDriver(vehicleDriver, transaction);
			var diff = AuditTrailData.GetDifference(previous, vehicleDriver);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.VehicleDriver,
				RecordNo = vehicleDriver.StartDateTime.ToString() + " - " + vehicleDriver.EndDateTime.ToString(),
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
