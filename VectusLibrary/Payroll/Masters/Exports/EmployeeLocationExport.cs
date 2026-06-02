using VectusLibrary.Accounts.Masters.Models;
using VectusLibrary.Common;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Payroll.Masters.Exports;

public static class EmployeeLocationExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<EmployeeLocationModel> locationData,
		ReportExportType exportType)
	{
		var stateUTs = await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT);

		var enrichedData = locationData.Select(location => new
		{
			location.Id,
			location.Name,
			StateUT = stateUTs.FirstOrDefault(su => su.Id == location.StateUTId)?.Name ?? "N/A",
			location.Remarks,
			Status = location.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(EmployeeLocationModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(EmployeeLocationModel.Name)] = new() { DisplayName = "Employee Location Name", Alignment = CellAlignment.Left, IsRequired = true },
			["StateUT"] = new() { DisplayName = "State/UT", Alignment = CellAlignment.Left },
			[nameof(EmployeeLocationModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(EmployeeLocationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(EmployeeLocationModel.Id),
			nameof(EmployeeLocationModel.Name),
			"StateUT",
			nameof(EmployeeLocationModel.Remarks),
			nameof(EmployeeLocationModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"EmployeeLocation_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"EMPLOYEE LOCATION MASTER",
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
				"EMPLOYEE LOCATION",
				"Employee Location Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
