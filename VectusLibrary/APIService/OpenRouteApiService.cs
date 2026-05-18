using System.Text.Json;

using VectusLibrary.DataAccess;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.APIService;

public static class OpenRouteApiService
{
	private static readonly HttpClient _httpClient = new();

	/// <summary>
	/// Gets a simple truck route estimate (distance, hours, fuel, cost) between two coordinates.
	/// Uses OpenRouteService's heavy-goods-vehicle (driving-hgv) profile.
	/// </summary>
	public static async Task<RouteEstimateModel> GetRouteEstimate(
		decimal fromLatitude, decimal fromLongitude,
		decimal toLatitude, decimal toLongitude)
	{
		var truckKmPerLitre = double.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.TruckMileageKmPerLitre)).Value);
		var dieselPricePerLitre = decimal.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.DieselPricePerLitre)).Value);

		var url = "https://api.openrouteservice.org/v2/directions/driving-hgv" +
			$"?api_key={Secrets.OpenRouteServiceApiKey}" +
			$"&start={fromLongitude},{fromLatitude}" +
			$"&end={toLongitude},{toLatitude}";

		using var response = await _httpClient.GetAsync(url);
		response.EnsureSuccessStatusCode();

		await using var stream = await response.Content.ReadAsStreamAsync();
		using var json = await JsonDocument.ParseAsync(stream);

		// GET directions returns GeoJSON: features[0].properties.summary { distance (m), duration (s) }
		var summary = json.RootElement
			.GetProperty("features")[0]
			.GetProperty("properties")
			.GetProperty("summary");

		var distanceKm = summary.GetProperty("distance").GetDouble() / 1000.0;
		var hours = summary.GetProperty("duration").GetDouble() / 3600.0;
		var fuelLitres = distanceKm / truckKmPerLitre;
		var cost = (decimal)fuelLitres * dieselPricePerLitre;

		return new RouteEstimateModel
		{
			DistanceKm = (int)Math.Round(distanceKm),
			Hours = (int)Math.Round(hours),
			FuelLitres = (int)Math.Round(fuelLitres),
			Cost = Math.Round(cost, 2)
		};
	}
}

public class RouteEstimateModel
{
	public int DistanceKm { get; set; }
	public int Hours { get; set; }
	public int FuelLitres { get; set; }
	public decimal Cost { get; set; }
}
