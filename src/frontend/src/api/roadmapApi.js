import axiosClient from "./axiosClient";

export const roadmapApi = {
  getRoadmaps: async () => {
    const response = await axiosClient.get("/roadmaps");
    return Array.isArray(response.data) ? response.data : [];
  },

  getRoadmapGraph: async (slug) => {
    const response = await axiosClient.get(`/roadmaps/${encodeURIComponent(slug)}/graph`);
    return response.data;
  },

  getCurrentEnrollment: async (roadmapVersionId) => {
    try {
      const response = await axiosClient.get("/roadmap-enrollments/current", {
        params: { roadmapVersionId },
      });

      return response.data || null;
    } catch (error) {
      if (error?.status === 204 || error?.status === 404) {
        return null;
      }

      throw error;
    }
  },

  enroll: async (roadmapVersionId) => {
    const response = await axiosClient.post("/roadmap-enrollments", {
      roadmapVersionId,
    });

    return response.data;
  },

  migrateEnrollment: async (roadmapEnrollmentId, targetRoadmapVersionId) => {
    const response = await axiosClient.post(
      `/roadmap-enrollments/${encodeURIComponent(roadmapEnrollmentId)}/migrate`,
      { targetRoadmapVersionId },
    );

    return response.data;
  },

  getNodeDetail: async (roadmapVersionId, nodeId) => {
    const response = await axiosClient.get(`/roadmaps/${roadmapVersionId}/nodes/${nodeId}`);
    return response.data;
  },

  updateNodeProgress: async ({ enrollmentId, nodeId, status }) => {
    const response = await axiosClient.patch(
      `/roadmap-enrollments/${enrollmentId}/nodes/${nodeId}/progress`,
      { status }
    );

    return response.data;
  },
};
