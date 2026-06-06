import axiosClient from "./axiosClient";

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

export const chatApi = {
  sendMessage: async ({ prompt, resourceId, conversationId }) => {
    // Validate the conversation id strictly. If it is not a valid string, fall back to EMPTY_GUID.
    const validConversationId = (typeof conversationId === "string" && conversationId.trim() !== "")
      ? conversationId
      : EMPTY_GUID;

    const res = await axiosClient.post("/chat", {
      prompt: prompt,
      resourceId: resourceId,
      conversationId: validConversationId 
    });
    
    return res.data;
  },
};