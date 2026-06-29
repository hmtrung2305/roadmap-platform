import axiosClient from "./axiosClient";

function unwrapEnvelope(response) {
  const payload = response.data;

  if (payload && typeof payload === "object") {
    if ("Data" in payload) return payload.Data;
    if ("data" in payload) return payload.data;
  }

  return payload;
}

export const adminUsersApi = {
  getUsers: async ({ search } = {}) => {
    const params = new URLSearchParams();

    if (search) {
      params.set("search", search);
    }

    const response = await axiosClient.get("/admin/users", { params });
    return unwrapEnvelope(response) || [];
  },

  getUser: async (userId) => {
    const response = await axiosClient.get(`/admin/users/${userId}`);
    return unwrapEnvelope(response);
  },

  assignRole: async (userId, roleId) => {
    const response = await axiosClient.post(`/admin/users/${userId}/roles/${roleId}`);
    return unwrapEnvelope(response);
  },

  revokeRole: async (userId, roleId) => {
    const response = await axiosClient.delete(`/admin/users/${userId}/roles/${roleId}`);
    return unwrapEnvelope(response);
  },
};
