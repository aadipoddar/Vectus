using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using System.Globalization;
using System.Linq;

using VectusLibrary.APIService;
using VectusLibrary.DataAccess;
using VectusLibrary.Fleet.Vehicle.Models;

namespace Vectus.Shared.Components.Map;

public partial class RouteMap : IAsyncDisposable
{
	[Parameter] public decimal? OriginLatitude { get; set; }
	[Parameter] public decimal? OriginLongitude { get; set; }
	[Parameter] public decimal? DestinationLatitude { get; set; }
	[Parameter] public decimal? DestinationLongitude { get; set; }
	[Parameter] public string PlaceholderText { get; set; } = "Select a route to preview it on the map.";
	[Parameter] public List<VehicleLocationModel> Vehicles { get; set; }

	private readonly string _elementId = $"routeMap-{Guid.NewGuid():N}";
	private string _origin;
	private string _destination;
	private string _drawn;

	private bool HasRoute => _origin is not null && _destination is not null;

	protected override void OnParametersSet()
	{
		_origin = Format(OriginLatitude, OriginLongitude);
		_destination = Format(DestinationLatitude, DestinationLongitude);
	}

	// A location with no geocoded coordinates arrives as (0, 0); treat that
	// (and nulls) as "no point" so we never ask Google for an ocean route.
	private static string Format(decimal? latitude, decimal? longitude) =>
		latitude is null || longitude is null || (latitude == 0 && longitude == 0)
			? null
			: $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}";

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!HasRoute)
		{
			_drawn = null;
			return;
		}

		// Skip the JS round-trip when the route hasn't actually changed.
		var route = $"{_origin}|{_destination}";
		if (route == _drawn)
			return;

		_drawn = route;

		// Route geometry is computed server-side/native (the Routes API has no
		// browser CORS). A null result still draws the pins + an error badge.
		MapRouteModel result = null;
		try
		{
			result = await GoogleMapsApiService.GetRoutePolyline(
				OriginLatitude.Value, OriginLongitude.Value,
				DestinationLatitude.Value, DestinationLongitude.Value);
		}
		catch { /* no route / quota / network — JS shows the fallback */ }

		await JSRuntime.InvokeVoidAsync("showRoute", _elementId,
			Secrets.GoogleMapsApiKey, Secrets.GoogleMapsMapId,
			_origin, _destination, result?.EncodedPolyline, result?.Summary);

		if (Vehicles is { Count: > 0 })
		{
			var markers = Vehicles
				.Where(v => v.HasValidPosition)
				.Select(v => new
				{
					code = v.Code,
					reg = v.RegNo,
					lat = v.Latitude,
					lng = v.Longitude,
					status = Status(v),
					speed = v.Speed,
					mode = v.VehicleMode,
					type = v.VehicleType,
					fuel = v.FuelLitre,
					tank = v.TankSize,
					odo = v.OdometerKM,
					today = v.DistanceCovered,
					ignition = v.IgnitionOn,
					overspeed = v.IsOverSpeed,
					alert = v.HasAlert,
					updated = v.LastUpdate == default
						? ""
						: v.LastUpdate.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
					address = v.Address
				})
				.ToArray();

			await JSRuntime.InvokeVoidAsync("showVehicles", _elementId, markers);
		}
	}

	private static string Status(VehicleLocationModel v) =>
		v.Speed > 0 ? "Moving" : v.IgnitionOn ? "Idle" : "Stopped";

	public async ValueTask DisposeAsync()
	{
		if (_drawn is not null)
		{
			try { await JSRuntime.InvokeVoidAsync("disposeRouteMap", _elementId); }
			catch { /* circuit/WebView already gone — nothing to clean up */ }
		}

		GC.SuppressFinalize(this);
	}
}
