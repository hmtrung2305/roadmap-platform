import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  Archive,
  BookOpenText,
  ChevronLeft,
  ChevronRight,
  Edit3,
  Eye,
  FileQuestion,
  MoreHorizontal,
  Plus,
  Search,
  Tag,
  Trash2,
} from "lucide-react";
import { toast } from "react-toastify";
import {
  contentManagerLearningModuleApi,
  getLearningModuleRouteSegment,
} from "../../../api/learningModuleApi";
import AppSelect from "../../../components/common/AppSelect";
import ConfirmActionDialog from "../../../components/learningModules/ConfirmActionDialog";
import {
  getStatusTone,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  prettyModuleStatus,
} from "../../../components/learningModules/learningModuleUi";

const statuses = ["draft", "published", "archived"];
const pageSize = 15;

const initialResult = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize,
  totalPages: 0,
  statusCounts: {
    draft: 0,
    published: 0,
    archived: 0,
  },
};

const difficultyOptions = [
  { value: "all", label: "All difficulties" },
  { value: "beginner", label: "Beginner" },
  { value: "intermediate", label: "Intermediate" },
  { value: "advanced", label: "Advanced" },
];

const sortOptions = [
  { value: "updated_desc", label: "Recently updated" },
  { value: "created_desc", label: "Recently created" },
  { value: "title_asc", label: "Title A–Z" },
  { value: "title_desc", label: "Title Z–A" },
];

function parsePage(value) {
  const parsed = Number.parseInt(value || "1", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1;
}

function isCanceledRequest(error) {
  return error?.code === "ERR_CANCELED" || error?.name === "CanceledError";
}

export default function ContentManagerLearningModulesPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const requestedStatus = searchParams.get("status");
  const activeStatus = statuses.includes(requestedStatus) ? requestedStatus : "draft";
  const searchQuery = searchParams.get("q") || "";
  const difficulty = searchParams.get("difficulty") || "all";
  const requestedSort = searchParams.get("sort") || "updated_desc";
  const sort = sortOptions.some((option) => option.value === requestedSort)
    ? requestedSort
    : "updated_desc";
  const requestedPage = parsePage(searchParams.get("page"));
  const searchParamsString = searchParams.toString();

  const [searchInput, setSearchInput] = useState(searchQuery);
  const [result, setResult] = useState(initialResult);
  const [openMenuId, setOpenMenuId] = useState(null);
  const [pendingAction, setPendingAction] = useState(null);
  const [isConfirmingAction, setIsConfirmingAction] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [refreshKey, setRefreshKey] = useState(0);

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

    async function loadModules() {
      try {
        setIsLoading(true);
        setError(null);

        const nextResult = await contentManagerLearningModuleApi.getModules({
          status: activeStatus,
          search: searchQuery,
          difficulty,
          sort,
          page: requestedPage,
          pageSize,
          signal: controller.signal,
        });

        setResult(nextResult);
        setOpenMenuId(null);

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
          setError(requestError?.message || "Unable to load modules.");
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    loadModules();

    return () => controller.abort();
  }, [
    activeStatus,
    difficulty,
    refreshKey,
    requestedPage,
    searchQuery,
    sort,
    searchParamsString,
    setSearchParams,
  ]);

  const visibleModules = result.items;
  const hasFilters = Boolean(searchQuery || difficulty !== "all");
  const pageRange = useMemo(() => {
    if (result.totalCount === 0) return null;

    return {
      start: (result.page - 1) * result.pageSize + 1,
      end: Math.min(result.page * result.pageSize, result.totalCount),
    };
  }, [result.page, result.pageSize, result.totalCount]);

  const reload = () => setRefreshKey((key) => key + 1);

  const resetFilters = () => {
    setSearchInput("");
    setQueryValues({ q: null, difficulty: null, page: null });
  };

  const requestDelete = (module) => {
    setPendingAction({ type: "delete", module });
  };

  const requestArchive = (module) => {
    setPendingAction({ type: "archive", module });
  };

  const confirmActionCopy = {
    delete: {
      tone: "danger",
      title: "Delete this draft?",
      description: "This draft module, its lessons, quiz, and indexed content will be removed.",
      confirmLabel: "Delete draft",
      cancelLabel: "Keep draft",
    },
    archive: {
      tone: "warning",
      title: "Archive this module?",
      description: "New learners will no longer be able to start this module, but enrolled learners can still access it.",
      confirmLabel: "Archive module",
      cancelLabel: "Keep published",
    },
  };

  const pendingActionCopy = pendingAction ? confirmActionCopy[pendingAction.type] : null;

  const confirmPendingAction = async () => {
    if (!pendingAction) return;

    try {
      setIsConfirmingAction(true);

      if (pendingAction.type === "delete") {
        await contentManagerLearningModuleApi.deleteDraftModule(
          pendingAction.module.skillModuleId,
        );
        toast.success("Draft module deleted.");
      }

      if (pendingAction.type === "archive") {
        await contentManagerLearningModuleApi.archiveModule(
          pendingAction.module.skillModuleId,
        );
        toast.success("Module archived.");
      }

      setPendingAction(null);
      reload();
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to update module.");
    } finally {
      setIsConfirmingAction(false);
    }
  };

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center gap-3">
            <div className="grid h-11 w-11 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
              <BookOpenText size={22} />
            </div>
            <div>
              <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                Workspace
              </p>
              <h1 className="text-2xl font-extrabold text-[#18332D]">
                Learning module management
              </h1>
            </div>
          </div>
        </section>

        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap gap-2">
            {statuses.map((status) => {
              const isActive = activeStatus === status;
              const count = result.statusCounts[status] || 0;

              return (
                <button
                  key={status}
                  type="button"
                  onClick={() => setQueryValues({ status, page: null })}
                  className={`inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 text-xs font-extrabold transition ${
                    isActive
                      ? "border-[#2FA084] bg-[#6FCF97]/24 text-[#1F6F5F] shadow-sm"
                      : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-[#F7F1E8]"
                  }`}
                >
                  {prettyModuleStatus[status]}
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

          <ModuleButton onClick={() => navigate("/content/learning-modules/create")}>
            <Plus size={15} /> Add module
          </ModuleButton>
        </div>

        <ModuleCard className="p-4">
          <div className="grid gap-3 lg:grid-cols-[minmax(280px,1fr)_180px_190px]">
            <label className="relative block">
              <span className="sr-only">Search modules</span>
              <Search
                size={17}
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
              />
              <input
                type="search"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder="Search modules or skills"
                className="h-10 w-full rounded-lg border border-[#B9D8CC] bg-white pl-9 pr-3 text-sm font-semibold text-[#18332D] outline-none transition placeholder:text-slate-400 focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
              />
            </label>

            <AppSelect
              value={difficulty}
              options={difficultyOptions}
              ariaLabel="Filter by difficulty"
              onChange={(nextDifficulty) => setQueryValues({
                difficulty: nextDifficulty === "all" ? null : nextDifficulty,
                page: null,
              })}
            />

            <AppSelect
              value={sort}
              options={sortOptions}
              ariaLabel="Sort modules"
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
          <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">
            Loading modules...
          </ModuleCard>
        ) : visibleModules.length === 0 ? (
          <ModuleEmptyState
            title={hasFilters
              ? "No matching modules"
              : `No ${prettyModuleStatus[activeStatus].toLowerCase()} modules`}
            action={hasFilters ? (
              <ModuleButton onClick={resetFilters}>Clear filters</ModuleButton>
            ) : (
              <ModuleButton onClick={() => navigate("/content/learning-modules/create")}>
                Add module
              </ModuleButton>
            )}
          >
            {hasFilters
              ? "Try another search or filter."
              : "Modules will appear here after they are created."}
          </ModuleEmptyState>
        ) : (
          <ModuleCard className="overflow-visible">
            <div className="hidden grid-cols-[minmax(340px,1.7fr)_190px_170px_120px_150px] gap-3 border-b border-[#B9D8CC] bg-[#F7F1E8]/70 px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-700 xl:grid">
              <span>Module</span>
              <span>Skill</span>
              <span>Content</span>
              <span>Status</span>
              <span className="text-right">Actions</span>
            </div>

            {visibleModules.map((module) => (
              <div
                key={module.skillModuleId}
                className="grid gap-3 border-b border-[#B9D8CC]/60 px-4 py-4 last:border-b-0 xl:grid-cols-[minmax(340px,1.7fr)_190px_170px_120px_150px] xl:items-center"
              >
                <div className="min-w-0">
                  <div className="truncate text-base font-bold text-[#18332D]">
                    {module.title}
                  </div>
                  {module.difficultyLevel && (
                    <div className="mt-1 text-xs font-semibold capitalize text-slate-500">
                      {module.difficultyLevel}
                    </div>
                  )}
                </div>

                <div>
                  <span className="inline-flex max-w-full items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-[#F7F1E8]/60 px-2.5 py-1 text-xs font-extrabold text-[#1F6F5F]">
                    <Tag size={13} />
                    <span className="truncate">{module.skillName || "No skill"}</span>
                  </span>
                </div>

                <div className="flex flex-wrap gap-2 text-sm font-bold text-slate-700">
                  <span className="inline-flex items-center gap-1.5 rounded-md bg-slate-100 px-2 py-1">
                    <BookOpenText size={14} /> {module.lessonCount}
                  </span>
                  <span className="inline-flex items-center gap-1.5 rounded-md bg-slate-100 px-2 py-1">
                    <FileQuestion size={14} /> {module.questionCount}
                  </span>
                </div>

                <div>
                  <ModuleBadge tone={getStatusTone(module.status)}>
                    {prettyModuleStatus[module.status] || module.status}
                  </ModuleBadge>
                </div>

                <div className="relative flex justify-start gap-2 xl:justify-end">
                  {module.status === "draft" ? (
                    <ModuleActionButton
                      onClick={() => navigate(
                        `/content/learning-modules/${getLearningModuleRouteSegment(module)}/edit`,
                      )}
                    >
                      <Edit3 size={14} strokeWidth={2.25} /> Edit
                    </ModuleActionButton>
                  ) : (
                    <ModuleActionButton
                      onClick={() => navigate(
                        `/content/learning-modules/${getLearningModuleRouteSegment(module)}/preview`,
                      )}
                    >
                      <Eye size={14} strokeWidth={2.25} /> Preview
                    </ModuleActionButton>
                  )}

                  {module.status !== "archived" && (
                    <button
                      type="button"
                      onClick={() => setOpenMenuId((current) => (
                        current === module.skillModuleId ? null : module.skillModuleId
                      ))}
                      className="inline-grid h-8 w-8 shrink-0 place-items-center rounded-md border border-[#B9D8CC] bg-white text-slate-600 transition hover:border-[#2FA084] hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                      aria-label="More actions"
                    >
                      <MoreHorizontal size={16} strokeWidth={2.5} />
                    </button>
                  )}

                  {module.status !== "archived"
                    && openMenuId === module.skillModuleId && (
                    <div className="absolute right-0 top-9 z-20 w-40 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 shadow-lg">
                      {module.status === "draft" && (
                        <>
                          <OverflowAction onClick={() => {
                            setOpenMenuId(null);
                            navigate(
                              `/content/learning-modules/${getLearningModuleRouteSegment(module)}/preview`,
                            );
                          }}>
                            <Eye size={14} /> Preview
                          </OverflowAction>
                          <OverflowAction tone="danger" onClick={() => {
                            setOpenMenuId(null);
                            requestDelete(module);
                          }}>
                            <Trash2 size={14} /> Delete
                          </OverflowAction>
                        </>
                      )}

                      {module.status === "published" && (
                        <OverflowAction tone="danger" onClick={() => {
                          setOpenMenuId(null);
                          requestArchive(module);
                        }}>
                          <Archive size={14} /> Archive
                        </OverflowAction>
                      )}
                    </div>
                  )}
                </div>
              </div>
            ))}

            {pageRange && (
              <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[#B9D8CC] px-4 py-3">
                <span className="text-xs font-bold text-slate-600">
                  {result.totalPages > 1
                    ? `Showing ${pageRange.start}–${pageRange.end} of ${result.totalCount} modules`
                    : `${result.totalCount} ${result.totalCount === 1 ? "module" : "modules"}`}
                </span>

                {result.totalPages > 1 && (
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setQueryValues({
                        page: result.page > 2 ? result.page - 1 : null,
                      })}
                      disabled={result.page <= 1}
                      className="inline-flex h-8 items-center gap-1 rounded-md border border-[#B9D8CC] bg-white px-2.5 text-xs font-extrabold text-slate-700 transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-45"
                    >
                      <ChevronLeft size={14} /> Previous
                    </button>

                    <span className="min-w-24 text-center text-xs font-bold text-slate-600">
                      Page {result.page} of {result.totalPages}
                    </span>

                    <button
                      type="button"
                      onClick={() => setQueryValues({ page: result.page + 1 })}
                      disabled={result.page >= result.totalPages}
                      className="inline-flex h-8 items-center gap-1 rounded-md border border-[#B9D8CC] bg-white px-2.5 text-xs font-extrabold text-slate-700 transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-45"
                    >
                      Next <ChevronRight size={14} />
                    </button>
                  </div>
                )}
              </div>
            )}
          </ModuleCard>
        )}

        <ConfirmActionDialog
          isOpen={Boolean(pendingAction)}
          tone={pendingActionCopy?.tone}
          title={pendingActionCopy?.title}
          description={pendingActionCopy?.description}
          confirmLabel={pendingActionCopy?.confirmLabel}
          cancelLabel={pendingActionCopy?.cancelLabel}
          isConfirming={isConfirmingAction}
          onCancel={() => setPendingAction(null)}
          onConfirm={confirmPendingAction}
        />
      </div>
    </ModulePageShell>
  );
}

function ModuleActionButton({ children, onClick }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex h-8 shrink-0 items-center justify-center gap-1.5 rounded-md border border-[#B9D8CC] bg-white px-2.5 text-xs font-extrabold leading-none text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#F7F1E8]"
    >
      {children}
    </button>
  );
}

function OverflowAction({ children, tone = "default", onClick }) {
  const styles = tone === "danger"
    ? "text-rose-600 hover:bg-rose-50"
    : "text-slate-700 hover:bg-[#F7F1E8] hover:text-[#1F6F5F]";

  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex w-full items-center gap-2 px-3 py-2 text-left text-xs font-extrabold transition ${styles}`}
    >
      {children}
    </button>
  );
}
