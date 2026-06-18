export function startBackendRedirect(url) {
  if (!url) return;
  window.location.assign(url);
}
