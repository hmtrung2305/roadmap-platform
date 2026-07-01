import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  CheckCircle2,
  ClipboardCheck,
  Eye,
  FileText,
  GitPullRequest,
  Layers3,
  Loader2,
  MessageSquare,
  RefreshCw,
  X,
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
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";

export default function ContentReviewerRoadmapReviewsPage() {
  const navigate = useNavigate();
  const [items, setItems] = useState([]);
  const [selectedRoadmap, setSelectedRoadmap] = useState(null);
  const [selectedDetail, setSelectedDetail] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [error, setError] = useState("");
  const [mutatingVersionId, setMutatingVersionId] = useState("");
  const [rejectTarget, setRejectTarget] = useState(null);
  const [rejectReason, setRejectReason] = useState("");
  const selectedRoadmapRef = useRef(null);

  const loadReviewDetail = useCallback(async (roadmap) => {
    if (!roadmap?.roadmapId || !roadmap?.roadmapVersionId) {
      setSelectedDetail(null);
      return;
    }

    selectedRoadmapRef.current = roadmap;
    setSelectedRoadmap(roadmap);
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

  const reviewEvents = selectedDetail?.reviewEvents || selectedRoadmap?.reviewEvents || [];
  const latestSubmittedEvent = useMemo(
    () => findLatestReviewEvent(reviewEvents, "submitted"),
    [reviewEvents],
  );
  const topNodes = useMemo(
    () => (selectedDetail?.nodes || []).slice(0, 12),
    [selectedDetail],
  );

  async function handleApprove(roadmap) {
    if (!roadmap?.roadmapVersionId || mutatingVersionId) return;

    setMutatingVersionId(roadmap.roadmapVersionId);
    setError("");

    try {
      await contentManagerRoadmapApi.approveVersion(roadmap.roadmapVersionId);
      toast.success("Roadmap version approved and published.");
      await loadReviews();
    } catch (requestError) {
      setError(getFriendlyApiErrorMessage(requestError, "Unable to approve roadmap version."));
    } finally {
      setMutatingVersionId("");
    }
  }

  function openRejectDialog(roadmap) {
    if (!roadmap?.roadmapVersionId || mutatingVersionId) return;

    setRejectTarget(roadmap);
    setRejectReason("");
  }

  async function confirmReject() {
    if (!rejectTarget?.roadmapVersionId || mutatingVersionId || !rejectReason.trim()) return;

    setMutatingVersionId(rejectTarget.roadmapVersionId);
    setError("");

    try {
      await contentManagerRoadmapApi.rejectVersion(rejectTarget.roadmapVersionId, {
        reason: rejectReason.trim(),
      });
      toast.success("Roadmap version returned for changes.");
      setRejectTarget(null);
      setRejectReason("");
      await loadReviews();
    } catch (requestError) {
      setError(getFriendlyApiErrorMessage(requestError, "Unable to reject roadmap version."));
    } finally {
      setMutatingVersionId("");
    }
  }

  function openEditor(roadmap) {
    navigate(`/content/roadmaps/${roadmap.roadmapId}/edit?versionId=${roadmap.roadmapVersionId}&from=reviews`);
  }

  const activeRoadmap = selectedDetail || selectedRoadmap;
  const activeVersionId = activeRoadmap?.roadmapVersionId;

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
                        <ModuleButton variant="secondary" onClick={() => openEditor(activeRoadmap)} disabled={Boolean(mutatingVersionId)}>
                          <Eye size={14} />
                          Open editor
                        </ModuleButton>
                        <ModuleButton variant="danger" onClick={() => openRejectDialog(activeRoadmap)} disabled={Boolean(mutatingVersionId)}>
                          {mutatingVersionId === activeRoadmap.roadmapVersionId ? <Loader2 size={14} className="animate-spin" /> : <XCircle size={14} />}
                          Reject
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
                        {topNodes.length === 0 ? (
                          <p className="text-sm font-semibold text-slate-600">No nodes were returned for this version.</p>
                        ) : (
                          <div className="grid gap-2 md:grid-cols-2">
                            {topNodes.map((node) => (
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
                              <article key={event.roadmapVersionReviewEventId || `${event.eventType}-${event.createdAt}`} className="rounded-lg border border-[#B9D8CC]/70 bg-white p-3">
                                <div className="flex flex-wrap items-center justify-between gap-2">
                                  <span className="text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F]">
                                    {getReviewEventLabel(event.eventType)}
                                  </span>
                                  <span className="text-[11px] font-bold text-slate-500">{formatDate(event.createdAt)}</span>
                                </div>
                                <p className="mt-2 whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
                                  {event.message}
                                </p>
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

        <RejectDialog
          roadmap={rejectTarget}
          reason={rejectReason}
          setReason={setRejectReason}
          isRejecting={Boolean(mutatingVersionId)}
          onClose={() => {
            setRejectTarget(null);
            setRejectReason("");
          }}
          onConfirm={confirmReject}
        />
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

function RejectDialog({ roadmap, reason, setReason, isRejecting, onClose, onConfirm }) {
  if (!roadmap) return null;

  const canReject = reason.trim().length > 0;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/35 p-4 backdrop-blur-sm">
      <div className="w-full max-w-lg overflow-hidden rounded-xl border border-[#B9D8CC] bg-white shadow-2xl">
        <div className="flex items-center justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
          <div>
            <h2 className="text-base font-extrabold text-[#18332D]">Reject roadmap version</h2>
            <p className="mt-1 text-xs font-semibold text-slate-600">{roadmap.title}</p>
          </div>
          <button type="button" onClick={onClose} className="rounded-lg p-2 text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D]">
            <X size={18} />
          </button>
        </div>

        <div className="space-y-3 p-4">
          <label className="block">
            <span className="mb-1.5 block text-[11px] font-bold uppercase tracking-wide text-slate-700">
              Reject reason
            </span>
            <textarea
              className={`${inputClass} min-h-[140px] resize-y`}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Explain what needs to be changed before this roadmap can be approved."
              maxLength={4000}
            />
          </label>
        </div>

        <div className="flex justify-end gap-2 border-t border-[#B9D8CC]/70 p-4">
          <ModuleButton variant="secondary" onClick={onClose} disabled={isRejecting}>Cancel</ModuleButton>
          <ModuleButton variant="danger" onClick={onConfirm} disabled={!canReject || isRejecting}>
            {isRejecting ? <Loader2 size={14} className="animate-spin" /> : <XCircle size={14} />}
            Reject
          </ModuleButton>
        </div>
      </div>
    </div>
  );
}

function findLatestReviewEvent(events, eventType) {
  return [...(events || [])]
    .filter((event) => String(event.eventType || "").toLowerCase() === eventType)
    .sort((left, right) => new Date(right.createdAt || 0) - new Date(left.createdAt || 0))[0] || null;
}

function getReviewEventLabel(eventType) {
  const normalized = String(eventType || "").toLowerCase();
  if (normalized === "submitted") return "Submitted";
  if (normalized === "rejected") return "Rejected";
  if (normalized === "approved") return "Approved";
  return eventType || "Review note";
}
