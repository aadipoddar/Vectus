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