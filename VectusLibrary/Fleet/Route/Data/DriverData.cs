using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Route.Data;

public static class DriverData
{
	public static async Task<int> InsertDriver(DriverModel driver, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertDriver, driver, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Driver.");

	public static async Task DeleteTransaction(DriverModel driver, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			driver.Status = false;
			await InsertDriver(driver, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.Driver,
				RecordNo = driver.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(DriverModel driver, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			driver.Status = true;
			await InsertDriver(driver, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.Driver,
				RecordNo = driver.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task<List<DriverOverviewModel>> LoadDriverOverview()
	{
		var drivers = await CommonData.LoadTableDataByStatus<DriverModel>(FleetNames.Driver);
		return [.. drivers.Select(d => new DriverOverviewModel
		{
			Id = d.Id,
			Name = d.Name,
			Mobile = d.Mobile,
			Code = d.Code,
			Remarks = d.Remarks,
			Status = d.Status
		})];
	}

	private static async Task ValidateTransaction(DriverModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Mobile = item.Mobile?.Trim() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Driver name is required. Please enter a valid driver name.");

		if (string.IsNullOrWhiteSpace(item.Mobile))
			throw new Exception("Mobile is required. Please enter a valid mobile number.");

		if (!item.Mobile.ValidatePhoneNumber())
			throw new Exception("Mobile must be exactly 10 numeric digits.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateDriverCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Driver code is required. Please try again.");

		var allDrivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);

		var existingByMobile = allDrivers.FirstOrDefault(vd => vd.Id != item.Id && vd.Mobile.Equals(item.Mobile, StringComparison.OrdinalIgnoreCase));
		if (existingByMobile is not null)
			throw new Exception($"Mobile '{item.Mobile}' already exists. Please choose a different mobile number.");

		var existingByCode = allDrivers.FirstOrDefault(vd => vd.Id != item.Id && vd.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Driver code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(DriverModel driver, int userId, string platform)
	{
		await ValidateTransaction(driver);

		var isUpdate = driver.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<DriverModel>(FleetNames.Driver, driver.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertDriver(driver, transaction);
			var diff = AuditTrailData.GetDifference(previous, driver);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.Driver,
				RecordNo = driver.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
