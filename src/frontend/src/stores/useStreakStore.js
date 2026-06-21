import { create } from "zustand";
import { getStreak, trackStreak as trackStreakApi } from "../api/streakApi";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCache,
  setCachedRequestData,
} from "../utils/requestCacheUtils";

const STREAK_CACHE_KEY = "streak:current";
const STREAK_TRACK_KEY = "streak:track";
const STREAK_CACHE_MS = 5 * 60 * 1000;

let streakRequestVersion = 0;

export const useStreakStore = create((set, get) => ({
  streak: null,
  loading: false,
  tracking: false,
  error: "",
  showAnimation: false,
  lastFetchedAt: 0,

  fetchStreak: async ({ force = false } = {}) => {
    const state = get();

    if (!force && state.streak && Date.now() - state.lastFetchedAt < STREAK_CACHE_MS) {
      return state.streak;
    }

    const requestVersion = streakRequestVersion;

    try {
      set({ loading: true, error: "" });

      const data = await cachedRequest(STREAK_CACHE_KEY, getStreak, {
        ttlMs: STREAK_CACHE_MS,
        force,
      });

      if (requestVersion !== streakRequestVersion) {
        return null;
      }

      set({
        streak: data,
        lastFetchedAt: Date.now(),
      });

      return data;
    } catch (error) {
      console.error("Failed to fetch streak:", error);

      if (requestVersion === streakRequestVersion) {
        set({
          streak: null,
          error: "Cannot load streak.",
        });
      }

      return null;
    } finally {
      if (requestVersion === streakRequestVersion) {
        set({ loading: false });
      }
    }
  },

  trackStreak: async () => {
    const requestVersion = streakRequestVersion;

    return guardedMutation(STREAK_TRACK_KEY, async () => {
      try {
        set({ tracking: true, error: "" });

        const data = await trackStreakApi();

        if (requestVersion !== streakRequestVersion) {
          return null;
        }

        setCachedRequestData(STREAK_CACHE_KEY, data);

        set({
          streak: data,
          showAnimation: data.increasedToday,
          lastFetchedAt: Date.now(),
        });

        return data;
      } catch (error) {
        console.error("Failed to track streak:", error);

        if (requestVersion === streakRequestVersion) {
          set({
            error: "Cannot track streak.",
          });
        }

        return null;
      } finally {
        if (requestVersion === streakRequestVersion) {
          set({
            tracking: false,
          });
        }
      }
    });
  },

  trackStreakIfNeeded: async () => {
    const state = get();

    if (state.tracking || state.streak?.isCompletedStreakToday) {
      return state.streak;
    }

    return get().trackStreak();
  },

  closeAnimation: () => {
    set({ showAnimation: false });
  },

  resetStreakState: () => {
    streakRequestVersion += 1;
    invalidateRequestCache(STREAK_CACHE_KEY);

    set({
      streak: null,
      loading: false,
      tracking: false,
      error: "",
      showAnimation: false,
      lastFetchedAt: 0,
    });
  },
}));
