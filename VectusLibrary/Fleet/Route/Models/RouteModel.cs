namespace VectusLibrary.Fleet.Route.Models;

public class RouteModel
{
	public int Id { get; set; }
	public int FromLocationId { get; set; }
	public int ToLocationId { get; set; }
	public string Code { get; set; }
	public int EstimatedHours { get; set; }
	public int EstimatedDistance { get; set; }
	public int EstimatedFuelConsumption { get; set; }
	public decimal EstimatedCost { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class RouteOverviewModel
{
	public int Id { get; set; }
	public int FromLocationId { get; set; }
	public string FromLocationName { get; set; }
	public int ToLocationId { get; set; }
	public string ToLocationName { get; set; }
	public string RouteDisplay { get; set; }
	public string Code { get; set; }
	public int EstimatedHours { get; set; }
	public int EstimatedDistance { get; set; }
	public int EstimatedFuelConsumption { get; set; }
	public decimal EstimatedCost { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}