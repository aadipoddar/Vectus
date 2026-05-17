using VectusLibrary.Common;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Vehicle.Exports;

public static class VehicleTypeExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<VehicleTypeModel> vehicleTypeData,
		ReportExportType exportType)
	{
		var enrichedData = vehicleTypeData.Select(vehicleType => new
		{
			vehicleType.Id,
			vehicleType.Name,
			vehicleType.Code,
			vehicleType.Remarks,
			Status = vehicleType.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(VehicleTypeModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleTypeModel.Name)] = new() { DisplayName = "Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VehicleTypeModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VehicleTypeModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(VehicleTypeModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(VehicleTypeModel.Id),
			nameof(VehicleTypeModel.Name),
			nameof(VehicleTypeModel.Code),
			nameof(VehicleTypeModel.Remarks),
			nameof(VehicleTypeModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Vehicle_Type_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"VEHICLE TYPE MASTER",
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
				"VEHICLE TYPE",
				"Vehicle Type Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}