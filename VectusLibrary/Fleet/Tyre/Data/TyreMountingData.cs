using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Tyre.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.Fleet.Tyre.Data;

public static class TyreMountingData
{
	private static async Task<int> InsertTyreMounting(TyreMountingModel tyreMounting, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.InsertTyreMounting, tyreMounting, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Tyre Mounting.");

	private static async Task<int> DeleteTyreMounting(int id, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(FleetNames.DeleteTyreMounting, new { Id = id }, transaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Tyre Mounting.");

	public static async Task DeleteTransaction(TyreMountingModel tyreMounting, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			await DeleteTyreMounting(tyreMounting.Id, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = FleetNames.TyreMounting,
				RecordNo = tyreMounting.TyreNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(TyreMountingModel item)
	{
		item.TyreNo = item.TyreNo?.Trim().ToUpper() ?? string.Empty;
		item.TyreModel = string.IsNullOrWhiteSpace(item.TyreModel) ? null : item.TyreModel.Trim().ToUpper();
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (string.IsNullOrWhiteSpace(item.TyreNo))
			throw new Exception("Tyre No is required. Please enter a valid tyre number.");

		if (item.TyreCompanyId <= 0)
			throw new Exception("Tyre Company is required. Please select a valid tyre company.");

		if (item.VehicleId <= 0)
			throw new Exception("Vehicle is required. Please select a valid vehicle.");

		if (item.MountingKM < 0)
			throw new Exception("Mounting KM cannot be negative.");

		if (item.DismountingKM.HasValue && item.DismountingKM.Value < item.MountingKM)
			throw new Exception("Dismounting KM cannot be less than Mounting KM.");

		if (item.MountingDateTime == default)
			throw new Exception("Mounting date is required. Please select a valid mounting date.");

		if (item.DismountingDateTime.HasValue && item.DismountingDateTime.Value < item.MountingDateTime)
			throw new Exception("Dismounting date cannot be earlier than Mounting date.");

		var allTyreMountings = await CommonData.LoadTableData<TyreMountingModel>(FleetNames.TyreMounting);
		var itemEnd = item.DismountingDateTime ?? DateTime.MaxValue;

		bool Overlaps(TyreMountingModel tyreMounting) =>
			tyreMounting.Id != item.Id &&
			item.MountingDateTime < (tyreMounting.DismountingDateTime ?? DateTime.MaxValue) &&
			tyreMounting.MountingDateTime < itemEnd;

		var tyreConflict = allTyreMountings.FirstOrDefault(x => x.TyreNo.Equals(item.TyreNo, StringComparison.OrdinalIgnoreCase) && Overlaps(x));
		if (tyreConflict is not null)
			throw new Exception($"Tyre No '{item.TyreNo}' is already mounted during the selected period. Please adjust the dates or choose a different tyre.");
	}

	public static async Task<int> SaveTransaction(TyreMountingModel tyreMounting, int userId, string platform)
	{
		await ValidateTransaction(tyreMounting);

		var isUpdate = tyreMounting.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<TyreMountingModel>(FleetNames.TyreMounting, tyreMounting.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertTyreMounting(tyreMounting, transaction);
			var diff = AuditTrailData.GetDifference(previous, tyreMounting);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = FleetNames.TyreMounting,
				RecordNo = tyreMounting.TyreNo,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
