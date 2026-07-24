import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  CheckCircle2,
  Clock3,
  Eye,
  FileText,
  GitPullRequest,
  History,
  Layers3,
  Loader2,
  MessageSquare,
  RefreshCw,
  X,
  XCircle,
} from "lucide-react";
import { toast } from "react-toastify";

import { contentManagerRoadmapApi } from "../../../api/contentRoadmapApi";
import ConfirmActionDialog from "../../../components/common/ConfirmActionDialog";
import {
  inputClass,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../../features/learningModules/components/learningModuleUi";
import RoadmapGraphCanvas from "../../../features/roadmapEditor/components/RoadmapGraphCanvas";
import {
  compareNodes,
  formatDate,
  formatVersionLabel,
  getNodeTone,
  getStatusTone,
  normalizeNodes,
  prettyReleaseType,
  prettyStatus,
} from "../../../features/roadmapEditor/roadmapEditorUtils";
import {
  clearReviewDraftCache,
  readReviewDraftCache,
  REVIEW_DRAFT_CACHE_TYPES,
  writeReviewDraftCache,
} from "../../../features/roadmapEditor/reviewDraftCache";
import {
  canEditLearningFields,
  canEditMappings,
  getResourceTitle,
  getSkillName,
  normalizeNodeType,
} from "../../../features/roadmapEditor/nodeRules";
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";

const queuePageSize = 20;

export default function ContentReviewerRoadmapReviewsPage() {
  const params = useParams();

  if (params.roadmapId && params.roadmapVersionId) {
    return <RoadmapReviewDetailPage />;
  }

  return <RoadmapReviewQueuePage />;
}

function RoadmapReviewQueuePage() {
  const navigate = useNavigate();
  const [items, setItems] = useState([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  const loadReviews = useCallback(
    async ({ force = false } = {}) => {
      setIsLoading(true);
      setError("");

      try {
        const result = await contentManagerRoadmapApi.getRoadmaps({
          status: "pending_review",
          sort: "updated_desc",
          page,
          pageSize: queuePageSize,
        });

        setItems(result.items || []);
        setTotalPages(Number(result.totalPages || 0));

        if (force) {
          toast.success("Review queue refreshed.");
        }
      } catch (requestError) {
        setError(
          getFriendlyApiErrorMessage(
            requestError,
            "Unable to load review queue.",
          ),
        );
      } finally {
        setIsLoading(false);
      }
    },
    [page],
  );

  useEffect(() => {
    let isActive = true;

    window.queueMicrotask(() => {
      if (isActive) {
        loadReviews();
      }
    });

    return () => {
      isActive = false;
    };
  }, [loadReviews]);

  const openReview = (roadmap) => {
    if (!roadmap?.roadmapId || !roadmap?.roadmapVersionId) return;
    navigate(
      `/content/reviews/${roadmap.roadmapId}/${roadmap.roadmapVersionId}`,
    );
  };

  return (
    <ModulePageShell compact>
      <div className="space-y-5">
        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <GitPullRequest size={22} />
              </div>
              <div className="min-w-0">
                <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                  Roadmap reviews
                </p>
                <h1 className="text-2xl font-extrabold text-[#18332D]">
                  Pending Review Queue
                </h1>
              </div>
            </div>

            <ModuleButton
              variant="secondary"
              onClick={() => loadReviews({ force: true })}
              disabled={isLoading}
            >
              {isLoading ? (
                <Loader2 size={14} className="animate-spin" />
              ) : (
                <RefreshCw size={14} />
              )}
              Refresh
            </ModuleButton>
          </div>
        </section>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
            {error}
          </div>
        )}

        {isLoading && items.length === 0 ? (
          <ModuleCard className="flex items-center justify-center gap-2 p-8 text-sm font-bold text-slate-600">
            <Loader2 size={16} className="animate-spin text-[#1F6F5F]" />
            Loading reviews...
          </ModuleCard>
        ) : items.length === 0 ? (
          <ModuleEmptyState title="No roadmap versions pending review" />
        ) : (
          <ModuleCard className="overflow-hidden p-0">
            <div className="hidden overflow-x-auto lg:block">
              <table className="min-w-full divide-y divide-[#B9D8CC]/70 text-left">
                <thead className="bg-[#F4FBF8] text-[11px] font-extrabold uppercase tracking-wide text-slate-600">
                  <tr>
                    <th className="px-4 py-3">Roadmap</th>
                    <th className="px-4 py-3">Career role</th>
                    <th className="px-4 py-3">Version</th>
                    <th className="px-4 py-3">Release</th>
                    <th className="px-4 py-3">Submitted</th>
                    <th className="px-4 py-3">Changelog</th>
                    <th className="px-4 py-3 text-right">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#B9D8CC]/60 bg-white text-sm">
                  {items.map((roadmap) => {
                    const submittedEvent = getSubmittedEvent(roadmap);

                    return (
                      <tr
                        key={roadmap.roadmapVersionId}
                        className="align-top hover:bg-[#F7F1E8]/50"
                      >
                        <td className="max-w-xs px-4 py-4">
                          <div className="font-extrabold text-[#18332D]">
                            {roadmap.title}
                          </div>
                        </td>
                        <td className="px-4 py-4 font-semibold text-slate-700">
                          {roadmap.careerRole?.name || "-"}
                        </td>
                        <td className="px-4 py-4">
                          <ModuleBadge tone="slate">
                            {formatVersionLabel(roadmap)}
                          </ModuleBadge>
                        </td>
                        <td className="px-4 py-4">
                          <ModuleBadge tone="blue">
                            {prettyReleaseType(roadmap.releaseType)}
                          </ModuleBadge>
                        </td>
                        <td className="px-4 py-4 font-semibold text-slate-600">
                          <div>
                            {formatDate(
                              submittedEvent?.createdAt || roadmap.updatedAt,
                            )}
                          </div>
                          {submittedEvent ? (
                            <div className="mt-1 text-[11px] font-semibold leading-4 text-slate-500">
                              Author: {formatReviewActor(submittedEvent)}
                            </div>
                          ) : null}
                        </td>
                        <td className="max-w-sm px-4 py-4 text-xs font-semibold leading-5 text-slate-600">
                          <span className="line-clamp-2">
                            {submittedEvent?.message ||
                              "No changelog submitted."}
                          </span>
                        </td>
                        <td className="px-4 py-4 text-right">
                          <ModuleButton onClick={() => openReview(roadmap)}>
                            <Eye size={14} />
                            Review
                          </ModuleButton>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            <div className="grid gap-3 p-3 lg:hidden">
              {items.map((roadmap) => {
                const submittedEvent = getSubmittedEvent(roadmap);

                return (
                  <article
                    key={roadmap.roadmapVersionId}
                    className="rounded-lg border border-[#B9D8CC]/70 bg-white p-4"
                  >
                    <div className="flex flex-wrap gap-2">
                      <ModuleBadge tone={getStatusTone(roadmap.status)}>
                        {prettyStatus(roadmap.status)}
                      </ModuleBadge>
                      <ModuleBadge tone="slate">
                        {formatVersionLabel(roadmap)}
                      </ModuleBadge>
                      <ModuleBadge tone="blue">
                        {prettyReleaseType(roadmap.releaseType)}
                      </ModuleBadge>
                    </div>
                    <h3 className="mt-3 text-base font-extrabold text-[#18332D]">
                      {roadmap.title}
                    </h3>
                    <p className="mt-1 text-sm font-semibold text-slate-600">
                      {roadmap.careerRole?.name || "-"}
                    </p>
                    <p className="mt-3 line-clamp-2 text-xs font-semibold leading-5 text-slate-600">
                      {submittedEvent?.message || "No changelog submitted."}
                    </p>
                    {submittedEvent ? (
                      <p className="mt-2 text-[11px] font-semibold leading-4 text-slate-500">
                        Author: {formatReviewActor(submittedEvent)}
                      </p>
                    ) : null}
                    <div className="mt-4">
                      <ModuleButton onClick={() => openReview(roadmap)}>
                        <Eye size={14} />
                        Review
                      </ModuleButton>
                    </div>
                  </article>
                );
              })}
            </div>
          </ModuleCard>
        )}

        {totalPages > 1 ? (
          <div className="flex justify-end gap-2">
            <ModuleButton
              variant="secondary"
              disabled={isLoading || page <= 1}
              onClick={() => setPage((current) => Math.max(current - 1, 1))}
            >
              Previous
            </ModuleButton>
            <ModuleButton
              variant="secondary"
              disabled={isLoading || page >= totalPages}
              onClick={() => setPage((current) => current + 1)}
            >
              Next
            </ModuleButton>
          </div>
        ) : null}
      </div>
    </ModulePageShell>
  );
}

function RoadmapReviewDetailPage() {
  const navigate = useNavigate();
  const { roadmapId, roadmapVersionId } = useParams();
  const [detail, setDetail] = useState(null);
  const [selectedNodeId, setSelectedNodeId] = useState("");
  const [reviewSuggestion, setReviewSuggestion] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [mutatingVersionId, setMutatingVersionId] = useState("");
  const [isApproveConfirmOpen, setIsApproveConfirmOpen] = useState(false);
  const [isRequestChangesOpen, setIsRequestChangesOpen] = useState(false);
  const [isRequestChangesConfirmOpen, setIsRequestChangesConfirmOpen] =
    useState(false);
  const [isHistoryOpen, setIsHistoryOpen] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!roadmapId || !roadmapVersionId) return;

    setIsLoading(true);
    setError("");

    try {
      const nextDetail = await contentManagerRoadmapApi.getRoadmapDetail({
        roadmapId,
        versionId: roadmapVersionId,
      });

      setDetail(nextDetail);
      const sortedNodes = normalizeNodes(nextDetail.nodes)
        .slice()
        .sort(compareNodes);
      const preferredNode =
        sortedNodes.find((node) => normalizeNodeType(node) === "phase") ||
        sortedNodes[0];
      setSelectedNodeId(preferredNode?.roadmapNodeId || "");
      setReviewSuggestion(
        readReviewDraftCache(
          REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion,
          roadmapVersionId,
        ),
      );
    } catch (requestError) {
      setDetail(null);
      setError(
        getFriendlyApiErrorMessage(
          requestError,
          "Unable to load review detail.",
        ),
      );
    } finally {
      setIsLoading(false);
    }
  }, [roadmapId, roadmapVersionId]);

  useEffect(() => {
    let isActive = true;

    window.queueMicrotask(() => {
      if (isActive) {
        loadDetail();
      }
    });

    return () => {
      isActive = false;
    };
  }, [loadDetail]);

  const allNodes = useMemo(
    () => normalizeNodes(detail?.nodes).slice().sort(compareNodes),
    [detail?.nodes],
  );

  const selectedNode = useMemo(
    () =>
      allNodes.find(
        (node) => String(node.roadmapNodeId) === String(selectedNodeId),
      ) || null,
    [allNodes, selectedNodeId],
  );

  const reviewEvents = useMemo(
    () => detail?.reviewEvents || [],
    [detail?.reviewEvents],
  );
  const latestSubmittedEvent = useMemo(
    () => findLatestReviewEvent(reviewEvents, "submitted"),
    [reviewEvents],
  );

  const handleBack = () => {
    navigate("/content/reviews");
  };

  const handleSelectNode = useCallback((nodeId) => {
    setSelectedNodeId(nodeId);
  }, []);

  const handleReviewSuggestionChange = (nextValue) => {
    setReviewSuggestion(nextValue);
    writeReviewDraftCache(
      REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion,
      roadmapVersionId,
      nextValue,
    );
  };

  async function handleApprove() {
    if (!detail?.roadmapVersionId || mutatingVersionId) return;

    setMutatingVersionId(detail.roadmapVersionId);
    setError("");

    try {
      await contentManagerRoadmapApi.approveVersion(detail.roadmapVersionId);
      clearReviewDraftCache(
        REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion,
        detail.roadmapVersionId,
      );
      toast.success("Roadmap version approved and published.");
      navigate("/content/reviews");
    } catch (requestError) {
      setError(
        getFriendlyApiErrorMessage(
          requestError,
          "Unable to approve roadmap version.",
        ),
      );
    } finally {
      setMutatingVersionId("");
      setIsApproveConfirmOpen(false);
    }
  }

  async function handleRequestChanges() {
    if (
      !detail?.roadmapVersionId ||
      mutatingVersionId ||
      !reviewSuggestion.trim()
    )
      return;

    setMutatingVersionId(detail.roadmapVersionId);
    setError("");

    try {
      await contentManagerRoadmapApi.rejectVersion(detail.roadmapVersionId, {
        reason: reviewSuggestion.trim(),
      });
      clearReviewDraftCache(
        REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion,
        detail.roadmapVersionId,
      );
      toast.success("Roadmap version returned for changes.");
      setReviewSuggestion("");
      navigate("/content/reviews");
    } catch (requestError) {
      setError(
        getFriendlyApiErrorMessage(
          requestError,
          "Unable to reject roadmap version.",
        ),
      );
    } finally {
      setMutatingVersionId("");
      setIsRequestChangesOpen(false);
      setIsRequestChangesConfirmOpen(false);
    }
  }

  if (isLoading && !detail) {
    return (
      <ModulePageShell compact>
        <ModuleCard className="flex items-center justify-center gap-2 p-10 text-center text-sm font-bold text-slate-600">
          <Loader2 size={16} className="animate-spin text-[#1F6F5F]" />
          Loading review workspace...
        </ModuleCard>
      </ModulePageShell>
    );
  }

  if (error && !detail) {
    return (
      <ModulePageShell compact>
        <div className="space-y-4">
          <button
            type="button"
            onClick={handleBack}
            className="inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to review queue
          </button>
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            {error}
          </ModuleCard>
        </div>
      </ModulePageShell>
    );
  }

  if (!detail) {
    return (
      <ModulePageShell compact>
        <ModuleEmptyState title="Roadmap version not found" />
      </ModulePageShell>
    );
  }

  const isMutatingThisVersion = mutatingVersionId === detail.roadmapVersionId;

  return (
    <ModulePageShell compact>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <button
            type="button"
            onClick={handleBack}
            className="inline-flex cursor-pointer items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to review queue
          </button>

          {isLoading && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-white px-2.5 py-1 text-xs font-bold text-slate-600 shadow-sm">
              <Loader2 size={13} className="animate-spin text-[#1F6F5F]" />
              Updating...
            </span>
          )}
        </div>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
            {error}
          </div>
        )}

        <ModuleCard className="overflow-hidden p-0">
          <div className="flex flex-wrap items-start justify-between gap-4 border-b border-[#B9D8CC]/70 bg-[#F4FBF8] p-5">
            <div className="min-w-0">
              <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                Roadmap review
              </p>
              <h1 className="mt-2 text-2xl font-black text-[#18332D]">
                {detail.title}
              </h1>
              <p className="mt-1 text-sm font-semibold text-slate-600">
                {detail.careerRole?.name ||
                  detail.slug ||
                  "Career role not set"}
              </p>
            </div>

            <div className="flex shrink-0 flex-wrap items-center justify-end gap-2">
              <ModuleBadge tone={getStatusTone(detail.status)}>
                {prettyStatus(detail.status)}
              </ModuleBadge>
              <ModuleBadge tone="slate">
                {formatVersionLabel(detail)}
              </ModuleBadge>
              <ModuleBadge tone="blue">
                {prettyReleaseType(detail.releaseType)}
              </ModuleBadge>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-4 px-5 py-3 text-xs font-bold text-slate-600">
            <span className="inline-flex items-center gap-2">
              <Layers3 size={14} className="text-[#1F6F5F]" />
              {detail.nodeCount || allNodes.length} nodes
            </span>
            <span className="inline-flex items-center gap-2">
              <Clock3 size={14} className="text-[#1F6F5F]" />
              Submitted{" "}
              {formatDate(latestSubmittedEvent?.createdAt || detail.updatedAt)}
            </span>
            {latestSubmittedEvent ? (
              <span className="inline-flex items-center gap-2">
                <MessageSquare size={14} className="text-[#1F6F5F]" />
                Author {formatReviewActor(latestSubmittedEvent)}
              </span>
            ) : null}
          </div>
        </ModuleCard>

        <LatestChangelogCard
          event={latestSubmittedEvent}
          isMutating={isMutatingThisVersion}
          isActionDisabled={Boolean(mutatingVersionId)}
          onViewHistory={() => setIsHistoryOpen(true)}
          onRequestChanges={() => setIsRequestChangesOpen(true)}
          onApprove={() => setIsApproveConfirmOpen(true)}
        />

        <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_500px] xl:items-stretch 2xl:grid-cols-[minmax(0,1.08fr)_560px]">
          <div className="min-w-0">
            <ModuleCard className="flex h-[720px] flex-col overflow-hidden p-0">
              <div className="flex items-center gap-2 border-b border-[#B9D8CC]/70 p-4">
                <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
                  <Layers3 size={16} />
                </div>
                <h2 className="text-base font-extrabold text-[#18332D]">
                  Roadmap view
                </h2>
              </div>

              <div className="min-h-0 flex-1 p-4">
                <RoadmapGraphCanvas
                  detail={detail}
                  nodes={allNodes}
                  selectedNodeId={selectedNodeId}
                  onSelect={handleSelectNode}
                  className="h-full min-h-0"
                />
              </div>
            </ModuleCard>
          </div>

          <aside className="xl:sticky xl:top-6 xl:h-[720px] xl:self-start">
            <SelectedNodeReviewPanel node={selectedNode} className="h-full" />
          </aside>
        </div>
      </div>

      <ConfirmActionDialog
        isOpen={isApproveConfirmOpen}
        tone="success"
        title="Approve roadmap version"
        description="Publish this roadmap version and remove it from the review queue."
        confirmLabel="Approve"
        cancelLabel="Cancel"
        isConfirming={Boolean(mutatingVersionId)}
        onCancel={() => setIsApproveConfirmOpen(false)}
        onConfirm={handleApprove}
      />

      <RequestChangesDialog
        isOpen={isRequestChangesOpen}
        value={reviewSuggestion}
        isSubmitting={Boolean(mutatingVersionId)}
        onChange={handleReviewSuggestionChange}
        onCancel={() => setIsRequestChangesOpen(false)}
        onSubmit={() => {
          setIsRequestChangesOpen(false);
          setIsRequestChangesConfirmOpen(true);
        }}
      />

      <ConfirmActionDialog
        isOpen={isRequestChangesConfirmOpen}
        tone="danger"
        title="Confirm rejection"
        description="Send this feedback to the content manager and return the roadmap version for changes."
        confirmLabel="Reject"
        cancelLabel="Back"
        isConfirming={Boolean(mutatingVersionId)}
        onCancel={() => {
          setIsRequestChangesConfirmOpen(false);
          setIsRequestChangesOpen(true);
        }}
        onConfirm={handleRequestChanges}
      />

      <ReviewHistoryDialog
        isOpen={isHistoryOpen}
        events={reviewEvents}
        onClose={() => setIsHistoryOpen(false)}
      />
    </ModulePageShell>
  );
}

function LatestChangelogCard({
  event,
  isMutating,
  isActionDisabled,
  onViewHistory,
  onRequestChanges,
  onApprove,
}) {
  const message = event?.message;

  return (
    <ModuleCard className="p-4">
      <div className="grid items-stretch gap-4 lg:grid-cols-[minmax(0,1fr)_240px] xl:grid-cols-[minmax(0,1fr)_260px]">
        <div className="min-w-0">
          <div className="mb-2 flex items-center gap-2 text-[#1F6F5F]">
            <MessageSquare size={17} />
            <h2 className="text-sm font-extrabold text-[#18332D]">
              Latest Changelog
            </h2>
          </div>
          <p className="max-h-36 overflow-y-auto whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
            {message || "No changelog was submitted for this version."}
          </p>
          {event ? (
            <p className="mt-2 text-xs font-bold text-slate-500">
              Author: {formatReviewActor(event)}
            </p>
          ) : null}
        </div>

        <div className="flex min-h-32 flex-col items-stretch justify-between gap-4 sm:items-end">
          <ModuleButton
            variant="secondary"
            onClick={onViewHistory}
            disabled={isActionDisabled}
          >
            <History size={14} />
            View history
          </ModuleButton>

          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              onClick={onRequestChanges}
              disabled={isActionDisabled}
              className="inline-flex min-w-[88px] items-center justify-center gap-2 rounded-lg border border-red-700 bg-red-600 px-3 py-1.5 text-xs font-bold leading-tight text-white shadow-sm transition hover:border-red-800 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-200 disabled:border-slate-300 disabled:bg-slate-300 disabled:text-slate-600"
            >
              {isMutating ? (
                <Loader2 size={14} className="animate-spin" />
              ) : (
                <XCircle size={14} />
              )}
              Reject
            </button>
            <ModuleButton onClick={onApprove} disabled={isActionDisabled}>
              {isMutating ? (
                <Loader2 size={14} className="animate-spin" />
              ) : (
                <CheckCircle2 size={14} />
              )}
              Approve
            </ModuleButton>
          </div>
        </div>
      </div>
    </ModuleCard>
  );
}

function SelectedNodeReviewPanel({ node, className = "" }) {
  const [activeTab, setActiveTab] = useState("details");
  const outcomes = useMemo(
    () => normalizeStringList(node?.learningOutcomes),
    [node?.learningOutcomes],
  );
  const criteria = useMemo(
    () => normalizeStringList(node?.completionCriteria),
    [node?.completionCriteria],
  );
  const skills = useMemo(() => normalizeNodes(node?.skills), [node?.skills]);
  const resources = useMemo(
    () => normalizeNodes(node?.resources),
    [node?.resources],
  );
  const nodeType = normalizeNodeType(node);
  const tabs = useMemo(() => {
    const nextTabs = [{ key: "details", label: "Details" }];

    if (nodeType === "choice_group") {
      nextTabs.push({ key: "requirements", label: "Requirements" });
    }

    if (node && canEditLearningFields(node)) {
      nextTabs.push({ key: "content", label: "Content" });
    }

    if (node && canEditMappings(node) && skills.length > 0) {
      nextTabs.push({ key: "skills", label: "Skills" });
    }

    if (node && canEditMappings(node) && resources.length > 0) {
      nextTabs.push({ key: "resources", label: "Resources" });
    }

    return nextTabs;
  }, [node, nodeType, resources.length, skills.length]);

  const selectedTabKey = tabs.some((tab) => tab.key === activeTab)
    ? activeTab
    : tabs[0]?.key || "details";

  if (!node) {
    return (
      <ReviewPanel
        icon={<FileText size={17} />}
        title="Selected node"
        className={className}
      >
        <div className="grid min-h-0 flex-1 place-items-center">
          <ModuleEmptyState title="Select a node" />
        </div>
      </ReviewPanel>
    );
  }

  return (
    <ReviewPanel
      icon={<FileText size={17} />}
      title="Selected node"
      className={className}
    >
      <div className="min-h-0 flex-1 space-y-4 overflow-y-auto pr-1">
        <div className="rounded-xl border border-[#B9D8CC]/70 bg-white p-3">
          <div className="flex flex-wrap gap-2">
            <ModuleBadge tone={getNodeTone(nodeType)}>
              {nodeType || "node"}
            </ModuleBadge>
            {node.isRequired ? (
              <ModuleBadge tone="green">Required</ModuleBadge>
            ) : (
              <ModuleBadge tone="amber">Optional</ModuleBadge>
            )}
            {node.isTrackable ? (
              <ModuleBadge tone="blue">Trackable</ModuleBadge>
            ) : null}
          </div>
          <h3 className="mt-3 text-lg font-black text-[#18332D]">
            {node.title || "Untitled node"}
          </h3>
        </div>

        <div className="rounded-lg border border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-2">
          <div
            className="grid gap-2 rounded-xl bg-white p-1 ring-1 ring-[#B9D8CC]/70"
            style={{
              gridTemplateColumns: `repeat(${tabs.length}, minmax(0, 1fr))`,
            }}
          >
            {tabs.map((tab) => (
              <button
                key={tab.key}
                type="button"
                onClick={() => setActiveTab(tab.key)}
                className={`rounded-lg px-3 py-2 text-xs font-extrabold transition ${
                  selectedTabKey === tab.key
                    ? "bg-[#1F6F5F] text-white shadow-sm"
                    : "text-slate-600 hover:bg-[#F7F1E8] hover:text-[#18332D]"
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        {selectedTabKey === "details" ? (
          <div className="space-y-4">
            {node.description ? (
              <p className="rounded-xl border border-[#B9D8CC]/70 bg-white p-3 whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
                {node.description}
              </p>
            ) : (
              <p className="rounded-xl border border-[#B9D8CC]/70 bg-white p-3 text-sm font-semibold text-slate-500">
                No description.
              </p>
            )}

            <div className="grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-1 2xl:grid-cols-2">
              <NodeInfoRow
                label="Difficulty"
                value={node.difficultyLevel || "-"}
              />
              <NodeInfoRow
                label="Estimated hours"
                value={node.estimatedHours ?? "-"}
              />
            </div>
          </div>
        ) : null}

        {selectedTabKey === "requirements" ? (
          <div className="space-y-4">
            <div className="grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-1 2xl:grid-cols-2">
              <NodeInfoRow
                label="Selection"
                value={formatSelectionType(node.selectionType)}
              />
              <NodeInfoRow
                label="Required count"
                value={node.requiredCount ?? "-"}
              />
            </div>
          </div>
        ) : null}

        {selectedTabKey === "content" ? (
          <div className="space-y-4">
            <CompactList
              title="Learning outcomes"
              items={outcomes}
              emptyText="No learning outcomes."
            />
            <CompactList
              title="Completion criteria"
              items={criteria}
              emptyText="No completion criteria."
            />
          </div>
        ) : null}

        {selectedTabKey === "skills" ? (
          <CompactList
            title="Mapped skills"
            items={skills.map(getSkillName)}
            emptyText="No skills mapped."
          />
        ) : null}

        {selectedTabKey === "resources" ? (
          <CompactList
            title="Mapped resources"
            items={resources.map(getResourceTitle)}
            emptyText="No resources mapped."
          />
        ) : null}
      </div>
    </ReviewPanel>
  );
}

function NodeInfoRow({ label, value }) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-lg border border-[#B9D8CC]/70 bg-white px-3 py-2">
      <span className="text-xs font-extrabold uppercase tracking-wide text-slate-500">
        {label}
      </span>
      <span className="text-sm font-bold text-[#18332D]">{value}</span>
    </div>
  );
}

function CompactList({ title, items, emptyText }) {
  const values = normalizeStringList(items);

  return (
    <section>
      <h4 className="mb-2 text-xs font-extrabold uppercase tracking-wide text-slate-600">
        {title}
      </h4>
      {values.length === 0 ? (
        <p className="rounded-lg border border-[#B9D8CC]/70 bg-white px-3 py-2 text-sm font-semibold text-slate-500">
          {emptyText}
        </p>
      ) : (
        <ul className="space-y-2">
          {values.slice(0, 8).map((item, index) => (
            <li
              key={`${item}-${index}`}
              className="rounded-lg border border-[#B9D8CC]/70 bg-white px-3 py-2 text-sm font-semibold leading-6 text-slate-700"
            >
              {item}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function RequestChangesDialog({
  isOpen,
  value,
  isSubmitting,
  onChange,
  onCancel,
  onSubmit,
}) {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm"
      onMouseDown={() => {
        if (!isSubmitting) onCancel();
      }}
    >
      <div
        className="w-full max-w-xl rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-base font-extrabold text-[#18332D]">
              Reject roadmap version
            </h2>
            <p className="mt-1 text-sm font-semibold leading-6 text-slate-600">
              Add the required feedback for the content manager.
            </p>
          </div>
          <button
            type="button"
            className="grid h-8 w-8 place-items-center rounded-full text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D]"
            onClick={onCancel}
            disabled={isSubmitting}
            aria-label="Close"
          >
            <X size={18} />
          </button>
        </div>

        <textarea
          className={`${inputClass} mt-4 min-h-[160px] resize-y bg-white`}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="Describe what must change before approval."
          maxLength={4000}
          autoFocus
        />
        {value ? (
          <p className="mt-1.5 text-[11px] font-semibold text-slate-600">
            Autosaved locally.
          </p>
        ) : null}

        <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <ModuleButton
            variant="secondary"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            Cancel
          </ModuleButton>
          <ModuleButton
            variant="danger"
            onClick={onSubmit}
            disabled={isSubmitting || !value.trim()}
          >
            {isSubmitting ? "Submitting..." : "Continue"}
          </ModuleButton>
        </div>
      </div>
    </div>
  );
}

function ReviewHistoryDialog({ isOpen, events, onClose }) {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm"
      onMouseDown={onClose}
    >
      <div
        className="max-h-[82vh] w-full max-w-3xl overflow-hidden rounded-xl border border-[#B9D8CC] bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="flex items-center justify-between gap-4 border-b border-[#B9D8CC]/70 p-5">
          <div className="flex items-center gap-2">
            <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
              <History size={16} />
            </div>
            <h2 className="text-base font-extrabold text-[#18332D]">
              Review history
            </h2>
          </div>
          <button
            type="button"
            className="grid h-8 w-8 place-items-center rounded-full text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D]"
            onClick={onClose}
            aria-label="Close"
          >
            <X size={18} />
          </button>
        </div>

        <div className="max-h-[calc(82vh-74px)] overflow-y-auto p-5">
          {!events?.length ? (
            <ModuleEmptyState title="No review history" />
          ) : (
            <div className="space-y-3">
              {events.map((event) => (
                <article
                  key={
                    event.roadmapVersionReviewEventId ||
                    `${event.eventType}-${event.createdAt}`
                  }
                  className="grid gap-3 rounded-lg border border-[#B9D8CC]/70 bg-[#F4FBF8] p-3 md:grid-cols-[150px_minmax(0,1fr)]"
                >
                  <div className="space-y-1">
                    <span
                      className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-extrabold uppercase tracking-wide ${getReviewEventTone(event.eventType)}`}
                    >
                      {getReviewEventLabel(event.eventType)}
                    </span>
                    <div className="text-[11px] font-bold text-slate-500">
                      {formatDate(event.createdAt)}
                    </div>
                    <div className="text-[11px] font-semibold leading-4 text-slate-600">
                      {getReviewEventActorLabel(event)}: {formatReviewActor(event)}
                    </div>
                  </div>
                  <div className="min-w-0">
                    <p className="whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
                      {event.message || "No message recorded."}
                    </p>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function ReviewPanel({ icon, title, children, className = "" }) {
  return (
    <section
      className={`flex flex-col rounded-lg border border-[#B9D8CC]/80 bg-[#F4FBF8] p-4 ${className}`}
    >
      <div className="mb-3 flex shrink-0 items-center gap-2 text-[#1F6F5F]">
        {icon}
        <h3 className="text-sm font-extrabold text-[#18332D]">{title}</h3>
      </div>
      {children}
    </section>
  );
}

function getSubmittedEvent(roadmap) {
  return (
    roadmap?.latestReviewEvent ||
    findLatestReviewEvent(roadmap?.reviewEvents || [], "submitted")
  );
}

function findLatestReviewEvent(events, eventType) {
  return (
    [...(events || [])]
      .filter(
        (event) => String(event.eventType || "").toLowerCase() === eventType,
      )
      .sort(
        (left, right) =>
          new Date(right.createdAt || 0) - new Date(left.createdAt || 0),
      )[0] || null
  );
}

function getReviewEventLabel(eventType) {
  const normalized = String(eventType || "").toLowerCase();
  if (normalized === "submitted") return "Changelog";
  if (normalized === "rejected") return "Rejected";
  if (normalized === "approved") return "Approved";
  return eventType || "Review note";
}

function getReviewEventActorLabel(event) {
  const normalized = String(event?.eventType || "").toLowerCase();
  if (normalized === "submitted") return "Author";
  if (normalized === "approved" || normalized === "rejected") return "Reviewer";
  return "Actor";
}

function formatReviewActor(event) {
  const displayName = String(event?.actorDisplayName || "").trim();
  const username = String(event?.actorUsername || "").trim();
  const userId = String(event?.actorUserId || "").trim();
  const primary = displayName || username || "Unknown user";
  const details = [];

  if (username && username.toLowerCase() !== primary.toLowerCase()) {
    details.push(`@${username}`);
  }

  if (userId) {
    details.push(`ID ${userId.slice(0, 8)}`);
  }

  return details.length > 0 ? `${primary} (${details.join(", ")})` : primary;
}

function getReviewEventTone(eventType) {
  const normalized = String(eventType || "").toLowerCase();
  if (normalized === "approved")
    return "border-emerald-200 bg-emerald-50 text-emerald-700";
  if (normalized === "rejected")
    return "border-amber-200 bg-amber-50 text-amber-700";
  return "border-[#B9D8CC] bg-white text-[#1F6F5F]";
}

function formatSelectionType(selectionType) {
  const normalized = String(selectionType || "complete_all").toLowerCase();
  if (normalized === "choose_any") return "Choose any";
  if (normalized === "choose_required_count") return "Choose required count";
  return "Complete all";
}

function normalizeStringList(items) {
  if (!Array.isArray(items)) return [];
  return items.map((item) => String(item || "").trim()).filter(Boolean);
}
