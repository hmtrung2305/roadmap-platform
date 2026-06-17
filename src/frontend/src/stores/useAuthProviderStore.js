import { create } from "zustand";
import {
  getAuthProvidersApi,
  unlinkAuthProviderApi,
} from "../api/authProviderApi";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCache,
  setCachedRequestData,
} from "../utils/requestCacheUtils";

const AUTH_PROVIDERS_CACHE_KEY = "auth-providers:me";
const AUTH_PROVIDERS_CACHE_TTL_MS = 2 * 60 * 1000;
const CONNECTING_PROVIDER_STORAGE_KEY = "techmap:connecting-provider";
const CONNECTING_PROVIDER_TTL_MS = 10 * 60 * 1000;
const CONNECTING_PROVIDER_BLOCK_MS = 30 * 1000;

let providersRequestVersion = 0;

function isFresh(timestamp) {
  return timestamp && Date.now() - timestamp < AUTH_PROVIDERS_CACHE_TTL_MS;
}

function normalizeProviderName(provider) {
  return String(provider || "").trim().toLowerCase();
}

function createConnectingProviderEntry(provider) {
  const normalizedProvider = normalizeProviderName(provider);

  if (!normalizedProvider) {
    return {
      provider: "",
      startedAt: 0,
    };
  }

  return {
    provider: normalizedProvider,
    startedAt: Date.now(),
  };
}

function readConnectingProviderEntry() {
  if (typeof window === "undefined") {
    return {
      provider: "",
      startedAt: 0,
    };
  }

  const storedValue = window.sessionStorage.getItem(
    CONNECTING_PROVIDER_STORAGE_KEY,
  );

  if (!storedValue) {
    return {
      provider: "",
      startedAt: 0,
    };
  }

  try {
    const parsedValue = JSON.parse(storedValue);
    const normalizedProvider = normalizeProviderName(parsedValue?.provider);
    const startedAt = Number(parsedValue?.startedAt || 0);

    if (
      !normalizedProvider ||
      !startedAt ||
      Date.now() - startedAt > CONNECTING_PROVIDER_TTL_MS
    ) {
      window.sessionStorage.removeItem(CONNECTING_PROVIDER_STORAGE_KEY);
      return {
        provider: "",
        startedAt: 0,
      };
    }

    return {
      provider: normalizedProvider,
      startedAt,
    };
  } catch {
    const normalizedProvider = normalizeProviderName(storedValue);

    if (!normalizedProvider) {
      window.sessionStorage.removeItem(CONNECTING_PROVIDER_STORAGE_KEY);
      return {
        provider: "",
        startedAt: 0,
      };
    }

    const fallbackEntry = createConnectingProviderEntry(normalizedProvider);
    writeConnectingProviderEntry(fallbackEntry);
    return fallbackEntry;
  }
}

function writeConnectingProviderEntry(entry) {
  if (typeof window === "undefined") return;

  const normalizedProvider = normalizeProviderName(entry?.provider);
  const startedAt = Number(entry?.startedAt || Date.now());

  if (normalizedProvider) {
    window.sessionStorage.setItem(
      CONNECTING_PROVIDER_STORAGE_KEY,
      JSON.stringify({
        provider: normalizedProvider,
        startedAt,
      }),
    );
    return;
  }

  window.sessionStorage.removeItem(CONNECTING_PROVIDER_STORAGE_KEY);
}

function clearConnectingProviderStorage() {
  if (typeof window === "undefined") return;
  window.sessionStorage.removeItem(CONNECTING_PROVIDER_STORAGE_KEY);
}

function isConnectingEntryBlocking(entry) {
  return Boolean(
    entry?.provider &&
      entry?.startedAt &&
      Date.now() - entry.startedAt <= CONNECTING_PROVIDER_BLOCK_MS,
  );
}

function providerIsLinked(providers, provider) {
  const normalizedProvider = normalizeProviderName(provider);

  if (!normalizedProvider) return false;

  return providers.some(
    (item) =>
      normalizeProviderName(item.provider) === normalizedProvider &&
      Boolean(item.isLinked),
  );
}

function getVisibleConnectingEntry(state) {
  const storedEntry = readConnectingProviderEntry();

  if (storedEntry.provider) return storedEntry;

  if (state.connectingProvider) {
    return {
      provider: normalizeProviderName(state.connectingProvider),
      startedAt: Number(state.connectingProviderStartedAt || 0),
    };
  }

  return {
    provider: "",
    startedAt: 0,
  };
}

const initialConnectingProviderEntry = readConnectingProviderEntry();

export const useAuthProviderStore = create((set, get) => ({
  providers: [],
  loading: false,
  actionLoading: false,
  connectingProvider: initialConnectingProviderEntry.provider,
  connectingProviderStartedAt: initialConnectingProviderEntry.startedAt,
  error: "",
  fetchedAt: 0,

  loadProviders: async ({ force = false } = {}) => {
    const state = get();
    const connectingEntry = getVisibleConnectingEntry(state);
    const shouldForceFreshProviders = force || Boolean(connectingEntry.provider);

    if (!shouldForceFreshProviders && isFresh(state.fetchedAt)) {
      return state.providers;
    }

    const requestVersion = providersRequestVersion;
    const requestStartedAt = Date.now();

    try {
      set((current) => ({
        loading: current.providers.length === 0,
        connectingProvider: connectingEntry.provider,
        connectingProviderStartedAt: connectingEntry.startedAt,
        error: "",
      }));

      const data = await cachedRequest(
        AUTH_PROVIDERS_CACHE_KEY,
        getAuthProvidersApi,
        {
          ttlMs: AUTH_PROVIDERS_CACHE_TTL_MS,
          force: shouldForceFreshProviders,
        },
      );

      if (requestVersion !== providersRequestVersion) {
        return data;
      }

      const providers = Array.isArray(data) ? data : [];
      const latestConnectingEntry = getVisibleConnectingEntry(get());
      const shouldClearConnectingProvider = Boolean(
        latestConnectingEntry.provider &&
          latestConnectingEntry.startedAt &&
          latestConnectingEntry.startedAt <= requestStartedAt,
      );

      if (shouldClearConnectingProvider) {
        clearConnectingProviderStorage();
      }

      set({
        providers,
        loading: false,
        error: "",
        fetchedAt: Date.now(),
        connectingProvider: shouldClearConnectingProvider
          ? ""
          : latestConnectingEntry.provider,
        connectingProviderStartedAt: shouldClearConnectingProvider
          ? 0
          : latestConnectingEntry.startedAt,
      });

      return providers;
    } catch (error) {
      if (requestVersion === providersRequestVersion) {
        const latestConnectingEntry = getVisibleConnectingEntry(get());
        const shouldClearConnectingProvider = Boolean(
          latestConnectingEntry.provider &&
            latestConnectingEntry.startedAt &&
            latestConnectingEntry.startedAt <= requestStartedAt,
        );

        if (shouldClearConnectingProvider) {
          clearConnectingProviderStorage();
        }

        set({
          loading: false,
          connectingProvider: shouldClearConnectingProvider
            ? ""
            : latestConnectingEntry.provider,
          connectingProviderStartedAt: shouldClearConnectingProvider
            ? 0
            : latestConnectingEntry.startedAt,
          error: getFriendlyApiErrorMessage(
            error,
            "Unable to load connected accounts.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === providersRequestVersion) {
        set({ loading: false });
      }
    }
  },

  unlinkProvider: async (provider) => {
    const normalizedProvider = normalizeProviderName(provider);
    const requestVersion = providersRequestVersion;

    try {
      set({ actionLoading: true, error: "" });

      const data = await guardedMutation(
        ["auth-provider", "unlink", normalizedProvider],
        () => unlinkAuthProviderApi(normalizedProvider),
      );

      invalidateRequestCache(AUTH_PROVIDERS_CACHE_KEY);

      if (requestVersion !== providersRequestVersion) {
        return data;
      }

      if (get().connectingProvider === normalizedProvider) {
        get().clearConnectingProvider(normalizedProvider);
      }

      await get().loadProviders({ force: true });
      return data;
    } catch (error) {
      if (requestVersion === providersRequestVersion) {
        set({
          error: getFriendlyApiErrorMessage(
            error,
            "Unable to disconnect account.",
          ),
        });
      }

      throw error;
    } finally {
      if (requestVersion === providersRequestVersion) {
        set({ actionLoading: false });
      }
    }
  },

  startConnectingProvider: (provider) => {
    const normalizedProvider = normalizeProviderName(provider);

    if (!normalizedProvider) return "";

    const state = get();
    const currentEntry = getVisibleConnectingEntry(state);

    if (state.actionLoading || providerIsLinked(state.providers, normalizedProvider)) {
      return "";
    }

    if (currentEntry.provider && isConnectingEntryBlocking(currentEntry)) {
      set({
        connectingProvider: currentEntry.provider,
        connectingProviderStartedAt: currentEntry.startedAt,
        error:
          "Another account connection is already in progress. Please try again shortly.",
      });
      return "";
    }

    const nextEntry = createConnectingProviderEntry(normalizedProvider);
    writeConnectingProviderEntry(nextEntry);

    set({
      connectingProvider: nextEntry.provider,
      connectingProviderStartedAt: nextEntry.startedAt,
      error: "",
    });

    return nextEntry.provider;
  },

  clearConnectingProvider: (provider) => {
    const normalizedProvider = normalizeProviderName(provider);
    const currentEntry = getVisibleConnectingEntry(get());

    if (normalizedProvider && currentEntry.provider !== normalizedProvider) {
      return false;
    }

    clearConnectingProviderStorage();
    set({
      connectingProvider: "",
      connectingProviderStartedAt: 0,
    });

    return true;
  },

  completeConnectingProvider: async (provider) => {
    const normalizedProvider = normalizeProviderName(provider);

    if (!normalizedProvider) return [];

    get().setProviderLinked(normalizedProvider, true);
    get().clearConnectingProvider(normalizedProvider);

    return get().loadProviders({ force: true });
  },

  handleProviderConnectionError: (provider, error, fallback) => {
    const normalizedProvider = normalizeProviderName(provider);

    if (normalizedProvider) {
      get().clearConnectingProvider(normalizedProvider);
    }

    const message = getFriendlyApiErrorMessage(error, fallback);
    set({ error: message });

    return message;
  },

  setProviderLinked: (provider, isLinked) => {
    const normalizedProvider = normalizeProviderName(provider);

    set((state) => {
      let didUpdate = false;

      const providers = state.providers.map((item) => {
        if (normalizeProviderName(item.provider) !== normalizedProvider) {
          return item;
        }

        didUpdate = true;

        return {
          ...item,
          isLinked,
        };
      });

      const nextProviders = didUpdate
        ? providers
        : [
            ...providers,
            {
              provider: normalizedProvider,
              isLinked,
            },
          ];

      const shouldClearConnectingProvider =
        Boolean(isLinked) && state.connectingProvider === normalizedProvider;

      if (shouldClearConnectingProvider) {
        clearConnectingProviderStorage();
      }

      setCachedRequestData(AUTH_PROVIDERS_CACHE_KEY, nextProviders);

      return {
        providers: nextProviders,
        fetchedAt: Date.now(),
        connectingProvider: shouldClearConnectingProvider
          ? ""
          : state.connectingProvider,
        connectingProviderStartedAt: shouldClearConnectingProvider
          ? 0
          : state.connectingProviderStartedAt,
      };
    });
  },

  invalidateProviders: () => {
    invalidateRequestCache(AUTH_PROVIDERS_CACHE_KEY);
    set({ fetchedAt: 0 });
  },

  resetProviders: () => {
    providersRequestVersion += 1;
    invalidateRequestCache(AUTH_PROVIDERS_CACHE_KEY);
    clearConnectingProviderStorage();
    set({
      providers: [],
      loading: false,
      actionLoading: false,
      connectingProvider: "",
      connectingProviderStartedAt: 0,
      error: "",
      fetchedAt: 0,
    });
  },

  clearProviderError: () => {
    set({ error: "" });
  },
}));
