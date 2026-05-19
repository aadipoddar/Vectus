// Google Maps JavaScript API — reusable route map (RouteMap.razor component).
// Uses the official Dynamic Library Import bootstrap loader; a single
// standard API key covers the Maps JavaScript API. Classic Marker is used
// so no Map ID / cloud styling is required (kept intentionally simple).
// State is keyed by element id so multiple maps can coexist on a page.

const _instances = new Map();
let _loaderInjected;

function injectLoader(key) {
	if (_loaderInjected) return;
	_loaderInjected = true;
	(g => { var h, a, k, p = "The Google Maps JavaScript API", c = "google", l = "importLibrary", q = "__ib__", m = document, b = window; b = b[c] || (b[c] = {}); var d = b.maps || (b.maps = {}), r = new Set, e = new URLSearchParams, u = () => h || (h = new Promise(async (f, n) => { await (a = m.createElement("script")); e.set("libraries", [...r] + ""); for (k in g) e.set(k.replace(/[A-Z]/g, t => "_" + t[0].toLowerCase()), g[k]); e.set("callback", c + ".maps." + q); a.src = `https://maps.${c}apis.com/maps/api/js?` + e; d[q] = f; a.onerror = () => h = n(Error(p + " could not load.")); a.nonce = m.querySelector("script[nonce]")?.nonce || ""; m.head.append(a) })); d[l] ? console.warn(p + " only loads once. Ignoring:", g) : d[l] = (f, ...n) => r.add(f) && u().then(() => d[l](f, ...n)) })({
		key: key,
		v: "weekly",
	});
}

function parseLatLng(value) {
	const [lat, lng] = value.split(",").map(Number);
	return { lat, lng };
}

async function drawRoute(inst, origin, destination) {
	const src = parseLatLng(origin);

	const result = await inst.directionsService.route({
		origin: src,
		destination: parseLatLng(destination),
		travelMode: "DRIVING",
	});
	inst.directionsRenderer.setDirections(result);

	const { Marker } = await google.maps.importLibrary("marker");
	if (inst.marker) inst.marker.setMap(null);
	inst.marker = new Marker({ position: src, map: inst.map, title: "Source" });

	// Keep the view zoomed in on the source (the renderer would otherwise
	// fit the whole route bounds — preserveViewport: true disables that).
	inst.map.setCenter(src);
	inst.map.setZoom(14);
}

window.initRouteMap = async function (elementId, key, origin, destination) {
	injectLoader(key);

	const { Map } = await google.maps.importLibrary("maps");
	const { DirectionsService, DirectionsRenderer } = await google.maps.importLibrary("routes");

	const element = document.getElementById(elementId);
	if (!element) return;

	let inst = _instances.get(elementId);
	if (!inst || inst.map.getDiv() !== element) {
		const map = new Map(element, {
			zoom: 14,
			center: parseLatLng(origin),
			zoomControl: true,
			mapTypeControl: false,
			streetViewControl: false,
			fullscreenControl: true,
		});
		inst = {
			map,
			directionsService: new DirectionsService(),
			directionsRenderer: new DirectionsRenderer({ map, preserveViewport: true }),
			marker: null,
		};
		_instances.set(elementId, inst);
	}

	await drawRoute(inst, origin, destination);
};

window.updateRouteMap = async function (elementId, origin, destination) {
	const inst = _instances.get(elementId);
	if (!inst) return;
	await drawRoute(inst, origin, destination);
};

window.disposeRouteMap = function (elementId) {
	const inst = _instances.get(elementId);
	if (!inst) return;
	if (inst.marker) inst.marker.setMap(null);
	if (inst.directionsRenderer) inst.directionsRenderer.setMap(null);
	_instances.delete(elementId);
};
