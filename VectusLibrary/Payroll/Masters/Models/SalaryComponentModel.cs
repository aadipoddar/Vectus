namespace VectusLibrary.Payroll.Masters.Models;

public class SalaryComponentModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }

	public string ComponentType { get; set; }
	public string CalculationType { get; set; }

	public string? BaseComponentCode { get; set; }
	public decimal? Percentage { get; set; }
	public string? StatutoryRuleCode { get; set; }

	public bool IsTaxable { get; set; }
	public bool IsPerquisite { get; set; }
	public bool AffectsPFBase { get; set; }
	public bool AffectsESIBase { get; set; }

	public int DisplayOrder { get; set; }

	public string? Remarks { get; set; }

	public bool Status { get; set; }
}
