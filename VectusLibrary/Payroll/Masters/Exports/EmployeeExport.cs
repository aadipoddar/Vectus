using VectusLibrary.Common;
using VectusLibrary.Payroll.Masters.Models;
using VectusLibrary.Utils.ExportUtils;

namespace VectusLibrary.Payroll.Masters.Exports;

public static class EmployeeExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<EmployeeModel> employeeData,
		ReportExportType exportType)
	{
		var departments = await CommonData.LoadTableData<DepartmentModel>(PayrollNames.Department);
		var designations = await CommonData.LoadTableData<DesignationModel>(PayrollNames.Designation);
		var locations = await CommonData.LoadTableData<EmployeeLocationModel>(PayrollNames.EmployeeLocation);

		// PII (Aadhaar, bank account no, PAN) is deliberately excluded from general exports.
		var enrichedData = employeeData.Select(employee => new
		{
			employee.Id,
			employee.Code,
			employee.Name,
			Department = departments.FirstOrDefault(d => d.Id == employee.DepartmentId)?.Name ?? "N/A",
			Designation = designations.FirstOrDefault(d => d.Id == employee.DesignationId)?.Name ?? "N/A",
			Location = locations.FirstOrDefault(l => l.Id == employee.EmployeeLocationId)?.Name ?? "N/A",
			DateOfJoining = employee.DateOfJoining.ToString("dd-MM-yyyy"),
			DateOfLeaving = employee.DateOfLeaving?.ToString("dd-MM-yyyy") ?? "—",
			employee.PaymentMode,
			employee.PFUAN,
			employee.ESINumber,
			PF = employee.IsPFApplicable ? "Yes" : "No",
			ESI = employee.IsESIApplicable ? "Yes" : "No",
			PT = employee.IsPTApplicable ? "Yes" : "No",
			employee.Phone,
			Status = employee.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			["Id"] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			["Code"] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			["Name"] = new() { DisplayName = "Employee Name", Alignment = CellAlignment.Left, IsRequired = true },
			["Department"] = new() { DisplayName = "Department", Alignment = CellAlignment.Left },
			["Designation"] = new() { DisplayName = "Designation", Alignment = CellAlignment.Left },
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left },
			["DateOfJoining"] = new() { DisplayName = "Date of Joining", Alignment = CellAlignment.Center },
			["DateOfLeaving"] = new() { DisplayName = "Date of Leaving", Alignment = CellAlignment.Center },
			["PaymentMode"] = new() { DisplayName = "Payment Mode", Alignment = CellAlignment.Center },
			["PFUAN"] = new() { DisplayName = "PF UAN", Alignment = CellAlignment.Left },
			["ESINumber"] = new() { DisplayName = "ESI Number", Alignment = CellAlignment.Left },
			["PF"] = new() { DisplayName = "PF", Alignment = CellAlignment.Center },
			["ESI"] = new() { DisplayName = "ESI", Alignment = CellAlignment.Center },
			["PT"] = new() { DisplayName = "PT", Alignment = CellAlignment.Center },
			["Phone"] = new() { DisplayName = "Phone", Alignment = CellAlignment.Left },
			["Status"] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			"Id", "Code", "Name", "Department", "Designation", "Location",
			"DateOfJoining", "DateOfLeaving", "PaymentMode", "PFUAN", "ESINumber",
			"PF", "ESI", "PT", "Phone", "Status"
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Employee_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"EMPLOYEE MASTER",
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
				"EMPLOYEE",
				"Employee Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
