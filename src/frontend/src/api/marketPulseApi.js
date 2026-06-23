import axiosClient from "./axiosClient";

function unwrapEnvelope(response) {
  const payload = response.data;

  if (payload && typeof payload === "object" && "ok" in payload) {
    if (payload.ok) {
      return payload.data;
    }

    const message = payload.error?.message || "Market Pulse request failed.";
    const error = new Error(message);
    error.code = payload.error?.code;
    error.details = payload.error?.details;
    throw error;
  }

  return payload;
}

function appendOptionalParams(params, values) {
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") {
      params.set(key, String(value));
    }
  });
}

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

    appendOptionalParams(params, {
      category,
      location,
      experience,
      source,
      salaryMinMonthlyVnd,
      salaryMaxMonthlyVnd,
    });

    const response = await axiosClient.get("/market-pulse/overview", { params });
    return unwrapEnvelope(response);
  },

  refresh: async (options = {}) => {
    const response = await axiosClient.post("/market-pulse/admin/refresh", options);
    return unwrapEnvelope(response);
  },

  getCrawlRuns: async ({ status, source, from, to, limit = 50 } = {}) => {
    const params = new URLSearchParams();
    appendOptionalParams(params, { status, source, from, to, limit });
    const response = await axiosClient.get("/market-pulse/admin/crawl-runs", { params });
    return unwrapEnvelope(response);
  },

  getFailedItems: async ({ status, source, from, to, limit = 50 } = {}) => {
    const params = new URLSearchParams();
    appendOptionalParams(params, { status, source, from, to, limit });
    const response = await axiosClient.get("/market-pulse/admin/failed-items", { params });
    return unwrapEnvelope(response);
  },

  retryFailedItem: async (failedItemId) => {
    const response = await axiosClient.post(`/market-pulse/admin/failed-items/${failedItemId}/retry`);
    return unwrapEnvelope(response);
  },

  retryFailedItems: async (failedItemIds) => {
    const response = await axiosClient.post("/market-pulse/admin/failed-items/retry", { failedItemIds });
    return unwrapEnvelope(response);
  },

  ignoreFailedItem: async (failedItemId) => {
    const response = await axiosClient.post(`/market-pulse/admin/failed-items/${failedItemId}/ignore`);
    return unwrapEnvelope(response);
  },

  ignoreFailedItems: async (failedItemIds) => {
    const response = await axiosClient.post("/market-pulse/admin/failed-items/ignore", { failedItemIds });
    return unwrapEnvelope(response);
  },

  getClassifierCategories: async () => {
    const response = await axiosClient.get("/market-pulse/admin/classifier/categories");
    return unwrapEnvelope(response);
  },

  getClassifierMappings: async () => {
    const response = await axiosClient.get("/market-pulse/admin/classifier/mappings");
    return unwrapEnvelope(response);
  },

  createClassifierMapping: async (payload) => {
    const response = await axiosClient.post("/market-pulse/admin/classifier/mappings", payload);
    return unwrapEnvelope(response);
  },

  updateClassifierMapping: async (mappingId, payload) => {
    const response = await axiosClient.put(`/market-pulse/admin/classifier/mappings/${mappingId}`, payload);
    return unwrapEnvelope(response);
  },

  deleteClassifierMapping: async (mappingId) => {
    const response = await axiosClient.delete(`/market-pulse/admin/classifier/mappings/${mappingId}`);
    return unwrapEnvelope(response);
  },

  testClassifier: async (text) => {
    const response = await axiosClient.post("/market-pulse/admin/classifier/test", { text });
    return unwrapEnvelope(response);
  },

  getSourceHealth: async () => {
    const response = await axiosClient.get("/market-pulse/admin/source-health");
    return unwrapEnvelope(response);
  },
};
