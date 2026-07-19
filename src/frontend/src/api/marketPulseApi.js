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
    salaryMinMonthlyVnd,
    salaryMaxMonthlyVnd,
    signal,
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
      salaryMinMonthlyVnd,
      salaryMaxMonthlyVnd,
    });

    const response = await axiosClient.get("/market-pulse/overview", { params, signal });
    return unwrapEnvelope(response);
  },

  getAdminDashboard: async ({ signal } = {}) => {
    const response = await axiosClient.get("/market-pulse/admin/dashboard", { signal });
    return unwrapEnvelope(response);
  },

  createRefreshOperation: async (options = {}) => {
    const response = await axiosClient.post("/market-pulse/admin/refresh-operations", options);
    return unwrapEnvelope(response);
  },

  getCurrentRefreshOperation: async ({ signal } = {}) => {
    const response = await axiosClient.get("/market-pulse/admin/refresh-operations/current", { signal });
    return unwrapEnvelope(response);
  },

  getRefreshOperation: async (operationId, { signal } = {}) => {
    const response = await axiosClient.get(`/market-pulse/admin/refresh-operations/${operationId}`, { signal });
    return unwrapEnvelope(response);
  },

  getImportRuns: async ({ status, from, to, limit = 50, signal } = {}) => {
    const params = new URLSearchParams();
    appendOptionalParams(params, { status, from, to, limit });
    const response = await axiosClient.get("/market-pulse/admin/import-runs", { params, signal });
    return unwrapEnvelope(response);
  },

  getOperationsFailures: async ({ status = "open", type, from, to, limit = 50, signal } = {}) => {
    const params = new URLSearchParams();
    appendOptionalParams(params, { status, type, from, to, limit });
    const response = await axiosClient.get("/market-pulse/admin/failures", { params, signal });
    return unwrapEnvelope(response);
  },

  retryOperationsFailures: async (failureIds) => {
    const response = await axiosClient.post("/market-pulse/admin/failures/retry", { failureIds });
    return unwrapEnvelope(response);
  },

  ignoreOperationsFailures: async (failureIds) => {
    const response = await axiosClient.post("/market-pulse/admin/failures/ignore", { failureIds });
    return unwrapEnvelope(response);
  },

  importLatest: async (options = {}) => {
    const response = await axiosClient.post("/market-pulse/admin/import-latest", {
      jobsApiPageSize: options.jobsApiPageSize ?? options.pageSize,
      jobsApiMaxItems: options.jobsApiMaxItems ?? options.maxItems,
      jobsApiMaxPages: options.jobsApiMaxPages,
    });
    return unwrapEnvelope(response);
  },

  syncPublicationHistory: async (options = {}) => {
    const response = await axiosClient.post("/market-pulse/admin/history-sync", {
      lookbackDays: options.lookbackDays,
      jobsApiPageSize: options.jobsApiPageSize ?? options.pageSize,
      jobsApiMaxItems: options.jobsApiMaxItems ?? options.maxItems,
    });
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

};
