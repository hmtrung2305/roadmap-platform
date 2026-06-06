import { BACKEND_BASE_URL } from "./apiConfig";
import axiosClient from "./axiosClient";

export async function getAuthProvidersApi(){
    const res = await axiosClient("/me/auth-providers");
    return res.data;
}

export const linkLocalLoginApi = async (payload) => {
    const res = await axiosClient.post("/me/auth-providers/local", payload);
    return res.data;
}
export const verifyLinkedLocalEmailApi = async(payload) => {
    const res = await axiosClient.post("/me/auth-providers/local/verify", payload);
    return res.data;
}
export const resendLinkedLocalVerificationApi = async () => {
    const res = await axiosClient.post("/me/auth-providers/local/resend-verification");
    return res.data;
}
export const requestLocalEmailChangeApi = async (payload) => {
    const res = await axiosClient.post("/me/auth-providers/local/email/change-request", payload);
    return res.data;
}

export const verifyLocalEmailChangeApi = async (payload) => {
  const response = await axiosClient.post(
    "/me/auth-providers/local/email/verify",
    payload
  );

  return response.data;
};

export const changeLocalPasswordApi = async (payload) => {
  const response = await axiosClient.put(
    "/me/auth-providers/local/password",
    payload
  );

  return response.data;
};

export const unlinkAuthProviderApi = async (provider) => {
  const response = await axiosClient.delete(`/me/auth-providers/${provider}`);
  return response.data;
};

export const redirectToGoogleLink = () => {
  window.location.href = `${BACKEND_BASE_URL}/api/me/auth-providers/google/link`;
};

export const redirectToGitHubLink = () => {
  window.location.href = `${BACKEND_BASE_URL}/api/me/auth-providers/github/link`;
};