using VectusLibrary.Common;
using VectusLibrary.Fleet.Tyre.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Fleet.Tyre.Exports;

public static class TyreCompanyExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<TyreCompanyModel> tyreCompanyData,
		ReportExportType exportType)
	{
		var enrichedData = tyreCompanyData.Select(tyreCompany => new
		{
			tyreCompany.Id,
			tyreCompany.Name,
			tyreCompany.Code,
			tyreCompany.Remarks,
			Status = tyreCompany.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(TyreCompanyModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TyreCompanyModel.Name)] = new() { DisplayName = "Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(TyreCompanyModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(TyreCompanyModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(TyreCompanyModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(TyreCompanyModel.Id),
			nameof(TyreCompanyModel.Name),
			nameof(TyreCompanyModel.Code),
			nameof(TyreCompanyModel.Remarks),
			nameof(TyreCompanyModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Tyre_Company_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"TYRE COMPANY MASTER",
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
				"TYRE COMPANY",
				"Tyre Company Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
