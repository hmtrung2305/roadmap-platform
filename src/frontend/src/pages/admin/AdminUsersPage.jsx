/* eslint-disable react-hooks/exhaustive-deps, react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  Check,
  Loader2,
  RefreshCw,
  Search,
  ShieldCheck,
  UserRound,
  UsersRound,
} from "lucide-react";
import { toast } from "react-toastify";

import { adminRbacApi } from "../../api/adminRbacApi";
import { adminUsersApi } from "../../api/adminUsersApi";
import { PERMISSIONS } from "../../constants/permissions";
import { useAuthStore } from "../../stores/useAuthStore";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { hasPermission } from "../../utils/authorizationUtils";

function getUserId(user) {
  return user?.userId || user?.UserId || "";
}

function getUsername(user) {
  return user?.username || user?.Username || "";
}

function getUserEmail(user) {
  return user?.email || user?.Email || "";
}

function getUserStatus(user) {
  return user?.status || user?.Status || "";
}

function getUserRoles(user) {
  return user?.roles || user?.Roles || [];
}

function getRoleId(role) {
  return role?.roleId || role?.RoleId || "";
}

function getRoleName(role) {
  return role?.roleName || role?.RoleName || "";
}

function isAdminRole(role) {
  return getRoleName(role).trim().toLowerCase() === "admin";
}

function sortByName(items, getName) {
  return [...items].sort((first, second) => getName(first).localeCompare(getName(second)));
}

function formatDate(value) {
  if (!value) return "-";

  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function EmptyState({ message }) {
  return (
    <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm font-bold text-slate-500">
      {message}
    </div>
  );
}

export default function AdminUsersPage() {
  const currentUser = useAuthStore((state) => state.user);
  const revalidateCurrentUser = useAuthStore((state) => state.revalidateCurrentUser);

  const canViewUsers = hasPermission(currentUser, PERMISSIONS.USER_VIEW_ANY);
  const canViewUserRoles = hasPermission(currentUser, PERMISSIONS.USER_ROLE_VIEW_ANY);
  const canAssignUserRole = hasPermission(currentUser, PERMISSIONS.USER_ROLE_ASSIGN_ANY);
  const canRevokeUserRole = hasPermission(currentUser, PERMISSIONS.USER_ROLE_REVOKE_ANY);

  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [selectedUserDetail, setSelectedUserDetail] = useState(null);
  const [search, setSearch] = useState("");
  const [searchDraft, setSearchDraft] = useState("");
  const [actionError, setActionError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isUserLoading, setIsUserLoading] = useState(false);
  const [mutatingRoleId, setMutatingRoleId] = useState("");

  const selectedUserRoleIds = useMemo(() => {
    return new Set(getUserRoles(selectedUserDetail).map(getRoleId).filter(Boolean));
  }, [selectedUserDetail]);

  const selectedUser = useMemo(() => {
    return users.find((user) => getUserId(user) === selectedUserId) || null;
  }, [selectedUserId, users]);
  const currentUserId = getUserId(currentUser);
  const isSelectedCurrentUser = Boolean(selectedUserId && selectedUserId === currentUserId);

  useEffect(() => {
    loadAdminUsers();
  }, [search]);

  useEffect(() => {
    if (!selectedUserId || !canViewUsers || !canViewUserRoles) {
      setSelectedUserDetail(null);
      return;
    }

    loadUserDetail(selectedUserId);
  }, [selectedUserId]);

  async function loadAdminUsers({ preferredUserId = selectedUserId } = {}) {
    if (!canViewUsers || !canViewUserRoles) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setActionError("");

    try {
      const [userData, roleData] = await Promise.all([
        adminUsersApi.getUsers({ search }),
        adminRbacApi.getRoles(),
      ]);

      const normalizedUsers = sortByName(userData || [], getUsername);
      const normalizedRoles = sortByName(roleData || [], getRoleName);
      const preferredUserExists = normalizedUsers.some((user) => getUserId(user) === preferredUserId);
      const nextUserId = preferredUserExists ? preferredUserId : getUserId(normalizedUsers[0]);

      setUsers(normalizedUsers);
      setRoles(normalizedRoles);
      setSelectedUserId(nextUserId || "");

      if (!nextUserId) {
        setSelectedUserDetail(null);
      }
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to load users."));
    } finally {
      setIsLoading(false);
    }
  }

  async function loadUserDetail(userId) {
    setIsUserLoading(true);
    setActionError("");

    try {
      const user = await adminUsersApi.getUser(userId);
      setSelectedUserDetail(user);
    } catch (error) {
      setSelectedUserDetail(null);
      setActionError(getFriendlyApiErrorMessage(error, "Unable to load user roles."));
    } finally {
      setIsUserLoading(false);
    }
  }

  async function handleToggleRole(role) {
    if (!selectedUserId) return;

    const roleId = getRoleId(role);
    const roleName = getRoleName(role);
    const isAssigned = selectedUserRoleIds.has(roleId);
    const canToggle = isAssigned ? canRevokeUserRole : canAssignUserRole;

    if (!roleId || !canToggle || mutatingRoleId) return;

    setMutatingRoleId(roleId);
    setActionError("");

    try {
      const user = isAssigned
        ? await adminUsersApi.revokeRole(selectedUserId, roleId)
        : await adminUsersApi.assignRole(selectedUserId, roleId);

      setSelectedUserDetail(user);
      setUsers((current) => current.map((item) => (
        getUserId(item) === selectedUserId ? user : item
      )));
      toast.success(`${isAssigned ? "Revoked" : "Assigned"} ${roleName}.`);

      if (selectedUserId === (currentUser?.userId || currentUser?.UserId)) {
        await revalidateCurrentUser({ force: true });
      }
    } catch (error) {
      setActionError(getFriendlyApiErrorMessage(error, "Unable to update user role."));
    } finally {
      setMutatingRoleId("");
    }
  }

  function handleSearchSubmit(event) {
    event.preventDefault();
    setSearch(searchDraft.trim());
  }

  if (!canViewUsers || !canViewUserRoles) {
    return (
      <div className="mx-auto max-w-5xl px-6 py-8">
        <EmptyState message="Requires user.view.any and user_role.view.any." />
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="mx-auto max-w-7xl px-6 py-8">
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 text-sm font-semibold text-slate-600 shadow-sm">
          Loading users...
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-[1320px] space-y-6 px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
            User Governance
          </p>
          <h1 className="mt-1 text-3xl font-black text-[#18332D]">
            Users
          </h1>
          <p className="mt-2 max-w-3xl text-sm font-semibold leading-6 text-slate-600">
            Review users and manage role assignments.
          </p>
        </div>

        <button
          type="button"
          onClick={() => loadAdminUsers({ preferredUserId: selectedUserId })}
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

      <section className="grid gap-5 xl:grid-cols-[360px_minmax(0,1fr)]">
        <aside className="rounded-lg border border-[#B9D8CC] bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h2 className="text-base font-black text-[#18332D]">Users</h2>
              <p className="mt-1 text-xs font-bold text-slate-500">{users.length} shown</p>
            </div>
            <span className="grid h-10 w-10 place-items-center rounded-md bg-[#6FCF97]/20 text-[#1F6F5F]">
              <UsersRound size={19} />
            </span>
          </div>

          <form className="mt-4 flex gap-2" onSubmit={handleSearchSubmit}>
            <label className="relative min-w-0 flex-1">
              <Search size={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                value={searchDraft}
                onChange={(event) => setSearchDraft(event.target.value)}
                className="h-10 w-full rounded-md border border-slate-200 bg-white pl-9 pr-3 text-sm font-bold text-slate-800 outline-none focus:border-[#6FCF97]"
                placeholder="Search user"
              />
            </label>
            <button
              type="submit"
              className="h-10 rounded-md bg-[#1F6F5F] px-4 text-sm font-extrabold text-white transition hover:bg-[#18584c]"
            >
              Search
            </button>
          </form>

          <div className="mt-4 space-y-2">
            {users.length === 0 ? (
              <EmptyState message="No users found." />
            ) : (
              users.map((user) => {
                const userId = getUserId(user);
                const isActive = userId === selectedUserId;
                const userRoles = getUserRoles(user);

                return (
                  <button
                    key={userId}
                    type="button"
                    onClick={() => setSelectedUserId(userId)}
                    className={`flex w-full items-center gap-3 rounded-md border px-3 py-2.5 text-left transition ${
                      isActive
                        ? "border-[#6FCF97] bg-[#6FCF97]/20"
                        : "border-slate-200 bg-white hover:border-[#B9D8CC] hover:bg-slate-50"
                    }`}
                  >
                    <span className={`grid h-10 w-10 shrink-0 place-items-center rounded-md ${
                      isActive ? "bg-[#1F6F5F] text-white" : "bg-slate-100 text-slate-600"
                    }`}>
                      <UserRound size={17} />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-extrabold text-[#18332D]">
                        {getUsername(user)}
                      </span>
                      <span className="block truncate text-xs font-bold text-slate-500">
                        {getUserEmail(user) || "No email"}
                      </span>
                      <span className="mt-1 block truncate text-[11px] font-extrabold uppercase text-[#1F6F5F]">
                        {userRoles.map(getRoleName).join(", ") || "No role"}
                      </span>
                    </span>
                  </button>
                );
              })
            )}
          </div>
        </aside>

        <main className="min-w-0 rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
          {!selectedUserId ? (
            <EmptyState message="Select a user." />
          ) : isUserLoading ? (
            <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-8 text-center text-sm font-bold text-slate-500">
              Loading selected user...
            </div>
          ) : (
            <>
              <div className="flex flex-col gap-4 border-b border-slate-200 pb-5 lg:flex-row lg:items-start lg:justify-between">
                <div className="min-w-0">
                  <div className="flex items-center gap-3">
                    <span className="grid h-12 w-12 shrink-0 place-items-center rounded-md bg-[#1F6F5F] text-white">
                      <UserRound size={21} />
                    </span>
                    <div className="min-w-0">
                      <h2 className="truncate text-2xl font-black text-[#18332D]">
                        {getUsername(selectedUserDetail || selectedUser)}
                      </h2>
                      <p className="truncate text-sm font-bold text-slate-500">
                        {getUserEmail(selectedUserDetail || selectedUser) || "No email"}
                      </p>
                    </div>
                  </div>
                  <div className="mt-4 grid gap-3 text-sm font-semibold text-slate-600 sm:grid-cols-3">
                    <InfoTile label="Status" value={getUserStatus(selectedUserDetail || selectedUser)} />
                    <InfoTile label="Created" value={formatDate(selectedUserDetail?.createdAt || selectedUserDetail?.CreatedAt)} />
                    <InfoTile label="Updated" value={formatDate(selectedUserDetail?.updatedAt || selectedUserDetail?.UpdatedAt)} />
                  </div>
                </div>
              </div>

              <div className="mt-5">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <h3 className="text-lg font-black text-[#18332D]">Role assignments</h3>
                    <p className="mt-1 text-xs font-bold text-slate-500">
                      {selectedUserRoleIds.size} assigned of {roles.length}
                    </p>
                  </div>
                  <ShieldCheck size={21} className="text-[#1F6F5F]" />
                </div>

                <div className="mt-4 divide-y divide-slate-100 rounded-lg border border-slate-200">
                  {roles.length === 0 ? (
                    <div className="p-4">
                      <EmptyState message="No roles found." />
                    </div>
                  ) : (
                    roles.map((role) => {
                      const roleId = getRoleId(role);
                      const roleName = getRoleName(role);
                      const isAssigned = selectedUserRoleIds.has(roleId);
                      const isSelfAdminRevoke = isSelectedCurrentUser && isAssigned && isAdminRole(role);
                      const canToggle = !isSelfAdminRevoke && (isAssigned ? canRevokeUserRole : canAssignUserRole);
                      const isBusy = mutatingRoleId === roleId;

                      return (
                        <div key={roleId} className="flex items-center gap-4 px-4 py-3">
                          <div className="min-w-0 flex-1">
                            <div className="break-words font-mono text-sm font-black text-slate-900">
                              {roleName}
                            </div>
                            <div className="mt-1 text-xs font-semibold text-slate-500">
                              Access role
                            </div>
                            {isSelfAdminRevoke && (
                              <div className="mt-1 text-xs font-extrabold text-[#1F6F5F]">
                                Protected on your account
                              </div>
                            )}
                          </div>
                          <button
                            type="button"
                            onClick={() => handleToggleRole(role)}
                            disabled={!canToggle || isBusy || Boolean(mutatingRoleId && !isBusy)}
                            className={`relative h-7 w-12 shrink-0 rounded-full border transition ${
                              isAssigned
                                ? "border-[#1F6F5F] bg-[#1F6F5F]"
                                : "border-slate-300 bg-slate-200"
                            } disabled:cursor-not-allowed disabled:opacity-50`}
                            title={
                              isSelfAdminRevoke
                                ? "You cannot revoke your own admin role"
                                : isAssigned
                                  ? "Requires user_role.revoke.any"
                                  : "Requires user_role.assign.any"
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
                    })
                  )}
                </div>
              </div>
            </>
          )}
        </main>
      </section>
    </div>
  );
}

function InfoTile({ label, value }) {
  return (
    <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2">
      <div className="text-[11px] font-extrabold uppercase text-slate-500">{label}</div>
      <div className="mt-1 truncate font-black text-slate-900">{value || "-"}</div>
    </div>
  );
}
