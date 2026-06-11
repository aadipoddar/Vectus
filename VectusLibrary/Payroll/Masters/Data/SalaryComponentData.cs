using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Models;

namespace VectusLibrary.Payroll.Masters.Data;

public static class SalaryComponentData
{
	private static async Task<int> InsertSalaryComponent(SalaryComponentModel component, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertSalaryComponent, component, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Salary Component.");

	public static async Task DeleteTransaction(SalaryComponentModel component, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			component.Status = false;
			await InsertSalaryComponent(component, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.SalaryComponent,
				RecordNo = component.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(SalaryComponentModel component, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			component.Status = true;
			await InsertSalaryComponent(component, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.SalaryComponent,
				RecordNo = component.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(SalaryComponentModel item)
	{
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.ComponentType = item.ComponentType?.Trim();
		item.CalculationType = item.CalculationType?.Trim();
		item.BaseComponentCode = string.IsNullOrWhiteSpace(item.BaseComponentCode) ? null : item.BaseComponentCode.Trim().ToUpper();
		item.StatutoryRuleCode = string.IsNullOrWhiteSpace(item.StatutoryRuleCode) ? null : item.StatutoryRuleCode.Trim().ToUpper();
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Component code is required. Please enter a unique code (e.g. BASIC, HRA, PF-EE).");

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Component name is required. Please enter a valid name.");

		if (item.ComponentType is null || Array.IndexOf(SalaryComponentOptions.ComponentTypes, item.ComponentType) < 0)
			throw new Exception("Component type must be either 'Earning' or 'Deduction'.");

		if (item.CalculationType is null || Array.IndexOf(SalaryComponentOptions.CalculationTypes, item.CalculationType) < 0)
			throw new Exception("Invalid calculation type. Please select a valid calculation type.");

		if (item.CalculationType is "PercentOfBase")
		{
			if (string.IsNullOrWhiteSpace(item.BaseComponentCode))
				throw new Exception("Base component code is required for a 'Percent of Base' component (e.g. HRA = % of BASIC).");

			if (item.Percentage is null or <= 0)
				throw new Exception("A percentage greater than zero is required for a 'Percent of Base' component.");
		}
		else
		{
			item.Percentage = item.Percentage is null or <= 0 ? null : item.Percentage;
		}

		if (item.CalculationType is "StatutoryRule" && string.IsNullOrWhiteSpace(item.StatutoryRuleCode))
			throw new Exception("Statutory rule code is required for a 'Statutory Rule' component (e.g. PF-EE, ESI, PT).");

		if (item.DisplayOrder < 0)
			throw new Exception("Display order cannot be negative.");

		var allComponents = await CommonData.LoadTableData<SalaryComponentModel>(PayrollNames.SalaryComponent);

		var existingByCode = allComponents.FirstOrDefault(c => c.Id != item.Id && c.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Component code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(SalaryComponentModel component, int userId, string platform)
	{
		await ValidateTransaction(component);

		var isUpdate = component.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<SalaryComponentModel>(PayrollNames.SalaryComponent, component.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertSalaryComponent(component, transaction);
			var diff = AuditTrailData.GetDifference(previous, component);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.SalaryComponent,
				RecordNo = component.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
