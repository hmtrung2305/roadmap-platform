import axiosClient from "./axiosClient";


export async function getMyProfileApi() {
    const res = await axiosClient.get("/me/profile");
    return res.data;
}
export async function updateMyProfileApi(payload) {
    const res = await axiosClient.patch("/me/profile", payload);
    return res.data;
}