using VectusLibrary.Common;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Payroll.Masters.Exports;

public static class SalaryComponentExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<SalaryComponentModel> componentData,
		ReportExportType exportType)
	{
		var enrichedData = componentData.Select(component => new
		{
			component.Id,
			component.Code,
			component.Name,
			component.ComponentType,
			component.CalculationType,
			BaseComponentCode = component.BaseComponentCode ?? "—",
			Percentage = component.Percentage?.ToString("0.####") ?? "—",
			StatutoryRuleCode = component.StatutoryRuleCode ?? "—",
			Taxable = component.IsTaxable ? "Yes" : "No",
			Perquisite = component.IsPerquisite ? "Yes" : "No",
			PFBase = component.AffectsPFBase ? "Yes" : "No",
			ESIBase = component.AffectsESIBase ? "Yes" : "No",
			component.DisplayOrder,
			component.Remarks,
			Status = component.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SalaryComponentModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SalaryComponentModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(SalaryComponentModel.Name)] = new() { DisplayName = "Component Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(SalaryComponentModel.ComponentType)] = new() { DisplayName = "Type", Alignment = CellAlignment.Center },
			[nameof(SalaryComponentModel.CalculationType)] = new() { DisplayName = "Calculation", Alignment = CellAlignment.Left },
			[nameof(SalaryComponentModel.BaseComponentCode)] = new() { DisplayName = "Base Component", Alignment = CellAlignment.Left },
			[nameof(SalaryComponentModel.Percentage)] = new() { DisplayName = "Percentage", Alignment = CellAlignment.Right },
			[nameof(SalaryComponentModel.StatutoryRuleCode)] = new() { DisplayName = "Statutory Rule", Alignment = CellAlignment.Left },
			["Taxable"] = new() { DisplayName = "Taxable", Alignment = CellAlignment.Center },
			["Perquisite"] = new() { DisplayName = "Perquisite", Alignment = CellAlignment.Center },
			["PFBase"] = new() { DisplayName = "PF Base", Alignment = CellAlignment.Center },
			["ESIBase"] = new() { DisplayName = "ESI Base", Alignment = CellAlignment.Center },
			[nameof(SalaryComponentModel.DisplayOrder)] = new() { DisplayName = "Order", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SalaryComponentModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(SalaryComponentModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(SalaryComponentModel.Id),
			nameof(SalaryComponentModel.Code),
			nameof(SalaryComponentModel.Name),
			nameof(SalaryComponentModel.ComponentType),
			nameof(SalaryComponentModel.CalculationType),
			nameof(SalaryComponentModel.BaseComponentCode),
			nameof(SalaryComponentModel.Percentage),
			nameof(SalaryComponentModel.StatutoryRuleCode),
			"Taxable",
			"Perquisite",
			"PFBase",
			"ESIBase",
			nameof(SalaryComponentModel.DisplayOrder),
			nameof(SalaryComponentModel.Remarks),
			nameof(SalaryComponentModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"SalaryComponent_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"SALARY COMPONENT MASTER",
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
				"SALARY COMPONENT",
				"Salary Component Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
