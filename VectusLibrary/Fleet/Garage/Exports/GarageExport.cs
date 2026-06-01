using VectusLibrary.Common;
using VectusLibrary.Fleet.Garage.Models;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Garage.Exports;

public static class GarageExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<GarageModel> garage,
		ReportExportType exportType)
	{
		var locations = await CommonData.LoadTableData<LocationModel>(FleetNames.Location);

		var enrichedData = garage.Select(route => new
		{
			route.Id,
			route.Name,
			route.Code,
			Location = locations.FirstOrDefault(rl => rl.Id == route.LocationId)?.Name ?? "N/A",
			route.Remarks,
			Status = route.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(GarageModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(GarageModel.Name)] = new() { DisplayName = "Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(GarageModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(GarageModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(GarageModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(GarageModel.Id),
			nameof(GarageModel.Name),
			nameof(GarageModel.Code),
			"Location",
			nameof(GarageModel.Remarks),
			nameof(GarageModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Vehicle_Garage_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"GARAGE MASTER",
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
				"GARAGE",
				"Garage Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
