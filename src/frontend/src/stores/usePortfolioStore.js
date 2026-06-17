import { create } from "zustand";

import { getMyPortfolioApi, getPortfolioByUsernameApi } from "../api/portfolioApi";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  getCachedRequestEntry,
  invalidateRequestCache,
  invalidateRequestCacheByPrefix,
} from "../utils/requestCacheUtils";

const OWN_PORTFOLIO_CACHE_KEY = "portfolio:view:me";
const PUBLIC_PORTFOLIO_CACHE_PREFIX = "portfolio:view:public:";
const PORTFOLIO_CACHE_TTL_MS = 5 * 60 * 1000;

let portfolioViewRequestVersion = 0;

function normalizeUsername(username) {
  return String(username || "").trim().toLowerCase();
}

function getPublicPortfolioCacheKey(username) {
  return `${PUBLIC_PORTFOLIO_CACHE_PREFIX}${normalizeUsername(username)}`;
}

function getPortfolioCacheKey({ username, isOwnPortfolio }) {
  return isOwnPortfolio ? OWN_PORTFOLIO_CACHE_KEY : getPublicPortfolioCacheKey(username);
}

function getPortfolioFetcher({ username, isOwnPortfolio }) {
  return isOwnPortfolio
    ? getMyPortfolioApi
    : () => getPortfolioByUsernameApi(username);
}

export const usePortfolioStore = create((set, get) => ({
  ownPortfolio: null,
  publicPortfolioByUsername: {},
  loadingByKey: {},
  loadedByKey: {},
  errorByKey: {},

  loadPortfolio: async ({ username, isOwnPortfolio = false, force = false } = {}) => {
    const normalizedUsername = normalizeUsername(username);

    if (!isOwnPortfolio && !normalizedUsername) {
      const message = "Username was not found.";
      set((state) => ({
        loadedByKey: {
          ...state.loadedByKey,
          [getPublicPortfolioCacheKey("missing")]: true,
        },
        errorByKey: {
          ...state.errorByKey,
          [getPublicPortfolioCacheKey("missing")]: message,
        },
      }));
      throw new Error(message);
    }

    const cacheKey = getPortfolioCacheKey({
      username: normalizedUsername,
      isOwnPortfolio,
    });
    const cachedPortfolio = getCachedRequestEntry(cacheKey, {
      ttlMs: PORTFOLIO_CACHE_TTL_MS,
    });
    const requestVersion = portfolioViewRequestVersion;

    if (!force && cachedPortfolio.hit) {
      set((state) => ({
        ownPortfolio: isOwnPortfolio ? cachedPortfolio.data : state.ownPortfolio,
        publicPortfolioByUsername: isOwnPortfolio
          ? state.publicPortfolioByUsername
          : {
              ...state.publicPortfolioByUsername,
              [normalizedUsername]: cachedPortfolio.data,
            },
        loadingByKey: {
          ...state.loadingByKey,
          [cacheKey]: false,
        },
        loadedByKey: {
          ...state.loadedByKey,
          [cacheKey]: true,
        },
        errorByKey: {
          ...state.errorByKey,
          [cacheKey]: "",
        },
      }));

      return cachedPortfolio.data;
    }

    try {
      set((state) => ({
        loadingByKey: {
          ...state.loadingByKey,
          [cacheKey]: force ? !get().getPortfolioSnapshot({ username: normalizedUsername, isOwnPortfolio }) : true,
        },
        errorByKey: {
          ...state.errorByKey,
          [cacheKey]: "",
        },
      }));

      const portfolio = await cachedRequest(
        cacheKey,
        getPortfolioFetcher({ username: normalizedUsername, isOwnPortfolio }),
        {
          ttlMs: PORTFOLIO_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion === portfolioViewRequestVersion) {
        set((state) => ({
          ownPortfolio: isOwnPortfolio ? portfolio : state.ownPortfolio,
          publicPortfolioByUsername: isOwnPortfolio
            ? state.publicPortfolioByUsername
            : {
                ...state.publicPortfolioByUsername,
                [normalizedUsername]: portfolio,
              },
          loadingByKey: {
            ...state.loadingByKey,
            [cacheKey]: false,
          },
          loadedByKey: {
            ...state.loadedByKey,
            [cacheKey]: true,
          },
          errorByKey: {
            ...state.errorByKey,
            [cacheKey]: "",
          },
        }));
      }

      return portfolio;
    } catch (error) {
      if (requestVersion === portfolioViewRequestVersion) {
        set((state) => ({
          loadingByKey: {
            ...state.loadingByKey,
            [cacheKey]: false,
          },
          loadedByKey: {
            ...state.loadedByKey,
            [cacheKey]: true,
          },
          errorByKey: {
            ...state.errorByKey,
            [cacheKey]: getFriendlyApiErrorMessage(
              error,
              "Could not load this portfolio.",
            ),
          },
        }));
      }

      throw error;
    } finally {
      if (requestVersion === portfolioViewRequestVersion) {
        set((state) => ({
          loadingByKey: {
            ...state.loadingByKey,
            [cacheKey]: false,
          },
        }));
      }
    }
  },

  getPortfolioSnapshot: ({ username, isOwnPortfolio = false } = {}) => {
    const normalizedUsername = normalizeUsername(username);
    const state = get();

    return isOwnPortfolio
      ? state.ownPortfolio
      : state.publicPortfolioByUsername[normalizedUsername] || null;
  },

  getPortfolioLoading: ({ username, isOwnPortfolio = false } = {}) => {
    const normalizedUsername = normalizeUsername(username);
    const cacheKey = getPortfolioCacheKey({
      username: normalizedUsername,
      isOwnPortfolio,
    });

    return Boolean(get().loadingByKey[cacheKey]);
  },

  getPortfolioLoaded: ({ username, isOwnPortfolio = false } = {}) => {
    const normalizedUsername = normalizeUsername(username);
    const cacheKey = getPortfolioCacheKey({
      username: normalizedUsername,
      isOwnPortfolio,
    });

    return Boolean(get().loadedByKey[cacheKey]);
  },

  getPortfolioError: ({ username, isOwnPortfolio = false } = {}) => {
    const normalizedUsername = normalizeUsername(username);
    const cacheKey = getPortfolioCacheKey({
      username: normalizedUsername,
      isOwnPortfolio,
    });

    return get().errorByKey[cacheKey] || "";
  },

  invalidateOwnPortfolio: () => {
    invalidateRequestCache(OWN_PORTFOLIO_CACHE_KEY);
    set((state) => {
      const nextLoadedByKey = { ...state.loadedByKey };
      delete nextLoadedByKey[OWN_PORTFOLIO_CACHE_KEY];

      return {
        ownPortfolio: null,
        loadedByKey: nextLoadedByKey,
      };
    });
  },

  invalidatePublicPortfolio: (username) => {
    const normalizedUsername = normalizeUsername(username);
    if (!normalizedUsername) return;

    invalidateRequestCache(getPublicPortfolioCacheKey(normalizedUsername));
    set((state) => {
      const nextPublicPortfolioByUsername = { ...state.publicPortfolioByUsername };
      delete nextPublicPortfolioByUsername[normalizedUsername];

      const cacheKey = getPublicPortfolioCacheKey(normalizedUsername);
      const nextLoadingByKey = { ...state.loadingByKey };
      const nextLoadedByKey = { ...state.loadedByKey };
      const nextErrorByKey = { ...state.errorByKey };
      delete nextLoadingByKey[cacheKey];
      delete nextLoadedByKey[cacheKey];
      delete nextErrorByKey[cacheKey];

      return {
        publicPortfolioByUsername: nextPublicPortfolioByUsername,
        loadingByKey: nextLoadingByKey,
        loadedByKey: nextLoadedByKey,
        errorByKey: nextErrorByKey,
      };
    });
  },

  invalidateAllPublicPortfolios: () => {
    invalidateRequestCacheByPrefix(PUBLIC_PORTFOLIO_CACHE_PREFIX);
    set((state) => {
      const nextLoadingByKey = { ...state.loadingByKey };
      const nextLoadedByKey = { ...state.loadedByKey };
      const nextErrorByKey = { ...state.errorByKey };

      Object.keys(nextLoadingByKey).forEach((key) => {
        if (key.startsWith(PUBLIC_PORTFOLIO_CACHE_PREFIX)) {
          delete nextLoadingByKey[key];
        }
      });
      Object.keys(nextLoadedByKey).forEach((key) => {
        if (key.startsWith(PUBLIC_PORTFOLIO_CACHE_PREFIX)) {
          delete nextLoadedByKey[key];
        }
      });
      Object.keys(nextErrorByKey).forEach((key) => {
        if (key.startsWith(PUBLIC_PORTFOLIO_CACHE_PREFIX)) {
          delete nextErrorByKey[key];
        }
      });

      return {
        publicPortfolioByUsername: {},
        loadingByKey: nextLoadingByKey,
        loadedByKey: nextLoadedByKey,
        errorByKey: nextErrorByKey,
      };
    });
  },

  invalidatePortfolioView: () => {
    get().invalidateOwnPortfolio();
    get().invalidateAllPublicPortfolios();
  },

  resetPortfolioView: () => {
    portfolioViewRequestVersion += 1;
    invalidateRequestCache(OWN_PORTFOLIO_CACHE_KEY);
    invalidateRequestCacheByPrefix(PUBLIC_PORTFOLIO_CACHE_PREFIX);

    set({
      ownPortfolio: null,
      publicPortfolioByUsername: {},
      loadingByKey: {},
      loadedByKey: {},
      errorByKey: {},
    });
  },
}));
