using VectusLibrary.Common;
using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;
using VectusLibrary.Payroll.Masters.Models;

namespace VectusLibrary.Payroll.Masters.Data;

public static class EmployeeData
{
	private static async Task<int> InsertEmployee(EmployeeModel employee, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertEmployee, employee, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Employee.");

	public static async Task DeleteTransaction(EmployeeModel employee, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			employee.Status = false;
			await InsertEmployee(employee, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.Employee,
				RecordNo = employee.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(EmployeeModel employee, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			employee.Status = true;
			await InsertEmployee(employee, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.Employee,
				RecordNo = employee.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(EmployeeModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.PaymentMode = string.IsNullOrWhiteSpace(item.PaymentMode) ? "Cash" : item.PaymentMode.Trim();
		item.BankName = string.IsNullOrWhiteSpace(item.BankName) ? null : item.BankName.Trim();
		item.BankAccountNo = string.IsNullOrWhiteSpace(item.BankAccountNo) ? null : item.BankAccountNo.Trim();
		item.IFSC = string.IsNullOrWhiteSpace(item.IFSC) ? null : item.IFSC.Trim().ToUpper();
		item.PANNo = string.IsNullOrWhiteSpace(item.PANNo) ? null : item.PANNo.Trim().ToUpper();
		item.AadhaarNo = string.IsNullOrWhiteSpace(item.AadhaarNo) ? null : item.AadhaarNo.Trim().RemoveSpace();
		item.PFUAN = string.IsNullOrWhiteSpace(item.PFUAN) ? null : item.PFUAN.Trim();
		item.PFNumber = string.IsNullOrWhiteSpace(item.PFNumber) ? null : item.PFNumber.Trim();
		item.ESINumber = string.IsNullOrWhiteSpace(item.ESINumber) ? null : item.ESINumber.Trim();
		item.Phone = string.IsNullOrWhiteSpace(item.Phone) ? null : item.Phone.Trim();
		item.Email = string.IsNullOrWhiteSpace(item.Email) ? null : item.Email.Trim();
		item.Address = string.IsNullOrWhiteSpace(item.Address) ? null : item.Address.Trim();
		item.DateOfLeaving = item.DateOfLeaving == default(DateTime) ? null : item.DateOfLeaving;
		item.ESICoveredUpto = item.ESICoveredUpto == default(DateTime) ? null : item.ESICoveredUpto;
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Employee name is required. Please enter a valid employee name.");

		if (item.DepartmentId <= 0)
			throw new Exception("Department is required. Please select a valid department.");

		if (item.DesignationId <= 0)
			throw new Exception("Designation is required. Please select a valid designation.");

		if (item.EmployeeLocationId <= 0)
			throw new Exception("Location is required. Please select a valid employee location.");

		if (item.DateOfJoining == default)
			throw new Exception("Date of joining is required. Please select a valid date.");

		if (item.DateOfLeaving is not null && item.DateOfLeaving < item.DateOfJoining)
			throw new Exception("Date of leaving cannot be earlier than the date of joining.");

		if (item.PaymentMode is not ("Bank" or "Cash"))
			throw new Exception("Payment mode must be either 'Bank' or 'Cash'.");

		if (item.IFSC is not null && !item.IFSC.ValidateIFSC())
			throw new Exception("Invalid IFSC format. Please enter a valid IFSC (e.g. SBIN0001234).");

		if (item.PANNo is not null && !item.PANNo.ValidatePAN())
			throw new Exception("Invalid PAN format. Please enter a valid PAN (e.g. ABCDE1234F).");

		if (item.AadhaarNo is not null && !item.AadhaarNo.ValidateAadhaar())
			throw new Exception("Invalid Aadhaar format. Please enter a valid 12-digit Aadhaar number.");

		if (item.Phone is not null && !item.Phone.ValidatePhoneNumber())
			throw new Exception("Invalid phone number format. Please enter a valid 10-digit phone number.");

		if (item.Email is not null && !item.Email.ValidateEmail())
			throw new Exception("Invalid email format. Please enter a valid email address.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateEmployeeCode();

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Employee code is required. Please try again.");

		var allEmployees = await CommonData.LoadTableData<EmployeeModel>(PayrollNames.Employee);

		var existingByCode = allEmployees.FirstOrDefault(e => e.Id != item.Id && e.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Employee code '{item.Code}' already exists. Please try again.");

		if (item.PANNo is not null)
		{
			var duplicatePAN = allEmployees.FirstOrDefault(e => e.Id != item.Id && e.PANNo != null && e.PANNo.Equals(item.PANNo, StringComparison.OrdinalIgnoreCase));
			if (duplicatePAN is not null)
				throw new Exception($"PAN '{item.PANNo}' is already associated with another employee.");
		}

		if (item.AadhaarNo is not null)
		{
			var duplicateAadhaar = allEmployees.FirstOrDefault(e => e.Id != item.Id && e.AadhaarNo != null && e.AadhaarNo.Equals(item.AadhaarNo, StringComparison.OrdinalIgnoreCase));
			if (duplicateAadhaar is not null)
				throw new Exception("This Aadhaar number is already associated with another employee.");
		}
	}

	public static async Task<int> SaveTransaction(EmployeeModel employee, int userId, string platform)
	{
		await ValidateTransaction(employee);

		var isUpdate = employee.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, employee.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertEmployee(employee, transaction);
			var diff = AuditTrailData.GetDifference(previous, employee);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.Employee,
				RecordNo = employee.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
