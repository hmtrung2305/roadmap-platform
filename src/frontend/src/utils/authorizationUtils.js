function normalizeValues(values) {
  if (!Array.isArray(values)) {
    return [];
  }

  return values
    .filter((value) => typeof value === "string")
    .map((value) => value.trim().toLowerCase())
    .filter(Boolean);
}

export function getUserPermissions(user) {
  return normalizeValues(user?.permissions);
}

export function getUserRoles(user) {
  return normalizeValues(user?.roles);
}

export function hasPermission(user, permission) {
  if (!permission) {
    return true;
  }

  return getUserPermissions(user).includes(permission.trim().toLowerCase());
}

export function hasAnyPermission(user, permissions = []) {
  if (!permissions.length) {
    return true;
  }

  const userPermissions = new Set(getUserPermissions(user));

  return permissions.some((permission) => (
    typeof permission === "string"
    && userPermissions.has(permission.trim().toLowerCase())
  ));
}

export function hasAllPermissions(user, permissions = []) {
  if (!permissions.length) {
    return true;
  }

  const userPermissions = new Set(getUserPermissions(user));

  return permissions.every((permission) => (
    typeof permission === "string"
    && userPermissions.has(permission.trim().toLowerCase())
  ));
}

export function hasRole(user, role) {
  if (!role) {
    return true;
  }

  return getUserRoles(user).includes(role.trim().toLowerCase());
}

export function hasAnyRole(user, roles = []) {
  if (!roles.length) {
    return true;
  }

  const userRoles = new Set(getUserRoles(user));

  return roles.some((role) => (
    typeof role === "string"
    && userRoles.has(role.trim().toLowerCase())
  ));
}

export function hasAllRoles(user, roles = []) {
  if (!roles.length) {
    return true;
  }

  const userRoles = new Set(getUserRoles(user));

  return roles.every((role) => (
    typeof role === "string"
    && userRoles.has(role.trim().toLowerCase())
  ));
}

export function canAccessRoute(user, options = {}) {
  const {
    anyPermissions = [],
    allPermissions = [],
    anyRoles = [],
    allRoles = [],
  } = options;

  return (
    hasAnyPermission(user, anyPermissions)
    && hasAllPermissions(user, allPermissions)
    && hasAnyRole(user, anyRoles)
    && hasAllRoles(user, allRoles)
  );
}
