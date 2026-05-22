namespace VectusLibrary.Fleet.Tyre.Models;

public class TyreMountingModel
{
	public int Id { get; set; }
	public string TyreNo { get; set; }
	public int TyreCompanyId { get; set; }
	public string? TyreModel { get; set; }
	public int VehicleId { get; set; }
	public decimal MountingKM { get; set; }
	public decimal? DismountingKM { get; set; }
	public DateTime MountingDateTime { get; set; }
	public DateTime? DismountingDateTime { get; set; }
	public string? Remarks { get; set; }
}
