using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.TripRequest.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.TripRequest.Exports;

public static class TripRequestReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<TripRequestOverviewModel> tripRequestData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		CompanyModel company = null,
		VehicleModel vehicle = null,
		RouteOverviewModel route = null,
		SDRModel sdr = null,
		string requestStatus = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(TripRequestOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.RouteCode)] = new() { DisplayName = "Route Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.FromLocation)] = new() { DisplayName = "From", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.ToLocation)] = new() { DisplayName = "To", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.RouteDisplay)] = new() { DisplayName = "Route", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.VehicleCode)] = new() { DisplayName = "Vehicle", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.SDRName)] = new() { DisplayName = "SDR", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.RequestStatus)] = new() { DisplayName = "Request Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TripRequestOverviewModel.EstimatedDistance)] = new() { DisplayName = "Est. Dist", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(TripRequestOverviewModel.EstimatedHours)] = new() { DisplayName = "Est. Hours", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(TripRequestOverviewModel.EstimatedFuelConsumption)] = new() { DisplayName = "Est. Fuel", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(TripRequestOverviewModel.EstimatedCost)] = new() { DisplayName = "Est. Cost", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(TripRequestOverviewModel.TransactionNo),
				nameof(TripRequestOverviewModel.TransactionDateTime),
				nameof(TripRequestOverviewModel.CompanyName),
				nameof(TripRequestOverviewModel.RouteCode),
				nameof(TripRequestOverviewModel.FromLocation),
				nameof(TripRequestOverviewModel.ToLocation),
				nameof(TripRequestOverviewModel.RouteDisplay),
				nameof(TripRequestOverviewModel.VehicleCode),
				nameof(TripRequestOverviewModel.SDRName),
				nameof(TripRequestOverviewModel.RequestStatus),
				nameof(TripRequestOverviewModel.EstimatedDistance),
				nameof(TripRequestOverviewModel.EstimatedHours),
				nameof(TripRequestOverviewModel.EstimatedFuelConsumption),
				nameof(TripRequestOverviewModel.EstimatedCost),
				nameof(TripRequestOverviewModel.FinancialYear),
				nameof(TripRequestOverviewModel.Remarks),
				nameof(TripRequestOverviewModel.CreatedByName),
				nameof(TripRequestOverviewModel.CreatedAt),
				nameof(TripRequestOverviewModel.CreatedFromPlatform),
				nameof(TripRequestOverviewModel.LastModifiedByUserName),
				nameof(TripRequestOverviewModel.LastModifiedAt),
				nameof(TripRequestOverviewModel.LastModifiedFromPlatform),
				nameof(TripRequestOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(TripRequestOverviewModel.Status));
		}
		else
		{
			columnOrder =
			[
				nameof(TripRequestOverviewModel.TransactionNo),
				nameof(TripRequestOverviewModel.TransactionDateTime),
				nameof(TripRequestOverviewModel.VehicleCode),
				nameof(TripRequestOverviewModel.RouteDisplay),
				nameof(TripRequestOverviewModel.RequestStatus),
				nameof(TripRequestOverviewModel.EstimatedDistance),
				nameof(TripRequestOverviewModel.EstimatedCost),
				nameof(TripRequestOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(TripRequestOverviewModel.Status));
		}

		string fileName = "TRIP_REQUEST_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var filters = new Dictionary<string, string>
		{
			["Company"] = company?.Name ?? null,
			["Vehicle"] = vehicle?.Code ?? null,
			["Route"] = route?.RouteDisplay ?? null,
			["SDR"] = sdr?.Name ?? null,
			["Request Status"] = requestStatus
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				tripRequestData,
				"TRIP REQUEST REPORT",
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
				tripRequestData,
				"TRIP REQUEST REPORT",
				"Trip Requests",
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
