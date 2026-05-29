// Draggable bottom sheet for the mobile vehicle picker. Drag the handle to
// resize the sheet between a peek (~40% screen) and nearly full (~88%), so the
// user can reveal more of the map below or more of the vehicle list above.
// Pointer events cover both touch and mouse; pointer capture keeps the drag
// alive even if the finger slips off the handle.

export function init(sheet, handle) {
	if (!sheet || !handle || sheet.dataset.sheetInit) return;
	sheet.dataset.sheetInit = "1";

	const minH = () => window.innerHeight * 0.40;
	const maxH = () => window.innerHeight * 0.88;

	let dragging = false, startY = 0, startH = 0;

	const onDown = (e) => {
		dragging = true;
		startY = e.clientY;
		startH = sheet.getBoundingClientRect().height;
		sheet.style.transition = "none";
		handle.setPointerCapture?.(e.pointerId);
		e.preventDefault();
	};

	const onMove = (e) => {
		if (!dragging) return;
		let h = startH - (e.clientY - startY); // drag up → taller, down → shorter
		h = Math.max(minH(), Math.min(h, maxH()));
		sheet.style.height = h + "px";
		sheet.style.maxHeight = "none";
	};

	const onUp = (e) => {
		if (!dragging) return;
		dragging = false;
		sheet.style.transition = "";
		handle.releasePointerCapture?.(e.pointerId);
	};

	handle.addEventListener("pointerdown", onDown);
	handle.addEventListener("pointermove", onMove);
	handle.addEventListener("pointerup", onUp);
	handle.addEventListener("pointercancel", onUp);
}
