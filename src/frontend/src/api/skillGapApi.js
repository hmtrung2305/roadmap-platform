import axiosClient from "./axiosClient";

const encode = (value) => encodeURIComponent(String(value || ""));

function normalizeArray(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

export const skillGapApi = {
  getCareerRoles: async () => {
    const response = await axiosClient.get("/skill-gap/career-roles");
    return normalizeArray(response.data);
  },

  getAssessmentLevels: async (careerRoleSlug) => {
    const response = await axiosClient.get(`/skill-gap/${encode(careerRoleSlug)}/levels`);
    return normalizeArray(response.data);
  },

  getAssessmentByLevel: async (careerRoleSlug, levelSlug) => {
    const response = await axiosClient.get(
      `/skill-gap/${encode(careerRoleSlug)}/assessment/${encode(levelSlug)}`,
    );
    return response.data;
  },

  analyzeSkillGap: async ({ careerRoleSlug, levelSlug, selectedNodeIds = [] }) => {
    const response = await axiosClient.post("/skill-gap/analyze", {
      careerRoleSlug,
      levelSlug,
      selectedNodeIds,
    });

    return response.data;
  },

  getHistory: async () => {
    const response = await axiosClient.get("/me/skill-gap/history");
    return normalizeArray(response.data);
  },

  getHistoryDetail: async (historyId) => {
    const response = await axiosClient.get(`/me/skill-gap/history/${encode(historyId)}`);
    return response.data;
  },

  deleteHistory: async (historyId) => {
    await axiosClient.delete(`/me/skill-gap/history/${encode(historyId)}`);
  },

  getAdminAssessmentLevels: async (careerRoleSlug) => {
    const response = await axiosClient.get(`/content/skill-gap/${encode(careerRoleSlug)}/levels`);
    return normalizeArray(response.data);
  },

  getAdminGroupsByLevel: async (careerRoleSlug, levelSlug) => {
    const response = await axiosClient.get(
      `/content/skill-gap/${encode(careerRoleSlug)}/levels/${encode(levelSlug)}/groups`,
    );
    return response.data;
  },

  updateAdminGroupsByLevel: async ({ careerRoleSlug, levelSlug, groupIds = [] }) => {
    await axiosClient.put(
      `/content/skill-gap/${encode(careerRoleSlug)}/levels/${encode(levelSlug)}/groups`,
      { groupIds },
    );
  },
};
