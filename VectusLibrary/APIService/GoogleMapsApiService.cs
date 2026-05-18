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

	/// <summary>
	/// Searches places by text using the Google Geocoding API and returns
	/// live "as you type" predictions (Google Maps style) for a search box.
	/// </summary>
	public static async Task<IEnumerable<PlaceModel>> SearchPlaces(string input)
	{
		var requestBody = new { input };

		using var request = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:autocomplete");
		request.Headers.Add("X-Goog-Api-Key", Secrets.GoogleMapsApiKey);
		request.Headers.Add("X-Goog-FieldMask", "suggestions.placePrediction.text,suggestions.placePrediction.placeId");
		request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

		using var response = await _httpClient.SendAsync(request);
		response.EnsureSuccessStatusCode();

		await using var stream = await response.Content.ReadAsStreamAsync();
		using var json = await JsonDocument.ParseAsync(stream);

		var places = new List<PlaceModel>();
		if (json.RootElement.TryGetProperty("suggestions", out var suggestions))
			foreach (var suggestion in suggestions.EnumerateArray())
				if (suggestion.TryGetProperty("placePrediction", out var prediction))
					places.Add(new()
					{
						Description = prediction.GetProperty("text").GetProperty("text").GetString(),
						PlaceId = prediction.GetProperty("placeId").GetString()
					});

		return places;
	}

	/// <summary>
	/// Resolves a place prediction (by place id) to its coordinates using the
	/// Google Place Details (New) API. Only the "location" field is requested,
	/// which keeps the call in the cheapest Place Details Essentials SKU.
	/// </summary>
	public static async Task<LocationModel> GetPlaceDetails(string placeId)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, $"https://places.googleapis.com/v1/places/{placeId}");
		request.Headers.Add("X-Goog-Api-Key", Secrets.GoogleMapsApiKey);
		request.Headers.Add("X-Goog-FieldMask", "location");

		using var response = await _httpClient.SendAsync(request);
		response.EnsureSuccessStatusCode();

		await using var stream = await response.Content.ReadAsStreamAsync();
		using var json = await JsonDocument.ParseAsync(stream);

		var location = json.RootElement.GetProperty("location");

		return new()
		{
			Latitude = location.GetProperty("latitude").GetDecimal(),
			Longitude = location.GetProperty("longitude").GetDecimal()
		};
	}
}

public class PlaceModel
{
	public string Description { get; set; }
	public string PlaceId { get; set; }
}
