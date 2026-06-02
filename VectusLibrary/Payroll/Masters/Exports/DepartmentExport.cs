using VectusLibrary.Common;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Payroll.Masters.Exports;

public static class DepartmentExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<DepartmentModel> departmentData,
		ReportExportType exportType)
	{
		var enrichedData = departmentData.Select(department => new
		{
			department.Id,
			department.Name,
			department.Remarks,
			Status = department.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DepartmentModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DepartmentModel.Name)] = new() { DisplayName = "Department Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DepartmentModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DepartmentModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DepartmentModel.Id),
			nameof(DepartmentModel.Name),
			nameof(DepartmentModel.Remarks),
			nameof(DepartmentModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Department_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DEPARTMENT MASTER",
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
				"DEPARTMENT",
				"Department Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
