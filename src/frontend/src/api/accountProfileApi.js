import axiosClient from "./axiosClient";

export async function getAccountProfileApi() {
  const response = await axiosClient.get("/me/account-profile");
  return response.data;
}

export async function updateAccountProfileApi(payload) {
  const response = await axiosClient.patch("/me/account-profile", payload);
  return response.data;
}
