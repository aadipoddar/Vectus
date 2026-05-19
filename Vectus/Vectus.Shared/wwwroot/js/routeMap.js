// Google Maps JavaScript API — reusable route map (RouteMap.razor).
// Modern stack only: a vector map (Map ID), AdvancedMarkerElement pins, a
// Polyline decoded from the Routes API result (computed server-side in
// GoogleMapsApiService — the Routes API has no browser CORS), and a live
// TrafficLayer. No deprecated google.maps.Marker, no DirectionsService.
// Each map is kept in a registry keyed by element id so a component
// cleans up its own.

const maps = new Map();
let loaderInjected;

function injectLoader(key) {
	if (loaderInjected) return;
	loaderInjected = true;
	(g => { var h, a, k, p = "The Google Maps JavaScript API", c = "google", l = "importLibrary", q = "__ib__", m = document, b = window; b = b[c] || (b[c] = {}); var d = b.maps || (b.maps = {}), r = new Set, e = new URLSearchParams, u = () => h || (h = new Promise(async (f, n) => { await (a = m.createElement("script")); e.set("libraries", [...r] + ""); for (k in g) e.set(k.replace(/[A-Z]/g, t => "_" + t[0].toLowerCase()), g[k]); e.set("callback", c + ".maps." + q); a.src = `https://maps.${c}apis.com/maps/api/js?` + e; d[q] = f; a.onerror = () => h = n(Error(p + " could not load.")); a.nonce = m.querySelector("script[nonce]")?.nonce || ""; m.head.append(a) })); d[l] ? console.warn(p + " only loads once. Ignoring:", g) : d[l] = (f, ...n) => r.add(f) && u().then(() => d[l](f, ...n)) })({
		key: key,
		v: "weekly",
	});
}

function parseLatLng(value) {
	const [lat, lng] = value.split(",").map(Number);
	return { lat, lng };
}

// Small badge inside the map showing road distance / drive time (or an
// error). Built once per map; styled inline to keep the widget self-contained.
function badge(inst) {
	if (!inst.badge) {
		const el = document.createElement("div");
		el.style.cssText =
			"position:absolute;top:10px;left:10px;padding:6px 12px;background:#fff;" +
			"font:600 13px/1.4 system-ui,sans-serif;color:#1f2937;border-radius:8px;" +
			"box-shadow:0 1px 4px rgba(0,0,0,.3);pointer-events:none;";
		inst.map.getDiv().appendChild(el);
		inst.badge = el;
	}
	return inst.badge;
}

async function createMap(element, center, mapId) {
	const { Map } = await google.maps.importLibrary("maps");

	const map = new Map(element, {
		zoom: 13,
		center,
		mapId, // required for AdvancedMarkerElement (vector map)
		mapTypeControl: false,
		streetViewControl: false,
		gestureHandling: "cooperative", // don't swallow page scroll on the form
		clickableIcons: false,          // no POI popups over the route
	});

	new google.maps.TrafficLayer().setMap(map); // live traffic overlay

	return { map, origin: null, destination: null, line: null, badge: null };
}

// Create the map if needed, then (re)draw the route on it. `encodedPolyline`
// and `summary` come from the server-side Routes API call; when they're null
// (no route / quota / network) we still show the two endpoints.
window.showRoute = async function (elementId, key, mapId, origin, destination, encodedPolyline, summary) {
	injectLoader(key);

	const element = document.getElementById(elementId);
	if (!element) return;

	const src = parseLatLng(origin);
	const dst = parseLatLng(destination);

	let inst = maps.get(elementId);
	if (!inst || inst.map.getDiv() !== element) {
		inst = await createMap(element, src, mapId);
		maps.set(elementId, inst);
	}

	// Clear the previous render before drawing the new route.
	if (inst.origin) inst.origin.map = null;
	if (inst.destination) inst.destination.map = null;
	if (inst.line) inst.line.setMap(null);

	const { AdvancedMarkerElement, PinElement } = await google.maps.importLibrary("marker");
	const makePin = (position, background, title) => new AdvancedMarkerElement({
		map: inst.map, position, title,
		content: new PinElement({ background, borderColor: "#ffffff", glyphColor: "#ffffff" }).element,
	});
	inst.origin = makePin(src, "#16a34a", "Origin");
	inst.destination = makePin(dst, "#dc2626", "Destination");

	const bounds = new google.maps.LatLngBounds();

	if (encodedPolyline) {
		const { encoding } = await google.maps.importLibrary("geometry");
		const path = encoding.decodePath(encodedPolyline);
		inst.line = new google.maps.Polyline({
			path,
			map: inst.map,
			strokeColor: "#2563eb",
			strokeWeight: 5,
			strokeOpacity: 0.85,
		});
		path.forEach(point => bounds.extend(point));
		badge(inst).textContent = summary;
	} else {
		bounds.extend(src);
		bounds.extend(dst);
		badge(inst).textContent = "Driving route unavailable for these locations";
	}

	inst.map.fitBounds(bounds, 48); // 48px padding so pins aren't on the edge
};

window.disposeRouteMap = function (elementId) {
	const inst = maps.get(elementId);
	if (!inst) return;
	if (inst.origin) inst.origin.map = null;
	if (inst.destination) inst.destination.map = null;
	if (inst.line) inst.line.setMap(null);
	if (inst.badge) inst.badge.remove();
	maps.delete(elementId);
};
