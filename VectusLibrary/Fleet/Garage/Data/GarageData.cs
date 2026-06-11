using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Garage.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Garage.Data;

public static class GarageData
{
	public static async Task<int> InsertGarage(GarageModel garage, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertGarage, garage, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Garage.");

	public static async Task DeleteTransaction(GarageModel garage, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			garage.Status = false;
			await InsertGarage(garage, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.Garage,
				RecordNo = garage.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(GarageModel garage, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			garage.Status = true;
			await InsertGarage(garage, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.Garage,
				RecordNo = garage.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(GarageModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Garage name is required. Please enter a valid garage name.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateGarageCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Garage code is required. Please try again.");

		if (item.LocationId <= 0)
			throw new Exception("Location is required. Please select a valid location.");

		var allGarages = await CommonData.LoadTableData<GarageModel>(FleetNames.Garage);

		var existingByName = allGarages.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Garage name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allGarages.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Garage code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(GarageModel garage, int userId, string platform)
	{
		await ValidateTransaction(garage);

		var isUpdate = garage.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<GarageModel>(FleetNames.Garage, garage.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertGarage(garage, transaction);
			var diff = AuditTrailData.GetDifference(previous, garage);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.Garage,
				RecordNo = garage.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
