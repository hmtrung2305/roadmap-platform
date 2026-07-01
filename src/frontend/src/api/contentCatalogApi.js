import axiosClient from "./axiosClient";
import { clearSkillApiCache } from "./skillApi";

function encode(value) {
  return encodeURIComponent(String(value));
}

function normalizePagedResponse(data, fallbackLimit = 20, fallbackOffset = 0) {
  return {
    items: Array.isArray(data?.items) ? data.items : [],
    totalCount: Number(data?.totalCount ?? 0),
    limit: Number(data?.limit ?? fallbackLimit),
    offset: Number(data?.offset ?? fallbackOffset),
    hasMore: Boolean(data?.hasMore),
  };
}

export const contentSkillCatalogApi = {
  searchSkills: async ({ search = "", category = "", limit = 20, offset = 0 } = {}) => {
    const response = await axiosClient.get("/content/skills", {
      params: {
        search: search?.trim() || undefined,
        category: category?.trim() || undefined,
        limit,
        offset,
      },
    });

    return normalizePagedResponse(response.data, limit, offset);
  },

  createSkill: async (payload) => {
    const response = await axiosClient.post("/content/skills", payload);
    clearSkillApiCache();
    return response.data;
  },

  updateSkill: async (skillId, payload) => {
    const response = await axiosClient.patch(`/content/skills/${encode(skillId)}`, payload);
    clearSkillApiCache(skillId);
    return response.data;
  },
};

export const contentLearningResourceCatalogApi = {
  searchResources: async ({ search = "", resourceType = "", difficultyLevel = "", limit = 20, offset = 0 } = {}) => {
    const response = await axiosClient.get("/content/learning-resources", {
      params: {
        search: search?.trim() || undefined,
        resourceType: resourceType || undefined,
        difficultyLevel: difficultyLevel || undefined,
        limit,
        offset,
      },
    });

    return normalizePagedResponse(response.data, limit, offset);
  },

  createResource: async (payload) => {
    const response = await axiosClient.post("/content/learning-resources", payload);
    return response.data;
  },

  updateResource: async (resourceId, payload) => {
    const response = await axiosClient.patch(`/content/learning-resources/${encode(resourceId)}`, payload);
    return response.data;
  },
};
