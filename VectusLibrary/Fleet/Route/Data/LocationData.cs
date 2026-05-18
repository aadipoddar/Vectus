using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Route.Data;

public static class LocationData
{
	public static async Task<int> InsertLocation(LocationModel location, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertLocation, location, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Location.");

	public static async Task DeleteTransaction(LocationModel location, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			location.Status = false;
			await InsertLocation(location, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.Location,
				RecordNo = location.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(LocationModel location, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			location.Status = true;
			await InsertLocation(location, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.Location,
				RecordNo = location.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(LocationModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Location name is required. Please enter a valid location name.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateLocationCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Location code is required. Please try again.");

		var allLocations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location);

		var existingByName = allLocations.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Location name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allLocations.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Location code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(LocationModel location, int userId, string platform)
	{
		await ValidateTransaction(location);

		var isUpdate = location.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<LocationModel>(FleetNames.Location, location.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertLocation(location, transaction);
			var diff = AuditTrailData.GetDifference(previous, location);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.Location,
				RecordNo = location.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
