import { create } from "zustand";
import { aiCreditApi } from "../api/aiCreditApi";
import { cachedRequest, invalidateRequestCache, setCachedRequestData } from "../utils/requestCacheUtils";

const AI_CREDIT_CACHE_KEY = "ai-credit:status";
const AI_CREDIT_TTL_MS = 60 * 1000;

let aiCreditRequestVersion = 0;

export const useAiCreditStore = create((set, get) => ({
  creditStatus: null,
  isLoadingCreditStatus: false,
  creditStatusError: null,

  loadCreditStatus: async ({ force = false } = {}) => {
    const requestVersion = ++aiCreditRequestVersion;

    try {
      set((state) => ({
        isLoadingCreditStatus: force || !state.creditStatus,
        creditStatusError: null,
      }));

      const status = await cachedRequest(
        AI_CREDIT_CACHE_KEY,
        aiCreditApi.getStatus,
        { ttlMs: AI_CREDIT_TTL_MS, force },
      );

      if (requestVersion === aiCreditRequestVersion) {
        set({
          creditStatus: status,
          isLoadingCreditStatus: false,
          creditStatusError: null,
        });
      }

      return status;
    } catch (error) {
      if (requestVersion === aiCreditRequestVersion) {
        set({
          creditStatus: null,
          isLoadingCreditStatus: false,
          creditStatusError: error?.message || "Unable to load AI credit status.",
        });
      }

      throw error;
    }
  },

  patchCreditStatus: (status) => {
    if (!status) return;

    setCachedRequestData(AI_CREDIT_CACHE_KEY, status);

    set({
      creditStatus: status,
      creditStatusError: null,
    });
  },

  invalidateCreditStatus: () => {
    invalidateRequestCache(AI_CREDIT_CACHE_KEY);
  },

  resetAiCredit: () => {
    aiCreditRequestVersion += 1;
    invalidateRequestCache(AI_CREDIT_CACHE_KEY);

    set({
      creditStatus: null,
      isLoadingCreditStatus: false,
      creditStatusError: null,
    });
  },

  getCreditStatus: () => get().creditStatus,
}));
