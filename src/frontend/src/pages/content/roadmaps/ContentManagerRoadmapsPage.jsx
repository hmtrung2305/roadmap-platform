/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  ChevronLeft,
  ChevronRight,
  Edit3,
  Grid2X2,
  List,
  Map as MapIcon,
  Search,
} from "lucide-react";

import { contentManagerRoadmapApi } from "../../../api/contentRoadmapApi";
import AppSelect from "../../../components/common/AppSelect";
import {
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../../components/learningModules/learningModuleUi";
import {
  initialRoadmapListResult,
  roadmapPageSize,
  roadmapSortOptions,
  roadmapStatuses,
} from "../../../features/roadmapEditor/roadmapEditorConstants";
import {
  formatDate,
  getStatusTone,
  isCanceledRequest,
  parsePage,
  prettyStatus,
} from "../../../features/roadmapEditor/roadmapEditorUtils";

const viewModes = [
  { value: "list", label: "List", icon: List },
  { value: "card", label: "Cards", icon: Grid2X2 },
];

function RoadmapCard({ roadmap, onEdit }) {
  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: 8 }}
      transition={{ duration: 0.18 }}
    >
      <ModuleCard className="overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md">
      <div className="space-y-4 p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0 flex-1">
            <div className="mb-2 flex flex-wrap items-center gap-2">
              <ModuleBadge tone={getStatusTone(roadmap.status)}>{prettyStatus(roadmap.status)}</ModuleBadge>
              <span className="rounded-full bg-[#F7F1E8] px-2.5 py-1 text-[11px] font-bold text-slate-600">
                v{roadmap.versionNumber}
              </span>
            </div>

            <h2 className="truncate text-lg font-extrabold text-[#18332D]">{roadmap.title}</h2>
            <p className="mt-1 text-sm font-semibold text-slate-600">
              Career Role: {roadmap.careerRole?.name || roadmap.slug}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[#B9D8CC]/70 pt-4">
          <span className="text-xs font-bold text-slate-500">
            Updated {formatDate(roadmap.updatedAt || roadmap.createdAt)}
          </span>
          <ModuleButton onClick={() => onEdit(roadmap)}>
            <Edit3 size={14} /> Open editor
          </ModuleButton>
        </div>
      </div>
      </ModuleCard>
    </motion.div>
  );
}

function RoadmapList({ roadmaps, onEdit }) {
  return (
    <ModuleCard className="overflow-hidden">
      <div className="hidden grid-cols-[minmax(280px,1.6fr)_220px_120px_150px_150px] gap-3 border-b border-[#B9D8CC] bg-[#F7F1E8]/70 px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-700 xl:grid">
        <span>Roadmap</span>
        <span>Career role</span>
        <span>Status</span>
        <span>Updated</span>
        <span className="text-right">Action</span>
      </div>

      <AnimatePresence mode="popLayout">
        {roadmaps.map((roadmap) => (
          <motion.div
            layout
            key={`${roadmap.roadmapId}-${roadmap.roadmapVersionId}`}
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -6 }}
            transition={{ duration: 0.16 }}
            className="grid gap-3 border-b border-[#B9D8CC]/60 px-4 py-4 last:border-b-0 xl:grid-cols-[minmax(280px,1.6fr)_220px_120px_150px_150px] xl:items-center"
          >
          <div className="min-w-0">
            <div className="truncate text-base font-extrabold text-[#18332D]">{roadmap.title}</div>
            <div className="mt-1 text-xs font-bold text-slate-500">v{roadmap.versionNumber}</div>
          </div>

          <div className="text-sm font-bold text-slate-700">
            {roadmap.careerRole?.name || roadmap.slug}
          </div>

          <div>
            <ModuleBadge tone={getStatusTone(roadmap.status)}>{prettyStatus(roadmap.status)}</ModuleBadge>
          </div>

          <div className="text-sm font-bold text-slate-600">
            {formatDate(roadmap.updatedAt || roadmap.createdAt)}
          </div>

          <div className="flex justify-start xl:justify-end">
            <ModuleButton size="xs" onClick={() => onEdit(roadmap)}>
              <Edit3 size={14} /> Open editor
            </ModuleButton>
          </div>
          </motion.div>
        ))}
      </AnimatePresence>
    </ModuleCard>
  );
}

export default function ContentManagerRoadmapsPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const requestedStatus = searchParams.get("status");
  const activeStatus = roadmapStatuses.includes(requestedStatus) ? requestedStatus : "draft";
  const searchQuery = searchParams.get("q") || "";
  const requestedSort = searchParams.get("sort") || "updated_desc";
  const sort = roadmapSortOptions.some((option) => option.value === requestedSort)
    ? requestedSort
    : "updated_desc";
  const requestedView = searchParams.get("view") || "list";
  const viewMode = viewModes.some((option) => option.value === requestedView) ? requestedView : "list";
  const requestedPage = parsePage(searchParams.get("page"));
  const searchParamsString = searchParams.toString();

  const [searchInput, setSearchInput] = useState(searchQuery);
  const [result, setResult] = useState(initialRoadmapListResult);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

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
  }, [activeStatus, requestedPage, searchQuery, searchParamsString, setSearchParams, sort]);

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

  const openEditor = (roadmap) => {
    const versionQuery = roadmap.roadmapVersionId ? `?versionId=${roadmap.roadmapVersionId}` : "";
    navigate(`/content/roadmaps/${roadmap.roadmapId}/edit${versionQuery}`);
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

            <div className="inline-flex rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/70 p-1">
              {viewModes.map((mode) => {
                const Icon = mode.icon;
                const isActive = viewMode === mode.value;

                return (
                  <button
                    key={mode.value}
                    type="button"
                    onClick={() => setQueryValues({ view: mode.value === "list" ? null : mode.value, page: null })}
                    className={`inline-flex h-8 items-center gap-1.5 rounded-md px-2.5 text-xs font-extrabold transition ${
                      isActive
                        ? "bg-white text-[#1F6F5F] shadow-sm"
                        : "text-slate-600 hover:bg-white/70 hover:text-[#1F6F5F]"
                    }`}
                  >
                    <Icon size={14} /> {mode.label}
                  </button>
                );
              })}
            </div>
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
                onClick={() => setQueryValues({ status: status === "draft" ? null : status, page: null })}
                className={`inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 text-xs font-extrabold transition ${
                  isActive
                    ? "border-[#2FA084] bg-[#6FCF97]/24 text-[#1F6F5F] shadow-sm"
                    : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-[#F7F1E8]"
                }`}
              >
                {prettyStatus(status)}
                <span className={`rounded-full px-2 py-0.5 text-[11px] ${
                  isActive ? "bg-white/75 text-[#1F6F5F]" : "bg-slate-100 text-slate-600"
                }`}
                >
                  {count}
                </span>
              </button>
            );
          })}
        </div>

        <ModuleCard className="p-4">
          <div className="grid gap-3 lg:grid-cols-[minmax(280px,1fr)_220px]">
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
              onChange={(nextSort) => setQueryValues({
                sort: nextSort === "updated_desc" ? null : nextSort,
                page: null,
              })}
            />
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
            title={hasFilters ? "No matching roadmaps" : `No ${prettyStatus(activeStatus).toLowerCase()} roadmaps`}
            action={hasFilters ? <ModuleButton onClick={resetFilters}>Clear filters</ModuleButton> : null}
          >
            {hasFilters ? "Try another search." : "Roadmaps will appear here after they are created."}
          </ModuleEmptyState>
        ) : viewMode === "card" ? (
          <div className="grid gap-4 xl:grid-cols-2">
            <AnimatePresence mode="popLayout">
              {result.items.map((roadmap) => (
                <RoadmapCard
                  key={`${roadmap.roadmapId}-${roadmap.roadmapVersionId}`}
                  roadmap={roadmap}
                  onEdit={openEditor}
                />
              ))}
            </AnimatePresence>
          </div>
        ) : (
          <RoadmapList roadmaps={result.items} onEdit={openEditor} />
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
                  onClick={() => setQueryValues({ page: result.page > 2 ? result.page - 1 : null })}
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
    </ModulePageShell>
  );
}
