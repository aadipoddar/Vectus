using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Vehicle.Data;

public static class HDRData
{
	public static async Task<int> InsertHDR(HDRModel hdr, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertHDR, hdr, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert HDR.");

	public static async Task DeleteTransaction(HDRModel hdr, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			hdr.Status = false;
			await InsertHDR(hdr, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.HDR,
				RecordNo = hdr.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(HDRModel hdr, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			hdr.Status = true;
			await InsertHDR(hdr, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.HDR,
				RecordNo = hdr.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(HDRModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("HDR name is required. Please enter a valid HDR name.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateHDRCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("HDR code is required. Please try again.");

		var allHDRs = await CommonData.LoadTableData<HDRModel>(FleetNames.HDR);

		var existingByName = allHDRs.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"HDR name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allHDRs.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"HDR code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(HDRModel hdr, int userId, string platform)
	{
		await ValidateTransaction(hdr);

		var isUpdate = hdr.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<HDRModel>(FleetNames.HDR, hdr.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertHDR(hdr, transaction);
			var diff = AuditTrailData.GetDifference(previous, hdr);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.HDR,
				RecordNo = hdr.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
