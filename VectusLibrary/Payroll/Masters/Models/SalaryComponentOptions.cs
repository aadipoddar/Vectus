namespace VectusLibrary.Payroll.Masters.Models;

public static class SalaryComponentOptions
{
	public static readonly string[] ComponentTypes = ["Earning", "Deduction"];

	public static readonly string[] CalculationTypes = ["Fixed", "PercentOfBase", "Slab", "StatutoryRule", "AdvanceRecovery", "Arrear"];
}
