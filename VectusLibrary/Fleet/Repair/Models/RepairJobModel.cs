namespace VectusLibrary.Fleet.Repair.Models;

public class RepairJobModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public string Job { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class RepairJobCartModel
{
	public string Job { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
	public string? Remarks { get; set; }
}

public class RepairJobOverviewModel
{
	public int Id { get; set; }
	public string Job { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
	public string? JobRemarks { get; set; }

	public int MasterId { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }

	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int GarageId { get; set; }
	public string GarageName { get; set; }

	public int VehicleId { get; set; }
	public string VehicleCode { get; set; }
	public decimal? CurrentKM { get; set; }

	public DateTime GarageInDateTime { get; set; }
	public DateTime? GarageOutDateTime { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal TotalAmount { get; set; }

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