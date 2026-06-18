import { BACKEND_BASE_URL } from "./apiConfig";
import axiosClient from "./axiosClient";

export async function getAuthProvidersApi() {
  const res = await axiosClient("/me/auth-providers");
  return res.data;
}

export const linkLocalLoginApi = async (payload) => {
  const res = await axiosClient.post("/me/auth-providers/local", payload);
  return res.data;
};

export const verifyLinkedLocalEmailApi = async (payload) => {
  const res = await axiosClient.post("/me/auth-providers/local/verify", payload);
  return res.data;
};

export const resendLinkedLocalVerificationApi = async () => {
  const res = await axiosClient.post("/me/auth-providers/local/resend-verification");
  return res.data;
};

export const requestLocalEmailChangeApi = async (payload) => {
  const res = await axiosClient.post("/me/auth-providers/local/email/change-request", payload);
  return res.data;
};

export const verifyLocalEmailChangeApi = async (payload) => {
  const response = await axiosClient.post(
    "/me/auth-providers/local/email/verify",
    payload,
  );

  return response.data;
};

export const resendLocalEmailChangeVerificationApi = async () => {
  const response = await axiosClient.post(
    "/me/auth-providers/local/email/resend-verification",
  );

  return response.data;
};

export const changeLocalPasswordApi = async (payload) => {
  const response = await axiosClient.put(
    "/me/auth-providers/local/password",
    payload,
  );

  return response.data;
};

export const unlinkAuthProviderApi = async (provider) => {
  const response = await axiosClient.delete(`/me/auth-providers/${provider}`);
  return response.data;
};

export const GOOGLE_LINK_URL = `${BACKEND_BASE_URL}/api/me/auth-providers/google/link`;
export const GITHUB_LINK_URL = `${BACKEND_BASE_URL}/api/me/auth-providers/github/link`;

function buildQuery(params = {}) {
  const query = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim()) {
      query.set(key, value);
    }
  });

  const queryString = query.toString();
  return queryString ? `?${queryString}` : "";
}

export const redirectToGoogleLink = () => {
  window.location.assign(GOOGLE_LINK_URL);
};

export const redirectToGitHubLink = ({ returnUrl } = {}) => {
  window.location.assign(`${GITHUB_LINK_URL}${buildQuery({ returnUrl })}`);
};
