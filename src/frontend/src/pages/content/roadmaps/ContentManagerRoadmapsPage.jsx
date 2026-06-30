/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion } from "framer-motion";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  ChevronLeft,
  ChevronRight,
  Edit3,
  Grid2X2,
  List,
  Map as MapIcon,
  MoreHorizontal,
  Plus,
  Search,
  Trash2,
} from "lucide-react";

import { toast } from "react-toastify";

import { contentManagerRoadmapApi } from "../../../api/contentRoadmapApi";
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";
import AppSelect from "../../../components/common/AppSelect";
import ConfirmActionDialog from "../../../features/learningModules/components/ConfirmActionDialog";
import {
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModuleField,
  ModulePageShell,
  inputClass,
  numberInputClass,
  selectClass,
} from "../../../features/learningModules/components/learningModuleUi";
import {
  initialRoadmapListResult,
  roadmapPageSize,
  roadmapSortOptions,
  roadmapStatuses,
} from "../../../features/roadmapEditor/roadmapEditorConstants";
import {
  formatDate,
  formatVersionLabel,
  getStatusTone,
  isCanceledRequest,
  parsePage,
  prettyStatus,
} from "../../../features/roadmapEditor/roadmapEditorUtils";

const viewModes = [
  { value: "list", label: "List", icon: List },
  { value: "card", label: "Cards", icon: Grid2X2 },
];

const initialCreateRoadmapForm = {
  careerRoleId: "",
  title: "",
  description: "",
  estimatedTotalHours: "",
};

function normalizeCareerRole(role) {
  return {
    careerRoleId: role?.careerRoleId || role?.CareerRoleId || "",
    name: role?.name || role?.Name || "",
    slug: role?.slug || role?.Slug || "",
  };
}

function normalizeCareerRoles(data) {
  return (Array.isArray(data) ? data : [])
    .map(normalizeCareerRole)
    .filter((role) => role.careerRoleId && role.name);
}

function CreateRoadmapModal({
  isOpen,
  form,
  setForm,
  careerRoles,
  isLoadingRoles,
  isCreating,
  error,
  onClose,
  onCreate,
}) {
  if (!isOpen) return null;

  return createPortal(
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/35 px-4 py-6">
      <div
        className="absolute inset-0"
        aria-hidden="true"
        onClick={() => {
          if (!isCreating) onClose();
        }}
      />
      <form
        onSubmit={(event) => {
          event.preventDefault();
          onCreate();
        }}
        className="relative z-[101] w-full max-w-xl rounded-2xl border border-[#B9D8CC] bg-white p-5 shadow-2xl"
      >
        <div className="mb-4 flex items-start justify-between gap-3">
          <div>
            <h2 className="text-lg font-extrabold text-[#18332D]">Create roadmap</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isCreating}
            className="rounded-lg px-2 py-1 text-sm font-black text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D] disabled:cursor-not-allowed disabled:opacity-60"
            aria-label="Close create roadmap modal"
          >
            ×
          </button>
        </div>

        <div className="space-y-4">
          <ModuleField label="Career role">
            <select
              value={form.careerRoleId}
              onChange={(event) =>
                setForm((current) => ({ ...current, careerRoleId: event.target.value }))
              }
              className={selectClass}
              disabled={isLoadingRoles || isCreating}
            >
              <option value="">
                {isLoadingRoles ? "Loading roles..." : "Select career role"}
              </option>
              {careerRoles.map((role) => (
                <option key={role.careerRoleId} value={role.careerRoleId}>
                  {role.name}
                </option>
              ))}
            </select>
          </ModuleField>

          <ModuleField label="Roadmap title">
            <input
              type="text"
              value={form.title}
              onChange={(event) =>
                setForm((current) => ({ ...current, title: event.target.value }))
              }
              className={inputClass}
              placeholder="Example: Backend Developer Roadmap"
              disabled={isCreating}
              maxLength={200}
            />
          </ModuleField>

          <ModuleField label="Description">
            <textarea
              value={form.description}
              onChange={(event) =>
                setForm((current) => ({ ...current, description: event.target.value }))
              }
              className={`${inputClass} min-h-24 resize-y`}
              disabled={isCreating}
            />
          </ModuleField>

          <ModuleField label="Estimated hours">
            <input
              type="number"
              min="1"
              step="1"
              value={form.estimatedTotalHours}
              onChange={(event) =>
                setForm((current) => ({ ...current, estimatedTotalHours: event.target.value }))
              }
              className={numberInputClass}
              disabled={isCreating}
            />
          </ModuleField>

          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-bold text-red-700">
              {error}
            </div>
          )}
        </div>

        <div className="mt-5 flex justify-end gap-2">
          <ModuleButton variant="secondary" onClick={onClose} disabled={isCreating}>
            Cancel
          </ModuleButton>
          <ModuleButton type="submit" disabled={isCreating || isLoadingRoles}>
            {isCreating ? "Creating..." : "Create draft"}
          </ModuleButton>
        </div>
      </form>
    </div>,
    document.body,
  );
}

function RoadmapActionsMenu({ roadmap, onDelete }) {
  const [isOpen, setIsOpen] = useState(false);
  const [menuPosition, setMenuPosition] = useState(null);
  const buttonRef = useRef(null);
  const menuRef = useRef(null);
  const isDraft = String(roadmap.status || "").toLowerCase() === "draft";

  const updateMenuPosition = () => {
    if (!buttonRef.current) return;

    const rect = buttonRef.current.getBoundingClientRect();
    const menuWidth = 176;
    const spacing = 8;
    const left = Math.min(
      Math.max(spacing, rect.right - menuWidth),
      window.innerWidth - menuWidth - spacing,
    );
    const top = Math.min(rect.bottom + spacing, window.innerHeight - 72);

    setMenuPosition({ top, left, width: menuWidth });
  };

  useEffect(() => {
    if (!isOpen) return undefined;

    updateMenuPosition();

    const onPointerDown = (event) => {
      if (
        menuRef.current?.contains(event.target) ||
        buttonRef.current?.contains(event.target)
      ) {
        return;
      }

      setIsOpen(false);
    };

    const onViewportChange = () => updateMenuPosition();

    document.addEventListener("mousedown", onPointerDown);
    window.addEventListener("resize", onViewportChange);
    window.addEventListener("scroll", onViewportChange, true);

    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      window.removeEventListener("resize", onViewportChange);
      window.removeEventListener("scroll", onViewportChange, true);
    };
  }, [isOpen]);

  const menu =
    isOpen && menuPosition
      ? createPortal(
          <div
            ref={menuRef}
            style={{
              top: menuPosition.top,
              left: menuPosition.left,
              width: menuPosition.width,
            }}
            className="fixed z-[90] rounded-xl border border-[#B9D8CC] bg-white p-1 shadow-xl"
          >
            <div className="group/tooltip relative">
              <button
                type="button"
                aria-disabled={!isDraft}
                onClick={() => {
                  if (!isDraft) return;
                  setIsOpen(false);
                  onDelete(roadmap);
                }}
                className={`flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm font-bold transition ${
                  isDraft
                    ? "text-rose-700 hover:bg-rose-50"
                    : "cursor-not-allowed text-slate-400"
                }`}
              >
                <Trash2 size={14} /> Delete
              </button>
              {!isDraft && (
                <div className="pointer-events-none absolute right-full top-1/2 z-[95] mr-2 hidden w-56 -translate-y-1/2 rounded-lg border border-[#B9D8CC] bg-[#18332D] px-3 py-2 text-xs font-semibold text-white shadow-xl group-hover/tooltip:block">
                  Only draft roadmap versions can be deleted.
                </div>
              )}
            </div>
          </div>,
          document.body,
        )
      : null;

  return (
    <div className="relative z-30">
      <button
        ref={buttonRef}
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="grid h-9 w-9 place-items-center rounded-lg border border-[#B9D8CC] bg-white text-slate-600 shadow-sm transition hover:border-[#2FA084] hover:bg-[#F7F1E8] hover:text-[#18332D]"
        aria-label="Roadmap actions"
      >
        <MoreHorizontal size={16} />
      </button>
      {menu}
    </div>
  );
}

function RoadmapCard({ roadmap, onEdit, onDelete }) {
  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: 8 }}
      transition={{ duration: 0.18 }}
    >
      <ModuleCard className="overflow-visible transition hover:-translate-y-0.5 hover:shadow-md">
        <div className="space-y-4 p-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="min-w-0 flex-1">
              <div className="mb-2 flex flex-wrap items-center gap-2">
                <ModuleBadge tone={getStatusTone(roadmap.status)}>
                  {prettyStatus(roadmap.status)}
                </ModuleBadge>
                <span className="rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-2 py-0.5 text-xs font-black text-[#18332D]">
                  {formatVersionLabel(roadmap)}
                </span>
              </div>

              <h2 className="truncate text-lg font-extrabold text-[#18332D]">
                {roadmap.title}
              </h2>
              <p className="mt-1 text-sm font-semibold text-slate-600">
                Career Role: {roadmap.careerRole?.name || roadmap.slug}
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[#B9D8CC]/70 pt-4">
            <span className="text-xs font-bold text-slate-500">
              Updated {formatDate(roadmap.updatedAt || roadmap.createdAt)}
            </span>
            <div className="flex items-center gap-2">
              <ModuleButton
                className="h-9 min-w-[132px] px-3"
                onClick={() => onEdit(roadmap)}
              >
                <Edit3 size={14} className="shrink-0" />{" "}
                <span>Open editor</span>
              </ModuleButton>
              <RoadmapActionsMenu roadmap={roadmap} onDelete={onDelete} />
            </div>
          </div>
        </div>
      </ModuleCard>
    </motion.div>
  );
}

function RoadmapList({ roadmaps, onEdit, onDelete }) {
  return (
    <ModuleCard className="overflow-visible">
      <div className="hidden grid-cols-[minmax(280px,1.6fr)_220px_120px_150px_190px] gap-3 border-b border-[#B9D8CC] bg-[#F7F1E8]/70 px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-700 xl:grid">
        <span>Roadmap</span>
        <span>Career role</span>
        <span>Status</span>
        <span>Updated</span>
        <span className="text-right">Action</span>
      </div>

      <div>
        {roadmaps.map((roadmap) => (
          <div
            key={`${roadmap.roadmapId}-${roadmap.roadmapVersionId}`}
            className="grid gap-3 border-b border-[#B9D8CC]/60 px-4 py-4 last:border-b-0 xl:grid-cols-[minmax(280px,1.6fr)_220px_120px_150px_190px] xl:items-center"
          >
            <div className="min-w-0">
              <div className="truncate text-base font-extrabold text-[#18332D]">
                {roadmap.title}
              </div>
            </div>

            <div className="text-sm font-bold text-slate-700">
              {roadmap.careerRole?.name || roadmap.slug}
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <ModuleBadge tone={getStatusTone(roadmap.status)}>
                {prettyStatus(roadmap.status)}
              </ModuleBadge>
              <span className="rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-2 py-0.5 text-xs font-black text-[#18332D]">
                {formatVersionLabel(roadmap)}
              </span>
            </div>

            <div className="text-sm font-bold text-slate-600">
              {formatDate(roadmap.updatedAt || roadmap.createdAt)}
            </div>

            <div className="flex justify-start gap-2 xl:justify-end">
              <ModuleButton
                className="h-9 min-w-[132px] px-3"
                onClick={() => onEdit(roadmap)}
              >
                <Edit3 size={14} className="shrink-0" />{" "}
                <span>Open editor</span>
              </ModuleButton>
              <RoadmapActionsMenu roadmap={roadmap} onDelete={onDelete} />
            </div>
          </div>
        ))}
      </div>
    </ModuleCard>
  );
}

export default function ContentManagerRoadmapsPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const requestedStatus = searchParams.get("status");
  const activeStatus = roadmapStatuses.includes(requestedStatus)
    ? requestedStatus
    : "draft";
  const searchQuery = searchParams.get("q") || "";
  const requestedSort = searchParams.get("sort") || "updated_desc";
  const sort = roadmapSortOptions.some(
    (option) => option.value === requestedSort,
  )
    ? requestedSort
    : "updated_desc";
  const requestedView = searchParams.get("view") || "list";
  const viewMode = viewModes.some((option) => option.value === requestedView)
    ? requestedView
    : "list";
  const requestedPage = parsePage(searchParams.get("page"));
  const searchParamsString = searchParams.toString();

  const [searchInput, setSearchInput] = useState(searchQuery);
  const [result, setResult] = useState(initialRoadmapListResult);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [reloadToken, setReloadToken] = useState(0);
  const [roadmapToDelete, setRoadmapToDelete] = useState(null);
  const [isDeletingDraft, setIsDeletingDraft] = useState(false);
  const [isCreateRoadmapOpen, setIsCreateRoadmapOpen] = useState(false);
  const [careerRoles, setCareerRoles] = useState([]);
  const [isLoadingCareerRoles, setIsLoadingCareerRoles] = useState(false);
  const [createRoadmapForm, setCreateRoadmapForm] = useState(initialCreateRoadmapForm);
  const [createRoadmapError, setCreateRoadmapError] = useState("");
  const [isCreatingRoadmap, setIsCreatingRoadmap] = useState(false);

  const setQueryValues = (changes, options) => {
    const next = new URLSearchParams(searchParamsString);

    Object.entries(changes).forEach(([key, value]) => {
      if (value === null || value === undefined || value === "") {
        next.delete(key);
      } else {
        next.set(key, String(value));
      }
    });

    setSearchParams(next, options);
  };

  useEffect(() => {
    setSearchInput(searchQuery);
  }, [searchQuery]);

  useEffect(() => {
    if (!isCreateRoadmapOpen || careerRoles.length > 0) return undefined;

    let isActive = true;

    async function loadCareerRoles() {
      try {
        setIsLoadingCareerRoles(true);
        setCreateRoadmapError("");

        const roles = normalizeCareerRoles(await contentManagerRoadmapApi.getCareerRoles());
        if (!isActive) return;

        setCareerRoles(roles);
        setCreateRoadmapForm((current) => ({
          ...current,
          careerRoleId: current.careerRoleId || roles[0]?.careerRoleId || "",
        }));
      } catch (requestError) {
        if (isActive) {
          setCreateRoadmapError(requestError?.message || "Unable to load career roles.");
        }
      } finally {
        if (isActive) {
          setIsLoadingCareerRoles(false);
        }
      }
    }

    loadCareerRoles();

    return () => {
      isActive = false;
    };
  }, [careerRoles.length, isCreateRoadmapOpen]);

  useEffect(() => {
    const normalizedSearch = searchInput.trim();
    if (normalizedSearch === searchQuery) return undefined;

    const timeoutId = window.setTimeout(() => {
      const next = new URLSearchParams(searchParamsString);

      if (normalizedSearch) {
        next.set("q", normalizedSearch);
      } else {
        next.delete("q");
      }

      next.delete("page");
      setSearchParams(next, { replace: true });
    }, 350);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput, searchQuery, searchParamsString, setSearchParams]);

  useEffect(() => {
    const controller = new AbortController();

    async function loadRoadmaps() {
      try {
        setIsLoading(true);
        setError("");

        const nextResult = await contentManagerRoadmapApi.getRoadmaps({
          status: activeStatus,
          search: searchQuery,
          sort,
          page: requestedPage,
          pageSize: roadmapPageSize,
          signal: controller.signal,
        });

        setResult(nextResult);

        if (nextResult.page !== requestedPage) {
          const next = new URLSearchParams(searchParamsString);

          if (nextResult.page > 1) {
            next.set("page", String(nextResult.page));
          } else {
            next.delete("page");
          }

          setSearchParams(next, { replace: true });
        }
      } catch (requestError) {
        if (!isCanceledRequest(requestError)) {
          setError(requestError?.message || "Unable to load roadmaps.");
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    loadRoadmaps();

    return () => controller.abort();
  }, [
    activeStatus,
    requestedPage,
    reloadToken,
    searchQuery,
    searchParamsString,
    setSearchParams,
    sort,
  ]);

  const pageRange = useMemo(() => {
    if (result.totalCount === 0) return null;

    return {
      start: (result.page - 1) * result.pageSize + 1,
      end: Math.min(result.page * result.pageSize, result.totalCount),
    };
  }, [result.page, result.pageSize, result.totalCount]);

  const hasFilters = Boolean(searchQuery);

  const resetFilters = () => {
    setSearchInput("");
    setQueryValues({ q: null, sort: null, page: null });
  };

  const openCreateRoadmap = () => {
    setCreateRoadmapError("");
    setCreateRoadmapForm({
      ...initialCreateRoadmapForm,
      careerRoleId: careerRoles[0]?.careerRoleId || "",
    });
    setIsCreateRoadmapOpen(true);
  };

  const closeCreateRoadmap = () => {
    if (isCreatingRoadmap) return;

    setIsCreateRoadmapOpen(false);
    setCreateRoadmapError("");
    setCreateRoadmapForm(initialCreateRoadmapForm);
  };

  const createRoadmap = async () => {
    const title = createRoadmapForm.title.trim();
    const careerRoleId = createRoadmapForm.careerRoleId;
    const estimatedTotalHoursText = String(createRoadmapForm.estimatedTotalHours || "").trim();
    const estimatedTotalHours = estimatedTotalHoursText
      ? Number(estimatedTotalHoursText)
      : null;

    if (!careerRoleId) {
      setCreateRoadmapError("Select a career role.");
      return;
    }

    if (!title) {
      setCreateRoadmapError("Roadmap title is required.");
      return;
    }

    if (estimatedTotalHours !== null
      && (!Number.isInteger(estimatedTotalHours) || estimatedTotalHours <= 0)) {
      setCreateRoadmapError("Estimated hours must be a positive whole number.");
      return;
    }

    try {
      setIsCreatingRoadmap(true);
      setCreateRoadmapError("");

      const createdRoadmap = await contentManagerRoadmapApi.createRoadmap({
        careerRoleId,
        title,
        description: createRoadmapForm.description.trim() || null,
        estimatedTotalHours,
      });

      toast.success("Roadmap draft created.");
      navigate(
        `/content/roadmaps/${createdRoadmap.roadmapId}/edit?versionId=${createdRoadmap.roadmapVersionId}`,
      );
    } catch (requestError) {
      setCreateRoadmapError(getFriendlyApiErrorMessage(requestError, "Unable to create roadmap."));
    } finally {
      setIsCreatingRoadmap(false);
    }
  };

  const openEditor = (roadmap) => {
    const versionQuery = roadmap.roadmapVersionId
      ? `?versionId=${roadmap.roadmapVersionId}`
      : "";
    navigate(`/content/roadmaps/${roadmap.roadmapId}/edit${versionQuery}`);
  };

  const confirmDeleteDraft = async () => {
    if (!roadmapToDelete?.roadmapVersionId) return;

    try {
      setIsDeletingDraft(true);
      await contentManagerRoadmapApi.deleteDraftVersion(
        roadmapToDelete.roadmapVersionId,
      );
      setRoadmapToDelete(null);
      setReloadToken((current) => current + 1);
      toast.success("Draft deleted.");
    } catch (deleteError) {
      toast.error(deleteError?.message || "Unable to delete draft.");
    } finally {
      setIsDeletingDraft(false);
    }
  };

  return (
    <ModulePageShell compact>
      <div className="space-y-5">
        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="grid h-11 w-11 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <MapIcon size={22} />
              </div>
              <div>
                <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                  Roadmap library
                </p>
                <h1 className="text-2xl font-extrabold text-[#18332D]">
                  Select a roadmap to manage
                </h1>
              </div>
            </div>

            <ModuleButton size="md" onClick={openCreateRoadmap}>
              <Plus size={15} /> Create roadmap
            </ModuleButton>
          </div>
        </section>

        <div className="flex flex-wrap gap-2">
          {roadmapStatuses.map((status) => {
            const isActive = activeStatus === status;
            const count = result.statusCounts[status] || 0;

            return (
              <button
                key={status}
                type="button"
                onClick={() =>
                  setQueryValues({
                    status: status === "draft" ? null : status,
                    page: null,
                  })
                }
                className={`inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 text-xs font-extrabold transition ${
                  isActive
                    ? "border-[#2FA084] bg-[#6FCF97]/24 text-[#1F6F5F] shadow-sm"
                    : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-[#F7F1E8]"
                }`}
              >
                {prettyStatus(status)}
                <span
                  className={`rounded-full px-2 py-0.5 text-[11px] ${
                    isActive
                      ? "bg-white/75 text-[#1F6F5F]"
                      : "bg-slate-100 text-slate-600"
                  }`}
                >
                  {count}
                </span>
              </button>
            );
          })}
        </div>

        <ModuleCard className="p-4">
          <div className="grid gap-3 lg:grid-cols-[minmax(320px,1fr)_220px_auto] lg:items-center">
            <label className="relative block">
              <span className="sr-only">Search roadmaps</span>
              <Search
                size={17}
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
              />
              <input
                type="search"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="Search by roadmap, role, or slug"
                className="h-10 w-full rounded-lg border border-[#B9D8CC] bg-white pl-9 pr-3 text-sm font-semibold text-[#18332D] outline-none transition placeholder:text-slate-400 focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
              />
            </label>

            <AppSelect
              value={sort}
              options={roadmapSortOptions}
              ariaLabel="Filter by updated time"
              onChange={(nextSort) =>
                setQueryValues({
                  sort: nextSort === "updated_desc" ? null : nextSort,
                  page: null,
                })
              }
            />

            <div className="inline-flex h-10 w-fit items-center rounded-lg border border-[#B9D8CC] bg-white p-1 shadow-sm">
              {viewModes.map((mode) => {
                const Icon = mode.icon;
                const isActive = viewMode === mode.value;

                return (
                  <button
                    key={mode.value}
                    type="button"
                    onClick={() =>
                      setQueryValues({
                        view: mode.value === "list" ? null : mode.value,
                        page: null,
                      })
                    }
                    className={`grid h-8 w-8 place-items-center rounded-md transition ${
                      isActive
                        ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
                        : "text-slate-600 hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                    }`}
                    aria-label={`${mode.label} view`}
                    title={`${mode.label} view`}
                  >
                    <Icon size={15} />
                  </button>
                );
              })}
            </div>
          </div>
        </ModuleCard>

        {error && (
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            {error}
          </ModuleCard>
        )}

        {isLoading ? (
          <ModuleCard className="p-8 text-center text-sm font-bold text-slate-600">
            Loading roadmaps...
          </ModuleCard>
        ) : result.items.length === 0 ? (
          <ModuleEmptyState
            title={
              hasFilters
                ? "No matching roadmaps"
                : `No ${prettyStatus(activeStatus).toLowerCase()} roadmaps`
            }
            action={
              hasFilters ? (
                <ModuleButton onClick={resetFilters}>
                  Clear filters
                </ModuleButton>
              ) : (
                <ModuleButton onClick={openCreateRoadmap}>
                  <Plus size={14} /> Create roadmap
                </ModuleButton>
              )
            }
          >
            {hasFilters
              ? "Try another search."
              : "Roadmaps will appear here after they are created."}
          </ModuleEmptyState>
        ) : viewMode === "card" ? (
          <div className="grid gap-4 xl:grid-cols-2">
            <AnimatePresence mode="popLayout">
              {result.items.map((roadmap) => (
                <RoadmapCard
                  key={`${roadmap.roadmapId}-${roadmap.roadmapVersionId}`}
                  roadmap={roadmap}
                  onEdit={openEditor}
                  onDelete={setRoadmapToDelete}
                />
              ))}
            </AnimatePresence>
          </div>
        ) : (
          <RoadmapList
            roadmaps={result.items}
            onEdit={openEditor}
            onDelete={setRoadmapToDelete}
          />
        )}

        {pageRange && (
          <ModuleCard className="flex flex-wrap items-center justify-between gap-3 p-4">
            <span className="text-xs font-bold text-slate-600">
              {result.totalPages > 1
                ? `Showing ${pageRange.start}-${pageRange.end} of ${result.totalCount} roadmaps`
                : `${result.totalCount} ${result.totalCount === 1 ? "roadmap" : "roadmaps"}`}
            </span>
            {result.totalPages > 1 && (
              <div className="flex items-center gap-2">
                <ModuleButton
                  variant="secondary"
                  size="xs"
                  disabled={result.page <= 1}
                  onClick={() =>
                    setQueryValues({
                      page: result.page > 2 ? result.page - 1 : null,
                    })
                  }
                >
                  <ChevronLeft size={13} /> Previous
                </ModuleButton>
                <span className="min-w-24 text-center text-xs font-bold text-slate-600">
                  Page {result.page} of {result.totalPages}
                </span>
                <ModuleButton
                  variant="secondary"
                  size="xs"
                  disabled={result.page >= result.totalPages}
                  onClick={() => setQueryValues({ page: result.page + 1 })}
                >
                  Next <ChevronRight size={13} />
                </ModuleButton>
              </div>
            )}
          </ModuleCard>
        )}
      </div>

      <CreateRoadmapModal
        isOpen={isCreateRoadmapOpen}
        form={createRoadmapForm}
        setForm={setCreateRoadmapForm}
        careerRoles={careerRoles}
        isLoadingRoles={isLoadingCareerRoles}
        isCreating={isCreatingRoadmap}
        error={createRoadmapError}
        onClose={closeCreateRoadmap}
        onCreate={createRoadmap}
      />

      <ConfirmActionDialog
        isOpen={Boolean(roadmapToDelete)}
        title="Delete version"
        description="This removes the selected version."
        confirmLabel="Delete"
        cancelLabel="Cancel"
        isConfirming={isDeletingDraft}
        onCancel={() => setRoadmapToDelete(null)}
        onConfirm={confirmDeleteDraft}
      />
    </ModulePageShell>
  );
}
