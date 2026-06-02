using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Models;

namespace VectusLibrary.Payroll.Masters.Data;

public static class DepartmentData
{
	private static async Task<int> InsertDepartment(DepartmentModel department, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertDepartment, department, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Department.");

	public static async Task DeleteTransaction(DepartmentModel department, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			department.Status = false;
			await InsertDepartment(department, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.Department,
				RecordNo = department.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(DepartmentModel department, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			department.Status = true;
			await InsertDepartment(department, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.Department,
				RecordNo = department.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(DepartmentModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Department name is required. Please enter a valid department name.");

		var allDepartments = await CommonData.LoadTableData<DepartmentModel>(PayrollNames.Department);

		var existingByName = allDepartments.FirstOrDefault(d => d.Id != item.Id && d.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Department name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(DepartmentModel department, int userId, string platform)
	{
		await ValidateTransaction(department);

		var isUpdate = department.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<DepartmentModel>(PayrollNames.Department, department.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertDepartment(department, transaction);
			var diff = AuditTrailData.GetDifference(previous, department);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.Department,
				RecordNo = department.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
