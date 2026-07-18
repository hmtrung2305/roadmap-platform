import axiosClient from "./axiosClient";

const encode = (value) => encodeURIComponent(String(value || ""));

function normalizeArray(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.items)) return data.items;
  if (Array.isArray(data?.data)) return data.data;
  return [];
}

function normalizeHistoryPage(data) {
  return {
    items: Array.isArray(data?.items) ? data.items : [],
    nextCursor:
      typeof data?.nextCursor === "string" && data.nextCursor
        ? data.nextCursor
        : null,
    hasMore: Boolean(data?.hasMore),
  };
}

export const skillGapApi = {
  getCareerRoles: async () => {
    const response = await axiosClient.get("/skill-gap/career-roles");
    return normalizeArray(response.data);
  },

  getPublishedRoadmapsByRole: async (careerRoleSlug) => {
    const response = await axiosClient.get(
      `/skill-gap/career-roles/${encode(careerRoleSlug)}/roadmaps`,
    );
    return normalizeArray(response.data);
  },

  getAssessmentByRoadmap: async (roadmapId) => {
    const response = await axiosClient.get(
      `/skill-gap/roadmaps/${encode(roadmapId)}/assessment`,
    );
    return response.data;
  },

  analyzeSkillGap: async ({ roadmapId, selectedSkillIds = [] }) => {
    const response = await axiosClient.post("/me/skill-gap/analyze", {
      roadmapId,
      selectedSkillIds,
    });

    return response.data;
  },

  getHistory: async ({ limit = 20, cursor = null } = {}) => {
    const params = { limit };

    if (cursor) {
      params.cursor = cursor;
    }

    const response = await axiosClient.get("/me/skill-gap/history", {
      params,
    });

    return normalizeHistoryPage(response.data);
  },

  getHistoryDetail: async (historyId) => {
    const response = await axiosClient.get(
      `/me/skill-gap/history/${encode(historyId)}`,
    );
    return response.data;
  },

  deleteHistory: async (historyId) => {
    await axiosClient.delete(`/me/skill-gap/history/${encode(historyId)}`);
  },

  getMyPublishedRoadmaps: async () => {
    const response = await axiosClient.get("/content/published-roadmaps");
    return normalizeArray(response.data);
  },

  getRoadmapCategories: async (roadmapId) => {
    const response = await axiosClient.get(
      `/content/roadmaps/${encode(roadmapId)}/categories`,
    );
    return response.data;
  },

  updateRoadmapCategories: async ({ roadmapId, categories = [] }) => {
    await axiosClient.put(
      `/content/roadmaps/${encode(roadmapId)}/categories`,
      categories.map((category, index) => ({
        categoryName: category.categoryName,
        displayOrder: category.displayOrder ?? index + 1,
      })),
    );
  },
};
