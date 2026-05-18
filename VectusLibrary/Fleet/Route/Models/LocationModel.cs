namespace VectusLibrary.Fleet.Route.Models;

public class LocationModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public decimal Latitude { get; set; }
	public decimal Longitude { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
