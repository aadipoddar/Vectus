using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Vehicle.Data;

public static class SDRData
{
	private static async Task<int> InsertSDR(SDRModel sdr, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertSDR, sdr, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert SDR.");

	public static async Task DeleteTransaction(SDRModel sdr, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			sdr.Status = false;
			await InsertSDR(sdr, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.SDR,
				RecordNo = sdr.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(SDRModel sdr, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			sdr.Status = true;
			await InsertSDR(sdr, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = FleetNames.SDR,
				RecordNo = sdr.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(SDRModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("SDR name is required. Please enter a valid SDR name.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateSDRCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("SDR code is required. Please try again.");

		if (item.UserId <= 0)
			throw new Exception("User is required. Please select a valid user.");

		var allSDRs = await CommonData.LoadTableData<SDRModel>(FleetNames.SDR);

		var existingByName = allSDRs.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"SDR name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allSDRs.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"SDR code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(SDRModel sdr, int userId, string platform)
	{
		await ValidateTransaction(sdr);

		var isUpdate = sdr.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<SDRModel>(FleetNames.SDR, sdr.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertSDR(sdr, transaction);
			var diff = AuditTrailData.GetDifference(previous, sdr);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.SDR,
				RecordNo = sdr.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
