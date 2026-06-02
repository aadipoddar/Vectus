namespace VectusLibrary.Payroll.Masters.Models;

public class EmployeeModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }

	public int DepartmentId { get; set; }
	public int DesignationId { get; set; }
	public int EmployeeLocationId { get; set; }

	public DateTime DateOfJoining { get; set; }
	public DateTime? DateOfLeaving { get; set; }

	public string PaymentMode { get; set; }
	public string? BankName { get; set; }
	public string? BankAccountNo { get; set; }
	public string? IFSC { get; set; }

	public string? PANNo { get; set; }
	public string? AadhaarNo { get; set; }

	public string? PFUAN { get; set; }
	public string? PFNumber { get; set; }
	public string? ESINumber { get; set; }

	public bool IsPFApplicable { get; set; }
	public bool IsESIApplicable { get; set; }
	public bool IsPTApplicable { get; set; }
	public bool IsLWFApplicable { get; set; }
	public bool ContributeOnHigherPFWage { get; set; }
	public DateTime? ESICoveredUpto { get; set; }

	public string? Phone { get; set; }
	public string? PersonalEmail { get; set; }
	public string? WorkEmail { get; set; }
	public string? Address { get; set; }

	public string? Remarks { get; set; }

	public bool Status { get; set; }
}
