namespace VectusLibrary.Payroll.Masters.Models;

public class StatutoryRuleModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public string? ContributionAccount { get; set; }
	public string RoundingMode { get; set; }
	public int? LedgerId { get; set; }
	public int? StateUTId { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class StatutoryRateModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public DateTime EffectiveFrom { get; set; }
	public decimal? EmployeeRate { get; set; }
	public decimal? EmployerRate { get; set; }
	public decimal? WageCeiling { get; set; }
	public decimal? MaxAmount { get; set; }
	public decimal? MinAmount { get; set; }
	public decimal? MinBasePercentOfGross { get; set; }
	public decimal? StandardDeduction { get; set; }
	public decimal? RebateAmount { get; set; }
	public decimal? RebateIncomeLimit { get; set; }
	public decimal? CessPercent { get; set; }
	public bool Status { get; set; }
}

public class StatutorySlabModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public decimal FromAmount { get; set; }
	public decimal? ToAmount { get; set; }
	public decimal? FixedAmount { get; set; }
	public decimal? Rate { get; set; }
	public bool Status { get; set; }
}

public class StatutoryRateCartModel
{
	public int Id { get; set; }
	public DateTime EffectiveFrom { get; set; } = DateTime.Today;
	public decimal? EmployeeRate { get; set; }
	public decimal? EmployerRate { get; set; }
	public decimal? WageCeiling { get; set; }
	public decimal? MaxAmount { get; set; }
	public decimal? MinAmount { get; set; }
	public decimal? MinBasePercentOfGross { get; set; }
	public decimal? StandardDeduction { get; set; }
	public decimal? RebateAmount { get; set; }
	public decimal? RebateIncomeLimit { get; set; }
	public decimal? CessPercent { get; set; }
	public List<StatutorySlabCartModel> Slabs { get; set; } = [];
}

public class StatutorySlabCartModel
{
	public decimal FromAmount { get; set; }
	public decimal? ToAmount { get; set; }
	public decimal? FixedAmount { get; set; }
	public decimal? Rate { get; set; }
}
