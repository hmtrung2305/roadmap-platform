export const UNAUTHORIZED_EVENT_NAME = "techmap:unauthorized";

export function dispatchUnauthorizedEvent(detail = {}) {
  if (typeof window === "undefined") return;

  window.dispatchEvent(
    new CustomEvent(UNAUTHORIZED_EVENT_NAME, {
      detail,
    }),
  );
}

export function subscribeToUnauthorizedEvent(callback) {
  if (typeof window === "undefined") {
    return () => {};
  }

  window.addEventListener(UNAUTHORIZED_EVENT_NAME, callback);

  return () => {
    window.removeEventListener(UNAUTHORIZED_EVENT_NAME, callback);
  };
}
