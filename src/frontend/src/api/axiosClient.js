import axios from "axios";
import { API_BASE_URL } from "./apiConfig";
import { normalizeApiError } from "../utils/apiErrorUtils";
import { dispatchUnauthorizedEvent } from "../utils/authEventUtils";

const PUBLIC_AUTH_ENDPOINTS = [
  "/auth/login",
  "/auth/register",
  "/auth/registration/verify-email",
  "/auth/registration/resend-verification",
];

function getRequestPath(url = "") {
  const rawUrl = String(url || "");

  try {
    const parsedUrl = new URL(rawUrl, API_BASE_URL);
    return parsedUrl.pathname.replace(/^\/api(?=\/)/, "");
  } catch {
    return rawUrl.split("?")[0];
  }
}

function shouldBroadcastUnauthorized(url) {
  const requestPath = getRequestPath(url);

  return !PUBLIC_AUTH_ENDPOINTS.some((endpoint) => requestPath.startsWith(endpoint));
}

const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
});

axiosClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const normalizedError = normalizeApiError({
      status: error.response?.status,
      data: error.response?.data,
      headers: error.response?.headers,
      url: error.config?.url,
      message: error.message,
    });

    if (normalizedError.status === 401 && shouldBroadcastUnauthorized(error.config?.url)) {
      dispatchUnauthorizedEvent({
        url: normalizedError.url,
        status: normalizedError.status,
        code: normalizedError.code,
      });
    }

    return Promise.reject(normalizedError);
  },
);

export default axiosClient;
