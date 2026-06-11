using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Models;

namespace VectusLibrary.Payroll.Masters.Data;

public static class EmployeeLocationData
{
	private static async Task<int> InsertEmployeeLocation(EmployeeLocationModel location, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertEmployeeLocation, location, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Employee Location.");

	public static async Task DeleteTransaction(EmployeeLocationModel location, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			location.Status = false;
			await InsertEmployeeLocation(location, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.EmployeeLocation,
				RecordNo = location.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(EmployeeLocationModel location, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			location.Status = true;
			await InsertEmployeeLocation(location, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.EmployeeLocation,
				RecordNo = location.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(EmployeeLocationModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.StateUTId = item.StateUTId is > 0 ? item.StateUTId : null;
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Location name is required. Please enter a valid location name.");

		var allLocations = await CommonData.LoadTableData<EmployeeLocationModel>(PayrollNames.EmployeeLocation);

		var existingByName = allLocations.FirstOrDefault(l => l.Id != item.Id && l.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Location name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(EmployeeLocationModel location, int userId, string platform)
	{
		await ValidateTransaction(location);

		var isUpdate = location.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<EmployeeLocationModel>(PayrollNames.EmployeeLocation, location.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertEmployeeLocation(location, transaction);
			var diff = AuditTrailData.GetDifference(previous, location);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.EmployeeLocation,
				RecordNo = location.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
