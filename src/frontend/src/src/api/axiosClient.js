import axios from "axios";
import { API_BASE_URL } from "./apiConfig";
const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // dùng để gửi cookie cùng với request
});

// axiosClient.interceptors.request.use(
//   (config) => {
//     const token = localStorage.getItem("accessToken");
//     if (token) {
//       config.headers.Authorization = `Bearer ${token}`;
//     }
//     return config;
//   },
//   (error) => {
//     return Promise.reject(error);
//   },
// );

axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status;
    const url = error.config?.url;

    const normalizedError = {
      status,
      message: extractErrorMessage(error),
      raw: error.response?.data,
      url,
    };

    if (status === 401) {
      console.warn("Unauthorized request:", url);

      const currentPath = window.location.pathname;

      const isPublicPath =
        currentPath === "/login" || currentPath === "/register";

      const isLoginRequest = url?.includes("/auth/login");
      const isRegisterRequest = url?.includes("/auth/register");
      const isMeRequest = url?.includes("/me");

      const shouldClearAuth =
        !isPublicPath && !isLoginRequest && !isRegisterRequest && !isMeRequest;

      if (shouldClearAuth) {
        const { useAuthStore } = await import("../stores/useAuthStore");

        useAuthStore.getState().clearAuth();

        window.location.href = "/login";
      }
    }

    return Promise.reject(normalizedError);
  },
);

function extractErrorMessage(error) {
  const data = error.response?.data;

  if (!data) {
    return error.message || "Network error. Please try again.";
  }

  if (typeof data === "string") {
    return data;
  }

  if (data.message) {
    return data.message;
  }

  if (data.error) {
    return data.error;
  }

  if (data.title && !data.errors) {
    return data.title;
  }

  if (data.errors) {
    const validationMessages = Object.values(data.errors)
      .flat()
      .filter(Boolean);

    if (validationMessages.length > 0) {
      return validationMessages[0];
    }
  }

  return "Something went wrong. Please try again.";
}

export default axiosClient;
