import axiosClient from "./axiosClient";

function parseMetadata(metadata) {
  if (!metadata) return null;

  try {
    return typeof metadata === "string" ? JSON.parse(metadata) : metadata;
  } catch {
    return null;
  }
}

function mapResource(item) {
  return {
    resourceId: item.resourceId || item.id,
    title: item.title || "Untitled Resource",
    skillId: item.skillId || null,
    skillName: item.skillName || item.skill?.skillName || "General",
    url: item.url || item.fileUrl || "",
    type: item.type || "article",
    durationMinutes: item.durationMinutes || 15,
    isCompleted: item.isCompleted || false,
    isCurrent: item.isCurrent || false,
    createdAt: item.createdAt || item.created_at || null,
    metadata: parseMetadata(item.metadata),
  };
}

export const resourceApi = {
  getAll: async () => {
    const response = await axiosClient.get("/resources");

    return response.data.map(mapResource);
  },

  upload: async ({ title, skillName, file }) => {
    const formData = new FormData();

    formData.append("title", title);
    formData.append("skillName", skillName);
    formData.append("file", file);

    const response = await axiosClient.post("/resources/upload", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });

    return response.data;
  },

  delete: async (resourceId) => {
    const response = await axiosClient.delete(`/resources/${resourceId}`);

    return response.data;
  },
};