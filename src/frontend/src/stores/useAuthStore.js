import { create } from "zustand";
import {
  getCurrentUserApi,
  loginApi,
  logoutApi,
  registerApi,
} from "../api/authApi";
import { useStreakStore } from "./useStreakStore";
import { PERMISSIONS } from "../constants/permissions";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import { hasPermission } from "../utils/authorizationUtils";
import { useProfileStore } from "./useProfileStore";
import { useAuthProviderStore } from "./useAuthProviderStore";
import { useRoadmapStore } from "./useRoadmapStore";
import { usePortfolioEditorStore } from "./usePortfolioEditorStore";
import { usePortfolioStore } from "./usePortfolioStore";
import { useSkillGapStore } from "./useSkillGapStore";
import { useLearningModuleStore } from "./useLearningModuleStore";
import { useAiCreditStore } from "./useAiCreditStore";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import { clearRequestCache } from "../utils/requestCacheUtils";

const SESSION_CACHE_MS = 5 * 60 * 1000;

async function loadStreakIfAllowed(user, options = {}) {
  if (!hasPermission(user, PERMISSIONS.STREAK_VIEW_SELF)) {
    useStreakStore.getState().resetStreakState();
    return null;
  }

  return useStreakStore.getState().fetchStreak(options);
}

let currentUserPromise = null;

function isFresh(timestamp) {
  return timestamp && Date.now() - timestamp < SESSION_CACHE_MS;
}

export const useAuthStore = create((set, get) => ({
  user: null,
  authLoading: false,
  authInitialized: false,
  authError: "",
  lastCheckedAt: 0,

  clearAuth: () => {
    currentUserPromise = null;
    clearRequestCache();

    set({
      user: null,
      authLoading: false,
      authInitialized: true,
      authError: "",
      lastCheckedAt: 0,
    });

    localStorage.removeItem("isLoggedIn");
    useStreakStore.getState().resetStreakState();
    useProfileStore.getState().resetProfile();
    useAuthProviderStore.getState().resetProviders();
    useRoadmapStore.getState().resetRoadmaps();
    usePortfolioEditorStore.getState().resetPortfolioEditor();
    usePortfolioStore.getState().resetPortfolioView();
    useSkillGapStore.getState().resetSkillGap();
    useLearningModuleStore.getState().resetLearningModules();
    useAiCreditStore.getState().resetAiCredit();
  },

  setAuthenticatedUser: (user) => {
    if (!user) return;

    set({
      user,
      authLoading: false,
      authInitialized: true,
      authError: "",
      lastCheckedAt: Date.now(),
    });

    localStorage.setItem("isLoggedIn", "true");
  },

  login: async (payload) => {
    try {
      set({ authLoading: true, authError: "" });

      const data = await loginApi(payload);

      if (!data?.user) {
        throw new Error("Login response does not contain user.");
      }

      localStorage.setItem("isLoggedIn", "true");

      const currentUser = await getCurrentUserApi();

      set({
        user: currentUser,
        authInitialized: true,
        lastCheckedAt: Date.now(),
      });

      await loadStreakIfAllowed(currentUser, { force: true });

      return currentUser;
    } catch (error) {
      set({
        user: null,
        authError: getFriendlyApiErrorMessage(error, "Login failed. Please try again."),
        authInitialized: true,
        lastCheckedAt: 0,
      });

      localStorage.removeItem("isLoggedIn");
      useStreakStore.getState().resetStreakState();
      useProfileStore.getState().resetProfile();
      useAuthProviderStore.getState().resetProviders();
      useRoadmapStore.getState().resetRoadmaps();
      usePortfolioEditorStore.getState().resetPortfolioEditor();
      usePortfolioStore.getState().resetPortfolioView();
      useSkillGapStore.getState().resetSkillGap();
      useLearningModuleStore.getState().resetLearningModules();
      useAiCreditStore.getState().resetAiCredit();

      throw error;
    } finally {
      set({ authLoading: false });
    }
  },

  register: async (payload) => {
    try {
      set({ authLoading: true, authError: "" });
      return await registerApi(payload);
    } catch (error) {
      set({ authError: getFriendlyApiErrorMessage(error, "Register failed. Please try again.") });
      throw error;
    } finally {
      set({ authLoading: false });
    }
  },

  logout: async () => {
    try {
      await logoutApi();
    } catch (error) {
      console.log("Logout failed:", error);
    } finally {
      get().clearAuth();
    }
  },

  loadCurrentUser: async ({ force = false } = {}) => {
    const state = get();

    if (!force && state.user && state.authInitialized && isFresh(state.lastCheckedAt)) {
      return state.user;
    }

    if (currentUserPromise) {
      return currentUserPromise;
    }

    currentUserPromise = (async () => {
      try {
        set((current) => ({
          authLoading: !current.user,
          authError: "",
        }));

        const currentUser = await getCurrentUserApi();

        set({
          user: currentUser,
          authInitialized: true,
          lastCheckedAt: Date.now(),
        });

        localStorage.setItem("isLoggedIn", "true");
        await loadStreakIfAllowed(currentUser);

        return currentUser;
      } catch (error) {
        if (error?.status === 401 || error?.status === 403) {
          get().clearAuth();
          return null;
        }

        console.log("Load current user failed:", error);

        set({
          authError: getFriendlyApiErrorMessage(error, "Unable to verify your session right now."),
          authInitialized: true,
        });

        return get().user || null;
      } finally {
        currentUserPromise = null;
        set({ authLoading: false, authInitialized: true });
      }
    })();

    return currentUserPromise;
  },

  revalidateCurrentUser: async ({ force = false } = {}) => {
    const state = get();

    if (!force && state.user && isFresh(state.lastCheckedAt)) {
      return state.user;
    }

    return get().loadCurrentUser({ force: true });
  },

  clearAuthError: () => {
    set({ authError: "" });
  },

  getIsAuthenticated: () => {
    return !!get().user;
  },
}));
