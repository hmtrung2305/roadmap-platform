const requestCache = new Map();
const inFlightRequests = new Map();
const pendingMutations = new Map();

let cacheVersion = 0;

function normalizeKey(key) {
  if (Array.isArray(key)) {
    return key.filter(Boolean).join(":");
  }

  return String(key || "");
}

function isFresh(entry, ttlMs) {
  return entry && ttlMs > 0 && Date.now() - entry.fetchedAt < ttlMs;
}

export function getCachedRequestEntry(key, { ttlMs = 0 } = {}) {
  const normalizedKey = normalizeKey(key);
  const entry = requestCache.get(normalizedKey);

  if (!isFresh(entry, ttlMs)) {
    return {
      hit: false,
      data: null,
    };
  }

  return {
    hit: true,
    data: entry.data,
  };
}

export function getCachedRequestData(key, { ttlMs = 0 } = {}) {
  const entry = getCachedRequestEntry(key, { ttlMs });

  return entry.hit ? entry.data : null;
}

export function setCachedRequestData(key, data) {
  requestCache.set(normalizeKey(key), {
    data,
    fetchedAt: Date.now(),
  });

  return data;
}

export async function cachedRequest(key, requestFn, { ttlMs = 60_000, force = false } = {}) {
  const normalizedKey = normalizeKey(key);

  if (!force) {
    const cached = getCachedRequestEntry(normalizedKey, { ttlMs });

    if (cached.hit) {
      return cached.data;
    }
  }

  const inFlightRequest = inFlightRequests.get(normalizedKey);

  if (inFlightRequest) {
    return inFlightRequest;
  }

  const requestCacheVersion = cacheVersion;
  const promise = Promise.resolve()
    .then(requestFn)
    .then((data) => {
      if (requestCacheVersion === cacheVersion) {
        setCachedRequestData(normalizedKey, data);
      }

      return data;
    })
    .finally(() => {
      if (inFlightRequests.get(normalizedKey) === promise) {
        inFlightRequests.delete(normalizedKey);
      }
    });

  inFlightRequests.set(normalizedKey, promise);

  return promise;
}

export async function guardedMutation(key, mutationFn) {
  const normalizedKey = normalizeKey(key);
  const pendingMutation = pendingMutations.get(normalizedKey);

  if (pendingMutation) {
    return pendingMutation;
  }

  const promise = Promise.resolve()
    .then(mutationFn)
    .finally(() => {
      if (pendingMutations.get(normalizedKey) === promise) {
        pendingMutations.delete(normalizedKey);
      }
    });

  pendingMutations.set(normalizedKey, promise);

  return promise;
}

export function isMutationPending(key) {
  return pendingMutations.has(normalizeKey(key));
}

export function invalidateRequestCache(key) {
  requestCache.delete(normalizeKey(key));
}

export function invalidateRequestCacheByPrefix(prefix) {
  const normalizedPrefix = normalizeKey(prefix);

  for (const key of requestCache.keys()) {
    if (key.startsWith(normalizedPrefix)) {
      requestCache.delete(key);
    }
  }
}

export function clearRequestCache() {
  cacheVersion += 1;
  requestCache.clear();
  inFlightRequests.clear();
  pendingMutations.clear();
}
