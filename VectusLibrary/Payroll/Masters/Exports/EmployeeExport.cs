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
			employee.BankName,
			employee.BankAccountNo,
			employee.IFSC,
			employee.PANNo,
			Aadhaar = MaskAadhaar(employee.AadhaarNo),
			employee.PFUAN,
			employee.PFNumber,
			employee.ESINumber,
			ESICoveredUpto = employee.ESICoveredUpto?.ToString("dd-MM-yyyy") ?? "—",
			PF = employee.IsPFApplicable ? "Yes" : "No",
			ESI = employee.IsESIApplicable ? "Yes" : "No",
			PT = employee.IsPTApplicable ? "Yes" : "No",
			LWF = employee.IsLWFApplicable ? "Yes" : "No",
			HigherPFWage = employee.ContributeOnHigherPFWage ? "Yes" : "No",
			employee.Phone,
			employee.PersonalEmail,
			employee.WorkEmail,
			employee.Address,
			employee.Remarks,
			Status = employee.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(EmployeeModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(EmployeeModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(EmployeeModel.Name)] = new() { DisplayName = "Employee Name", Alignment = CellAlignment.Left, IsRequired = true },
			["Department"] = new() { DisplayName = "Department", Alignment = CellAlignment.Left },
			["Designation"] = new() { DisplayName = "Designation", Alignment = CellAlignment.Left },
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.DateOfJoining)] = new() { DisplayName = "Date of Joining", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.DateOfLeaving)] = new() { DisplayName = "Date of Leaving", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.PaymentMode)] = new() { DisplayName = "Payment Mode", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.BankName)] = new() { DisplayName = "Bank Name", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.BankAccountNo)] = new() { DisplayName = "Bank Account No", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.IFSC)] = new() { DisplayName = "IFSC", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.PANNo)] = new() { DisplayName = "PAN No", Alignment = CellAlignment.Left },
			["Aadhaar"] = new() { DisplayName = "Aadhaar", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.PFUAN)] = new() { DisplayName = "PF UAN", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.PFNumber)] = new() { DisplayName = "PF Number", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.ESINumber)] = new() { DisplayName = "ESI Number", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.ESICoveredUpto)] = new() { DisplayName = "ESI Covered Upto", Alignment = CellAlignment.Center },
			["PF"] = new() { DisplayName = "PF", Alignment = CellAlignment.Center },
			["ESI"] = new() { DisplayName = "ESI", Alignment = CellAlignment.Center },
			["PT"] = new() { DisplayName = "PT", Alignment = CellAlignment.Center },
			["LWF"] = new() { DisplayName = "LWF", Alignment = CellAlignment.Center },
			["HigherPFWage"] = new() { DisplayName = "Higher PF Wage", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.Phone)] = new() { DisplayName = "Phone", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.PersonalEmail)] = new() { DisplayName = "Personal Email", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.WorkEmail)] = new() { DisplayName = "Work Email", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Address)] = new() { DisplayName = "Address", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(EmployeeModel.Id),
			nameof(EmployeeModel.Code),
			nameof(EmployeeModel.Name),
			"Department",
			"Designation",
			"Location",
			nameof(EmployeeModel.DateOfJoining),
			nameof(EmployeeModel.DateOfLeaving),
			nameof(EmployeeModel.PaymentMode),
			nameof(EmployeeModel.BankName),
			nameof(EmployeeModel.BankAccountNo),
			nameof(EmployeeModel.IFSC),
			nameof(EmployeeModel.PANNo),
			"Aadhaar",
			nameof(EmployeeModel.PFUAN),
			nameof(EmployeeModel.PFNumber),
			nameof(EmployeeModel.ESINumber),
			nameof(EmployeeModel.ESICoveredUpto),
			"PF",
			"ESI",
			"PT",
			"LWF",
			"HigherPFWage",
			nameof(EmployeeModel.Phone),
			nameof(EmployeeModel.PersonalEmail),
			nameof(EmployeeModel.WorkEmail),
			nameof(EmployeeModel.Address),
			nameof(EmployeeModel.Remarks),
			nameof(EmployeeModel.Status)
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

	private static string MaskAadhaar(string aadhaar) =>
		string.IsNullOrWhiteSpace(aadhaar) ? "—" : $"XXXX XXXX {aadhaar[^4..]}";
}
