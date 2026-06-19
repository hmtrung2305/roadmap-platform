import { create } from "zustand";
import {
  getAccountProfileApi,
  updateAccountProfileApi,
} from "../api/accountProfileApi";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCache,
  setCachedRequestData,
} from "../utils/requestCacheUtils";

const ACCOUNT_PROFILE_CACHE_KEY = "account-profile:me";
const ACCOUNT_PROFILE_CACHE_TTL_MS = 5 * 60 * 1000;

let accountProfileRequestVersion = 0;

function isFresh(timestamp) {
  return timestamp && Date.now() - timestamp < ACCOUNT_PROFILE_CACHE_TTL_MS;
}

export const useAccountProfileStore = create((set, get) => ({
  accountProfile: null,
  loading: false,
  saving: false,
  error: "",
  fetchedAt: 0,

  loadAccountProfile: async ({ force = false } = {}) => {
    const state = get();

    if (!force && isFresh(state.fetchedAt)) {
      return state.accountProfile;
    }

    const requestVersion = accountProfileRequestVersion;

    try {
      set((current) => ({
        loading: !current.accountProfile,
        error: "",
      }));

      const data = await cachedRequest(
        ACCOUNT_PROFILE_CACHE_KEY,
        getAccountProfileApi,
        {
          ttlMs: ACCOUNT_PROFILE_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion !== accountProfileRequestVersion) {
        return data;
      }

      set({
        accountProfile: data,
        loading: false,
        error: "",
        fetchedAt: Date.now(),
      });

      return data;
    } catch (error) {
      if (requestVersion === accountProfileRequestVersion) {
        set({
          loading: false,
          error: getFriendlyApiErrorMessage(
            error,
            "Unable to load account profile.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === accountProfileRequestVersion) {
        set({ loading: false });
      }
    }
  },

  updateAccountProfile: async (payload) => {
    const requestVersion = accountProfileRequestVersion;

    try {
      set({ saving: true, error: "" });

      const data = await guardedMutation("account-profile:update", () =>
        updateAccountProfileApi(payload),
      );

      if (requestVersion !== accountProfileRequestVersion) {
        return data;
      }

      const nextAccountProfile = data ?? {
        ...(get().accountProfile || {}),
        ...payload,
      };

      setCachedRequestData(ACCOUNT_PROFILE_CACHE_KEY, nextAccountProfile);

      set({
        accountProfile: nextAccountProfile,
        saving: false,
        error: "",
        fetchedAt: Date.now(),
      });

      return nextAccountProfile;
    } catch (error) {
      if (requestVersion === accountProfileRequestVersion) {
        set({
          saving: false,
          error: getFriendlyApiErrorMessage(
            error,
            "Unable to update account profile.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === accountProfileRequestVersion) {
        set({ saving: false });
      }
    }
  },

  patchLocalAccountProfile: (partial) => {
    set((state) => {
      const nextAccountProfile = {
        ...(state.accountProfile || {}),
        ...partial,
      };

      setCachedRequestData(ACCOUNT_PROFILE_CACHE_KEY, nextAccountProfile);

      return {
        accountProfile: nextAccountProfile,
        fetchedAt: Date.now(),
      };
    });
  },

  invalidateAccountProfile: () => {
    invalidateRequestCache(ACCOUNT_PROFILE_CACHE_KEY);
    set({ fetchedAt: 0 });
  },

  resetAccountProfile: () => {
    accountProfileRequestVersion += 1;
    invalidateRequestCache(ACCOUNT_PROFILE_CACHE_KEY);
    set({
      accountProfile: null,
      loading: false,
      saving: false,
      error: "",
      fetchedAt: 0,
    });
  },

  clearAccountProfileError: () => {
    set({ error: "" });
  },
}));
