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

// Small badge showing road distance / drive time (or an error). Registered
// as a real map control (TOP_CENTER) so Google lays it out and it never
// overlaps the map-type / fullscreen / Street View controls.
function badge(inst) {
	if (!inst.badge) {
		const el = document.createElement("div");
		el.style.cssText =
			"margin:10px;padding:6px 12px;background:#fff;" +
			"font:600 13px/1.4 system-ui,sans-serif;color:#1f2937;border-radius:8px;" +
			"box-shadow:0 1px 4px rgba(0,0,0,.3);pointer-events:none;";
		inst.map.controls[google.maps.ControlPosition.TOP_CENTER].push(el);
		inst.badge = el;
	}
	return inst.badge;
}

async function createMap(element, center, mapId, mobile) {
	const { Map } = await google.maps.importLibrary("maps");

	const map = new Map(element, {
		zoom: 13,
		center,
		mapId, // required for AdvancedMarkerElement (vector map)
		// On mobile the map is full-bleed, so strip the desktop chrome and let
		// one finger pan / pinch zoom ("greedy"). On desktop keep the controls
		// and "cooperative" so the page can still scroll past an embedded map.
		mapTypeControl: !mobile,
		streetViewControl: !mobile,
		fullscreenControl: !mobile,
		zoomControl: !mobile,
		zoomControlOptions: { position: google.maps.ControlPosition.RIGHT_BOTTOM },
		gestureHandling: mobile ? "greedy" : "cooperative",
		clickableIcons: false,          // no POI popups over the route
	});

	new google.maps.TrafficLayer().setMap(map); // live traffic overlay

	return {
		map, mobile, origin: null, destination: null, line: null, badge: null,
		vehicleMarkers: [], infoWindow: null, vehiclesDrawn: false, focusControl: null,
	};
}

// A custom control button (sits above the +/- zoom buttons) that recentres
// the map on the current origin. Added once; it reads inst.origin each click
// so it always targets the latest route's start.
function addFocusControl(inst) {
	if (inst.focusControl) return;

	// Bigger touch target on mobile, and pinned top-right so it clears the
	// bottom sheet (the desktop zoom controls sit bottom-right).
	const size = inst.mobile ? 48 : 40;
	const btn = document.createElement("button");
	btn.type = "button";
	btn.title = "Focus on origin";
	btn.style.cssText =
		`width:${size}px;height:${size}px;margin:10px;border:0;border-radius:${inst.mobile ? 24 : 8}px;cursor:pointer;` +
		"background:#fff;box-shadow:0 2px 8px rgba(0,0,0,.25);display:flex;" +
		"align-items:center;justify-content:center;";
	btn.innerHTML =
		'<svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="#1f2937" ' +
		'stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' +
		'<circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/></svg>';
	btn.addEventListener("click", () => {
		if (!inst.origin) return;
		inst.map.setCenter(inst.origin.position);
		inst.map.setZoom(18);
	});

	const position = inst.mobile ? google.maps.ControlPosition.RIGHT_TOP : google.maps.ControlPosition.RIGHT_BOTTOM;
	inst.map.controls[position].push(btn);
	inst.focusControl = btn;
}

const VEHICLE_COLORS = { Moving: "#16a34a", Idle: "#f59e0b", Stopped: "#6b7280" };

function esc(s) {
	return (s ?? "").toString().replace(/[&<>]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;" }[c]));
}

// A status-coloured circular badge with a white truck glyph. Custom HTML
// content keeps the vehicle markers clearly distinct from the route's
// teardrop origin/destination pins. (Lucide "truck" icon — matches the
// stroke-style icons used elsewhere in the app.)
function vehicleContent(color) {
	const el = document.createElement("div");
	el.style.cssText =
		"width:28px;height:28px;border-radius:50%;display:flex;align-items:center;" +
		"justify-content:center;background:" + color + ";border:2px solid #fff;" +
		"box-shadow:0 1px 4px rgba(0,0,0,.4);";
	el.innerHTML =
		'<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="#fff" ' +
		'stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' +
		'<path d="M10 17h4V5H2v12h3"/>' +
		'<path d="M20 17h2v-3.34a4 4 0 0 0-1.17-2.83L19 9h-5v8h1"/>' +
		'<circle cx="7.5" cy="17.5" r="2.5"/>' +
		'<circle cx="17.5" cy="17.5" r="2.5"/></svg>';
	return el;
}

// Builds the click-card for a vehicle: header + status chip, a small
// stats grid, an alert line, then last-seen / address footer.
function infoHtml(v) {
	const color = VEHICLE_COLORS[v.status] ?? "#6b7280";
	const cell = (label, value) =>
		`<div><div style="color:#6b7280;font-size:11px">${label}</div>` +
		`<div style="font-weight:600">${value}</div></div>`;

	const fuel = v.tank > 0 ? `${Math.round(v.fuel)} / ${Math.round(v.tank)} L` : `${Math.round(v.fuel)} L`;
	const sub = [v.reg, v.type].filter(Boolean).map(esc).join(" · ");
	const alerts = [];
	if (v.overspeed) alerts.push("Over speed");
	if (v.alert) alerts.push("Alert");

	const grid =
		cell("Speed", `${v.speed} km/h`) +
		cell("Fuel", fuel) +
		cell("Odometer", `${Math.round(v.odo).toLocaleString()} km`) +
		cell("Trip distance", `${Math.round(v.today)} km`) +
		cell("Ignition", v.ignition ? "On" : "Off") +
		cell("Mode", esc(v.mode || "—"));

	return (
		`<div style="font:13px/1.45 system-ui,sans-serif;color:#1f2937;min-width:230px;max-width:280px">` +
		`<div style="display:flex;align-items:center;justify-content:space-between;gap:8px">` +
		`<span style="font-weight:700;font-size:14px">${esc(v.code)}</span>` +
		`<span style="background:${color};color:#fff;font-size:11px;font-weight:600;padding:2px 8px;border-radius:999px">${esc(v.status)}</span>` +
		`</div>` +
		(sub ? `<div style="color:#6b7280;margin-top:2px">${sub}</div>` : "") +
		`<div style="display:grid;grid-template-columns:1fr 1fr;gap:6px 14px;margin:8px 0;padding:8px 0;border-top:1px solid #e5e7eb;border-bottom:1px solid #e5e7eb">${grid}</div>` +
		(alerts.length ? `<div style="color:#dc2626;font-weight:600;margin-bottom:4px">⚠ ${esc(alerts.join(" · "))}</div>` : "") +
		(v.updated ? `<div style="color:#6b7280">Updated ${esc(v.updated)}</div>` : "") +
		(v.address ? `<div style="color:#6b7280;margin-top:2px">${esc(v.address)}</div>` : "") +
		`</div>`
	);
}

// Plot the live fleet once per map. Vehicle pins are smaller than the
// route's origin/destination pins (scale 0.8) and coloured by status, so
// they stay visually distinct. Click a pin for its details.
window.showVehicles = async function (elementId, vehicles, dotNetRef) {
	const inst = maps.get(elementId);
	if (!inst || inst.vehiclesDrawn) return;
	inst.vehiclesDrawn = true;
	inst.dotNetRef = dotNetRef;
	inst.markerByCode = new Map();

	const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");
	const info = inst.infoWindow ?? (inst.infoWindow = new google.maps.InfoWindow());

	for (const v of vehicles) {
		const color = VEHICLE_COLORS[v.status] ?? "#6b7280";
		const marker = new AdvancedMarkerElement({
			map: inst.map,
			position: { lat: v.lat, lng: v.lng },
			title: `${v.code} — ${v.status}`,
			gmpClickable: true,
			content: vehicleContent(color),
		});
		marker.addListener("gmp-click", () => {
			info.setContent(infoHtml(v));
			info.open(inst.map, marker);
			// Notify Blazor so the form/grid select this vehicle too.
			inst.dotNetRef?.invokeMethodAsync("SelectVehicle", v.code);
		});
		inst.vehicleMarkers.push(marker);
		inst.markerByCode.set(v.code, { marker, v });
	}
};

// Selection driven from the form/grid: open the matching vehicle's card and
// pan to it. A null/empty code just closes any open card.
window.highlightVehicle = function (elementId, code) {
	const inst = maps.get(elementId);
	if (!inst || !inst.infoWindow) return;
	if (!code) { inst.infoWindow.close(); return; }
	const entry = inst.markerByCode?.get(code);
	if (!entry) return;
	inst.infoWindow.setContent(infoHtml(entry.v));
	inst.infoWindow.open(inst.map, entry.marker);
	inst.map.panTo(entry.marker.position);
};

// Create the map if needed, then (re)draw the route on it. `encodedPolyline`
// and `summary` come from the server-side Routes API call; when they're null
// (no route / quota / network) we still show the two endpoints.
window.showRoute = async function (elementId, key, mapId, origin, destination, encodedPolyline, summary, mobile) {
	injectLoader(key);

	const element = document.getElementById(elementId);
	if (!element) return;

	const src = parseLatLng(origin);
	const dst = parseLatLng(destination);

	let inst = maps.get(elementId);
	if (!inst || inst.map.getDiv() !== element) {
		inst = await createMap(element, src, mapId, mobile);
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

	addFocusControl(inst); // "focus on origin" button above the zoom controls

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
		if (!inst.mobile) badge(inst).textContent = summary;
	} else {
		bounds.extend(src);
		bounds.extend(dst);
		if (!inst.mobile) badge(inst).textContent = "Driving route unavailable for these locations";
	}

	// On mobile a bottom sheet covers the lower ~60% of the screen, so pad the
	// fit so the whole route stays in the visible strip above the sheet.
	const padding = inst.mobile
		? { top: 90, right: 40, bottom: Math.round(element.clientHeight * 0.6) + 24, left: 40 }
		: 48; // desktop: even 48px padding so pins aren't on the edge
	inst.map.fitBounds(bounds, padding);
};

window.disposeRouteMap = function (elementId) {
	const inst = maps.get(elementId);
	if (!inst) return;
	if (inst.origin) inst.origin.map = null;
	if (inst.destination) inst.destination.map = null;
	if (inst.line) inst.line.setMap(null);
	for (const m of inst.vehicleMarkers) m.map = null;
	if (inst.infoWindow) inst.infoWindow.close();
	if (inst.badge) inst.badge.remove();
	maps.delete(elementId);
};
