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
	public int? SDRId { get; set; }
	public string? Remarks { get; set; }
	public bool AvailableStatus { get; set; }
	public bool Status { get; set; }
}


public class VehicleLocationModel
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
	public int? SDRId { get; set; }
	public string? Remarks { get; set; }
	public bool AvailableStatus { get; set; }
	public bool Status { get; set; }

	public string SDR { get; set; }
	public bool InGarage { get; set; }

	// Identity — map to Vectus Vehicle.Code via VehicleId (fallback RegNo).
	public string VehicleId { get; set; }
	public string RegNo { get; set; }
	public string ShortName { get; set; }
	public string VehicleType { get; set; }

	// Position
	public decimal Latitude { get; set; }
	public decimal Longitude { get; set; }
	public string Address { get; set; }
	public bool HasValidPosition => Latitude != 0 && Longitude != 0;

	// Haversine distance in KM
	public decimal? HaversineDistance { get; set; }

	// Movement
	public int Speed { get; set; }
	public int TopSpeed { get; set; }
	public int AverageSpeed { get; set; }
	public string VehicleMode { get; set; }
	public bool IgnitionOn { get; set; }

	// Freshness (IST, from epoch-ms lastComunicationTime)
	public DateTime LastUpdate { get; set; }

	// Fuel
	public decimal FuelLitre { get; set; }
	public decimal TankSize { get; set; }
	public decimal ExpectedFuelMileage { get; set; }

	// Distance
	public decimal OdometerKM { get; set; }
	public decimal DistanceCovered { get; set; }

	// Flags
	public bool IsOverSpeed { get; set; }
	public bool InsideGeoFence { get; set; }
	public bool HasAlert { get; set; }

	// GPS subscription
	public DateTime? GpsExpiryDate { get; set; }
	public int GpsExpiryDays { get; set; }
}
