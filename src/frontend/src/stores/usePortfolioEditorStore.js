import { create } from "zustand";

import {
  getMyPortfolioApi,
  updatePortfolioRepositoriesApi,
} from "../api/portfolioApi";
import {
  generateRepositoryInsightApi,
  getSavedGitHubRepositoriesApi,
  syncGitHubRepositoriesApi,
} from "../api/githubRepositoryApi";
import { useAuthProviderStore } from "./useAuthProviderStore";
import { useAiCreditStore } from "./useAiCreditStore";
import { usePortfolioStore } from "./usePortfolioStore";
import {
  getInitiallySelectedIds,
  getRepositoryId,
} from "../features/portfolio/utils/portfolioEditUtils";
import {
  getCreditStatus,
  getFriendlyApiErrorMessage,
} from "../utils/apiErrorUtils";
import {
  getGitHubConnectionAction,
  getGitHubErrorMessage,
  isGitHubConnectionError,
} from "../utils/githubErrorUtils";
import {
  cachedRequest,
  getCachedRequestEntry,
  guardedMutation,
  invalidateRequestCache,
  invalidateRequestCacheByPrefix,
  setCachedRequestData,
} from "../utils/requestCacheUtils";
import { MAX_SHOWCASE_REPOSITORIES } from "../features/portfolio/constants/portfolioLimits";

const PORTFOLIO_CACHE_KEY = "portfolio-editor:me";
const REPOSITORIES_CACHE_KEY = "portfolio-editor:github-repositories";
const PORTFOLIO_CACHE_TTL_MS = 5 * 60 * 1000;
const REPOSITORIES_CACHE_TTL_MS = 2 * 60 * 1000;

let portfolioEditorRequestVersion = 0;

function normalizeProviderName(provider) {
  return String(provider || "")
    .trim()
    .toLowerCase();
}

function getGitHubProvider(providers) {
  return (providers || []).find(
    (provider) => normalizeProviderName(provider?.provider) === "github",
  );
}

function normalizeRepositories(data) {
  return Array.isArray(data) ? data : [];
}

function getRepositorySelection(repositories) {
  return getInitiallySelectedIds(repositories).slice(
    0,
    MAX_SHOWCASE_REPOSITORIES,
  );
}

function filterSelectedIdsForRepositories(selectedIds, repositories) {
  const repositoryIdSet = new Set(
    repositories.map(getRepositoryId).filter(Boolean),
  );

  return selectedIds
    .filter((repositoryId) => repositoryIdSet.has(repositoryId))
    .slice(0, MAX_SHOWCASE_REPOSITORIES);
}

function getInsightRepositoryId(repository, fallbackId) {
  return getRepositoryId(repository) || fallbackId;
}

function patchRepositoryInsight(repositories, repositoryId, insight) {
  return repositories.map((repository) => {
    const currentRepositoryId = getInsightRepositoryId(repository);

    if (currentRepositoryId !== repositoryId) {
      return repository;
    }

    return {
      ...repository,
      insight,
    };
  });
}

function getRepositoryInsightStatus(insight) {
  return String(insight?.analysisStatus || insight?.status || "")
    .trim()
    .toLowerCase();
}

function getRepositoryInsightErrorMessage(insight) {
  return (
    insight?.errorMessage ||
    insight?.analysisMessage ||
    insight?.message ||
    "AI insight generation failed. Please check the repository README and try again."
  );
}

function patchSelectedRepositories(repositories, selectedIds) {
  const selectedIdSet = new Set(selectedIds);

  return repositories.map((repository) => {
    const repositoryId = getRepositoryId(repository);

    if (!repositoryId) return repository;

    return {
      ...repository,
      isSelectedForPortfolio: selectedIdSet.has(repositoryId),
      isSelected: selectedIdSet.has(repositoryId),
    };
  });
}

export const usePortfolioEditorStore = create((set, get) => ({
  portfolio: null,
  portfolioLoading: false,
  portfolioError: "",

  repositories: [],
  selectedIds: [],
  isGitHubLinked: false,
  githubConnectionAction: "connect",
  repositoryLoading: false,
  syncing: false,
  reloadingSelection: false,
  saving: false,
  analyzingRepositoryIds: {},
  repoError: "",
  repoSuccess: "",

  initEditor: async ({ force = false } = {}) => {
    const requestVersion = portfolioEditorRequestVersion;
    const cachedPortfolio = getCachedRequestEntry(PORTFOLIO_CACHE_KEY, {
      ttlMs: PORTFOLIO_CACHE_TTL_MS,
    });
    const cachedRepositories = getCachedRequestEntry(REPOSITORIES_CACHE_KEY, {
      ttlMs: REPOSITORIES_CACHE_TTL_MS,
    });

    try {
      set((state) => {
        const cachedRepositoryList = cachedRepositories.hit
          ? normalizeRepositories(cachedRepositories.data)
          : state.repositories;

        return {
          portfolio:
            !force && cachedPortfolio.hit && !state.portfolio
              ? cachedPortfolio.data
              : state.portfolio,
          repositories:
            !force && cachedRepositories.hit && state.repositories.length === 0
              ? cachedRepositoryList
              : state.repositories,
          selectedIds:
            !force && cachedRepositories.hit && state.selectedIds.length === 0
              ? getRepositorySelection(cachedRepositoryList)
              : state.selectedIds,
          portfolioLoading: force
            ? !state.portfolio
            : !state.portfolio && !cachedPortfolio.hit,
          repositoryLoading: force
            ? state.isGitHubLinked && state.repositories.length === 0
            : state.isGitHubLinked &&
              state.repositories.length === 0 &&
              !cachedRepositories.hit,
          portfolioError: "",
          repoError: "",
          repoSuccess: "",
        };
      });

      const [portfolioData, providers] = await Promise.all([
        get().loadPortfolio({ force }),
        useAuthProviderStore.getState().loadProviders({ force }),
      ]);

      if (requestVersion !== portfolioEditorRequestVersion) {
        return { portfolio: portfolioData, providers };
      }

      const githubProvider = getGitHubProvider(providers);
      const isGitHubLinked = Boolean(githubProvider?.isLinked);

      set({
        isGitHubLinked,
        githubConnectionAction: isGitHubLinked ? "connected" : "connect",
      });

      if (isGitHubLinked) {
        try {
          await get().loadRepositories({ force });
        } catch {
          // Repository loading errors are already reflected in repoError.
          // Keep the editor usable so the user can reconnect or retry sync.
        }
      } else if (requestVersion === portfolioEditorRequestVersion) {
        set({ repositories: [], selectedIds: [], repositoryLoading: false });
      }

      return { portfolio: portfolioData, providers };
    } catch (error) {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({
          portfolioError:
            useAuthProviderStore.getState().error ||
            getFriendlyApiErrorMessage(
              error,
              "Could not load your portfolio editor.",
            ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({ portfolioLoading: false, repositoryLoading: false });
      }
    }
  },

  loadPortfolio: async ({ force = false } = {}) => {
    const requestVersion = portfolioEditorRequestVersion;

    const portfolio = await cachedRequest(
      PORTFOLIO_CACHE_KEY,
      getMyPortfolioApi,
      {
        ttlMs: PORTFOLIO_CACHE_TTL_MS,
        force,
      },
    );

    if (requestVersion === portfolioEditorRequestVersion) {
      set({ portfolio });
    }

    return portfolio;
  },

  loadRepositories: async ({ force = false, resetSelection = true } = {}) => {
    const requestVersion = portfolioEditorRequestVersion;
    const cachedRepositories = getCachedRequestEntry(REPOSITORIES_CACHE_KEY, {
      ttlMs: REPOSITORIES_CACHE_TTL_MS,
    });

    try {
      set((state) => ({
        repositoryLoading: force
          ? state.repositories.length === 0
          : state.repositories.length === 0 && !cachedRepositories.hit,
        repoError: "",
      }));

      const data = await cachedRequest(
        REPOSITORIES_CACHE_KEY,
        getSavedGitHubRepositoriesApi,
        {
          ttlMs: REPOSITORIES_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion !== portfolioEditorRequestVersion) {
        return normalizeRepositories(data);
      }

      const repositories = normalizeRepositories(data);

      set((state) => ({
        repositories,
        selectedIds: resetSelection
          ? getRepositorySelection(repositories)
          : state.selectedIds,
        repositoryLoading: false,
        repoError: "",
      }));

      return repositories;
    } catch (error) {
      if (requestVersion === portfolioEditorRequestVersion) {
        if (isGitHubConnectionError(error)) {
          set({
            isGitHubLinked: false,
            githubConnectionAction: getGitHubConnectionAction(error),
          });
        }

        set({
          repositoryLoading: false,
          repoError: getGitHubErrorMessage(
            error,
            "Cannot load repositories. Please connect GitHub first.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({ repositoryLoading: false });
      }
    }
  },

  syncRepositories: async () => {
    const state = get();

    if (
      state.syncing ||
      state.saving ||
      state.reloadingSelection ||
      state.repositoryLoading
    ) {
      return normalizeRepositories(state.repositories);
    }

    const requestVersion = portfolioEditorRequestVersion;

    try {
      set({ syncing: true, repoError: "", repoSuccess: "" });

      const data = await guardedMutation(
        "portfolio-editor:github-repositories:sync",
        syncGitHubRepositoriesApi,
      );

      invalidateRequestCache(REPOSITORIES_CACHE_KEY);

      if (requestVersion !== portfolioEditorRequestVersion) {
        return normalizeRepositories(data);
      }

      const repositories = normalizeRepositories(data);
      const selectedIds = getRepositorySelection(repositories);

      setCachedRequestData(REPOSITORIES_CACHE_KEY, repositories);
      usePortfolioStore.getState().invalidatePortfolioView();

      set({
        repositories,
        selectedIds,
        isGitHubLinked: true,
        githubConnectionAction: "connected",
        repoSuccess:
          "Repositories synced. Your featured repository selection has been reset.",
      });

      return repositories;
    } catch (error) {
      if (requestVersion === portfolioEditorRequestVersion) {
        if (isGitHubConnectionError(error)) {
          set({
            isGitHubLinked: false,
            githubConnectionAction: getGitHubConnectionAction(error),
          });
        }

        set({
          repoError: getGitHubErrorMessage(
            error,
            "Cannot sync repositories. Please connect GitHub first.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({ syncing: false });
      }
    }
  },

  reloadSavedSelection: async () => {
    const state = get();

    if (
      state.reloadingSelection ||
      state.saving ||
      state.syncing ||
      state.repositoryLoading
    ) {
      return normalizeRepositories(state.repositories);
    }

    const requestVersion = portfolioEditorRequestVersion;

    try {
      set({ reloadingSelection: true, repoError: "", repoSuccess: "" });
      const repositories = await get().loadRepositories({
        force: true,
        resetSelection: true,
      });

      if (requestVersion === portfolioEditorRequestVersion) {
        set({ repoSuccess: "Saved repository selection restored." });
      }

      return repositories;
    } catch (error) {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({
          repoError: getGitHubErrorMessage(
            error,
            "Cannot reload saved repository selection.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({ reloadingSelection: false });
      }
    }
  },

  toggleRepository: (repositoryId) => {
    const state = get();

    if (
      !repositoryId ||
      state.saving ||
      state.syncing ||
      state.reloadingSelection ||
      state.repositoryLoading
    ) {
      return;
    }

    const isSelected = state.selectedIds.includes(repositoryId);

    if (!isSelected && state.selectedIds.length >= MAX_SHOWCASE_REPOSITORIES) {
      set({
        repoSuccess: "",
        repoError: `You can select up to ${MAX_SHOWCASE_REPOSITORIES} repositories for your portfolio.`,
      });
      return;
    }

    set((state) => ({
      repoError: "",
      repoSuccess: "",
      selectedIds: isSelected
        ? state.selectedIds.filter((id) => id !== repositoryId)
        : [...state.selectedIds, repositoryId],
    }));
  },

  generateInsight: async (repositoryId, { force = false } = {}) => {
    if (!repositoryId) return null;

    const state = get();

    if (
      state.saving ||
      state.syncing ||
      state.reloadingSelection ||
      state.repositoryLoading
    ) {
      return null;
    }

    const requestVersion = portfolioEditorRequestVersion;
    const mutationKey = [
      "portfolio-editor",
      "repository-insight",
      repositoryId,
      force ? "force" : "normal",
    ];

    try {
      set((state) => ({
        analyzingRepositoryIds: {
          ...state.analyzingRepositoryIds,
          [repositoryId]: true,
        },
        repoError: "",
        repoSuccess: "",
      }));

      const insight = await guardedMutation(mutationKey, () =>
        generateRepositoryInsightApi(repositoryId, { force }),
      );

      if (requestVersion !== portfolioEditorRequestVersion) {
        return insight;
      }

      const repositories = patchRepositoryInsight(
        get().repositories,
        repositoryId,
        insight,
      );
      const insightCreditStatus = getCreditStatus(insight);
      const insightStatus = getRepositoryInsightStatus(insight);

      if (insightCreditStatus) {
        useAiCreditStore.getState().patchCreditStatus(insightCreditStatus);
      } else {
        useAiCreditStore.getState().invalidateCreditStatus();
      }

      setCachedRequestData(REPOSITORIES_CACHE_KEY, repositories);
      usePortfolioStore.getState().invalidatePortfolioView();

      if (insightStatus === "failed") {
        set({
          repositories,
          repoError: getRepositoryInsightErrorMessage(insight),
          repoSuccess: "",
        });

        return insight;
      }

      set({
        repositories,
        repoSuccess: force
          ? "Repository AI summary regenerated."
          : "Repository AI summary generated.",
      });

      return insight;
    } catch (error) {
      if (requestVersion === portfolioEditorRequestVersion) {
        if (isGitHubConnectionError(error)) {
          set({
            isGitHubLinked: false,
            githubConnectionAction: getGitHubConnectionAction(error),
          });
        }

        const errorCreditStatus = getCreditStatus(error);

        if (errorCreditStatus) {
          useAiCreditStore.getState().patchCreditStatus(errorCreditStatus);
        }

        set({
          repoError: getGitHubErrorMessage(
            error,
            "Cannot generate repository AI summary.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === portfolioEditorRequestVersion) {
        set((state) => {
          const nextAnalyzingRepositoryIds = {
            ...state.analyzingRepositoryIds,
          };
          delete nextAnalyzingRepositoryIds[repositoryId];

          return {
            analyzingRepositoryIds: nextAnalyzingRepositoryIds,
          };
        });
      }
    }
  },

  saveSelection: async () => {
    const state = get();

    if (
      state.saving ||
      state.syncing ||
      state.reloadingSelection ||
      state.repositoryLoading
    ) {
      return null;
    }

    const requestVersion = portfolioEditorRequestVersion;
    const selectedIds = state.selectedIds;

    if (selectedIds.length > MAX_SHOWCASE_REPOSITORIES) {
      set({
        repoError: `You can select up to ${MAX_SHOWCASE_REPOSITORIES} repositories for your portfolio.`,
        repoSuccess: "",
      });
      return null;
    }

    try {
      set({ saving: true, repoError: "", repoSuccess: "" });

      await guardedMutation("portfolio-editor:save-selection", () =>
        updatePortfolioRepositoriesApi(selectedIds),
      );

      invalidateRequestCache(REPOSITORIES_CACHE_KEY);

      if (requestVersion !== portfolioEditorRequestVersion) {
        return null;
      }

      const patchedRepositories = patchSelectedRepositories(
        get().repositories,
        selectedIds,
      );
      setCachedRequestData(REPOSITORIES_CACHE_KEY, patchedRepositories);

      set({
        repositories: patchedRepositories,
        repoSuccess: "Portfolio projects saved successfully.",
      });

      usePortfolioStore.getState().invalidatePortfolioView();
      await get().loadRepositories({ force: true, resetSelection: true });

      return selectedIds;
    } catch (error) {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({
          repoError: getFriendlyApiErrorMessage(
            error,
            "Cannot save selected repositories.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === portfolioEditorRequestVersion) {
        set({ saving: false });
      }
    }
  },

  setRepositoryError: (message) => {
    set({ repoError: message || "", repoSuccess: "" });
  },

  clearRepositoryMessages: () => {
    set({ repoError: "", repoSuccess: "" });
  },

  resetPortfolioEditor: () => {
    portfolioEditorRequestVersion += 1;
    invalidateRequestCacheByPrefix("portfolio-editor:");
    set({
      portfolio: null,
      portfolioLoading: false,
      portfolioError: "",
      repositories: [],
      selectedIds: [],
      isGitHubLinked: false,
      githubConnectionAction: "connect",
      repositoryLoading: false,
      syncing: false,
      reloadingSelection: false,
      saving: false,
      analyzingRepositoryIds: {},
      repoError: "",
      repoSuccess: "",
    });
  },
}));
