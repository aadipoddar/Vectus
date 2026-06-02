namespace VectusLibrary.Payroll.Masters.Models;

public class EmployeeLocationModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int? StateUTId { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
