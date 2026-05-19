namespace VectusLibrary.Fleet.Garage.Models;

public class GarageModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public int LocationId { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
