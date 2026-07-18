import { useCallback, useEffect, useRef, useState } from "react";
import { ChevronDown, Eye, History, Loader2, Trash2 } from "lucide-react";
import { skillGapApi } from "../../../api/skillGapApi";
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";
import {
  formatDateTime,
  normalizeSkillGapHistory,
  normalizeSkillGapResult,
} from "../utils/skillGapUtils";

const HISTORY_PAGE_SIZE = 20;

export default function SkillGapHistoryPanel({ onViewResult }) {
  const [items, setItems] = useState([]);
  const [nextCursor, setNextCursor] = useState(null);
  const [hasMore, setHasMore] = useState(false);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [busyId, setBusyId] = useState("");
  const [error, setError] = useState("");
  const historyRequestVersionRef = useRef(0);

  const loadHistory = useCallback(async ({ cursor = null, append = false } = {}) => {
    const requestVersion = ++historyRequestVersionRef.current;

    if (append) {
      setIsLoadingMore(true);
    } else {
      setIsInitialLoading(true);
      setIsLoadingMore(false);
    }

    setError("");

    try {
      const page = await skillGapApi.getHistory({
        limit: HISTORY_PAGE_SIZE,
        cursor,
      });

      if (requestVersion !== historyRequestVersionRef.current) return;

      const normalizedItems = normalizeSkillGapHistory(page.items);

      setItems((current) => {
        if (!append) return normalizedItems;

        const itemsById = new Map(
          [...current, ...normalizedItems].map((item) => [
            item.skillGapAnalysisHistoryId,
            item,
          ]),
        );

        return [...itemsById.values()];
      });
      setNextCursor(page.nextCursor);
      setHasMore(page.hasMore);
    } catch (err) {
      if (requestVersion !== historyRequestVersionRef.current) return;

      setError(getFriendlyApiErrorMessage(err, "Unable to load skill gap history."));
    } finally {
      if (requestVersion === historyRequestVersionRef.current) {
        setIsInitialLoading(false);
        setIsLoadingMore(false);
      }
    }
  }, []);

  useEffect(() => {
    loadHistory();
  }, [loadHistory]);

  const handleRefresh = () => {
    setNextCursor(null);
    setHasMore(false);
    loadHistory();
  };

  const handleLoadMore = () => {
    if (!hasMore || !nextCursor || isLoadingMore) return;

    loadHistory({
      cursor: nextCursor,
      append: true,
    });
  };

  const handleView = async (historyId) => {
    if (!historyId) return;

    setBusyId(historyId);
    setError("");

    try {
      const detail = await skillGapApi.getHistoryDetail(historyId);
      onViewResult(normalizeSkillGapResult(detail));
    } catch (err) {
      setError(getFriendlyApiErrorMessage(err, "Unable to open this history item."));
    } finally {
      setBusyId("");
    }
  };

  const handleDelete = async (historyId) => {
    if (!historyId) return;

    const confirmed = window.confirm("Delete this skill gap history item?");
    if (!confirmed) return;

    setBusyId(historyId);
    setError("");

    try {
      await skillGapApi.deleteHistory(historyId);
      setItems((current) => current.filter((item) => item.skillGapAnalysisHistoryId !== historyId));
    } catch (err) {
      setError(getFriendlyApiErrorMessage(err, "Unable to delete this history item."));
    } finally {
      setBusyId("");
    }
  };

  return (
    <section className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm sm:p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
            <History size={14} /> History
          </p>
          <h2 className="mt-3 text-2xl font-black text-[#18332D]">Your saved analyses</h2>
          <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
            Each item is a snapshot of the roadmap skill checklist at the time you analyzed it.
          </p>
        </div>
        <button
          type="button"
          onClick={handleRefresh}
          disabled={isInitialLoading || isLoadingMore}
          className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isInitialLoading ? <Loader2 className="animate-spin" size={16} /> : null}
          Refresh
        </button>
      </div>

      {error && (
        <div className="mt-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
          {error}
        </div>
      )}

      {isInitialLoading ? (
        <div className="mt-6 grid place-items-center rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 py-14 text-sm font-bold text-slate-500">
          <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={24} /> Loading history...
        </div>
      ) : items.length === 0 ? (
        <div className="mt-6 rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 p-8 text-center">
          <p className="text-sm font-extrabold text-[#18332D]">No history yet</p>
          <p className="mt-1 text-xs font-semibold text-slate-500">
            Run a new analysis and it will appear here.
          </p>
        </div>
      ) : (
        <div className="mt-6">
          <div className="space-y-3">
            {items.map((item) => {
              const historyId = item.skillGapAnalysisHistoryId;
              const isBusy = busyId === historyId;

              return (
                <article key={historyId} className="rounded-2xl border border-[#B9D8CC]/80 bg-white p-4 shadow-sm">
                  <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                    <div className="min-w-0">
                      <h3 className="truncate text-base font-black text-[#18332D]">{item.roadmapTitle}</h3>
                      <p className="mt-1 text-xs font-semibold text-slate-500">
                        {item.careerRoleName || "Career role"}
                        {item.authorName ? ` · by ${item.authorName}` : ""}
                        {` · ${formatDateTime(item.createdAt)}`}
                      </p>
                      <div className="mt-3 flex flex-wrap gap-2 text-xs font-extrabold">
                        <span className="rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-[#1F6F5F]">
                          {item.matchedSkills} have
                        </span>
                        <span className="rounded-full border border-[#F1D9A8] bg-[#FFF7E6] px-3 py-1 text-[#8A5A12]">
                          {item.missingSkills} missing
                        </span>
                        <span className="rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-slate-600">
                          {item.totalSkills} total
                        </span>
                      </div>
                    </div>

                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => handleView(historyId)}
                        disabled={isBusy}
                        className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2 text-xs font-extrabold text-white transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:hover:translate-y-0"
                      >
                        {isBusy ? <Loader2 className="animate-spin" size={14} /> : <Eye size={14} />}
                        View
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(historyId)}
                        disabled={isBusy}
                        className="inline-flex items-center justify-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-xs font-extrabold text-red-700 transition hover:-translate-y-0.5 hover:bg-red-100 disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
                      >
                        <Trash2 size={14} /> Delete
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>

          {hasMore && (
            <div className="mt-5 flex justify-center">
              <button
                type="button"
                onClick={handleLoadMore}
                disabled={isLoadingMore || !nextCursor}
                className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-5 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#EAF7F1] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isLoadingMore ? (
                  <Loader2 className="animate-spin" size={16} />
                ) : (
                  <ChevronDown size={16} />
                )}
                Load more
              </button>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
