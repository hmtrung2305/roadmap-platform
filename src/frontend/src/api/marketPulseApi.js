import axiosClient from "./axiosClient";

export const marketPulseApi = {
  getOverview: async ({
    days = 30,
    skills = [],
    category,
    location,
    experience,
    source,
    salaryMinMonthlyVnd,
    salaryMaxMonthlyVnd,
  } = {}) => {
    const params = new URLSearchParams();
    params.set("days", String(days));

    skills.forEach((skill) => {
      if (skill) {
        params.append("skills", skill);
      }
    });

    const optionalParams = {
      category,
      location,
      experience,
      source,
      salaryMinMonthlyVnd,
      salaryMaxMonthlyVnd,
    };

    Object.entries(optionalParams).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        params.set(key, String(value));
      }
    });

    const response = await axiosClient.get("/market-pulse/overview", { params });
    return response.data;
  },
};
