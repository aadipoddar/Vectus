using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Models;

namespace VectusLibrary.Payroll.Masters.Data;

public static class StatutoryRuleData
{
	private static async Task<int> InsertStatutoryRule(StatutoryRuleModel rule, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertStatutoryRule, rule, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Statutory Rule.");

	private static async Task<int> InsertStatutoryRate(StatutoryRateModel rate, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertStatutoryRate, rate, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Statutory Rate.");

	private static async Task<int> InsertStatutorySlab(StatutorySlabModel slab, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertStatutorySlab, slab, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Statutory Slab.");

	public static async Task DeleteTransaction(StatutoryRuleModel rule, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			rule.Status = false;
			await InsertStatutoryRule(rule, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.StatutoryRule,
				RecordNo = rule.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(StatutoryRuleModel rule, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			rule.Status = true;
			await InsertStatutoryRule(rule, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.StatutoryRule,
				RecordNo = rule.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(StatutoryRuleModel rule, List<StatutoryRateCartModel> rates)
	{
		rule.Code = rule.Code?.Trim().ToUpper() ?? string.Empty;
		rule.Name = rule.Name?.Trim().ToUpper() ?? string.Empty;
		rule.ContributionAccount = string.IsNullOrWhiteSpace(rule.ContributionAccount) ? null : rule.ContributionAccount.Trim();
		rule.RoundingMode = string.IsNullOrWhiteSpace(rule.RoundingMode) ? "None" : rule.RoundingMode.Trim();
		rule.Remarks = string.IsNullOrWhiteSpace(rule.Remarks) ? null : rule.Remarks.Trim();
		rule.LedgerId = rule.LedgerId is > 0 ? rule.LedgerId : null;
		rule.StateUTId = rule.StateUTId is > 0 ? rule.StateUTId : null;
		rule.Status = true;

		if (string.IsNullOrWhiteSpace(rule.Code))
			throw new Exception("Rule code is required. Please enter a unique code (e.g. PF-EE, ESI, PT, TDS).");

		if (string.IsNullOrWhiteSpace(rule.Name))
			throw new Exception("Rule name is required. Please enter a valid name.");

		if (Array.IndexOf(StatutoryRuleOptions.RoundingModes, rule.RoundingMode) < 0)
			throw new Exception("Rounding mode must be 'None', 'Nearest' or 'Up'.");

		if (rates is null || rates.Count == 0)
			throw new Exception("A statutory rule must have at least one effective-dated rate.");

		if (rates.Any(r => r.EffectiveFrom == default))
			throw new Exception("Every rate must have an effective-from date.");

		if (rates.GroupBy(r => r.EffectiveFrom.Date).Any(g => g.Count() > 1))
			throw new Exception("Two rates cannot share the same effective-from date.");

		foreach (var rate in rates)
		{
			if (rate.Slabs.GroupBy(s => s.FromAmount).Any(g => g.Count() > 1))
				throw new Exception("Slab bands within a rate cannot share the same 'from' amount.");

			if (rate.Slabs.Any(s => s.FromAmount < 0))
				throw new Exception("A slab's 'from' amount cannot be negative.");

			if (rate.Slabs.Any(s => s.ToAmount is not null && s.ToAmount <= s.FromAmount))
				throw new Exception("Each slab's 'to' amount must be greater than its 'from' amount.");
		}

		var allRules = await CommonData.LoadTableData<StatutoryRuleModel>(PayrollNames.StatutoryRule);
		var existingByCode = allRules.FirstOrDefault(r => r.Id != rule.Id && r.Code.Equals(rule.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Rule code '{rule.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(StatutoryRuleModel rule, List<StatutoryRateCartModel> rates, int userId, string platform)
	{
		await ValidateTransaction(rule, rates);

		var isUpdate = rule.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<StatutoryRuleModel>(PayrollNames.StatutoryRule, rule.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			rule.Id = await InsertStatutoryRule(rule, transaction);

			if (isUpdate)
				await SoftDeleteExistingRates(rule.Id, transaction);

			foreach (var rate in rates)
			{
				var rateId = await InsertStatutoryRate(new()
				{
					Id = 0,
					MasterId = rule.Id,
					EffectiveFrom = rate.EffectiveFrom,
					EmployeeRate = rate.EmployeeRate,
					EmployerRate = rate.EmployerRate,
					WageCeiling = rate.WageCeiling,
					MaxAmount = rate.MaxAmount,
					MinAmount = rate.MinAmount,
					MinBasePercentOfGross = rate.MinBasePercentOfGross,
					StandardDeduction = rate.StandardDeduction,
					RebateAmount = rate.RebateAmount,
					RebateIncomeLimit = rate.RebateIncomeLimit,
					CessPercent = rate.CessPercent,
					Status = true
				}, transaction);

				foreach (var slab in rate.Slabs)
					await InsertStatutorySlab(new()
					{
						Id = 0,
						MasterId = rateId,
						FromAmount = slab.FromAmount,
						ToAmount = slab.ToAmount,
						FixedAmount = slab.FixedAmount,
						Rate = slab.Rate,
						Status = true
					}, transaction);
			}

			var diff = AuditTrailData.GetDifference(previous, rule);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.StatutoryRule,
				RecordNo = rule.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);

			return rule.Id;
		});
	}

	private static async Task SoftDeleteExistingRates(int ruleId, SqlDataAccessTransaction transaction)
	{
		var existingRates = await CommonData.LoadTableDataByMasterId<StatutoryRateModel>(PayrollNames.StatutoryRate, ruleId, transaction);
		foreach (var rate in existingRates)
		{
			var existingSlabs = await CommonData.LoadTableDataByMasterId<StatutorySlabModel>(PayrollNames.StatutorySlab, rate.Id, transaction);
			foreach (var slab in existingSlabs)
			{
				slab.Status = false;
				await InsertStatutorySlab(slab, transaction);
			}

			rate.Status = false;
			await InsertStatutoryRate(rate, transaction);
		}
	}
}
