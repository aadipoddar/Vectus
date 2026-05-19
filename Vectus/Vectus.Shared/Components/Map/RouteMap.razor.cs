using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using VectusLibrary.DataAccess;

namespace Vectus.Shared.Components.Map;

public partial class RouteMap : IAsyncDisposable
{
	[Parameter] public decimal? OriginLatitude { get; set; }
	[Parameter] public decimal? OriginLongitude { get; set; }
	[Parameter] public decimal? DestinationLatitude { get; set; }
	[Parameter] public decimal? DestinationLongitude { get; set; }
	[Parameter] public string PlaceholderText { get; set; } = "Select a route to preview it on the map.";

	private readonly string _elementId = $"routeMap-{Guid.NewGuid():N}";
	private string _origin;
	private string _destination;
	private bool _ready;
	private bool _dirty;
	private bool _initialized;

	protected override void OnParametersSet()
	{
		var origin = Format(OriginLatitude, OriginLongitude);
		var destination = Format(DestinationLatitude, DestinationLongitude);

		if (origin == _origin && destination == _destination)
			return;

		_origin = origin;
		_destination = destination;
		_ready = origin is not null && destination is not null;
		_dirty = _ready;

		if (!_ready)
			_initialized = false;
	}

	private static string Format(decimal? latitude, decimal? longitude) =>
		latitude is null || longitude is null
			? null
			: $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}";

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!_ready || !_dirty)
			return;

		_dirty = false;

		if (!_initialized)
		{
			_initialized = true;
			await JSRuntime.InvokeVoidAsync("initRouteMap", _elementId, Secrets.GoogleMapsApiKey, _origin, _destination);
		}
		else
		{
			await JSRuntime.InvokeVoidAsync("updateRouteMap", _elementId, _origin, _destination);
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!_initialized)
			return;

		try { await JSRuntime.InvokeVoidAsync("disposeRouteMap", _elementId); }
		catch { /* circuit/WebView already gone — nothing to clean up */ }

		GC.SuppressFinalize(this);
	}
}
