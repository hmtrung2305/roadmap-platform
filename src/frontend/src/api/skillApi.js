import axiosClient from "./axiosClient";

export const skillApi = {
  searchSkills: async ({ search = "", category = "", limit = 20, offset = 0 } = {}) => {
    const response = await axiosClient.get("/skills", {
      params: {
        search: search || undefined,
        category: category || undefined,
        limit,
        offset,
      },
    });

    return {
      items: Array.isArray(response.data?.items) ? response.data.items : [],
      totalCount: Number(response.data?.totalCount ?? 0),
      limit: Number(response.data?.limit ?? limit),
      offset: Number(response.data?.offset ?? offset),
      hasMore: Boolean(response.data?.hasMore),
    };
  },

  getSuggestions: async ({ limit = 6 } = {}) => {
    const response = await axiosClient.get("/skills/suggestions", {
      params: { limit },
    });

    return Array.isArray(response.data) ? response.data : [];
  },

  getSkillById: async (skillId) => {
    const response = await axiosClient.get(`/skills/${encodeURIComponent(skillId)}`);
    return response.data;
  },

  getCategories: async () => {
    const response = await axiosClient.get("/skills/categories");
    return Array.isArray(response.data) ? response.data : [];
  },
};
