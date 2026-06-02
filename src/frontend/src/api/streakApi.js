import axiosClient from "./axiosClient"

export const getStreak = async () => {
    const response = await axiosClient.get("/streak");
    return response.data;
}
export const trackStreak = async () => {
    const response = await axiosClient.post("/streak/track");
    return response.data;
}