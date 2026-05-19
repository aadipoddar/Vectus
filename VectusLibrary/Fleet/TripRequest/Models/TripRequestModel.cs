namespace VectusLibrary.Fleet.TripRequest.Models;

public class TripRequestModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public int RouteId { get; set; }
	public int VehicleId { get; set; }
	public string RequestStatus { get; set; }
	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
}

public class TripRequestOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }

	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int RouteId { get; set; }
	public string RouteCode { get; set; }
	public string FromLocation { get; set; }
	public string ToLocation { get; set; }
	public string RouteDisplay { get; set; }
	public int EstimatedDistance { get; set; }
	public int EstimatedHours { get; set; }
	public int EstimatedFuelConsumption { get; set; }
	public decimal EstimatedCost { get; set; }

	public int VehicleId { get; set; }
	public string VehicleCode { get; set; }
	public int? SDRId { get; set; }
	public string? SDRName { get; set; }

	public string RequestStatus { get; set; }

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

public enum RequestStatus
{
	Requested,
	Accepted,
	Rejected,
}
