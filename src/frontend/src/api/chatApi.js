import axiosClient from "./axiosClient";

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

export const chatApi = {
  getCredits: async () => {
    const res = await axiosClient.get("/chat/credits");

    return res.data;
  },

  sendMessage: async ({ prompt, resourceId, conversationId }) => {
    const validConversationId =
      typeof conversationId === "string" && conversationId.trim() !== ""
        ? conversationId
        : EMPTY_GUID;

    const res = await axiosClient.post("/chat", {
      prompt,
      resourceId,
      conversationId: validConversationId,
    });

    return res.data;
  },
};