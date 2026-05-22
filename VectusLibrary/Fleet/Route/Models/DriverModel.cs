namespace VectusLibrary.Fleet.Route.Models;

public class DriverModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Mobile { get; set; }
	public string Code { get; set; }
	public string? Remarks { get; set; }
	public string? LicenseUrl { get; set; }
	public string LicenseNo { get; set; }
	public DateTime LicenseExpiryDateTime { get; set; }
	public bool Status { get; set; }
}

public class DriverOverviewModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Mobile { get; set; }
	public string DisplayName => $"{Name} ({Mobile})";
	public string Code { get; set; }
	public string? Remarks { get; set; }
	public string LicenseNo { get; set; }
	public DateTime LicenseExpiryDateTime { get; set; }
	public bool Status { get; set; }
}