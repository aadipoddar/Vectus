using VectusLibrary.Common;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Route.Exports;

public static class DriverExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<DriverModel> driverData,
		ReportExportType exportType)
	{
		var enrichedData = driverData.Select(driver => new
		{
			driver.Id,
			driver.Name,
			driver.Mobile,
			driver.Code,
			driver.Remarks,
			driver.LicenseNo,
			LicenseExpiryDateTime = driver.LicenseExpiryDateTime?.ToString("dd/MM/yyyy"),
			Status = driver.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DriverModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DriverModel.Name)] = new() { DisplayName = "Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DriverModel.Mobile)] = new() { DisplayName = "Mobile", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DriverModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DriverModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DriverModel.LicenseNo)] = new() { DisplayName = "License No", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DriverModel.LicenseExpiryDateTime)] = new() { DisplayName = "License Expiry", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DriverModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DriverModel.Id),
			nameof(DriverModel.Name),
			nameof(DriverModel.Mobile),
			nameof(DriverModel.Code),
			nameof(DriverModel.Remarks),
			nameof(DriverModel.LicenseNo),
			nameof(DriverModel.LicenseExpiryDateTime),
			nameof(DriverModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Driver_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DRIVER MASTER",
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
				"DRIVER MASTER",
				"Driver Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}