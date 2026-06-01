using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Vehicle.Exports;

public static class VehicleExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<VehicleModel> vehicleData,
		ReportExportType exportType)
	{
		var vehicleTypes = await CommonData.LoadTableData<VehicleTypeModel>(FleetNames.VehicleType);
		var companies = await CommonData.LoadTableData<CompanyModel>(AccountNames.Company);
		var sdrs = await CommonData.LoadTableData<SDRModel>(FleetNames.SDR);

		var enrichedData = vehicleData.Select(vehicle => new
		{
			vehicle.Id,
			vehicle.Code,
			vehicle.ShortCode,
			vehicle.ChasisCode,
			vehicle.EngineCode,
			PurchaseDate = vehicle.PurchaseDate.ToString("dd-MMM-yyyy"),
			vehicle.OpeningKM,
			VehicleType = vehicleTypes.FirstOrDefault(vt => vt.Id == vehicle.VehicleTypeId)?.Name ?? "N/A",
			Company = companies.FirstOrDefault(c => c.Id == vehicle.CompanyId)?.Name ?? "N/A",
			SDR = sdrs.FirstOrDefault(o => o.Id == vehicle.SDRId)?.Name ?? "N/A",
			vehicle.Remarks,
			AvailableStatus = vehicle.AvailableStatus ? "Available" : "Unavailable",
			Status = vehicle.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(VehicleModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VehicleModel.ShortCode)] = new() { DisplayName = "Short Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VehicleModel.ChasisCode)] = new() { DisplayName = "Chasis Code", Alignment = CellAlignment.Left },
			[nameof(VehicleModel.EngineCode)] = new() { DisplayName = "Engine Code", Alignment = CellAlignment.Left },
			[nameof(VehicleModel.PurchaseDate)] = new() { DisplayName = "Purchase Date", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleModel.OpeningKM)] = new() { DisplayName = "Opening KM", Alignment = CellAlignment.Right, Format = "#,##0.00" },
			["VehicleType"] = new() { DisplayName = "Vehicle Type", Alignment = CellAlignment.Left },
			["Company"] = new() { DisplayName = "Company", Alignment = CellAlignment.Left },
			["SDR"] = new() { DisplayName = "SDR", Alignment = CellAlignment.Left },
			[nameof(VehicleModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(VehicleModel.AvailableStatus)] = new() { DisplayName = "Available Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VehicleModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(VehicleModel.Id),
			nameof(VehicleModel.Code),
			nameof(VehicleModel.ShortCode),
			nameof(VehicleModel.ChasisCode),
			nameof(VehicleModel.EngineCode),
			nameof(VehicleModel.PurchaseDate),
			nameof(VehicleModel.OpeningKM),
			"VehicleType",
			"Company",
			"SDR",
			nameof(VehicleModel.Remarks),
			nameof(VehicleModel.AvailableStatus),
			nameof(VehicleModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Vehicle_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"VEHICLE MASTER",
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"VEHICLE",
				"Vehicle Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}