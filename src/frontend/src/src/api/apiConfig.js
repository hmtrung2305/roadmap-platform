const DEFAULT_BACKEND_BASE_URL = import.meta.env.DEV ? "https://localhost:7103" : "";

function normalizeBackendBaseUrl(value) {
  return (value || DEFAULT_BACKEND_BASE_URL)
    .replace(/\/api\/?$/, "")
    .replace(/\/$/, "");
}

export const BACKEND_BASE_URL = normalizeBackendBaseUrl(
  import.meta.env.VITE_BACKEND_BASE_URL || import.meta.env.VITE_API_BASE_URL
);

export const API_BASE_URL = `${BACKEND_BASE_URL}/api`;