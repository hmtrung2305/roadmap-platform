import { create } from "zustand";
import { getMyProfileApi, updateMyProfileApi } from "../api/profileApi";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCache,
  setCachedRequestData,
} from "../utils/requestCacheUtils";

const PROFILE_CACHE_KEY = "profile:me";
const PROFILE_CACHE_TTL_MS = 5 * 60 * 1000;

let profileRequestVersion = 0;

function isFresh(timestamp) {
  return timestamp && Date.now() - timestamp < PROFILE_CACHE_TTL_MS;
}

export const useProfileStore = create((set, get) => ({
  profile: null,
  loading: false,
  saving: false,
  error: "",
  fetchedAt: 0,

  loadProfile: async ({ force = false } = {}) => {
    const state = get();

    if (!force && isFresh(state.fetchedAt)) {
      return state.profile;
    }

    const requestVersion = profileRequestVersion;

    try {
      set((current) => ({
        loading: !current.profile,
        error: "",
      }));

      const data = await cachedRequest(
        PROFILE_CACHE_KEY,
        getMyProfileApi,
        {
          ttlMs: PROFILE_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion !== profileRequestVersion) {
        return data;
      }

      set({
        profile: data,
        loading: false,
        error: "",
        fetchedAt: Date.now(),
      });

      return data;
    } catch (error) {
      if (requestVersion === profileRequestVersion) {
        set({
          loading: false,
          error: getFriendlyApiErrorMessage(error, "Unable to load profile."),
        });
      }

      throw error;
    } finally {
      if (requestVersion === profileRequestVersion) {
        set({ loading: false });
      }
    }
  },

  updateProfile: async (payload) => {
    const requestVersion = profileRequestVersion;

    try {
      set({ saving: true, error: "" });

      const data = await guardedMutation("profile:update", () =>
        updateMyProfileApi(payload),
      );

      if (requestVersion !== profileRequestVersion) {
        return data;
      }

      const nextProfile = data ?? {
        ...(get().profile || {}),
        ...payload,
      };

      setCachedRequestData(PROFILE_CACHE_KEY, nextProfile);

      set({
        profile: nextProfile,
        saving: false,
        error: "",
        fetchedAt: Date.now(),
      });

      return nextProfile;
    } catch (error) {
      if (requestVersion === profileRequestVersion) {
        set({
          saving: false,
          error: getFriendlyApiErrorMessage(error, "Unable to update profile."),
        });
      }

      throw error;
    } finally {
      if (requestVersion === profileRequestVersion) {
        set({ saving: false });
      }
    }
  },

  patchLocalProfile: (partial) => {
    set((state) => {
      const nextProfile = {
        ...(state.profile || {}),
        ...partial,
      };

      setCachedRequestData(PROFILE_CACHE_KEY, nextProfile);

      return {
        profile: nextProfile,
        fetchedAt: Date.now(),
      };
    });
  },

  invalidateProfile: () => {
    invalidateRequestCache(PROFILE_CACHE_KEY);
    set({ fetchedAt: 0 });
  },

  resetProfile: () => {
    profileRequestVersion += 1;
    invalidateRequestCache(PROFILE_CACHE_KEY);
    set({
      profile: null,
      loading: false,
      saving: false,
      error: "",
      fetchedAt: 0,
    });
  },

  clearProfileError: () => {
    set({ error: "" });
  },
}));
