import axiosClient from "./axiosClient";

export async function getSavedGitHubRepositoriesApi() {
    const res = await axiosClient.get("/integrations/github/repositories");
    return res.data;
}
export async function syncGitHubRepositoriesApi() {
    const res = await axiosClient.post("/integrations/github/repositories/sync");
    return res.data;
}

export async function generateRepositoryInsightApi(repositoryId, { force = false } = {}) {
    const safeRepositoryId = encodeURIComponent(repositoryId);
    const res = await axiosClient.post(
        `/integrations/github/repositories/${safeRepositoryId}/insight`,
        null,
        {
            params: force ? { force: true } : undefined,
        }
    );
    return res.data;
}
