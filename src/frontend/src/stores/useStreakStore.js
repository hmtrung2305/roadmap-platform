import { create } from "zustand";
import { getStreak, trackStreak } from "../api/streakApi";

export const useStreakStore = create((set, get) => ({
  streak: null,
  loading: false,
  tracking: false,
  error: "",
  showAnimation: false,
  lastFetchedAt: 0,

  fetchStreak: async ({ force = false } = {}) => {
    const state = get();

    if (!force && state.streak && Date.now() - state.lastFetchedAt < 5 * 60 * 1000) {
      return state.streak;
    }

    if (state.loading) return null;

    try {
      set({ loading: true, error: "" });

      const data = await getStreak();

      set({
        streak: data,
        lastFetchedAt: Date.now(),
      });

      return data;
    } catch (error) {
      console.error("Failed to fetch streak:", error);

      set({
        streak: null,
        error: "Cannot load streak.",
      });

      return null;
    } finally {
      set({ loading: false });
    }
  },

  trackStreak: async () => {
    if (get().tracking) return null;

    try {
      set({ tracking: true, error: "" });

      const data = await trackStreak();

      set({
        streak: data,
        showAnimation: data.increasedToday,
        lastFetchedAt: Date.now(),
      });

      return data;
    } catch (error) {
      console.error("Failed to track streak:", error);

      set({
        error: "Cannot track streak.",
      });

      return null;
    } finally {
      set({
        tracking: false,
      });
    }
  },

  closeAnimation: () => {
    set({ showAnimation: false });
  },

  resetStreakState: () => {
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