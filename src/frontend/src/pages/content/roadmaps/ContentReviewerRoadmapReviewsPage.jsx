import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CheckCircle2, Eye, Loader2, RefreshCw, XCircle } from "lucide-react";
import { toast } from "react-toastify";

import { contentManagerRoadmapApi } from "../../../api/contentRoadmapApi";
import {
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
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [mutatingVersionId, setMutatingVersionId] = useState("");

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

      setItems(result.items || []);

      if (force) {
        toast.success("Review queue refreshed.");
      }
    } catch (requestError) {
      setError(getFriendlyApiErrorMessage(requestError, "Unable to load review queue."));
    } finally {
      setIsLoading(false);
    }
  }, []);

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

  async function handleReject(roadmap) {
    if (!roadmap?.roadmapVersionId || mutatingVersionId) return;

    setMutatingVersionId(roadmap.roadmapVersionId);
    setError("");

    try {
      await contentManagerRoadmapApi.rejectVersion(roadmap.roadmapVersionId);
      toast.success("Roadmap version returned for changes.");
      await loadReviews();
    } catch (requestError) {
      setError(getFriendlyApiErrorMessage(requestError, "Unable to reject roadmap version."));
    } finally {
      setMutatingVersionId("");
    }
  }

  function openReview(roadmap) {
    navigate(`/content/roadmaps/${roadmap.roadmapId}/edit?versionId=${roadmap.roadmapVersionId}&from=reviews`);
  }

  return (
    <ModulePageShell>
      <div className="mx-auto max-w-6xl space-y-5">
        <section className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
              Review
            </p>
            <h1 className="mt-1 text-3xl font-black text-[#18332D]">
              Roadmap Approvals
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
          <ModuleCard className="overflow-hidden p-0">
            <div className="divide-y divide-[#B9D8CC]/70">
              {items.map((roadmap) => {
                const isBusy = mutatingVersionId === roadmap.roadmapVersionId;
                const status = String(roadmap.status || "").toLowerCase();

                return (
                  <article
                    key={roadmap.roadmapVersionId}
                    className="grid gap-4 p-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center"
                  >
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <h2 className="truncate text-base font-extrabold text-[#18332D]">
                          {roadmap.title}
                        </h2>
                        <ModuleBadge tone={getStatusTone(status)}>
                          {prettyStatus(status)}
                        </ModuleBadge>
                        <ModuleBadge tone="slate">
                          {formatVersionLabel(roadmap)}
                        </ModuleBadge>
                      </div>

                      <div className="mt-2 grid gap-2 text-xs font-semibold text-slate-600 sm:grid-cols-3">
                        <span className="truncate">
                          Role: {roadmap.careerRole?.name || roadmap.slug || "-"}
                        </span>
                        <span>{roadmap.nodeCount || 0} nodes</span>
                        <span>Updated {formatDate(roadmap.updatedAt || roadmap.createdAt)}</span>
                      </div>
                    </div>

                    <div className="flex flex-wrap gap-2">
                      <ModuleButton variant="secondary" onClick={() => openReview(roadmap)} disabled={Boolean(mutatingVersionId)}>
                        <Eye size={14} />
                        Open
                      </ModuleButton>
                      <ModuleButton variant="danger" onClick={() => handleReject(roadmap)} disabled={Boolean(mutatingVersionId)}>
                        {isBusy ? <Loader2 size={14} className="animate-spin" /> : <XCircle size={14} />}
                        Reject
                      </ModuleButton>
                      <ModuleButton onClick={() => handleApprove(roadmap)} disabled={Boolean(mutatingVersionId)}>
                        {isBusy ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                        Approve
                      </ModuleButton>
                    </div>
                  </article>
                );
              })}
            </div>
          </ModuleCard>
        )}

      </div>
    </ModulePageShell>
  );
}
