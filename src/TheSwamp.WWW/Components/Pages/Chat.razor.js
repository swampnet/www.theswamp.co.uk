// Scrolls the element to the bottom. Used on initial page load and whenever a
// new message arrives, to keep the latest message in view.
export function scrollToBottom(elementId) {
	const el = document.getElementById(elementId);
	if (el) {
		el.scrollTop = el.scrollHeight;
	}
}

// Not currently used, but retained for future use if smart-scroll behaviour is needed —
// e.g. to avoid yanking the user back to the bottom when they've scrolled up to read history.
export function scrollToBottomIfNearEnd(elementId, threshold = 100) {
	const el = document.getElementById(elementId);
	if (!el) {
		return;
	}
	const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
	if (distanceFromBottom <= threshold) {
		el.scrollTop = el.scrollHeight;
	}
}
