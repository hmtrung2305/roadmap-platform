import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  AlertTriangle,
  ArrowRight,
  BookOpenText,
  CheckCircle2,
  Clock3,
  FileQuestion,
  Layers3,
  Plus,
  Rocket,
} from "lucide-react";

import {
  contentManagerLearningModuleApi,
  getLearningModuleRouteSegment,
} from "../../api/learningModuleApi";
import {
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  getStatusTone,
  prettyModuleStatus,
} from "../../features/learningModules/components/learningModuleUi";

const emptyOverview = {
  metrics: {
    drafts: 0,
    readyToPublish: 0,
    needsAttention: 0,
    published: 0,
  },
  readyToPublish: [],
  needsAttention: [],
  recentDrafts: [],
  recentlyPublished: [],
};

function isCanceledRequest(error) {
  return error?.code === "ERR_CANCELED" || error?.name === "CanceledError";
}

function formatDate(value) {
  if (!value) return "—";

  try {
    return new Intl.DateTimeFormat(undefined, {
      month: "short",
      day: "numeric",
    }).format(new Date(value));
  } catch {
    return "—";
  }
}

function getModuleEditUrl(module, tab = null) {
  const segment = getLearningModuleRouteSegment(module);
  return `/content/learning-modules/${segment}/edit${tab ? `?tab=${tab}` : ""}`;
}

function getModulePreviewUrl(module) {
  const segment = getLearningModuleRouteSegment(module);
  return `/content/learning-modules/${segment}/preview`;
}

function getAttentionTab(item) {
  const key = String(item?.checkKey || item?.label || "").toLowerCase();

  if (key.includes("lesson") || key.includes("index")) return "lessons";
  if (key.includes("quiz") || key.includes("question")) return "quiz";
  if (key.includes("preview")) return "preview";
  if (key.includes("publish")) return "publish";
  return "overview";
}

function StatCard({ icon: Icon, label, value }) {
  return (
    <ModuleCard className="p-4">
      <div className="flex items-center gap-3">
        <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/18 text-[#1F6F5F]">
          <Icon size={19} />
        </div>
        <div className="min-w-0">
          <div className="text-2xl font-extrabold leading-none text-[#18332D]">
            {value}
          </div>
          <div className="mt-1 text-xs font-bold text-slate-600">
            {label}
          </div>
        </div>
      </div>
    </ModuleCard>
  );
}

function SectionAction({ label, onClick }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-extrabold text-[#1F6F5F] hover:bg-[#EAF7F1]"
    >
      {label} <ArrowRight size={13} />
    </button>
  );
}

function ModuleListCard({ icon: Icon, title, count, action, children }) {
  return (
    <ModuleCard className="overflow-hidden">
      <div className="flex items-center justify-between gap-3 border-b border-[#B9D8CC]/70 px-4 py-3">
        <div className="flex min-w-0 items-center gap-2">
          <div className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
            <Icon size={16} />
          </div>
          <div className="min-w-0">
            <h2 className="truncate text-sm font-extrabold text-[#18332D]">
              {title}
            </h2>
            <p className="text-xs font-bold text-slate-500">
              {count} {count === 1 ? "module" : "modules"}
            </p>
          </div>
        </div>
        {action}
      </div>
      <div className="divide-y divide-[#B9D8CC]/60">
        {children}
      </div>
    </ModuleCard>
  );
}

function EmptyListRow({ children }) {
  return (
    <div className="px-4 py-6 text-sm font-semibold text-slate-600">
      {children}
    </div>
  );
}

function ModuleMiniRow({ module, actionLabel, onAction, children }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
      <div className="min-w-0 flex-1">
        <div className="truncate text-sm font-extrabold text-[#18332D]">
          {module.title}
        </div>
        {children || (
          <div className="mt-1 flex flex-wrap items-center gap-2 text-xs font-bold text-slate-600">
            <span className="truncate">{module.skillName || "No skill"}</span>
            <span>•</span>
            <span>{module.lessonCount} lessons</span>
            <span>•</span>
            <span>{module.questionCount} questions</span>
          </div>
        )}
      </div>

      <ModuleButton variant="secondary" size="xs" onClick={onAction}>
        {actionLabel}
      </ModuleButton>
    </div>
  );
}

export default function ContentManagerOverviewPage() {
  const navigate = useNavigate();

  const [overview, setOverview] = useState(emptyOverview);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function loadOverview() {
      try {
        setIsLoading(true);
        setError("");

        const data = await contentManagerLearningModuleApi.getWorkspaceOverview({
          signal: controller.signal,
        });

        setOverview({
          metrics: data?.metrics ?? emptyOverview.metrics,
          readyToPublish: Array.isArray(data?.readyToPublish) ? data.readyToPublish : [],
          needsAttention: Array.isArray(data?.needsAttention) ? data.needsAttention : [],
          recentDrafts: Array.isArray(data?.recentDrafts) ? data.recentDrafts : [],
          recentlyPublished: Array.isArray(data?.recentlyPublished) ? data.recentlyPublished : [],
        });
      } catch (requestError) {
        if (!isCanceledRequest(requestError)) {
          setError(requestError?.message || "Unable to load overview.");
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    loadOverview();

    return () => controller.abort();
  }, []);

  const hasAnyContent = useMemo(
    () => Object.values(overview.metrics).some((value) => Number(value) > 0),
    [overview.metrics],
  );

  if (isLoading) {
    return (
      <ModulePageShell>
        <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">
          Loading overview...
        </ModuleCard>
      </ModulePageShell>
    );
  }

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="grid h-11 w-11 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <Layers3 size={22} />
              </div>
              <div>
                <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                  Content Manager
                </p>
                <h1 className="text-2xl font-extrabold text-[#18332D]">
                  Overview
                </h1>
              </div>
            </div>

            <ModuleButton onClick={() => navigate("/content/learning-modules/create")}>
              <Plus size={15} /> Add module
            </ModuleButton>
          </div>
        </section>

        {error && (
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            {error}
          </ModuleCard>
        )}

        {!hasAnyContent ? (
          <ModuleEmptyState
            title="No modules yet"
            action={(
              <ModuleButton onClick={() => navigate("/content/learning-modules/create")}>
                Add module
              </ModuleButton>
            )}
          >
            Start with a draft module.
          </ModuleEmptyState>
        ) : (
          <>
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <StatCard icon={BookOpenText} label="Drafts" value={overview.metrics.drafts} />
              <StatCard icon={CheckCircle2} label="Ready" value={overview.metrics.readyToPublish} />
              <StatCard icon={AlertTriangle} label="Needs attention" value={overview.metrics.needsAttention} />
              <StatCard icon={Rocket} label="Published" value={overview.metrics.published} />
            </div>

            <div className="grid gap-5 xl:grid-cols-2">
              <ModuleListCard
                icon={CheckCircle2}
                title="Ready to publish"
                count={overview.readyToPublish.length}
                action={(
                  <SectionAction
                    label="View drafts"
                    onClick={() => navigate("/content/learning-modules?status=draft")}
                  />
                )}
              >
                {overview.readyToPublish.length === 0 ? (
                  <EmptyListRow>No modules ready.</EmptyListRow>
                ) : overview.readyToPublish.map((module) => (
                  <ModuleMiniRow
                    key={module.skillModuleId}
                    module={module}
                    actionLabel="Review"
                    onAction={() => navigate(getModuleEditUrl(module, "publish"))}
                  />
                ))}
              </ModuleListCard>

              <ModuleListCard
                icon={AlertTriangle}
                title="Needs attention"
                count={overview.needsAttention.length}
                action={(
                  <SectionAction
                    label="View drafts"
                    onClick={() => navigate("/content/learning-modules?status=draft")}
                  />
                )}
              >
                {overview.needsAttention.length === 0 ? (
                  <EmptyListRow>No items need attention.</EmptyListRow>
                ) : overview.needsAttention.map((item) => {
                  const tab = getAttentionTab(item);

                  return (
                    <div
                      key={`${item.module.skillModuleId}-${item.checkKey || item.label}`}
                      className="flex flex-wrap items-center justify-between gap-3 px-4 py-3"
                    >
                      <div className="min-w-0 flex-1">
                        <div className="truncate text-sm font-extrabold text-[#18332D]">
                          {item.module.title}
                        </div>
                        <div className="mt-1 flex flex-wrap items-center gap-2 text-xs font-bold text-slate-600">
                          <span className="rounded-full bg-amber-100 px-2 py-0.5 text-amber-800">
                            {item.label || "Action needed"}
                          </span>
                          <span className="truncate">{item.message}</span>
                        </div>
                      </div>
                      <ModuleButton
                        variant="secondary"
                        size="xs"
                        onClick={() => navigate(getModuleEditUrl(item.module, tab))}
                      >
                        Fix
                      </ModuleButton>
                    </div>
                  );
                })}
              </ModuleListCard>
            </div>

            <div className="grid gap-5 xl:grid-cols-2">
              <ModuleListCard
                icon={Clock3}
                title="Recent drafts"
                count={overview.recentDrafts.length}
              >
                {overview.recentDrafts.length === 0 ? (
                  <EmptyListRow>No draft modules.</EmptyListRow>
                ) : overview.recentDrafts.map((module) => (
                  <ModuleMiniRow
                    key={module.skillModuleId}
                    module={module}
                    actionLabel="Continue"
                    onAction={() => navigate(getModuleEditUrl(module))}
                  />
                ))}
              </ModuleListCard>

              <ModuleListCard
                icon={FileQuestion}
                title="Recently published"
                count={overview.recentlyPublished.length}
              >
                {overview.recentlyPublished.length === 0 ? (
                  <EmptyListRow>No published modules.</EmptyListRow>
                ) : overview.recentlyPublished.map((module) => (
                  <ModuleMiniRow
                    key={module.skillModuleId}
                    module={module}
                    actionLabel="Preview"
                    onAction={() => navigate(getModulePreviewUrl(module))}
                  >
                    <div className="mt-1 flex flex-wrap items-center gap-2 text-xs font-bold text-slate-600">
                      <ModuleBadge tone={getStatusTone(module.status)}>
                        {prettyModuleStatus[module.status] || module.status}
                      </ModuleBadge>
                      <span>Published {formatDate(module.publishedAt)}</span>
                    </div>
                  </ModuleMiniRow>
                ))}
              </ModuleListCard>
            </div>
          </>
        )}
      </div>
    </ModulePageShell>
  );
}
