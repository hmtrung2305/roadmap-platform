export function getCurrentReturnUrl() {
  return `${window.location.pathname}${window.location.search}${window.location.hash}`;
}
