using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Utils.ExportUtils;

using VectusLibrary.Fleet.VehicleDocument.Models;

namespace VectusLibrary.Fleet.VehicleDocument.Exports;

public static class VehicleDocumentExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportTransaction(
		IEnumerable<VehicleDocumentModel> vehicleDocumentData,
		ReportExportType exportType)
	{
		var financialYears = await CommonData.LoadTableData<FinancialYearModel>(AccountNames.FinancialYear);
		var vehicleDocumentTypes = await CommonData.LoadTableData<VehicleDocumentTypeModel>(FleetNames.VehicleDocumentType);
		var vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);

		var enrichedData = vehicleDocumentData.Select(vehicleDocument => new
		{
			vehicleDocument.Id,
			vehicleDocument.TransactionNo,
			vehicleDocument.TransactionDateTime,
			VehicleDocumentType = vehicleDocumentTypes.FirstOrDefault(vdt => vdt.Id == vehicleDocument.VehicleDocumentTypeId)?.Name,
			Vehicle = vehicles.FirstOrDefault(v => v.Id == vehicleDocument.VehicleId)?.Code,
			vehicleDocument.CurrentKM,
			vehicleDocument.Rate,
			vehicleDocument.RenewalDate,
			vehicleDocument.Remarks,
			vehicleDocument.DocumentUrl,
			Status = vehicleDocument.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(VehicleDocumentModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleDocumentModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IsRequired = true, IncludeInTotal = false },
			[nameof(VehicleDocumentModel.TransactionDateTime)] = new() { DisplayName = "Transaction Date", Alignment = CellAlignment.Center, Format = "dd-MMM-yyyy", IncludeInTotal = false },
			["VehicleDocumentType"] = new() { DisplayName = "Document Type", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["Vehicle"] = new() { DisplayName = "Vehicle", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentModel.CurrentKM)] = new() { DisplayName = "Current KM", Alignment = CellAlignment.Right, Format = "#,##0.00", IncludeInTotal = false },
			[nameof(VehicleDocumentModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "#,##0.00", IncludeInTotal = false },
			[nameof(VehicleDocumentModel.RenewalDate)] = new() { DisplayName = "Renewal Date", Alignment = CellAlignment.Center, Format = "dd-MMM-yyyy", IncludeInTotal = false },
			[nameof(VehicleDocumentModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentModel.DocumentUrl)] = new() { DisplayName = "Document URL", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(VehicleDocumentModel.Id),
			nameof(VehicleDocumentModel.TransactionNo),
			nameof(VehicleDocumentModel.TransactionDateTime),
			"VehicleDocumentType",
			"Vehicle",
			nameof(VehicleDocumentModel.CurrentKM),
			nameof(VehicleDocumentModel.Rate),
			nameof(VehicleDocumentModel.RenewalDate),
			nameof(VehicleDocumentModel.Remarks),
			nameof(VehicleDocumentModel.DocumentUrl),
			nameof(VehicleDocumentModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Vehicle_Document_Transaction_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"VEHICLE DOCUMENT TRANSACTION",
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			return (stream, fileName + ".pdf");
		}

		var excelStream = await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"VEHICLE DOCUMENT TRANSACTION",
			"Vehicle Document Data",
			null,
			null,
			columnSettings,
			columnOrder
		);

		return (excelStream, fileName + ".xlsx");
	}
}