using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Models;

namespace VectusLibrary.Payroll.Masters.Data;

public static class SalaryStructureData
{
	private static async Task<int> InsertSalaryStructure(SalaryStructureModel structure, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertSalaryStructure, structure, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Salary Structure.");

	private static async Task<int> InsertSalaryStructureLine(SalaryStructureLineModel line, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertSalaryStructureLine, line, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Salary Structure Line.");

	private static async Task<string> BuildRecordNo(SalaryStructureModel structure, SqlDataAccessTransaction transaction)
	{
		var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, structure.EmployeeId, transaction);
		return $"{employee?.Name ?? $"Employee #{structure.EmployeeId}"} ({structure.EffectiveFrom:dd-MM-yyyy})";
	}

	public static List<SalaryStructureLineModel> ConvertCartToLines(IEnumerable<SalaryStructureLineCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new SalaryStructureLineModel
		{
			Id = 0,
			MasterId = masterId,
			SalaryComponentId = item.SalaryComponentId,
			Amount = item.Amount,
			OverridePercentage = item.OverridePercentage,
			Status = true
		})];

	public static async Task DeleteTransaction(SalaryStructureModel structure, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			structure.Status = false;
			await InsertSalaryStructure(structure, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.SalaryStructure,
				RecordNo = await BuildRecordNo(structure, transaction),
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(SalaryStructureModel structure, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			structure.Status = true;
			await InsertSalaryStructure(structure, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.SalaryStructure,
				RecordNo = await BuildRecordNo(structure, transaction),
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(SalaryStructureModel structure, List<SalaryStructureLineModel> lines)
	{
		structure.Remarks = string.IsNullOrWhiteSpace(structure.Remarks) ? null : structure.Remarks.Trim();
		structure.Status = true;

		if (structure.EmployeeId <= 0)
			throw new Exception("Employee is required. Please select an employee.");

		if (structure.EffectiveFrom == default)
			throw new Exception("Effective from date is required. Please select a valid date.");

		if (structure.CTC is < 0)
			throw new Exception("CTC cannot be negative.");

		if (lines is null || lines.Count == 0)
			throw new Exception("A salary structure must have at least one component line.");

		if (lines.Any(l => l.SalaryComponentId <= 0))
			throw new Exception("Every line must reference a valid salary component.");

		if (lines.Any(l => (l.Amount ?? 0) < 0 || (l.OverridePercentage ?? 0) < 0))
			throw new Exception("Component amounts and percentages cannot be negative.");

		if (lines.GroupBy(l => l.SalaryComponentId).Any(g => g.Count() > 1))
			throw new Exception("The same salary component cannot appear more than once in a structure.");

		var allStructures = await CommonData.LoadTableData<SalaryStructureModel>(PayrollNames.SalaryStructure);
		var duplicate = allStructures.FirstOrDefault(s =>
			s.Id != structure.Id &&
			s.Status &&
			s.EmployeeId == structure.EmployeeId &&
			s.EffectiveFrom.Date == structure.EffectiveFrom.Date);
		if (duplicate is not null)
			throw new Exception("This employee already has a salary structure effective from the selected date.");
	}

	public static async Task<int> SaveTransaction(SalaryStructureModel structure, List<SalaryStructureLineModel> lines, int userId, string platform)
	{
		await ValidateTransaction(structure, lines);

		var isUpdate = structure.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<SalaryStructureModel>(PayrollNames.SalaryStructure, structure.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			structure.Id = await InsertSalaryStructure(structure, transaction);

			if (isUpdate)
			{
				var existingLines = await CommonData.LoadTableDataByMasterId<SalaryStructureLineModel>(PayrollNames.SalaryStructureLine, structure.Id, transaction);
				foreach (var line in existingLines)
				{
					line.Status = false;
					await InsertSalaryStructureLine(line, transaction);
				}
			}

			foreach (var line in lines)
			{
				line.MasterId = structure.Id;
				await InsertSalaryStructureLine(line, transaction);
			}

			var diff = AuditTrailData.GetDifference(previous, structure);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.SalaryStructure,
				RecordNo = await BuildRecordNo(structure, transaction),
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);

			return structure.Id;
		});
	}
}
