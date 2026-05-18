using VectusLibrary.Common;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Route.Exports;

public static class LocationExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<LocationModel> locationData,
		ReportExportType exportType)
	{
		var enrichedData = locationData.Select(location => new
		{
			location.Id,
			location.Name,
			location.Code,
			location.Latitude,
			location.Longitude,
			location.Remarks,
			Status = location.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(LocationModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(LocationModel.Name)] = new() { DisplayName = "Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(LocationModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(LocationModel.Latitude)] = new() { DisplayName = "Latitude", Alignment = CellAlignment.Right, Format = "0.0000000000", IncludeInTotal = false },
			[nameof(LocationModel.Longitude)] = new() { DisplayName = "Longitude", Alignment = CellAlignment.Right, Format = "0.0000000000", IncludeInTotal = false },
			[nameof(LocationModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(LocationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(LocationModel.Id),
			nameof(LocationModel.Name),
			nameof(LocationModel.Code),
			nameof(LocationModel.Latitude),
			nameof(LocationModel.Longitude),
			nameof(LocationModel.Remarks),
			nameof(LocationModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Route_Location_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"LOCATION MASTER",
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
				"LOCATION",
				"Location Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
