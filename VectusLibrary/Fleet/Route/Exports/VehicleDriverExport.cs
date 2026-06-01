using VectusLibrary.Common;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Route.Exports;

public static class VehicleDriverExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<VehicleDriverModel> vehicleDriver,
		ReportExportType exportType)
	{
		var drivers = await CommonData.LoadTableData<DriverModel>(FleetNames.Driver);
		var vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);

		var enrichedData = vehicleDriver.Select(vd => new
		{
			vd.Id,
			Vehicle = vehicles.FirstOrDefault(v => v.Id == vd.VehicleId)?.Code ?? "N/A",
			Driver = drivers.FirstOrDefault(d => d.Id == vd.DriverId)?.Name ?? "N/A",
			StartDateTime = vd.StartDateTime.ToString("dd-MMM-yyyy"),
			EndDateTime = vd.EndDateTime?.ToString("dd-MMM-yyyy") ?? "N/A",
			vd.Remarks
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(VehicleDriverModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			["Vehicle"] = new() { DisplayName = "Vehicle", Alignment = CellAlignment.Left, IsRequired = true },
			["Driver"] = new() { DisplayName = "Driver", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VehicleDriverModel.StartDateTime)] = new() { DisplayName = "Start", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleDriverModel.EndDateTime)] = new() { DisplayName = "End", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleDriverModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left }
		};

		List<string> columnOrder =
		[
			nameof(VehicleDriverModel.Id),
			"Vehicle",
			"Driver",
			nameof(VehicleDriverModel.StartDateTime),
			nameof(VehicleDriverModel.EndDateTime),
			nameof(VehicleDriverModel.Remarks)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Vehicle_Driver_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"VEHICLE DRIVER MASTER",
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
				"VEHICLE DRIVER",
				"Vehicle Driver Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
