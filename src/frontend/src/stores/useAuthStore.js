import { create } from "zustand";
import {
  getCurrentUserApi,
  loginApi,
  logoutApi,
  registerApi,
} from "../api/authApi";
import { useStreakStore } from "./useStreakStore";

export const useAuthStore = create((set, get) => ({
  user: null,
  authLoading: false,
  authInitialized: false,
  authError: "",

  login: async (payload) => {
    try {
      set({
        authLoading: true,
        authError: "",
      });
      const data = await loginApi(payload);

      if (!data?.user) {
        throw new Error("Login response does not contain user.");
      }
      set({
        user: data.user,
        authInitialized: true,
      });

      localStorage.setItem("isLoggedIn", "true");
      await useStreakStore.getState().fetchStreak();
    } catch (error) {
      set({
        user: null,
        authError: error?.message || "Login failed. Please try again.",
        authInitialized: true,
      });
      useStreakStore.getState().resetStreakState();
      throw error;
    } finally {
      set({ authLoading: false });
    }
  },

  register: async (payload) => {
    try {
      set({
        authLoading: true,
        authError: "",
      });

      const data = await registerApi(payload);

      return data;
    } catch (error) {
      set({
        authError: error.message || "Register failed. Please try again.",
      });

      throw error;
    } finally {
      set({
        authLoading: false,
      });
    }
  },
  logout: async () => {
    try {
      await logoutApi();
    } catch (error) {
      console.log("Logout failed:", error);
    } finally {
      set({
        user: null,
        authLoading: false,
        authInitialized: true,
      });

      localStorage.removeItem("isLoggedIn");
      useStreakStore.getState().resetStreakState();
    }
  },

  loadCurrentUser: async () => {
    try {
      set({
        authLoading: true,
        authError: "",
      });

      const currentUser = await getCurrentUserApi();

      set({ user: currentUser });
      localStorage.setItem("isLoggedIn", "true");

      await useStreakStore.getState().fetchStreak();
    } catch (error) {
      if (error.response?.status !== 401) {
        console.log("Load current user failed:", error);
      }
      set({ user: null });

      localStorage.removeItem("isLoggedIn");
      await useStreakStore.getState().resetStreakState();
    } finally {
      set({ authLoading: false, authInitialized: true });
    }
  },
  clearAuthError: () => {
    set({ authError: "" });
  },
  getIsAuthenticated: () => {
    return !!get().user;
  },
}));
