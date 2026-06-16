import { BACKEND_BASE_URL } from "./apiConfig";
import axiosClient from "./axiosClient";
import { startBackendRedirect } from "../utils/backendRedirect";

export const GOOGLE_AUTH_URL = `${BACKEND_BASE_URL}/api/auth/google/login`;

export const GITHUB_AUTH_URL = `${BACKEND_BASE_URL}/api/auth/github/login`;

export const startGoogleLogin = () => startBackendRedirect(GOOGLE_AUTH_URL);

export const startGitHubLogin = () => startBackendRedirect(GITHUB_AUTH_URL);

export const loginApi =  async (payload) => {
    const response = await axiosClient.post("/auth/login", payload);
    return response.data;
}
export const registerApi = async (payload) => {
    const response = await axiosClient.post("/auth/register", payload);
    return response.data;
}
export const verifyRegistrationEmailApi = async (payload) => {
    const response = await  axiosClient.post("/auth/registration/verify-email", payload);
    return response.data;
}
export const resendRegistrationVerificationApi = async (payload) => {
    const response = await axiosClient.post("/auth/registration/resend-verification", payload);
    return response.data;
}
export const getCurrentUserApi = async () => {
    const response = await axiosClient.get("/me");
    return response.data;
}
export const logoutApi = async () => {
    const  response =  await axiosClient.post("/auth/logout");
    return response.data;
}
export const updateCurrentUserApi = async (payload) => {
  const response = await axiosClient.patch("/me", payload);
  return response.data;
};
