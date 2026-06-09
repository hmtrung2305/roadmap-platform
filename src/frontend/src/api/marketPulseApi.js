import axiosClient from "./axiosClient";

export const marketPulseApi = {
  getOverview: async ({ days = 30, skills = [] } = {}) => {
    const params = new URLSearchParams();
    params.set("days", String(days));

    skills.forEach((skill) => {
      if (skill) {
        params.append("skills", skill);
      }
    });

    const response = await axiosClient.get("/market-pulse/overview", { params });
    return response.data;
  },
};
