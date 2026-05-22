namespace VectusLibrary.Fleet.Route.Models;

public class VehicleDriverModel
{
	public int Id { get; set; }
	public int VehicleId { get; set; }
	public int DriverId { get; set; }
	public DateTime StartDateTime { get; set; }
	public DateTime? EndDateTime { get; set; }
	public string? Remarks { get; set; }
}

public class VehicleDriverOverviewModel
{
	public int Id { get; set; }
	public int VehicleId { get; set; }
	public string VehicleCode { get; set; }
	public int DriverId { get; set; }
	public string DriverName { get; set; }
	public DateTime StartDateTime { get; set; }
	public DateTime? EndDateTime { get; set; }
	public string? Remarks { get; set; }
}

public class VehicleDriverOverlapModel
{
	public int Id { get; set; }
	public int VehicleId { get; set; }
	public int DriverId { get; set; }
	public DateTime StartDateTime { get; set; }
	public DateTime? EndDateTime { get; set; }
	public string ConflictType { get; set; }
}