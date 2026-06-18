const DEFAULT_BACKEND_BASE_URL = import.meta.env.DEV ? "https://localhost:7103" : "";
const CONFIGURED_BACKEND_BASE_URL =
  import.meta.env.VITE_BACKEND_BASE_URL || import.meta.env.VITE_API_BASE_URL || "";

function normalizeBackendBaseUrl(value) {
  return (value || DEFAULT_BACKEND_BASE_URL)
    .replace(/\/api\/?$/, "")
    .replace(/\/$/, "");
}

if (!import.meta.env.DEV && !CONFIGURED_BACKEND_BASE_URL) {
  throw new Error("VITE_BACKEND_BASE_URL must be set for production builds.");
}

export const BACKEND_BASE_URL = normalizeBackendBaseUrl(CONFIGURED_BACKEND_BASE_URL);

export const API_BASE_URL = `${BACKEND_BASE_URL}/api`;

export const CAPTCHA_PROVIDER =
  import.meta.env.VITE_CAPTCHA_PROVIDER || "Turnstile";

export const CAPTCHA_SITE_KEY =
  import.meta.env.VITE_CAPTCHA_SITE_KEY ||
  import.meta.env.VITE_TURNSTILE_SITE_KEY ||
  "";

export const CAPTCHA_ENABLED =
  CAPTCHA_PROVIDER.toLowerCase() === "turnstile" && CAPTCHA_SITE_KEY.length > 0;