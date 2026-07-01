import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  CheckCircle2,
  ClipboardCheck,
  FileText,
  GitPullRequest,
  Layers3,
  Loader2,
  MessageSquare,
  RefreshCw,
  XCircle,
} from "lucide-react";
import { toast } from "react-toastify";

import { contentManagerRoadmapApi } from "../../../api/contentRoadmapApi";
import {
  inputClass,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../../features/learningModules/components/learningModuleUi";
import {
  formatDate,
  formatVersionLabel,
  getStatusTone,
  prettyStatus,
} from "../../../features/roadmapEditor/roadmapEditorUtils";
import {
  clearReviewDraftCache,
  readReviewDraftCache,
  REVIEW_DRAFT_CACHE_TYPES,
  writeReviewDraftCache,
} from "../../../features/roadmapEditor/reviewDraftCache";
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";

export default function ContentReviewerRoadmapReviewsPage() {
  const [items, setItems] = useState([]);
  const [selectedRoadmap, setSelectedRoadmap] = useState(null);
  const [selectedDetail, setSelectedDetail] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [error, setError] = useState("");
  const [mutatingVersionId, setMutatingVersionId] = useState("");
  const [reviewSuggestion, setReviewSuggestion] = useState("");
  const selectedRoadmapRef = useRef(null);

  const loadReviewDetail = useCallback(async (roadmap) => {
    if (!roadmap?.roadmapId || !roadmap?.roadmapVersionId) {
      setSelectedDetail(null);
      return;
    }

    selectedRoadmapRef.current = roadmap;
    setSelectedRoadmap(roadmap);
    setReviewSuggestion(readReviewDraftCache(
      REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion,
      roadmap.roadmapVersionId,
    ));
    setIsLoadingDetail(true);
    setError("");

    try {
      const detail = await contentManagerRoadmapApi.getRoadmapDetail({
        roadmapId: roadmap.roadmapId,
        versionId: roadmap.roadmapVersionId,
      });
      setSelectedDetail(detail);
    } catch (requestError) {
      setSelectedDetail(null);
      setError(getFriendlyApiErrorMessage(requestError, "Unable to load review detail."));
    } finally {
      setIsLoadingDetail(false);
    }
  }, []);

  const loadReviews = useCallback(async ({ force = false } = {}) => {
    setIsLoading(true);
    setError("");

    try {
      const result = await contentManagerRoadmapApi.getRoadmaps({
        status: "pending_review",
        sort: "updated_desc",
        page: 1,
        pageSize: 100,
      });

      const nextItems = result.items || [];
      setItems(nextItems);

      const currentSelected = selectedRoadmapRef.current;
      const stillSelected = currentSelected
        ? nextItems.find((item) => item.roadmapVersionId === currentSelected.roadmapVersionId)
        : null;
      const nextSelected = stillSelected || nextItems[0] || null;

      if (nextSelected) {
        await loadReviewDetail(nextSelected);
      } else {
        selectedRoadmapRef.current = null;
        setSelectedRoadmap(null);
        setSelectedDetail(null);
      }

      if (force) {
        toast.success("Review queue refreshed.");
      }
    } catch (requestError) {
      setError(getFriendlyApiErrorMessage(requestError, "Unable to load review queue."));
    } finally {
      setIsLoading(false);
    }
  }, [loadReviewDetail]);

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

  const reviewEvents = useMemo(
    () => selectedDetail?.reviewEvents || selectedRoadmap?.reviewEvents || [],
    [selectedDetail?.reviewEvents, selectedRoadmap?.reviewEvents],
  );
  const latestSubmittedEvent = useMemo(
    () => findLatestReviewEvent(reviewEvents, "submitted"),
    [reviewEvents],
  );
  const outlineNodes = useMemo(
    () => [...(selectedDetail?.nodes || [])].sort((first, second) => {
      const firstOrder = Number(first.orderIndex ?? first.OrderIndex ?? 0);
      const secondOrder = Number(second.orderIndex ?? second.OrderIndex ?? 0);
      return firstOrder - secondOrder;
    }),
    [selectedDetail],
  );

  async function handleApprove(roadmap) {
    if (!roadmap?.roadmapVersionId || mutatingVersionId) return;

    setMutatingVersionId(roadmap.roadmapVersionId);
    setError("");

    try {
      await contentManagerRoadmapApi.approveVersion(roadmap.roadmapVersionId);
      clearReviewDraftCache(REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion, roadmap.roadmapVersionId);
      toast.success("Roadmap version approved and published.");
      await loadReviews();
    } catch (requestError) {
      setError(getFriendlyApiErrorMessage(requestError, "Unable to approve roadmap version."));
    } finally {
      setMutatingVersionId("");
    }
  }

  async function handleRequestChanges(roadmap) {
    if (!roadmap?.roadmapVersionId || mutatingVersionId || !reviewSuggestion.trim()) return;

    setMutatingVersionId(roadmap.roadmapVersionId);
    setError("");

    try {
      await contentManagerRoadmapApi.rejectVersion(roadmap.roadmapVersionId, {
        reason: reviewSuggestion.trim(),
      });
      clearReviewDraftCache(REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion, roadmap.roadmapVersionId);
      toast.success("Roadmap version returned for changes.");
      setReviewSuggestion("");
      await loadReviews();
    } catch (requestError) {
      setError(getFriendlyApiErrorMessage(requestError, "Unable to reject roadmap version."));
    } finally {
      setMutatingVersionId("");
    }
  }

  const activeRoadmap = selectedDetail || selectedRoadmap;
  const activeVersionId = activeRoadmap?.roadmapVersionId;
  const handleReviewSuggestionChange = (nextValue) => {
    setReviewSuggestion(nextValue);
    writeReviewDraftCache(
      REVIEW_DRAFT_CACHE_TYPES.reviewerSuggestion,
      activeVersionId,
      nextValue,
    );
  };

  return (
    <ModulePageShell>
      <div className="mx-auto max-w-7xl space-y-5">
        <section className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
              Reviewer workspace
            </p>
            <h1 className="mt-1 text-3xl font-black text-[#18332D]">
              Roadmap Review Queue
            </h1>
          </div>

          <ModuleButton variant="secondary" onClick={() => loadReviews({ force: true })} disabled={isLoading}>
            {isLoading ? <Loader2 size={14} className="animate-spin" /> : <RefreshCw size={14} />}
            Refresh
          </ModuleButton>
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
          <div className="grid gap-5 lg:grid-cols-[360px_minmax(0,1fr)]">
            <ModuleCard className="overflow-hidden p-0">
              <div className="border-b border-[#B9D8CC]/70 p-4">
                <div className="flex items-center gap-2">
                  <GitPullRequest size={17} className="text-[#1F6F5F]" />
                  <h2 className="text-sm font-extrabold text-[#18332D]">Pending queue</h2>
                </div>
                <p className="mt-1 text-xs font-semibold text-slate-600">{items.length} version(s) waiting.</p>
              </div>

              <div className="max-h-[calc(100vh-260px)] overflow-y-auto p-2 scrollbar-thin scrollbar-track-[#F7F1E8] scrollbar-thumb-[#B9D8CC]">
                {items.map((roadmap) => {
                  const isSelected = roadmap.roadmapVersionId === activeVersionId;
                  const submittedEvent = findLatestReviewEvent(roadmap.reviewEvents || [], "submitted");

                  return (
                    <button
                      key={roadmap.roadmapVersionId}
                      type="button"
                      onClick={() => loadReviewDetail(roadmap)}
                      className={[
                        "w-full rounded-lg border p-3 text-left transition",
                        isSelected
                          ? "border-[#2FA084] bg-[#F4FBF8] shadow-sm"
                          : "border-transparent hover:border-[#B9D8CC] hover:bg-[#F7F1E8]",
                      ].join(" ")}
                    >
                      <div className="flex flex-wrap items-center gap-2">
                        <ModuleBadge tone={getStatusTone(roadmap.status)}>
                          {prettyStatus(roadmap.status)}
                        </ModuleBadge>
                        <ModuleBadge tone="slate">{formatVersionLabel(roadmap)}</ModuleBadge>
                      </div>
                      <h3 className="mt-2 line-clamp-2 text-sm font-extrabold text-[#18332D]">
                        {roadmap.title}
                      </h3>
                      <p className="mt-1 truncate text-xs font-semibold text-slate-600">
                        {roadmap.careerRole?.name || roadmap.slug || "-"}
                      </p>
                      {submittedEvent ? (
                        <p className="mt-2 line-clamp-2 text-xs font-semibold leading-5 text-slate-600">
                          {submittedEvent.message}
                        </p>
                      ) : null}
                    </button>
                  );
                })}
              </div>
            </ModuleCard>

            <ModuleCard className="overflow-hidden p-0">
              {!activeRoadmap ? (
                <ModuleEmptyState title="Select a roadmap version to review" />
              ) : (
                <div className="min-h-[620px]">
                  <div className="border-b border-[#B9D8CC]/70 bg-[#F4FBF8] p-5">
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <ModuleBadge tone={getStatusTone(activeRoadmap.status)}>
                            {prettyStatus(activeRoadmap.status)}
                          </ModuleBadge>
                          <ModuleBadge tone="slate">{formatVersionLabel(activeRoadmap)}</ModuleBadge>
                          <ModuleBadge tone="blue">{String(activeRoadmap.releaseType || "release")}</ModuleBadge>
                        </div>
                        <h2 className="mt-3 text-2xl font-black text-[#18332D]">
                          {activeRoadmap.title}
                        </h2>
                        <p className="mt-1 text-sm font-semibold text-slate-600">
                          {activeRoadmap.careerRole?.name || activeRoadmap.slug || "-"}
                        </p>
                      </div>

                      <div className="flex flex-wrap gap-2">
                        <ModuleButton
                          variant="danger"
                          onClick={() => handleRequestChanges(activeRoadmap)}
                          disabled={Boolean(mutatingVersionId) || !reviewSuggestion.trim()}
                        >
                          {mutatingVersionId === activeRoadmap.roadmapVersionId ? <Loader2 size={14} className="animate-spin" /> : <XCircle size={14} />}
                          Request changes
                        </ModuleButton>
                        <ModuleButton onClick={() => handleApprove(activeRoadmap)} disabled={Boolean(mutatingVersionId)}>
                          {mutatingVersionId === activeRoadmap.roadmapVersionId ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                          Approve
                        </ModuleButton>
                      </div>
                    </div>
                  </div>

                  {isLoadingDetail ? (
                    <div className="flex items-center justify-center gap-2 p-10 text-sm font-bold text-slate-600">
                      <Loader2 size={16} className="animate-spin text-[#1F6F5F]" />
                      Loading review packet...
                    </div>
                  ) : (
                    <div className="space-y-5 p-5">
                      <ReviewPanel icon={<MessageSquare size={17} />} title="Review suggestion">
                        <textarea
                          className={`${inputClass} min-h-[120px] resize-y bg-white`}
                          value={reviewSuggestion}
                          onChange={(event) => handleReviewSuggestionChange(event.target.value)}
                          placeholder="Suggest what the content manager should change before approval."
                          maxLength={4000}
                        />
                        {reviewSuggestion ? (
                          <p className="mt-1.5 text-[11px] font-semibold text-slate-600">
                            Autosaved locally.
                          </p>
                        ) : null}
                      </ReviewPanel>

                      <section className="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_minmax(280px,0.8fr)]">
                        <ReviewPanel
                          icon={<MessageSquare size={17} />}
                          title="Submitted changelog"
                        >
                          <p className="whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
                            {latestSubmittedEvent?.message || "No changelog was submitted for this version."}
                          </p>
                        </ReviewPanel>

                        <ReviewPanel icon={<ClipboardCheck size={17} />} title="Review checklist">
                          <div className="grid gap-2 text-xs font-bold text-slate-700">
                            <span>{activeRoadmap.nodeCount || selectedDetail?.nodeCount || 0} total nodes</span>
                            <span>{activeRoadmap.trackableNodeCount || selectedDetail?.trackableNodeCount || 0} trackable nodes</span>
                            <span>{activeRoadmap.resourceMappingCount || selectedDetail?.resourceMappingCount || 0} resource mappings</span>
                            <span>{activeRoadmap.skillMappingCount || selectedDetail?.skillMappingCount || 0} skill mappings</span>
                            <span>Last edited {formatDate(activeRoadmap.updatedAt || activeRoadmap.createdAt)}</span>
                          </div>
                        </ReviewPanel>
                      </section>

                      <ReviewPanel icon={<Layers3 size={17} />} title="Node outline">
                        {outlineNodes.length === 0 ? (
                          <p className="text-sm font-semibold text-slate-600">No nodes were returned for this version.</p>
                        ) : (
                          <div className="grid max-h-[460px] gap-2 overflow-y-auto pr-1 md:grid-cols-2">
                            {outlineNodes.map((node) => (
                              <article key={node.roadmapNodeId} className="rounded-lg border border-[#B9D8CC]/70 bg-white p-3">
                                <div className="flex flex-wrap items-center gap-2">
                                  <ModuleBadge tone="slate">{node.nodeType}</ModuleBadge>
                                  {node.isRequired ? <ModuleBadge tone="green">Required</ModuleBadge> : <ModuleBadge tone="amber">Optional</ModuleBadge>}
                                  {node.isTrackable ? <ModuleBadge tone="blue">Trackable</ModuleBadge> : null}
                                </div>
                                <h3 className="mt-2 text-sm font-extrabold text-[#18332D]">{node.title}</h3>
                                {node.description ? (
                                  <p className="mt-1 line-clamp-2 text-xs font-semibold leading-5 text-slate-600">
                                    {node.description}
                                  </p>
                                ) : null}
                              </article>
                            ))}
                          </div>
                        )}
                      </ReviewPanel>

                      <ReviewPanel icon={<FileText size={17} />} title="Review timeline">
                        {reviewEvents.length === 0 ? (
                          <p className="text-sm font-semibold text-slate-600">No review events recorded.</p>
                        ) : (
                          <div className="grid gap-3">
                            {reviewEvents.map((event) => (
                              <article key={event.roadmapVersionReviewEventId || `${event.eventType}-${event.createdAt}`} className="grid gap-3 rounded-lg border border-[#B9D8CC]/70 bg-white p-3 md:grid-cols-[150px_minmax(0,1fr)]">
                                <div className="space-y-1">
                                  <span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-extrabold uppercase tracking-wide ${getReviewEventTone(event.eventType)}`}>
                                    {getReviewEventLabel(event.eventType)}
                                  </span>
                                  <div className="text-[11px] font-bold text-slate-500">{formatDate(event.createdAt)}</div>
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
                      </ReviewPanel>
                    </div>
                  )}
                </div>
              )}
            </ModuleCard>
          </div>
        )}

      </div>
    </ModulePageShell>
  );
}

function ReviewPanel({ icon, title, children }) {
  return (
    <section className="rounded-lg border border-[#B9D8CC]/80 bg-[#F4FBF8] p-4">
      <div className="mb-3 flex items-center gap-2 text-[#1F6F5F]">
        {icon}
        <h3 className="text-sm font-extrabold text-[#18332D]">{title}</h3>
      </div>
      {children}
    </section>
  );
}

function findLatestReviewEvent(events, eventType) {
  return [...(events || [])]
    .filter((event) => String(event.eventType || "").toLowerCase() === eventType)
    .sort((left, right) => new Date(right.createdAt || 0) - new Date(left.createdAt || 0))[0] || null;
}

function getReviewEventLabel(eventType) {
  const normalized = String(eventType || "").toLowerCase();
  if (normalized === "submitted") return "Changelog";
  if (normalized === "rejected") return "Rejected";
  if (normalized === "approved") return "Approved";
  return eventType || "Review note";
}

function getReviewEventTone(eventType) {
  const normalized = String(eventType || "").toLowerCase();
  if (normalized === "approved") return "border-emerald-200 bg-emerald-50 text-emerald-700";
  if (normalized === "rejected") return "border-amber-200 bg-amber-50 text-amber-700";
  return "border-[#B9D8CC] bg-white text-[#1F6F5F]";
}
