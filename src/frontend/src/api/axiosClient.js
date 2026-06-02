import axios from "axios";
import { API_BASE_URL } from "./apiConfig";
const axiosClient = axios.create({
  baseURL: API_BASE_URL,
 withCredentials: true,  // dùng để gửi cookie cùng với request
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
  (error) => {
    const status = error.response?.status;
    const url = error.config?.url;

    if (status === 401) {
      console.warn("Unauthorized request:", url);
    }

    return Promise.reject(error);
  }
);
export default axiosClient;
