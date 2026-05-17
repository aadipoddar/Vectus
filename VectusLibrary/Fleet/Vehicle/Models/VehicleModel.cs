namespace VectusLibrary.Fleet.Vehicle.Models;

public class VehicleModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string ShortCode { get; set; }
	public string ChasisCode { get; set; }
	public string EngineCode { get; set; }
	public DateTime PurchaseDate { get; set; }
	public decimal OpeningKM { get; set; }
	public int VehicleTypeId { get; set; }
	public int CompanyId { get; set; }
	public int? HDRId { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
