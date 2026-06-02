namespace VectusLibrary.Payroll.Masters.Models;

public class SalaryStructureModel
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public DateTime EffectiveFrom { get; set; }
	public decimal? CTC { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class SalaryStructureLineModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public int SalaryComponentId { get; set; }
	public decimal? Amount { get; set; }
	public decimal? OverridePercentage { get; set; }
	public bool Status { get; set; }
}

public class SalaryStructureLineCartModel
{
	public int SalaryComponentId { get; set; }
	public string SalaryComponentCode { get; set; }
	public string SalaryComponentName { get; set; }
	public string ComponentType { get; set; }
	public string CalculationType { get; set; }
	public decimal? Amount { get; set; }
	public decimal? OverridePercentage { get; set; }
}
