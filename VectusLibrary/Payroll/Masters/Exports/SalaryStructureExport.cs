using VectusLibrary.Common;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Payroll.Masters.Exports;

public static class SalaryStructureExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<SalaryStructureModel> structureData,
		ReportExportType exportType)
	{
		var employees = await CommonData.LoadTableData<EmployeeModel>(PayrollNames.Employee);

		var enrichedData = structureData.Select(structure => new
		{
			structure.Id,
			Employee = employees.FirstOrDefault(e => e.Id == structure.EmployeeId)?.Name ?? "N/A",
			EffectiveFrom = structure.EffectiveFrom.ToString("dd-MM-yyyy"),
			CTC = structure.CTC?.ToString("N2") ?? "—",
			structure.Remarks,
			Status = structure.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SalaryStructureModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			["Employee"] = new() { DisplayName = "Employee", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(SalaryStructureModel.EffectiveFrom)] = new() { DisplayName = "Effective From", Alignment = CellAlignment.Center },
			[nameof(SalaryStructureModel.CTC)] = new() { DisplayName = "CTC", Alignment = CellAlignment.Right },
			[nameof(SalaryStructureModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(SalaryStructureModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(SalaryStructureModel.Id),
			"Employee",
			nameof(SalaryStructureModel.EffectiveFrom),
			nameof(SalaryStructureModel.CTC),
			nameof(SalaryStructureModel.Remarks),
			nameof(SalaryStructureModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"SalaryStructure_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"SALARY STRUCTURE MASTER",
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: false
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"SALARY STRUCTURE",
				"Salary Structure Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
