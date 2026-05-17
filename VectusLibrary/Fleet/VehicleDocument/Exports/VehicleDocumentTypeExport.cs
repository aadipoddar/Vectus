using VectusLibrary.Common;
using VectusLibrary.Utils.ExportUtils;

using VectusLibrary.Fleet.VehicleDocument.Models;

namespace VectusLibrary.Fleet.VehicleDocument.Exports;

public static class VehicleDocumentTypeExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<VehicleDocumentTypeModel> vehicleDocumentTypeData,
		ReportExportType exportType)
	{
		var enrichedData = vehicleDocumentTypeData.Select(vehicleDocumentType => new
		{
			vehicleDocumentType.Id,
			vehicleDocumentType.Name,
			vehicleDocumentType.Code,
			vehicleDocumentType.Rate,
			vehicleDocumentType.Remarks,
			Status = vehicleDocumentType.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(VehicleDocumentTypeModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleDocumentTypeModel.Name)] = new() { DisplayName = "Document Type Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VehicleDocumentTypeModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VehicleDocumentTypeModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "#,##0.00", IncludeInTotal = false },
			[nameof(VehicleDocumentTypeModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(VehicleDocumentTypeModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(VehicleDocumentTypeModel.Id),
			nameof(VehicleDocumentTypeModel.Name),
			nameof(VehicleDocumentTypeModel.Code),
			nameof(VehicleDocumentTypeModel.Rate),
			nameof(VehicleDocumentTypeModel.Remarks),
			nameof(VehicleDocumentTypeModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Vehicle_Document_Type_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"VEHICLE DOCUMENT TYPE MASTER",
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
				"VEHICLE DOCUMENT TYPE",
				"Vehicle Document Type Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
