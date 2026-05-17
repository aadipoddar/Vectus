using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.VehicleDocument.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.VehicleDocument.Data;

public static class VehicleDocumentTypeData
{
	public static async Task<int> InsertVehicleDocumentType(VehicleDocumentTypeModel vehicleDocumentType, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertDocumentType, vehicleDocumentType, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Vehicle Document Type.");

	public static async Task DeleteTransaction(VehicleDocumentTypeModel vehicleDocumentType, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			vehicleDocumentType.Status = false;
			await InsertVehicleDocumentType(vehicleDocumentType, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.VehicleDocumentType,
				RecordNo = vehicleDocumentType.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(VehicleDocumentTypeModel vehicleDocumentType, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			vehicleDocumentType.Status = true;
			await InsertVehicleDocumentType(vehicleDocumentType, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.VehicleDocumentType,
				RecordNo = vehicleDocumentType.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(VehicleDocumentTypeModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Vehicle Document Type name is required. Please enter a valid document type name.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateVehicleDocumentTypeCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Vehicle Document Type code is required. Please try again.");

		if (item.Rate < 0)
			throw new Exception("Rate cannot be negative.");

		var allTypes = await CommonData.LoadTableData<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType);

		var existingByName = allTypes.FirstOrDefault(vdt => vdt.Id != item.Id && vdt.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Vehicle Document Type name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allTypes.FirstOrDefault(vdt => vdt.Id != item.Id && vdt.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Vehicle Document Type code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(VehicleDocumentTypeModel vehicleDocumentType, int userId, string platform)
	{
		await ValidateTransaction(vehicleDocumentType);

		var isUpdate = vehicleDocumentType.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType, vehicleDocumentType.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertVehicleDocumentType(vehicleDocumentType, transaction);
			var diff = AuditTrailData.GetDifference(previous, vehicleDocumentType);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.VehicleDocumentType,
				RecordNo = vehicleDocumentType.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
