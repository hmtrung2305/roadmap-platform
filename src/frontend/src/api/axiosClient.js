import axios from "axios";
import { API_BASE_URL } from "./apiConfig";
import { normalizeApiError } from "../utils/apiErrorUtils";

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

    if (normalizedError.status === 401) {
      console.warn("Unauthorized request:", normalizedError.url);
    }

    return Promise.reject(normalizedError);
  }
);

export default axiosClient;
