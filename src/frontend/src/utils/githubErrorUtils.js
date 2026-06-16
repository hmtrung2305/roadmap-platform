import {
  getApiErrorCode,
  getFriendlyApiErrorMessage,
} from "./apiErrorUtils";

const RECONNECT_GITHUB_ERROR_CODES = new Set([
  "GITHUB_TOKEN_MISSING",
  "GITHUB_TOKEN_INVALID",
]);

const CONNECT_GITHUB_ERROR_CODES = new Set([
  "GITHUB_NOT_LINKED",
]);

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
  return getFriendlyApiErrorMessage(error, fallback);
}
