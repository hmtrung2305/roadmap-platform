import axiosClient from "./axiosClient";

function normalizeArray(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

export const skillGapApi = {
  getCareerRoles: async () => {
    const response = await axiosClient.get("/career-roles");
    return normalizeArray(response.data);
  },

  getCareerRoleBySlug: async (slug) => {
    const response = await axiosClient.get(`/career-roles/${slug}`);
    return response.data;
  },

  getAssessmentSkills: async (slug) => {
    const response = await axiosClient.get(`/career-roles/${slug}/assessment-skills`);
    return response.data;
  },

  analyzeSkillGap: async ({ careerRoleSlug, selectedSkillSlugs = [] }) => {
    const response = await axiosClient.post("/career-roles/skill-gap/analyze", {
      careerRoleSlug,
      selectedSkillSlugs,
    });

    return response.data;
  },
};
