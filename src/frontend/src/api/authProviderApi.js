import { BACKEND_BASE_URL } from "./apiConfig";
import axiosClient from "./axiosClient";

export async function getAuthProvidersApi(){
    const res = await axiosClient("/me/auth-providers");
    return res.data;
}

export function redirectToGitHubLink(){
    window.location.href =  `${BACKEND_BASE_URL}/api/me/auth-providers/github/link`;
}