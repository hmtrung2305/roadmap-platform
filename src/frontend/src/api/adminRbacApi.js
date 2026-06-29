import axiosClient from "./axiosClient";

function unwrapEnvelope(response) {
  const payload = response.data;

  if (payload && typeof payload === "object") {
    if ("Data" in payload) return payload.Data;
    if ("data" in payload) return payload.data;
  }

  return payload;
}

export const adminRbacApi = {
  getRoles: async () => {
    const response = await axiosClient.get("/Role");
    return unwrapEnvelope(response) || [];
  },

  getRole: async (roleId) => {
    const response = await axiosClient.get(`/Role/${roleId}`);
    return unwrapEnvelope(response);
  },

  createRole: async (payload) => {
    const response = await axiosClient.post("/Role", payload);
    return unwrapEnvelope(response);
  },

  updateRole: async (roleId, payload) => {
    const response = await axiosClient.put(`/Role/${roleId}`, payload);
    return unwrapEnvelope(response);
  },

  deleteRole: async (roleId) => {
    await axiosClient.delete(`/Role/${roleId}`);
    return null;
  },

  replaceRolePermissions: async (roleId, permissionIds) => {
    const response = await axiosClient.put(`/Role/${roleId}/permissions`, { permissionIds });
    return unwrapEnvelope(response);
  },

  grantRolePermission: async (roleId, permissionId) => {
    const response = await axiosClient.post(`/Role/${roleId}/permissions/${permissionId}`);
    return unwrapEnvelope(response);
  },

  revokeRolePermission: async (roleId, permissionId) => {
    const response = await axiosClient.delete(`/Role/${roleId}/permissions/${permissionId}`);
    return unwrapEnvelope(response);
  },

  getPermissions: async () => {
    const response = await axiosClient.get("/Permission");
    return unwrapEnvelope(response) || [];
  },

  getPermission: async (permissionId) => {
    const response = await axiosClient.get(`/Permission/${permissionId}`);
    return unwrapEnvelope(response);
  },

  createPermission: async (payload) => {
    const response = await axiosClient.post("/Permission", payload);
    return unwrapEnvelope(response);
  },

  updatePermission: async (permissionId, payload) => {
    const response = await axiosClient.put(`/Permission/${permissionId}`, payload);
    return unwrapEnvelope(response);
  },

  deletePermission: async (permissionId) => {
    await axiosClient.delete(`/Permission/${permissionId}`);
    return null;
  },
};
