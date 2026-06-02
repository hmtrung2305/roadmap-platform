import axiosClient from "./axiosClient";

export async function getSavedGitHubRepositoriesApi() {
    const res = await axiosClient.get("/integrations/github/repositories");
    return res.data;
}
export async function syncGitHubRepositoriesApi() {
    const res = await axiosClient.post("/integrations/github/repositories/sync");
    return res.data;
}