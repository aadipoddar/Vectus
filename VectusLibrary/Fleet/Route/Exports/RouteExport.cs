using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Route.Exports;

public static class RouteExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<RouteModel> route,
		ReportExportType exportType)
	{
		var locations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location);

		var enrichedData = route.Select(route => new
		{
			route.Id,
			FromLocation = locations.FirstOrDefault(rl => rl.Id == route.FromLocationId)?.Name ?? "N/A",
			ToLocation = locations.FirstOrDefault(rl => rl.Id == route.ToLocationId)?.Name ?? "N/A",
			route.Code,
			route.EstimatedHours,
			route.EstimatedDistance,
			route.EstimatedFuelConsumption,
			route.EstimatedCost,
			route.Remarks,
			Status = route.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RouteModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			["FromLocation"] = new() { DisplayName = "From Location", Alignment = CellAlignment.Left, IsRequired = true },
			["ToLocation"] = new() { DisplayName = "To Location", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(RouteModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(RouteModel.EstimatedHours)] = new() { DisplayName = "Estimated Hours", Alignment = CellAlignment.Right, Format = "#,##0", IncludeInTotal = false },
			[nameof(RouteModel.EstimatedDistance)] = new() { DisplayName = "Estimated Distance", Alignment = CellAlignment.Right, Format = "#,##0", IncludeInTotal = false },
			[nameof(RouteModel.EstimatedFuelConsumption)] = new() { DisplayName = "Estimated Fuel", Alignment = CellAlignment.Right, Format = "#,##0", IncludeInTotal = false },
			[nameof(RouteModel.EstimatedCost)] = new() { DisplayName = "Estimated Cost", Alignment = CellAlignment.Right, Format = "#,##0.00", IncludeInTotal = false },
			[nameof(RouteModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(RouteModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(RouteModel.Id),
			"FromLocation",
			"ToLocation",
			nameof(RouteModel.Code),
			nameof(RouteModel.EstimatedHours),
			nameof(RouteModel.EstimatedDistance),
			nameof(RouteModel.EstimatedFuelConsumption),
			nameof(RouteModel.EstimatedCost),
			nameof(RouteModel.Remarks),
			nameof(RouteModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Vehicle_Route_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"ROUTE MASTER",
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
				"ROUTE",
				"Route Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}