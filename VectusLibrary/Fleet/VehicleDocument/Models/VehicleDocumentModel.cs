namespace VectusLibrary.Fleet.VehicleDocument.Models;

public class VehicleDocumentModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int VehicleDocumentTypeId { get; set; }
	public int VehicleId { get; set; }
	public decimal CurrentKM { get; set; }
	public decimal Rate { get; set; }
	public DateTime RenewalDate { get; set; }
	public string? Remarks { get; set; }
	public string? DocumentUrl { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
}

public class VehicleDocumentRenewalOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public DateTime TransactionDateTime { get; set; }

	public int VehicleDocumentTypeId { get; set; }
	public string VehicleDocumentTypeName { get; set; }
	public string VehicleDocumentTypeCode { get; set; }

	public int VehicleId { get; set; }
	public string VehicleCode { get; set; }

	public decimal CurrentKM { get; set; }
	public decimal Rate { get; set; }
	public DateTime RenewalDate { get; set; }
	public int DaysRemaining { get; set; }

	public string? DocumentUrl { get; set; }

	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public int? LastModifiedBy { get; set; }
	public string? LastModifiedByUserName { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
	public bool Status { get; set; }
}
