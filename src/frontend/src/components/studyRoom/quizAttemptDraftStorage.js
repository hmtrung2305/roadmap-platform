const STORAGE_PREFIX = "learning-module:quiz-attempt-draft:";
const MAX_AGE_MS = 7 * 24 * 60 * 60 * 1000;

function getStorageKey(attemptId) {
  return `${STORAGE_PREFIX}${attemptId}`;
}

function canUseStorage() {
  return typeof window !== "undefined" && Boolean(window.localStorage);
}

export function loadQuizAttemptDraft(attemptId) {
  if (!attemptId || !canUseStorage()) return {};

  try {
    const raw = window.localStorage.getItem(getStorageKey(attemptId));
    if (!raw) return {};

    const parsed = JSON.parse(raw);
    const updatedAt = Number(parsed?.updatedAt || 0);

    if (!updatedAt || Date.now() - updatedAt > MAX_AGE_MS) {
      window.localStorage.removeItem(getStorageKey(attemptId));
      return {};
    }

    return parsed?.answers && typeof parsed.answers === "object"
      ? parsed.answers
      : {};
  } catch {
    return {};
  }
}

export function saveQuizAttemptDraft(attemptId, answers) {
  if (!attemptId || !canUseStorage()) return;

  try {
    window.localStorage.setItem(
      getStorageKey(attemptId),
      JSON.stringify({
        updatedAt: Date.now(),
        answers,
      }),
    );
  } catch {
    // Quiz progress can continue in memory when storage is unavailable.
  }
}

export function clearQuizAttemptDraft(attemptId) {
  if (!attemptId || !canUseStorage()) return;

  try {
    window.localStorage.removeItem(getStorageKey(attemptId));
  } catch {
    // Ignore storage cleanup failures.
  }
}
