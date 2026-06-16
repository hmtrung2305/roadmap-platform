const RECONNECT_GITHUB_ERROR_CODES = new Set([
  "GITHUB_TOKEN_MISSING",
  "GITHUB_TOKEN_INVALID",
]);

const CONNECT_GITHUB_ERROR_CODES = new Set([
  "GITHUB_NOT_LINKED",
]);

export function getApiErrorCode(error) {
  return error?.code || error?.raw?.code || null;
}

export function requiresGitHubReconnect(error) {
  return RECONNECT_GITHUB_ERROR_CODES.has(getApiErrorCode(error));
}

export function requiresGitHubConnect(error) {
  return CONNECT_GITHUB_ERROR_CODES.has(getApiErrorCode(error));
}

export function isGitHubConnectionError(error) {
  return requiresGitHubReconnect(error) || requiresGitHubConnect(error);
}

export function getGitHubConnectionAction(error) {
  if (requiresGitHubReconnect(error)) return "reconnect";
  if (requiresGitHubConnect(error)) return "connect";
  return null;
}

export function getGitHubErrorMessage(error, fallback) {
  const code = getApiErrorCode(error);

  if (code === "GITHUB_NOT_LINKED") {
    return "Connect GitHub before syncing repositories or generating repository summaries.";
  }

  if (code === "GITHUB_TOKEN_MISSING" || code === "GITHUB_TOKEN_INVALID") {
    return "Your GitHub connection needs to be refreshed. Reconnect GitHub to continue.";
  }

  if (code === "GITHUB_RATE_LIMITED") {
    const retryAfterSeconds = error?.retryAfterSeconds || error?.raw?.retryAfterSeconds;
    return retryAfterSeconds
      ? `GitHub rate limit reached. Try again in ${retryAfterSeconds} seconds.`
      : "GitHub rate limit reached. Please try again later.";
  }

  if (code === "GITHUB_API_ERROR") {
    return error?.message || "GitHub could not be reached right now. Please try again later.";
  }

  return error?.message || fallback;
}
