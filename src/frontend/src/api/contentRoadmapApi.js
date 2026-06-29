import axiosClient from "./axiosClient";

const encode = (value) => encodeURIComponent(value);

export const contentManagerRoadmapApi = {
  getRoadmaps: async ({ status, search, sort = "updated_desc", page = 1, pageSize = 20, signal } = {}) => {
    const response = await axiosClient.get("/content/roadmaps", {
      params: {
        status: status && status !== "all" ? status : undefined,
        search: search?.trim() || undefined,
        sort,
        page,
        pageSize,
      },
      signal,
    });

    if (Array.isArray(response.data)) {
      return {
        items: response.data,
        totalCount: response.data.length,
        page: 1,
        pageSize: response.data.length,
        totalPages: response.data.length > 0 ? 1 : 0,
        statusCounts: { draft: 0, published: 0, archived: 0 },
      };
    }

    return {
      items: Array.isArray(response.data?.items) ? response.data.items : [],
      totalCount: Number(response.data?.totalCount ?? 0),
      page: Number(response.data?.page ?? page),
      pageSize: Number(response.data?.pageSize ?? pageSize),
      totalPages: Number(response.data?.totalPages ?? 0),
      statusCounts: {
        draft: Number(response.data?.statusCounts?.draft ?? response.data?.statusCounts?.Draft ?? 0),
        published: Number(response.data?.statusCounts?.published ?? response.data?.statusCounts?.Published ?? 0),
        archived: Number(response.data?.statusCounts?.archived ?? response.data?.statusCounts?.Archived ?? 0),
      },
    };
  },

  getRoadmapDetail: async ({ roadmapId, versionId, signal }) => {
    const response = await axiosClient.get(`/content/roadmaps/${encode(roadmapId)}`, {
      params: { versionId: versionId || undefined },
      signal,
    });

    return response.data;
  },

  createRoadmap: async (payload) => {
    const response = await axiosClient.post("/content/roadmaps", payload);

    return response.data;
  },

  getCareerRoles: async () => {
    const response = await axiosClient.get("/skill-gap/career-roles");

    if (Array.isArray(response.data)) return response.data;
    if (Array.isArray(response.data?.items)) return response.data.items;
    return [];
  },

  cloneVersionToDraft: async (roadmapVersionId, payload = {}) => {
    const response = await axiosClient.post(
      `/content/roadmap-versions/${encode(roadmapVersionId)}/clone-draft`,
      payload,
    );

    return response.data;
  },

  createPatchDraft: async (roadmapVersionId, payload = {}) => {
    const response = await axiosClient.post(
      `/content/roadmap-versions/${encode(roadmapVersionId)}/patch-draft`,
      payload,
    );

    return response.data;
  },

  validateVersion: async (roadmapVersionId) => {
    const response = await axiosClient.post(
      `/content/roadmap-versions/${encode(roadmapVersionId)}/validate`,
    );

    return response.data;
  },

  publishVersion: async (roadmapVersionId) => {
    const response = await axiosClient.post(
      `/content/roadmap-versions/${encode(roadmapVersionId)}/publish`,
    );

    return response.data;
  },

  deleteDraftVersion: async (roadmapVersionId) => {
    await axiosClient.delete(`/content/roadmap-versions/${encode(roadmapVersionId)}`);
  },

  createNode: async (roadmapVersionId, payload) => {
    const response = await axiosClient.post(
      `/content/roadmap-versions/${encode(roadmapVersionId)}/nodes`,
      payload,
    );

    return response.data;
  },

  moveNode: async (roadmapNodeId, direction) => {
    const response = await axiosClient.post(
      `/content/roadmap-nodes/${encode(roadmapNodeId)}/move`,
      { direction },
    );

    return response.data;
  },

  deleteNode: async (roadmapNodeId) => {
    const response = await axiosClient.delete(`/content/roadmap-nodes/${encode(roadmapNodeId)}`);

    return response.data;
  },
  updateVersionMetadata: async (roadmapVersionId, payload) => {
    const response = await axiosClient.patch(
      `/content/roadmap-versions/${encode(roadmapVersionId)}/metadata`,
      payload,
    );

    return response.data;
  },

  updateNodeMetadata: async (roadmapNodeId, payload) => {
    const response = await axiosClient.patch(
      `/content/roadmap-nodes/${encode(roadmapNodeId)}/metadata`,
      payload,
    );

    return response.data;
  },

  searchLearningResources: async ({ search = "", limit = 20 } = {}) => {
    const response = await axiosClient.get("/content/learning-resources", {
      params: {
        search: search?.trim() || undefined,
        limit,
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  },

  addNodeResource: async (roadmapNodeId, learningResourceId) => {
    const response = await axiosClient.post(
      `/content/roadmap-nodes/${encode(roadmapNodeId)}/resources`,
      { learningResourceId },
    );

    return response.data;
  },

  removeNodeResource: async (roadmapNodeId, learningResourceId) => {
    const response = await axiosClient.delete(
      `/content/roadmap-nodes/${encode(roadmapNodeId)}/resources/${encode(learningResourceId)}`,
    );

    return response.data;
  },

  addNodeSkill: async (roadmapNodeId, skillId) => {
    const response = await axiosClient.post(
      `/content/roadmap-nodes/${encode(roadmapNodeId)}/skills`,
      { skillId },
    );

    return response.data;
  },

  removeNodeSkill: async (roadmapNodeId, skillId) => {
    const response = await axiosClient.delete(
      `/content/roadmap-nodes/${encode(roadmapNodeId)}/skills/${encode(skillId)}`,
    );

    return response.data;
  },
};
