namespace VectusLibrary.Fleet.VehicleDocument.Models;

public class VehicleDocumentTypeModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public decimal Rate { get; set; }
    public string? Remarks { get; set; }
    public bool Status { get; set; }
}