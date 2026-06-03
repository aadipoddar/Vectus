using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Payroll.Masters.Exports;

public static class StatutoryRuleExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<StatutoryRuleModel> ruleData,
		ReportExportType exportType)
	{
		var ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
		var states = await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT);

		var enrichedData = ruleData.Select(rule => new
		{
			rule.Id,
			rule.Code,
			rule.Name,
			ContributionAccount = rule.ContributionAccount ?? "—",
			rule.RoundingMode,
			Ledger = ledgers.FirstOrDefault(l => l.Id == rule.LedgerId)?.Name ?? "—",
			StateUT = states.FirstOrDefault(s => s.Id == rule.StateUTId)?.Name ?? "—",
			rule.Remarks,
			Status = rule.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(StatutoryRuleModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StatutoryRuleModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(StatutoryRuleModel.Name)] = new() { DisplayName = "Rule Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(StatutoryRuleModel.ContributionAccount)] = new() { DisplayName = "ECR A/c", Alignment = CellAlignment.Center },
			[nameof(StatutoryRuleModel.RoundingMode)] = new() { DisplayName = "Rounding", Alignment = CellAlignment.Center },
			["Ledger"] = new() { DisplayName = "Posting Ledger", Alignment = CellAlignment.Left },
			["StateUT"] = new() { DisplayName = "State/UT", Alignment = CellAlignment.Left },
			[nameof(StatutoryRuleModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(StatutoryRuleModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(StatutoryRuleModel.Id),
			nameof(StatutoryRuleModel.Code),
			nameof(StatutoryRuleModel.Name),
			nameof(StatutoryRuleModel.ContributionAccount),
			nameof(StatutoryRuleModel.RoundingMode),
			"Ledger",
			"StateUT",
			nameof(StatutoryRuleModel.Remarks),
			nameof(StatutoryRuleModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"StatutoryRule_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"STATUTORY RULE MASTER",
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"STATUTORY RULE",
				"Statutory Rule Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
