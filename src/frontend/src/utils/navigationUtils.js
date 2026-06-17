import {
  ADMIN_SURFACE_PERMISSIONS,
  CONTENT_MANAGER_SURFACE_PERMISSIONS,
  LEARNER_SURFACE_PERMISSIONS,
} from "../constants/permissions";
import { hasAnyPermission } from "./authorizationUtils";

export const DEFAULT_LEARNER_ROUTE = "/roadmaps";
export const DEFAULT_CONTENT_MANAGER_ROUTE = "/content/learning-modules";
export const DEFAULT_ADMIN_ROUTE = "/admin";

const AUTH_ENTRY_PATHS = new Set([
  "/login",
  "/register",
  "/verify-email",
]);

function normalizePath(path) {
  if (typeof path !== "string") {
    return "";
  }

  const trimmed = path.trim();

  if (!trimmed || !trimmed.startsWith("/")) {
    return "";
  }

  if (trimmed.startsWith("//")) {
    return "";
  }

  return trimmed;
}

export function getCurrentReturnUrl() {
  return `${window.location.pathname}${window.location.search}${window.location.hash}`;
}

export function getPathFromLocation(location) {
  if (!location) {
    return "";
  }

  if (typeof location === "string") {
    return normalizePath(location);
  }

  const pathname = normalizePath(location.pathname);

  if (!pathname) {
    return "";
  }

  return `${pathname}${location.search || ""}${location.hash || ""}`;
}

export function getDefaultAuthenticatedRoute(user) {
  if (hasAnyPermission(user, ADMIN_SURFACE_PERMISSIONS)) {
    return DEFAULT_ADMIN_ROUTE;
  }

  if (hasAnyPermission(user, CONTENT_MANAGER_SURFACE_PERMISSIONS)) {
    return DEFAULT_CONTENT_MANAGER_ROUTE;
  }

  if (hasAnyPermission(user, LEARNER_SURFACE_PERMISSIONS)) {
    return DEFAULT_LEARNER_ROUTE;
  }

  return "/";
}

export function canAccessSurfacePath(user, path) {
  const normalizedPath = normalizePath(path);

  if (!normalizedPath) {
    return false;
  }

  if (AUTH_ENTRY_PATHS.has(normalizedPath)) {
    return false;
  }

  if (normalizedPath === "/admin" || normalizedPath.startsWith("/admin/")) {
    return hasAnyPermission(user, ADMIN_SURFACE_PERMISSIONS);
  }

  if (normalizedPath === "/content" || normalizedPath.startsWith("/content/")) {
    return hasAnyPermission(user, CONTENT_MANAGER_SURFACE_PERMISSIONS);
  }

  if (normalizedPath === "/settings" || normalizedPath.startsWith("/settings/")) {
    return hasAnyPermission(user, LEARNER_SURFACE_PERMISSIONS);
  }

  const learnerPaths = [
    "/home",
    "/profile",
    "/portfolio",
    "/resources",
    "/study",
    "/learning-modules",
    "/roadmap",
    "/roadmaps",
    "/market-pulse",
    "/skill-gap",
    "/skill-gap-analysis",
  ];

  if (learnerPaths.some((prefix) => normalizedPath === prefix || normalizedPath.startsWith(`${prefix}/`))) {
    return hasAnyPermission(user, LEARNER_SURFACE_PERMISSIONS);
  }

  return false;
}

export function resolvePostLoginRedirect(user, fromLocation) {
  const requestedPath = getPathFromLocation(fromLocation);

  if (requestedPath) {
    const requestedPathname = requestedPath.split(/[?#]/, 1)[0];

    if (canAccessSurfacePath(user, requestedPathname)) {
      return requestedPath;
    }
  }

  return getDefaultAuthenticatedRoute(user);
}
