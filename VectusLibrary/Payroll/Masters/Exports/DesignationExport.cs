using VectusLibrary.Common;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Payroll.Masters.Exports;

public static class DesignationExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<DesignationModel> designationData,
		ReportExportType exportType)
	{
		var enrichedData = designationData.Select(designation => new
		{
			designation.Id,
			designation.Name,
			designation.Remarks,
			Status = designation.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DesignationModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DesignationModel.Name)] = new() { DisplayName = "Designation Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DesignationModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DesignationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DesignationModel.Id),
			nameof(DesignationModel.Name),
			nameof(DesignationModel.Remarks),
			nameof(DesignationModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Designation_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DESIGNATION MASTER",
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
				"DESIGNATION",
				"Designation Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
