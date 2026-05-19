using System.Globalization;
using System.Text.Json;

using VectusLibrary.DataAccess;

namespace VectusLibrary.APIService;

public static class VamosysApiService
{
	private static readonly HttpClient _httpClient = new();
	private static readonly TimeZoneInfo _ist = ResolveIst();

	public static async Task<List<VamosysVehicleModel>> GetLiveVehicles()
	{
		var url = Secrets.VamosysAPI;

		using var response = await _httpClient.GetAsync(url);
		response.EnsureSuccessStatusCode();

		await using var stream = await response.Content.ReadAsStreamAsync();
		using var json = await JsonDocument.ParseAsync(stream);

		var vehicles = new List<VamosysVehicleModel>();
		if (json.RootElement.ValueKind != JsonValueKind.Array)
			return vehicles;

		foreach (var v in json.RootElement.EnumerateArray())
			vehicles.Add(new()
			{
				VehicleId = Str(v, "vehicleId"),
				RegNo = Str(v, "regNo"),
				ShortName = Str(v, "shortName"),
				VehicleType = Str(v, "vehicleType"),

				Latitude = Dec(v, "latitude"),
				Longitude = Dec(v, "longitude"),
				Address = Str(v, "address"),

				Speed = Int(v, "speed"),
				TopSpeed = Int(v, "topSpeed"),
				AverageSpeed = Int(v, "averageSpeed"),
				VehicleMode = Str(v, "vehicleMode"),
				IgnitionOn = string.Equals(Str(v, "ignitionStatus"), "ON", StringComparison.OrdinalIgnoreCase),

				LastUpdate = ToIst(Long(v, "lastComunicationTime")),

				FuelLitre = Dec(v, "fuelLitre"),
				TankSize = Dec(v, "tankSize"),
				ExpectedFuelMileage = Dec(v, "expectedFuelMileage"),

				OdometerKM = Dec(v, "odoDistance"),
				DistanceCovered = Dec(v, "distanceCovered"),

				IsOverSpeed = Flag(v, "isOverSpeed"),
				InsideGeoFence = Flag(v, "insideGeoFence"),
				HasAlert = Flag(v, "alert"),

				GpsExpiryDate = Date(v, "expiryDate"),
				GpsExpiryDays = Int(v, "expiryDays")
			});

		return vehicles;
	}

	#region JSON helpers
	private static string Str(JsonElement e, string name) =>
		e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

	private static int Int(JsonElement e, string name) =>
		e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n) ? n : 0;

	private static long Long(JsonElement e, string name) =>
		e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n) ? n : 0;

	private static decimal Dec(JsonElement e, string name)
	{
		if (e.TryGetProperty(name, out var p))
		{
			if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var n))
				return n;
			if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
				return s;
		}
		return 0;
	}

	private static bool Flag(JsonElement e, string name) =>
		string.Equals(Str(e, name), "Y", StringComparison.OrdinalIgnoreCase);

	private static DateTime? Date(JsonElement e, string name) =>
		DateTime.TryParseExact(Str(e, name), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
			? d
			: null;

	private static DateTime ToIst(long epochMs) =>
		epochMs <= 0
			? default
			: TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(epochMs), _ist).DateTime;

	private static TimeZoneInfo ResolveIst()
	{
		foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
			try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
			catch { /* try the next id */ }

		return TimeZoneInfo.Utc;
	}
	#endregion
}

public class VamosysVehicleModel
{
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
