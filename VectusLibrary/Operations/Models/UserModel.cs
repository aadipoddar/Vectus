namespace VectusLibrary.Operations.Models;

public class UserModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Phone { get; set; }
	public string? Email { get; set; }
	public string Password { get; set; }
	public bool Accounts { get; set; }
	public bool Payroll { get; set; }
	public bool Fleet { get; set; }
	public bool Reports { get; set; }
	public bool Admin { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
	public int CodeResends { get; set; }
	public int FailedAttempts { get; set; }
	public int? LastCode { get; set; }
	public string? LastCodeDeviceId { get; set; }
	public DateTime? LastCodeDateTime { get; set; }
}

public enum UserRoles
{
	Accounts,
	Payroll,
	Fleet,
	Reports,
	Admin
}