using System.Text;
using System.Text.Json;

using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Route.Models;
using VectusLibrary.Operations.Data;
using VectusLibrary.Operations.Models;

namespace VectusLibrary.APIService;

public static class GoogleMapsApiService
{
	private static readonly HttpClient _httpClient = new();

	public static async Task<RouteModel> GetRouteEstimate(
		decimal fromLatitude, decimal fromLongitude,
		decimal toLatitude, decimal toLongitude)
	{
		var truckKmPerLitre = double.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.TruckMileageKmPerLitre)).Value);
		var dieselPricePerLitre = decimal.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.DieselPricePerLitre)).Value);

		var requestBody = new
		{
			origin = new { location = new { latLng = new { latitude = fromLatitude, longitude = fromLongitude } } },
			destination = new { location = new { latLng = new { latitude = toLatitude, longitude = toLongitude } } },
			travelMode = "DRIVE",
			routingPreference = "TRAFFIC_AWARE"
		};

		using var request = new HttpRequestMessage(HttpMethod.Post, "https://routes.googleapis.com/directions/v2:computeRoutes");
		request.Headers.Add("X-Goog-Api-Key", Secrets.GoogleMapsApiKey);
		request.Headers.Add("X-Goog-FieldMask", "routes.distanceMeters,routes.duration");
		request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

		using var response = await _httpClient.SendAsync(request);
		response.EnsureSuccessStatusCode();

		await using var stream = await response.Content.ReadAsStreamAsync();
		using var json = await JsonDocument.ParseAsync(stream);

		var route = json.RootElement.GetProperty("routes")[0];

		var distanceKm = route.GetProperty("distanceMeters").GetInt32() / 1000.0;
		var hours = double.Parse(route.GetProperty("duration").GetString().TrimEnd('s')) / 3600.0;

		var fuelLitres = distanceKm / truckKmPerLitre;
		var cost = (decimal)fuelLitres * dieselPricePerLitre;

		return new()
		{
			EstimatedDistance = (int)Math.Round(distanceKm),
			EstimatedHours = (int)Math.Round(hours),
			EstimatedFuelConsumption = (int)Math.Round(fuelLitres),
			EstimatedCost = Math.Round(cost, 2)
		};
	}
}
