/* eslint-disable react-hooks/exhaustive-deps, react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  Check,
  KeyRound,
  Loader2,
  LockKeyhole,
  Pencil,
  Plus,
  RefreshCw,
  Save,
  Search,
  ShieldCheck,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "react-toastify";

import { adminRbacApi } from "../../api/adminRbacApi";
import { PERMISSIONS } from "../../constants/permissions";
import { useAuthStore } from "../../stores/useAuthStore";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { hasAnyPermission, hasPermission } from "../../utils/authorizationUtils";

const PAGE_PERMISSIONS = [
  PERMISSIONS.ROLE_VIEW_ANY,
  PERMISSIONS.PERMISSION_VIEW_ANY,
  PERMISSIONS.ROLE_PERMISSION_VIEW_ANY,
];

const SYSTEM_PERMISSION_NAMES = new Set(Object.values(PERMISSIONS));

const RESOURCE_LABELS = {
  account: "Account",
  auth_provider: "Auth Providers",
  profile: "Profile",
  portfolio: "Portfolio",
  repository: "Repositories",
  repo_insight: "Repository Insights",
  ai_credit: "AI Credit",
  streak: "Streak",
  roadmap: "Roadmaps",
  roadmap_node: "Roadmap Nodes",
  roadmap_enrollment: "Roadmap Enrollment",
  roadmap_progress: "Roadmap Progress",
  learning_module: "Learning Modules",
  learning_module_enrollment: "Module Enrollment",
  learning_module_lesson: "Module Lessons",
  learning_module_progress: "Module Progress",
  learning_module_quiz: "Module Quizzes",
  learning_module_quiz_attempt: "Quiz Attempts",
  learning_module_quiz_question: "Quiz Questions",
  learning_module_chat: "Module AI Chat",
  career_role: "Career Roles",
  skill_gap_analysis: "Skill Gap Analysis",
  skill_gap_analysis_history: "Skill Gap History",
  skill_gap_config: "Skill Gap Config",
  market_pulse: "Market Pulse",
  skill: "Skills",
  user: "Users",
  role: "Roles",
  permission: "Permissions",
  role_permission: "Role Permissions",
  user_role: "User Roles",
  system_health: "System Health",
};

const ACTION_LABELS = {
  archive: "Archive",
  assign: "Assign",
  create: "Create",
  delete: "Delete",
  generate: "Generate",
  link: "Link",
  manage: "Manage",
  preview: "Preview",
  publish: "Publish",
  reorder: "Reorder",
  reindex: "Reindex",
  restore: "Restore",
  revoke: "Revoke",
  submit: "Submit",
  suspend: "Suspend",
  sync: "Sync",
  track: "Track",
  unlink: "Unlink",
  update: "Update",
  upsert: "Create or update",
  use: "Use",
  view: "View",
};

const SCOPE_LABELS = {
  any: "platform-wide",
  catalog: "catalog",
  enrolled: "enrolled content",
  own: "owned content",
  published: "published content",
  self: "own account",
};

function getId(item, keys) {
  for (const key of keys) {
    if (item?.[key]) return item[key];
  }

  return "";
}

function getRoleId(role) {
  return getId(role, ["roleId", "RoleId"]);
}

function getRoleName(role) {
  return role?.roleName || role?.RoleName || "";
}

function getPermissionId(permission) {
  return getId(permission, ["permissionId", "PermissionId"]);
}

function getPermissionName(permission) {
  return permission?.permissionName || permission?.PermissionName || "";
}

function isAdminRoleName(roleName) {
  return String(roleName || "").trim().toLowerCase() === "admin";
}

function isSystemPermissionName(permissionName) {
  return SYSTEM_PERMISSION_NAMES.has(String(permissionName || "").trim().toLowerCase());
}

function getResource(permissionName) {
  return String(permissionName || "").split(".")[0] || "other";
}

function humanizeToken(value) {
  return String(value || "")
    .replaceAll("_", " ")
    .replace(/\b\w/g, (character) => character.toUpperCase());
}

function getResourceLabel(resource) {
  return RESOURCE_LABELS[resource] || humanizeToken(resource);
}

function describePermission(permissionName) {
  const [resource, action, scope] = String(permissionName || "").split(".");
  const actionLabel = ACTION_LABELS[action] || humanizeToken(action);
  const resourceLabel = getResourceLabel(resource).toLowerCase();
  const scopeLabel = SCOPE_LABELS[scope] || scope;

  return `${actionLabel} ${resourceLabel}${scopeLabel ? ` for ${scopeLabel}` : ""}`;
}

function normalizeSearch(value) {
  return String(value || "").trim().toLowerCase();
}

function filterBySearch(items, getName, search) {
  const keyword = normalizeSearch(search);
  if (!keyword) return items;

  return items.filter((item) => getName(item).toLowerCase().includes(keyword));
}

function groupPermissions(permissions) {
  return permissions.reduce((groups, permission) => {
    const name = getPermissionName(permission);
    const resource = getResource(name);

    if (!groups.has(resource)) {
      groups.set(resource, []);
    }

    groups.get(resource).push(permission);
    return groups;
  }, new Map());
}

function sortByName(items, getName) {
  return [...items].sort((first, second) => getName(first).localeCompare(getName(second)));
}

function PermissionGate({ icon: Icon = LockKeyhole, message }) {
  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center">
      <div className="mx-auto grid h-11 w-11 place-items-center rounded-md bg-white text-slate-500">
        <Icon size={19} />
      </div>
      <p className="mt-3 text-sm font-extrabold text-slate-700">{message}</p>
    </div>
  );
}

export default function AdminRolesPermissionsPage() {
  const user = useAuthStore((state) => state.user);
  const revalidateCurrentUser = useAuthStore((state) => state.revalidateCurrentUser);

  const canOpenPage = hasAnyPermission(user, PAGE_PERMISSIONS);
  const canViewRoles = hasPermission(user, PERMISSIONS.ROLE_VIEW_ANY);
  const canCreateRole = hasPermission(user, PERMISSIONS.ROLE_CREATE_ANY);
  const canUpdateRole = hasPermission(user, PERMISSIONS.ROLE_UPDATE_ANY);
  const canDeleteRole = hasPermission(user, PERMISSIONS.ROLE_DELETE_ANY);
  const canViewPermissions = hasPermission(user, PERMISSIONS.PERMISSION_VIEW_ANY);
  const canCreatePermission = hasPermission(user, PERMISSIONS.PERMISSION_CREATE_ANY);
  const canUpdatePermission = hasPermission(user, PERMISSIONS.PERMISSION_UPDATE_ANY);
  const canDeletePermission = hasPermission(user, PERMISSIONS.PERMISSION_DELETE_ANY);
  const canViewRolePermissions = hasPermission(user, PERMISSIONS.ROLE_PERMISSION_VIEW_ANY);
  const canAssignRolePermission = hasPermission(user, PERMISSIONS.ROLE_PERMISSION_ASSIGN_ANY);
  const canRevokeRolePermission = hasPermission(user, PERMISSIONS.ROLE_PERMISSION_REVOKE_ANY);

  const [roles, setRoles] = useState([]);
  const [permissions, setPermissions] = useState([]);
  const [selectedRoleId, setSelectedRoleId] = useState("");
  const [selectedRoleDetail, setSelectedRoleDetail] = useState(null);
  const [newRoleName, setNewRoleName] = useState("");
  const [roleNameDraft, setRoleNameDraft] = useState("");
  const [newPermissionName, setNewPermissionName] = useState("");
  const [editingPermissionId, setEditingPermissionId] = useState("");
  const [permissionDraftName, setPermissionDraftName] = useState("");
  const [permissionSearch, setPermissionSearch] = useState("");
  const [catalogSearch, setCatalogSearch] = useState("");
  const [activeGroup, setActiveGroup] = useState("all");
  const [actionError, setActionError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isRoleLoading, setIsRoleLoading] = useState(false);
  const [isCreatingRole, setIsCreatingRole] = useState(false);
  const [isSavingRole, setIsSavingRole] = useState(false);
  const [isDeletingRole, setIsDeletingRole] = useState(false);
  const [isCreatingPermission, setIsCreatingPermission] = useState(false);
  const [savingPermissionId, setSavingPermissionId] = useState("");
  const [deletingPermissionId, setDeletingPermissionId] = useState("");
  const [mutatingPermissionId, setMutatingPermissionId] = useState("");

  const selectedRoleName = getRoleName(selectedRoleDetail);
  const assignedPermissionIds = useMemo(() => {
    return new Set((selectedRoleDetail?.permissions || selectedRoleDetail?.Permissions || [])
      .map(getPermissionId)
      .filter(Boolean));
  }, [selectedRoleDetail]);

  const filteredPermissions = useMemo(() => (
    sortByName(filterBySearch(permissions, getPermissionName, permissionSearch), getPermissionName)
  ), [permissions, permissionSearch]);

  const permissionGroups = useMemo(() => groupPermissions(filteredPermissions), [filteredPermissions]);
  const groupOptions = useMemo(() => {
    return [...groupPermissions(sortByName(permissions, getPermissionName)).keys()]
      .sort((first, second) => getResourceLabel(first).localeCompare(getResourceLabel(second)));
  }, [permissions]);

  const visibleGroups = useMemo(() => {
    if (activeGroup === "all") return [...permissionGroups.entries()];
    return permissionGroups.has(activeGroup) ? [[activeGroup, permissionGroups.get(activeGroup)]] : [];
  }, [activeGroup, permissionGroups]);

  const filteredCatalogPermissions = useMemo(() => (
    sortByName(filterBySearch(permissions, getPermissionName, catalogSearch), getPermissionName)
  ), [catalogSearch, permissions]);

  const isSelectedAdminRole = isAdminRoleName(selectedRoleName);
  const roleNameChanged = roleNameDraft.trim().toLowerCase() !== selectedRoleName.trim().toLowerCase();

  useEffect(() => {
    loadAdminData();
  }, []);

  useEffect(() => {
    setRoleNameDraft(selectedRoleName || "");
  }, [selectedRoleId, selectedRoleName]);

  useEffect(() => {
    if (!selectedRoleId || !canViewRoles || !canViewRolePermissions) {
      setSelectedRoleDetail(null);
      return;
    }

    loadRoleDetail(selectedRoleId);
  }, [selectedRoleId]);

  async function loadAdminData({ preferredRoleId = selectedRoleId } = {}) {
    setIsLoading(true);
    setActionError("");

    try {
      const [roleData, permissionData] = await Promise.all([
        canViewRoles ? adminRbacApi.getRoles() : Promise.resolve([]),
        canViewPermissions ? adminRbacApi.getPermissions() : Promise.resolve([]),
      ]);

      const normalizedRoles = sortByName(roleData || [], getRoleName);
      const normalizedPermissions = sortByName(permissionData || [], getPermissionName);
      const preferredRoleExists = normalizedRoles.some((role) => getRoleId(role) === preferredRoleId);
      const nextRoleId = preferredRoleExists ? preferredRoleId : getRoleId(normalizedRoles[0]);

      setRoles(normalizedRoles);
      setPermissions(normalizedPermissions);
      setSelectedRoleId(nextRoleId || "");

      if (!nextRoleId) {
        setSelectedRoleDetail(null);
      }
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to load roles and permissions."));
    } finally {
      setIsLoading(false);
    }
  }

  async function loadRoleDetail(roleId) {
    setIsRoleLoading(true);
    setActionError("");

    try {
      const role = await adminRbacApi.getRole(roleId);
      setSelectedRoleDetail(role);
    } catch (error) {
      setSelectedRoleDetail(null);
      setActionError(getFriendlyApiErrorMessage(error, "Unable to load role permissions."));
    } finally {
      setIsRoleLoading(false);
    }
  }

  async function refreshCurrentUserPermissions() {
    await revalidateCurrentUser({ force: true });
  }

  async function handleCreateRole(event) {
    event.preventDefault();
    if (!canCreateRole || !newRoleName.trim()) return;

    setIsCreatingRole(true);
    setActionError("");

    try {
      const role = await adminRbacApi.createRole({ roleName: newRoleName });
      setNewRoleName("");
      toast.success("Role created.");
      await loadAdminData({ preferredRoleId: getRoleId(role) });
      await refreshCurrentUserPermissions();
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to create role."));
    } finally {
      setIsCreatingRole(false);
    }
  }

  async function handleSaveRoleName(event) {
    event.preventDefault();
    if (!canUpdateRole || isSelectedAdminRole || !selectedRoleId || !roleNameDraft.trim() || !roleNameChanged) return;

    setIsSavingRole(true);
    setActionError("");

    try {
      await adminRbacApi.updateRole(selectedRoleId, { roleName: roleNameDraft });
      toast.success("Role updated.");
      await loadAdminData({ preferredRoleId: selectedRoleId });
      await refreshCurrentUserPermissions();
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to update role."));
    } finally {
      setIsSavingRole(false);
    }
  }

  async function handleDeleteRole() {
    if (!canDeleteRole || isSelectedAdminRole || !selectedRoleId) return;

    const confirmed = window.confirm(`Delete role "${selectedRoleName}"?`);
    if (!confirmed) return;

    setIsDeletingRole(true);
    setActionError("");

    try {
      await adminRbacApi.deleteRole(selectedRoleId);
      toast.success("Role deleted.");
      await loadAdminData({ preferredRoleId: "" });
      await refreshCurrentUserPermissions();
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to delete role."));
    } finally {
      setIsDeletingRole(false);
    }
  }

  async function handleTogglePermission(permission) {
    if (!selectedRoleId) return;

    const permissionId = getPermissionId(permission);
    const permissionName = getPermissionName(permission);
    const isAssigned = assignedPermissionIds.has(permissionId);
    const canToggle = !isSelectedAdminRole && (isAssigned ? canRevokeRolePermission : canAssignRolePermission);

    if (!canToggle || mutatingPermissionId) return;

    setMutatingPermissionId(permissionId);
    setActionError("");

    try {
      const role = isAssigned
        ? await adminRbacApi.revokeRolePermission(selectedRoleId, permissionId)
        : await adminRbacApi.grantRolePermission(selectedRoleId, permissionId);

      setSelectedRoleDetail(role);
      toast.success(`${isAssigned ? "Revoked" : "Assigned"} ${permissionName}.`);
      await refreshCurrentUserPermissions();
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to update role permission."));
    } finally {
      setMutatingPermissionId("");
    }
  }

  async function handleCreatePermission(event) {
    event.preventDefault();
    if (!canCreatePermission || !newPermissionName.trim()) return;

    setIsCreatingPermission(true);
    setActionError("");

    try {
      await adminRbacApi.createPermission({ permissionName: newPermissionName });
      setNewPermissionName("");
      toast.success("Permission created.");
      await loadAdminData({ preferredRoleId: selectedRoleId });
      await refreshCurrentUserPermissions();
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to create permission."));
    } finally {
      setIsCreatingPermission(false);
    }
  }

  function startEditingPermission(permission) {
    setEditingPermissionId(getPermissionId(permission));
    setPermissionDraftName(getPermissionName(permission));
  }

  async function handleUpdatePermission(permissionId) {
    const permission = permissions.find((item) => getPermissionId(item) === permissionId);
    if (!canUpdatePermission || isSystemPermissionName(getPermissionName(permission)) || !permissionDraftName.trim()) return;

    setSavingPermissionId(permissionId);
    setActionError("");

    try {
      await adminRbacApi.updatePermission(permissionId, { permissionName: permissionDraftName });
      setEditingPermissionId("");
      setPermissionDraftName("");
      toast.success("Permission updated.");
      await loadAdminData({ preferredRoleId: selectedRoleId });
      if (selectedRoleId) await loadRoleDetail(selectedRoleId);
      await refreshCurrentUserPermissions();
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to update permission."));
    } finally {
      setSavingPermissionId("");
    }
  }

  async function handleDeletePermission(permission) {
    const permissionId = getPermissionId(permission);
    const permissionName = getPermissionName(permission);

    if (!canDeletePermission || !permissionId) return;
    if (isSystemPermissionName(permissionName)) return;

    const confirmed = window.confirm(`Delete permission "${permissionName}"?`);
    if (!confirmed) return;

    setDeletingPermissionId(permissionId);
    setActionError("");

    try {
      await adminRbacApi.deletePermission(permissionId);
      toast.success("Permission deleted.");
      await loadAdminData({ preferredRoleId: selectedRoleId });
      if (selectedRoleId) await loadRoleDetail(selectedRoleId);
      await refreshCurrentUserPermissions();
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to delete permission."));
    } finally {
      setDeletingPermissionId("");
    }
  }

  if (!canOpenPage) {
    return (
      <div className="mx-auto max-w-5xl px-6 py-8">
        <PermissionGate message="You do not have access to role and permission governance." />
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="mx-auto max-w-7xl px-6 py-8">
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 text-sm font-semibold text-slate-600 shadow-sm">
          Loading roles and permissions...
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-[1480px] space-y-6 px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
            Access Control
          </p>
          <h1 className="mt-1 text-3xl font-black text-[#18332D]">
            Roles & Permissions
          </h1>
          <p className="mt-2 max-w-3xl text-sm font-semibold leading-6 text-slate-600">
            Manage role names, permission assignments, and the permission catalog.
          </p>
        </div>

        <button
          type="button"
          onClick={() => loadAdminData({ preferredRoleId: selectedRoleId })}
          className="inline-flex h-10 items-center justify-center gap-2 rounded-md border border-[#B9D8CC] bg-white px-4 text-sm font-extrabold text-[#1F6F5F] transition hover:border-[#6FCF97]"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </div>

      {actionError && (
        <div className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
          <AlertTriangle size={17} />
          <span>{actionError}</span>
        </div>
      )}

      <section className="grid gap-5 xl:grid-cols-[290px_minmax(0,1fr)_360px]">
        <aside className="rounded-lg border border-[#B9D8CC] bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h2 className="text-base font-black text-[#18332D]">Roles</h2>
              <p className="mt-1 text-xs font-bold text-slate-500">{roles.length} total</p>
            </div>
            <span className="grid h-10 w-10 place-items-center rounded-md bg-[#6FCF97]/20 text-[#1F6F5F]">
              <ShieldCheck size={19} />
            </span>
          </div>

          {canCreateRole && (
            <form className="mt-4 flex gap-2" onSubmit={handleCreateRole}>
              <input
                value={newRoleName}
                onChange={(event) => setNewRoleName(event.target.value)}
                className="h-10 min-w-0 flex-1 rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800 outline-none focus:border-[#6FCF97]"
                placeholder="new_role"
              />
              <button
                type="submit"
                disabled={isCreatingRole || !newRoleName.trim()}
                className="grid h-10 w-10 shrink-0 place-items-center rounded-md bg-[#1F6F5F] text-white transition hover:bg-[#18584c] disabled:cursor-not-allowed disabled:opacity-50"
                title="Create role"
              >
                {isCreatingRole ? <Loader2 size={16} className="animate-spin" /> : <Plus size={16} />}
              </button>
            </form>
          )}

          <div className="mt-4 space-y-2">
            {!canViewRoles ? (
              <PermissionGate message="Requires role.view.any." />
            ) : roles.length === 0 ? (
              <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 px-3 py-6 text-center text-sm font-bold text-slate-500">
                No roles found.
              </div>
            ) : (
              roles.map((role) => {
                const roleId = getRoleId(role);
                const roleName = getRoleName(role);
                const isActive = roleId === selectedRoleId;

                return (
                  <button
                    key={roleId}
                    type="button"
                    onClick={() => setSelectedRoleId(roleId)}
                    className={`flex w-full items-center gap-3 rounded-md border px-3 py-2.5 text-left transition ${
                      isActive
                        ? "border-[#6FCF97] bg-[#6FCF97]/20"
                        : "border-slate-200 bg-white hover:border-[#B9D8CC] hover:bg-slate-50"
                    }`}
                  >
                    <span className={`grid h-9 w-9 shrink-0 place-items-center rounded-md ${
                      isActive ? "bg-[#1F6F5F] text-white" : "bg-slate-100 text-slate-600"
                    }`}>
                      <ShieldCheck size={16} />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-extrabold text-[#18332D]">{roleName}</span>
                      <span className="block text-xs font-bold text-slate-500">Role</span>
                    </span>
                  </button>
                );
              })
            )}
          </div>
        </aside>

        <main className="min-w-0 rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
          {!selectedRoleId ? (
            <PermissionGate icon={ShieldCheck} message="Select or create a role." />
          ) : !canViewRolePermissions ? (
            <PermissionGate message="Requires role_permission.view.any." />
          ) : isRoleLoading ? (
            <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-8 text-center text-sm font-bold text-slate-500">
              Loading selected role...
            </div>
          ) : (
            <>
              <div className="flex flex-col gap-4 border-b border-slate-200 pb-5 lg:flex-row lg:items-start lg:justify-between">
                <form className="min-w-0 flex-1" onSubmit={handleSaveRoleName}>
                  <label className="block">
                    <span className="text-xs font-extrabold uppercase text-slate-500">Role name</span>
                    <div className="mt-1 flex gap-2">
                      <input
                        value={roleNameDraft}
                        onChange={(event) => setRoleNameDraft(event.target.value)}
                        disabled={!canUpdateRole || isSelectedAdminRole}
                        className="h-11 min-w-0 flex-1 rounded-md border border-slate-200 bg-white px-3 text-base font-black text-[#18332D] outline-none focus:border-[#6FCF97] disabled:bg-slate-50 disabled:text-slate-500"
                      />
                      {canUpdateRole && (
                        <button
                          type="submit"
                          disabled={isSelectedAdminRole || isSavingRole || !roleNameChanged || !roleNameDraft.trim()}
                          className="inline-flex h-11 items-center gap-2 rounded-md bg-[#1F6F5F] px-4 text-sm font-extrabold text-white transition hover:bg-[#18584c] disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isSavingRole ? <Loader2 size={16} className="animate-spin" /> : <Save size={16} />}
                          Save
                        </button>
                      )}
                    </div>
                    {isSelectedAdminRole && (
                      <p className="mt-2 inline-flex items-center gap-1.5 text-xs font-extrabold text-[#1F6F5F]">
                        <LockKeyhole size={13} />
                        Built-in admin role is protected
                      </p>
                    )}
                  </label>
                </form>

                {canDeleteRole && !isSelectedAdminRole && (
                  <button
                    type="button"
                    onClick={handleDeleteRole}
                    disabled={isDeletingRole}
                    className="inline-flex h-11 items-center justify-center gap-2 rounded-md border border-red-200 bg-red-50 px-4 text-sm font-extrabold text-red-700 transition hover:bg-red-100 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {isDeletingRole ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                    Delete
                  </button>
                )}
              </div>

              <div className="mt-5 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <h2 className="text-lg font-black text-[#18332D]">Permission toggles</h2>
                  <p className="mt-1 text-xs font-bold text-slate-500">
                    {assignedPermissionIds.size} assigned of {permissions.length}
                  </p>
                </div>
                <label className="relative block min-w-0 lg:w-80">
                  <Search size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                  <input
                    value={permissionSearch}
                    onChange={(event) => setPermissionSearch(event.target.value)}
                    className="h-10 w-full rounded-md border border-slate-200 bg-white pl-9 pr-3 text-sm font-bold text-slate-800 outline-none focus:border-[#6FCF97]"
                    placeholder="Search permissions"
                  />
                </label>
              </div>

              {canViewPermissions ? (
                <>
                  <div className="mt-4 flex gap-2 overflow-x-auto pb-1">
                    <button
                      type="button"
                      onClick={() => setActiveGroup("all")}
                      className={`h-9 shrink-0 rounded-md border px-3 text-xs font-extrabold ${
                        activeGroup === "all"
                          ? "border-[#6FCF97] bg-[#6FCF97]/20 text-[#1F6F5F]"
                          : "border-slate-200 bg-white text-slate-600 hover:border-[#B9D8CC]"
                      }`}
                    >
                      All
                    </button>
                    {groupOptions.map((resource) => (
                      <button
                        key={resource}
                        type="button"
                        onClick={() => setActiveGroup(resource)}
                        className={`h-9 shrink-0 rounded-md border px-3 text-xs font-extrabold ${
                          activeGroup === resource
                            ? "border-[#6FCF97] bg-[#6FCF97]/20 text-[#1F6F5F]"
                            : "border-slate-200 bg-white text-slate-600 hover:border-[#B9D8CC]"
                        }`}
                      >
                        {getResourceLabel(resource)}
                      </button>
                    ))}
                  </div>

                  <div className="mt-5 space-y-5">
                    {visibleGroups.length === 0 ? (
                      <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm font-bold text-slate-500">
                        No permissions match the current filter.
                      </div>
                    ) : (
                      visibleGroups.map(([resource, groupPermissionsList]) => (
                        <section key={resource} className="rounded-lg border border-slate-200">
                          <div className="flex items-center justify-between gap-3 border-b border-slate-200 bg-slate-50 px-4 py-3">
                            <div>
                              <h3 className="text-sm font-black text-slate-900">{getResourceLabel(resource)}</h3>
                              <p className="text-xs font-bold text-slate-500">{groupPermissionsList.length} permissions</p>
                            </div>
                            <KeyRound size={17} className="text-[#1F6F5F]" />
                          </div>
                          <div className="divide-y divide-slate-100">
                            {groupPermissionsList.map((permission) => {
                              const permissionId = getPermissionId(permission);
                              const permissionName = getPermissionName(permission);
                              const isAssigned = assignedPermissionIds.has(permissionId);
                              const canToggle = !isSelectedAdminRole && (isAssigned ? canRevokeRolePermission : canAssignRolePermission);
                              const isBusy = mutatingPermissionId === permissionId;

                              return (
                                <div key={permissionId} className="flex items-center gap-4 px-4 py-3">
                                  <div className="min-w-0 flex-1">
                                    <div className="break-words font-mono text-sm font-black text-slate-900">
                                      {permissionName}
                                    </div>
                                    <div className="mt-1 text-xs font-semibold text-slate-500">
                                      {describePermission(permissionName)}
                                    </div>
                                  </div>
                                  <button
                                    type="button"
                                    onClick={() => handleTogglePermission(permission)}
                                    disabled={!canToggle || isBusy || Boolean(mutatingPermissionId && !isBusy)}
                                    className={`relative h-7 w-12 shrink-0 rounded-full border transition ${
                                      isAssigned
                                        ? "border-[#1F6F5F] bg-[#1F6F5F]"
                                        : "border-slate-300 bg-slate-200"
                                    } disabled:cursor-not-allowed disabled:opacity-50`}
                                    title={
                                      isSelectedAdminRole
                                        ? "Built-in admin role permissions are protected"
                                        : isAssigned
                                          ? "Requires role_permission.revoke.any"
                                          : "Requires role_permission.assign.any"
                                    }
                                  >
                                    <span
                                      className={`absolute top-1 grid h-5 w-5 place-items-center rounded-full bg-white text-[10px] text-[#1F6F5F] shadow transition ${
                                        isAssigned ? "left-6" : "left-1"
                                      }`}
                                    >
                                      {isBusy ? <Loader2 size={12} className="animate-spin" /> : isAssigned ? <Check size={12} /> : null}
                                    </span>
                                  </button>
                                </div>
                              );
                            })}
                          </div>
                        </section>
                      ))
                    )}
                  </div>
                </>
              ) : (
                <div className="mt-5">
                  <PermissionGate message="Requires permission.view.any." />
                </div>
              )}
            </>
          )}
        </main>

        <aside className="rounded-lg border border-[#B9D8CC] bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h2 className="text-base font-black text-[#18332D]">Permission Catalog</h2>
              <p className="mt-1 text-xs font-bold text-slate-500">{permissions.length} total</p>
            </div>
            <span className="grid h-10 w-10 place-items-center rounded-md bg-[#6FCF97]/20 text-[#1F6F5F]">
              <KeyRound size={18} />
            </span>
          </div>

          {!canViewPermissions ? (
            <div className="mt-4">
              <PermissionGate message="Requires permission.view.any." />
            </div>
          ) : (
            <>
              {canCreatePermission && (
                <form className="mt-4 flex gap-2" onSubmit={handleCreatePermission}>
                  <input
                    value={newPermissionName}
                    onChange={(event) => setNewPermissionName(event.target.value)}
                    className="h-10 min-w-0 flex-1 rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800 outline-none focus:border-[#6FCF97]"
                    placeholder="resource.action.scope"
                  />
                  <button
                    type="submit"
                    disabled={isCreatingPermission || !newPermissionName.trim()}
                    className="grid h-10 w-10 shrink-0 place-items-center rounded-md bg-[#1F6F5F] text-white transition hover:bg-[#18584c] disabled:cursor-not-allowed disabled:opacity-50"
                    title="Create permission"
                  >
                    {isCreatingPermission ? <Loader2 size={16} className="animate-spin" /> : <Plus size={16} />}
                  </button>
                </form>
              )}

              <label className="relative mt-4 block">
                <Search size={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                <input
                  value={catalogSearch}
                  onChange={(event) => setCatalogSearch(event.target.value)}
                  className="h-10 w-full rounded-md border border-slate-200 bg-white pl-9 pr-3 text-sm font-bold text-slate-800 outline-none focus:border-[#6FCF97]"
                  placeholder="Search catalog"
                />
              </label>

              <div className="mt-4 max-h-[calc(100vh-260px)] space-y-2 overflow-y-auto pr-1">
                {filteredCatalogPermissions.length === 0 ? (
                  <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 px-3 py-6 text-center text-sm font-bold text-slate-500">
                    No permissions found.
                  </div>
                ) : (
                  filteredCatalogPermissions.map((permission) => {
                    const permissionId = getPermissionId(permission);
                    const permissionName = getPermissionName(permission);
                    const isEditing = editingPermissionId === permissionId;
                    const isSystemPermission = isSystemPermissionName(permissionName);

                    return (
                      <div key={permissionId} className="rounded-md border border-slate-200 bg-slate-50 p-3">
                        {isEditing ? (
                          <div className="space-y-2">
                            <input
                              value={permissionDraftName}
                              onChange={(event) => setPermissionDraftName(event.target.value)}
                              className="h-9 w-full rounded-md border border-slate-200 bg-white px-2 font-mono text-xs font-bold text-slate-900 outline-none focus:border-[#6FCF97]"
                            />
                            <div className="flex justify-end gap-2">
                              <button
                                type="button"
                                onClick={() => handleUpdatePermission(permissionId)}
                                disabled={isSystemPermission || savingPermissionId === permissionId || !permissionDraftName.trim()}
                                className="grid h-8 w-8 place-items-center rounded-md bg-[#1F6F5F] text-white disabled:cursor-not-allowed disabled:opacity-50"
                                title="Save permission"
                              >
                                {savingPermissionId === permissionId ? <Loader2 size={14} className="animate-spin" /> : <Check size={14} />}
                              </button>
                              <button
                                type="button"
                                onClick={() => {
                                  setEditingPermissionId("");
                                  setPermissionDraftName("");
                                }}
                                className="grid h-8 w-8 place-items-center rounded-md border border-slate-200 bg-white text-slate-600"
                                title="Cancel"
                              >
                                <X size={14} />
                              </button>
                            </div>
                          </div>
                        ) : (
                          <div className="flex items-start gap-3">
                            <div className="min-w-0 flex-1">
                              <div className="break-words font-mono text-xs font-black text-slate-900">
                                {permissionName}
                              </div>
                              <div className="mt-1 text-xs font-semibold text-slate-500">
                                {describePermission(permissionName)}
                              </div>
                            </div>
                            <div className="flex shrink-0 gap-1">
                              {isSystemPermission && (
                                <span
                                  className="grid h-8 w-8 place-items-center rounded-md border border-[#B9D8CC] bg-white text-[#1F6F5F]"
                                  title="System permission"
                                >
                                  <LockKeyhole size={14} />
                                </span>
                              )}
                              {canUpdatePermission && !isSystemPermission && (
                                <button
                                  type="button"
                                  onClick={() => startEditingPermission(permission)}
                                  className="grid h-8 w-8 place-items-center rounded-md border border-slate-200 bg-white text-slate-600 hover:text-[#1F6F5F]"
                                  title="Edit permission"
                                >
                                  <Pencil size={14} />
                                </button>
                              )}
                              {canDeletePermission && !isSystemPermission && (
                                <button
                                  type="button"
                                  onClick={() => handleDeletePermission(permission)}
                                  disabled={deletingPermissionId === permissionId}
                                  className="grid h-8 w-8 place-items-center rounded-md border border-red-200 bg-red-50 text-red-700 disabled:cursor-not-allowed disabled:opacity-50"
                                  title="Delete permission"
                                >
                                  {deletingPermissionId === permissionId ? <Loader2 size={14} className="animate-spin" /> : <Trash2 size={14} />}
                                </button>
                              )}
                            </div>
                          </div>
                        )}
                      </div>
                    );
                  })
                )}
              </div>
            </>
          )}
        </aside>
      </section>
    </div>
  );
}
