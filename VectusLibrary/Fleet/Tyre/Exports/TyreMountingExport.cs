using VectusLibrary.Common;
using VectusLibrary.Fleet.Tyre.Models;
using VectusLibrary.Fleet.Vehicle.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Tyre.Exports;

public static class TyreMountingExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportTransaction(
		IEnumerable<TyreMountingModel> tyreMountingData,
		ReportExportType exportType)
	{
		var tyreCompanies = await CommonData.LoadTableData<TyreCompanyModel>(FleetNames.TyreCompany);
		var vehicles = await CommonData.LoadTableData<VehicleModel>(FleetNames.Vehicle);

		var enrichedData = tyreMountingData.Select(tyreMounting => new
		{
			tyreMounting.Id,
			tyreMounting.TyreNo,
			TyreCompany = tyreCompanies.FirstOrDefault(tc => tc.Id == tyreMounting.TyreCompanyId)?.Name ?? "N/A",
			tyreMounting.TyreModel,
			Vehicle = vehicles.FirstOrDefault(v => v.Id == tyreMounting.VehicleId)?.Code ?? "N/A",
			tyreMounting.MountingKM,
			tyreMounting.DismountingKM,
			tyreMounting.MountingDateTime,
			tyreMounting.DismountingDateTime,
			DistanceCovered = tyreMounting.DismountingKM.HasValue
				? $"{tyreMounting.DismountingKM.Value - tyreMounting.MountingKM:N0} KM"
				: "In Service",
			DaysServed = tyreMounting.DismountingDateTime.HasValue
				? $"{(tyreMounting.DismountingDateTime.Value.Date - tyreMounting.MountingDateTime.Date).Days} {((tyreMounting.DismountingDateTime.Value.Date - tyreMounting.MountingDateTime.Date).Days == 1 ? "day" : "days")}"
				: "In Service",
			tyreMounting.Remarks
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(TyreMountingModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TyreMountingModel.TyreNo)] = new() { DisplayName = "Tyre No", Alignment = CellAlignment.Left, IsRequired = true, IncludeInTotal = false },
			["TyreCompany"] = new() { DisplayName = "Tyre Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TyreMountingModel.TyreModel)] = new() { DisplayName = "Tyre Model", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["Vehicle"] = new() { DisplayName = "Vehicle", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TyreMountingModel.MountingKM)] = new() { DisplayName = "Mounting KM", Alignment = CellAlignment.Right, Format = "#,##0", IncludeInTotal = false },
			[nameof(TyreMountingModel.DismountingKM)] = new() { DisplayName = "Dismounting KM", Alignment = CellAlignment.Right, Format = "#,##0", IncludeInTotal = false },
			[nameof(TyreMountingModel.MountingDateTime)] = new() { DisplayName = "Mounting Date", Alignment = CellAlignment.Center, Format = "dd-MMM-yyyy", IncludeInTotal = false },
			[nameof(TyreMountingModel.DismountingDateTime)] = new() { DisplayName = "Dismounting Date", Alignment = CellAlignment.Center, Format = "dd-MMM-yyyy", IncludeInTotal = false },
			["DistanceCovered"] = new() { DisplayName = "Distance Covered", Alignment = CellAlignment.Right, IncludeInTotal = false },
			["DaysServed"] = new() { DisplayName = "Days Served", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(TyreMountingModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(TyreMountingModel.Id),
			nameof(TyreMountingModel.TyreNo),
			"TyreCompany",
			nameof(TyreMountingModel.TyreModel),
			"Vehicle",
			nameof(TyreMountingModel.MountingKM),
			nameof(TyreMountingModel.DismountingKM),
			nameof(TyreMountingModel.MountingDateTime),
			nameof(TyreMountingModel.DismountingDateTime),
			"DistanceCovered",
			"DaysServed",
			nameof(TyreMountingModel.Remarks)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Tyre_Mounting_Transaction_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"TYRE MOUNTING TRANSACTION",
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			return (stream, fileName + ".pdf");
		}

		var excelStream = await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"TYRE MOUNTING TRANSACTION",
			"Tyre Mounting Data",
			null,
			null,
			columnSettings,
			columnOrder
		);

		return (excelStream, fileName + ".xlsx");
	}
}
