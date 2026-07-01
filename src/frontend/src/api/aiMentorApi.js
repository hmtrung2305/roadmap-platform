import axiosClient from "./axiosClient";

export const aiMentorApi = {
  getConversations: async () => {
    const response = await axiosClient.get("/ai-mentor/conversations");
    return Array.isArray(response.data) ? response.data : [];
  },

  getMessages: async (conversationId) => {
    if (!conversationId) return [];

    const response = await axiosClient.get(
      `/ai-mentor/conversations/${conversationId}/messages`,
    );

    return Array.isArray(response.data) ? response.data : [];
  },

  sendMessage: async (payload) => {
    const response = await axiosClient.post("/ai-mentor/chat", payload);
    return response.data;
  },

  archiveConversation: async (conversationId) => {
    if (!conversationId) return;
    await axiosClient.delete(`/ai-mentor/conversations/${conversationId}`);
  },
};
