import { create } from "zustand";
import { chatApi } from "../api/chatApi";

const createUserMessage = (content) => ({
  id: crypto.randomUUID(),
  role: "user",
  content,
  createdAt: new Date().toISOString(),
});

const createAssistantMessage = (content) => ({
  id: crypto.randomUUID(),
  role: "assistant",
  content,
  createdAt: new Date().toISOString(),
});

export const useChatStore = create((set, get) => ({
  messagesByResourceId: {},
  conversationIdByResourceId: {},

  isSending: false,
  creditStatus: null,
  error: null,

  loadCreditStatus: async () => {
    try {
      const creditStatus = await chatApi.getCredits();

      set({
        creditStatus,
      });
    } catch (error) {
      console.log("Load AI credit status failed:", error);
    }
  },

  getMessagesByResourceId: (resourceId) => {
    return get().messagesByResourceId[resourceId] || [];
  },

  sendMessage: async ({ resourceId, prompt }) => {
    if (!resourceId || !prompt.trim()) return;

    const userMessage = createUserMessage(prompt.trim());
    const currentMessages = get().messagesByResourceId[resourceId] || [];
    const currentConversationId =
      get().conversationIdByResourceId[resourceId] || null;

    try {
      set((state) => ({
        isSending: true,
        error: null,
        messagesByResourceId: {
          ...state.messagesByResourceId,
          [resourceId]: [...currentMessages, userMessage],
        },
      }));

      const data = await chatApi.sendMessage({
        prompt: prompt.trim(),
        resourceId,
        conversationId: currentConversationId,
      });

      const assistantText = data.response || "AI has not replied yet.";
      const nextConversationId = data.conversationId || currentConversationId;
      const assistantMessage = createAssistantMessage(assistantText);

      set((state) => ({
        isSending: true,
        error: null,
        messagesByResourceId: {
          ...state.messagesByResourceId,
          [resourceId]: [
            ...(state.messagesByResourceId[resourceId] || []),
            assistantMessage,
          ],
        },
        conversationIdByResourceId: {
          ...state.conversationIdByResourceId,
          [resourceId]: nextConversationId,
        },
        creditStatus: data.credits || state.creditStatus,
      }));
    } catch (error) {
      console.log("Send chat message failed:", error);

      const limitMessage =
        error.status === 429
          ? "You have used all AI credits for today. Your credits reset tomorrow."
          : "The service is currently experiencing high demand. Please try again in a few moments.";

      set({
        error: limitMessage,
        creditStatus: error.raw?.credits || get().creditStatus,
      });
    } finally {
      set({
        isSending: false,
      });
    }
  },

  clearChatByResourceId: (resourceId) => {
    set((state) => {
      const nextMessagesByResourceId = { ...state.messagesByResourceId };
      const nextConversationIdByResourceId = {
        ...state.conversationIdByResourceId,
      };

      delete nextMessagesByResourceId[resourceId];
      delete nextConversationIdByResourceId[resourceId];

      return {
        messagesByResourceId: nextMessagesByResourceId,
        conversationIdByResourceId: nextConversationIdByResourceId,
      };
    });
  },

  clearError: () => {
    set({
      error: null,
    });
  },
}));