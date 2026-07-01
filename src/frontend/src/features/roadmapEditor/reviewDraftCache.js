const REVIEW_DRAFT_CACHE_PREFIX = "roadmap-platform.review-draft";

export const REVIEW_DRAFT_CACHE_TYPES = {
  contentManagerChangelog: "content-manager-changelog",
  reviewerSuggestion: "reviewer-suggestion",
};

function getStorage() {
  if (typeof window === "undefined") return null;

  try {
    return window.localStorage || null;
  } catch {
    return null;
  }
}

function getReviewDraftCacheKey(type, roadmapVersionId) {
  const normalizedType = String(type || "").trim();
  const normalizedVersionId = String(roadmapVersionId || "").trim();

  if (!normalizedType || !normalizedVersionId) return "";

  return `${REVIEW_DRAFT_CACHE_PREFIX}.${normalizedType}.${normalizedVersionId}`;
}

export function readReviewDraftCache(type, roadmapVersionId) {
  const storage = getStorage();
  const key = getReviewDraftCacheKey(type, roadmapVersionId);

  if (!storage || !key) return "";

  try {
    return storage.getItem(key) || "";
  } catch {
    return "";
  }
}

export function writeReviewDraftCache(type, roadmapVersionId, value) {
  const storage = getStorage();
  const key = getReviewDraftCacheKey(type, roadmapVersionId);

  if (!storage || !key) return;

  try {
    if (String(value || "").length === 0) {
      storage.removeItem(key);
      return;
    }

    storage.setItem(key, String(value));
  } catch {
    // Cache failures should never block the review workflow.
  }
}

export function clearReviewDraftCache(type, roadmapVersionId) {
  const storage = getStorage();
  const key = getReviewDraftCacheKey(type, roadmapVersionId);

  if (!storage || !key) return;

  try {
    storage.removeItem(key);
  } catch {
    // Cache failures should never block the review workflow.
  }
}
