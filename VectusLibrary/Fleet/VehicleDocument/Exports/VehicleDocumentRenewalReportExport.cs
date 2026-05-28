using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Fleet.VehicleDocument.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.VehicleDocument.Exports;

public static class VehicleDocumentRenewalReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<VehicleDocumentRenewalOverviewModel> renewalData,
		ReportExportType exportType,
		bool showAllColumns = true,
		VehicleModel vehicle = null,
		VehicleDocumentTypeModel documentType = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(VehicleDocumentRenewalOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.TransactionDateTime)] = new() { DisplayName = "Last Entry", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(VehicleDocumentRenewalOverviewModel.VehicleCode)] = new() { DisplayName = "Vehicle", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.VehicleDocumentTypeName)] = new() { DisplayName = "Document Type", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.VehicleDocumentTypeCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(VehicleDocumentRenewalOverviewModel.RenewalDate)] = new() { DisplayName = "Renewal Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.DaysRemaining)] = new() { DisplayName = "Days Remaining", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(VehicleDocumentRenewalOverviewModel.CurrentKM)] = new() { DisplayName = "Current KM", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(VehicleDocumentRenewalOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleDocumentRenewalOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(VehicleDocumentRenewalOverviewModel.TransactionNo),
				nameof(VehicleDocumentRenewalOverviewModel.TransactionDateTime),
				nameof(VehicleDocumentRenewalOverviewModel.VehicleCode),
				nameof(VehicleDocumentRenewalOverviewModel.VehicleDocumentTypeName),
				nameof(VehicleDocumentRenewalOverviewModel.VehicleDocumentTypeCode),
				nameof(VehicleDocumentRenewalOverviewModel.RenewalDate),
				nameof(VehicleDocumentRenewalOverviewModel.DaysRemaining),
				nameof(VehicleDocumentRenewalOverviewModel.CurrentKM),
				nameof(VehicleDocumentRenewalOverviewModel.Rate),
				nameof(VehicleDocumentRenewalOverviewModel.Remarks),
				nameof(VehicleDocumentRenewalOverviewModel.CreatedByName),
				nameof(VehicleDocumentRenewalOverviewModel.CreatedAt),
				nameof(VehicleDocumentRenewalOverviewModel.CreatedFromPlatform),
				nameof(VehicleDocumentRenewalOverviewModel.LastModifiedByUserName),
				nameof(VehicleDocumentRenewalOverviewModel.LastModifiedAt),
				nameof(VehicleDocumentRenewalOverviewModel.LastModifiedFromPlatform)
			];
		}
		else
		{
			columnOrder =
			[
				nameof(VehicleDocumentRenewalOverviewModel.TransactionNo),
				nameof(VehicleDocumentRenewalOverviewModel.TransactionDateTime),
				nameof(VehicleDocumentRenewalOverviewModel.VehicleCode),
				nameof(VehicleDocumentRenewalOverviewModel.VehicleDocumentTypeName),
				nameof(VehicleDocumentRenewalOverviewModel.RenewalDate),
				nameof(VehicleDocumentRenewalOverviewModel.DaysRemaining)
			];

			if (vehicle is not null)
				columnOrder.Remove(nameof(VehicleDocumentRenewalOverviewModel.VehicleCode));

			if (documentType is not null)
				columnOrder.Remove(nameof(VehicleDocumentRenewalOverviewModel.VehicleDocumentTypeName));
		}

		string fileName = "DOCUMENT_RENEWAL_REPORT";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				renewalData,
				"DOCUMENT RENEWAL REPORT",
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns,
				new()
				{
					["Vehicle"] = vehicle?.Code ?? null,
					["Document Type"] = documentType?.Name ?? null
				}
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				renewalData,
				"DOCUMENT RENEWAL REPORT",
				"Document Renewals",
				null,
				null,
				columnSettings,
				columnOrder,
				new()
				{
					["Vehicle"] = vehicle?.Code ?? null,
					["Document Type"] = documentType?.Name ?? null,
				}
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
