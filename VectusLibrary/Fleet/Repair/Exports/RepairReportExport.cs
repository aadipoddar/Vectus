using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Garage.Models;
using VectusLibrary.Fleet.Repair.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Repair.Exports;

public static class RepairReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<RepairOverviewModel> repairData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		CompanyModel company = null,
		VehicleModel vehicle = null,
		GarageModel garage = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RepairOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.VehicleCode)] = new() { DisplayName = "Vehicle", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.GarageName)] = new() { DisplayName = "Garage", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.CurrentKM)] = new() { DisplayName = "Current KM", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RepairOverviewModel.GarageInDateTime)] = new() { DisplayName = "Garage In", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairOverviewModel.GarageOutDateTime)] = new() { DisplayName = "Garage Out", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairOverviewModel.TotalItems)] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RepairOverviewModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RepairOverviewModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RepairOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(RepairOverviewModel.TransactionNo),
				nameof(RepairOverviewModel.TransactionDateTime),
				nameof(RepairOverviewModel.CompanyName),
				nameof(RepairOverviewModel.VehicleCode),
				nameof(RepairOverviewModel.GarageName),
				nameof(RepairOverviewModel.CurrentKM),
				nameof(RepairOverviewModel.GarageInDateTime),
				nameof(RepairOverviewModel.GarageOutDateTime),
				nameof(RepairOverviewModel.TotalItems),
				nameof(RepairOverviewModel.TotalQuantity),
				nameof(RepairOverviewModel.TotalAmount),
				nameof(RepairOverviewModel.FinancialYear),
				nameof(RepairOverviewModel.Remarks),
				nameof(RepairOverviewModel.CreatedByName),
				nameof(RepairOverviewModel.CreatedAt),
				nameof(RepairOverviewModel.CreatedFromPlatform),
				nameof(RepairOverviewModel.LastModifiedByUserName),
				nameof(RepairOverviewModel.LastModifiedAt),
				nameof(RepairOverviewModel.LastModifiedFromPlatform),
				nameof(RepairOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(RepairOverviewModel.Status));
		}
		else
		{
			columnOrder =
			[
				nameof(RepairOverviewModel.TransactionNo),
				nameof(RepairOverviewModel.TransactionDateTime),
				nameof(RepairOverviewModel.VehicleCode),
				nameof(RepairOverviewModel.GarageName),
				nameof(RepairOverviewModel.TotalItems),
				nameof(RepairOverviewModel.TotalAmount),
				nameof(RepairOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(RepairOverviewModel.Status));
		}

		string fileName = "REPAIR_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var filters = new Dictionary<string, string>
		{
			["Company"] = company?.Name ?? null,
			["Vehicle"] = vehicle?.Code ?? null,
			["Garage"] = garage?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				repairData,
				"REPAIR REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns,
				filters
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				repairData,
				"REPAIR REPORT",
				"Repairs",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				filters
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportJobReport(
		IEnumerable<RepairJobOverviewModel> repairJobData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		CompanyModel company = null,
		VehicleModel vehicle = null,
		GarageModel garage = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RepairJobOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.VehicleCode)] = new() { DisplayName = "Vehicle", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.GarageName)] = new() { DisplayName = "Garage", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.Job)] = new() { DisplayName = "Job", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.Quantity)] = new() { DisplayName = "Quantity", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RepairJobOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RepairJobOverviewModel.JobRemarks)] = new() { DisplayName = "Job Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.CurrentKM)] = new() { DisplayName = "Current KM", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.GarageInDateTime)] = new() { DisplayName = "Garage In", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.GarageOutDateTime)] = new() { DisplayName = "Garage Out", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.Remarks)] = new() { DisplayName = "Trans Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RepairJobOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(RepairJobOverviewModel.TransactionNo),
				nameof(RepairJobOverviewModel.TransactionDateTime),
				nameof(RepairJobOverviewModel.CompanyName),
				nameof(RepairJobOverviewModel.VehicleCode),
				nameof(RepairJobOverviewModel.GarageName),
				nameof(RepairJobOverviewModel.Job),
				nameof(RepairJobOverviewModel.Quantity),
				nameof(RepairJobOverviewModel.Rate),
				nameof(RepairJobOverviewModel.Total),
				nameof(RepairJobOverviewModel.JobRemarks),
				nameof(RepairJobOverviewModel.CurrentKM),
				nameof(RepairJobOverviewModel.GarageInDateTime),
				nameof(RepairJobOverviewModel.GarageOutDateTime),
				nameof(RepairJobOverviewModel.FinancialYear),
				nameof(RepairJobOverviewModel.Remarks),
				nameof(RepairJobOverviewModel.CreatedByName),
				nameof(RepairJobOverviewModel.CreatedAt),
				nameof(RepairJobOverviewModel.CreatedFromPlatform),
				nameof(RepairJobOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(RepairJobOverviewModel.Status));
		}
		else
		{
			columnOrder =
			[
				nameof(RepairJobOverviewModel.TransactionNo),
				nameof(RepairJobOverviewModel.TransactionDateTime),
				nameof(RepairJobOverviewModel.VehicleCode),
				nameof(RepairJobOverviewModel.Job),
				nameof(RepairJobOverviewModel.Quantity),
				nameof(RepairJobOverviewModel.Rate),
				nameof(RepairJobOverviewModel.Total),
				nameof(RepairJobOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(RepairJobOverviewModel.Status));
		}

		string fileName = "REPAIR_DETAILS_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var filters = new Dictionary<string, string>
		{
			["Company"] = company?.Name ?? null,
			["Vehicle"] = vehicle?.Code ?? null,
			["Garage"] = garage?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				repairJobData,
				"REPAIR DETAILS REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns,
				filters
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				repairJobData,
				"REPAIR DETAILS REPORT",
				"Repair Jobs",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				filters
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
