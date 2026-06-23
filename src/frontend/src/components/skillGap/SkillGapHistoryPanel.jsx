/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  CalendarClock,
  Eye,
  History,
  Loader2,
  RefreshCw,
} from "lucide-react";

import { skillGapApi } from "../../api/skillGapApi";
import { PERMISSIONS } from "../../constants/permissions";
import { useAuthStore } from "../../stores/useAuthStore";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { hasPermission } from "../../utils/authorizationUtils";
import { getAssessmentLevelStyle, normalizeSkillGapResult, toArray } from "./skillGapUtils";

function formatDateTime(value) {
  if (!value) return "—";

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";

  return date.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function getNextAnalyzeDate(value) {
  if (!value) return "—";

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";

  date.setDate(date.getDate() + 7);
  return formatDateTime(date.toISOString());
}

function normalizeHistoryItem(item) {
  const historyId = item?.historyId ?? item?.HistoryId ?? item?.id ?? item?.Id ?? "";

  return {
    ...item,
    historyId: String(historyId || ""),
    careerRoleName: item?.careerRoleName ?? item?.CareerRoleName ?? "Selected role",
    levelName: item?.levelName ?? item?.LevelName ?? "Assessment level",
    matchedSkills: Number(item?.matchedSkills ?? item?.MatchedSkills ?? 0),
    totalSkills: Number(item?.totalSkills ?? item?.TotalSkills ?? 0),
    missingSkills: Number(item?.missingSkills ?? item?.MissingSkills ?? 0),
    createdAt: item?.createdAt ?? item?.CreatedAt ?? null,
  };
}

export default function SkillGapHistoryPanel({ onViewResult }) {
  const user = useAuthStore((state) => state.user);
  const canViewHistory = hasPermission(user, PERMISSIONS.SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF);
  const [historyItems, setHistoryItems] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [activeHistoryId, setActiveHistoryId] = useState("");
  const [error, setError] = useState("");

  const latestByRoleLevel = useMemo(() => {
    const map = new Map();

    historyItems.forEach((item) => {
      const key = `${item.careerRoleName}::${item.levelName}`;
      if (!map.has(key)) {
        map.set(key, item);
      }
    });

    return map;
  }, [historyItems]);

  const loadHistory = async () => {
    if (!canViewHistory) return;

    try {
      setIsLoading(true);
      setError("");
      const response = await skillGapApi.getHistory();
      setHistoryItems(toArray(response).map(normalizeHistoryItem).filter((item) => item.historyId));
    } catch (loadError) {
      setError(getFriendlyApiErrorMessage(loadError, "Unable to load skill gap history."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadHistory();
  }, [canViewHistory]);

  const openHistoryResult = async (historyId) => {
    if (!historyId) return;

    try {
      setActiveHistoryId(historyId);
      setError("");
      const detail = await skillGapApi.getHistoryDetail(historyId);
      const rawResult = detail?.result ?? detail?.Result;
      const normalizedResult = normalizeSkillGapResult(rawResult);

      onViewResult?.(normalizedResult);
    } catch (detailError) {
      setError(getFriendlyApiErrorMessage(detailError, "Unable to open this history report."));
    } finally {
      setActiveHistoryId("");
    }
  };

  if (!canViewHistory) {
    return (
      <section className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-6 text-center shadow-sm">
        <div className="mx-auto grid h-12 w-12 place-items-center rounded-2xl bg-[#F7F1E8] text-[#1F6F5F]">
          <History size={22} />
        </div>
        <h2 className="mt-4 text-xl font-extrabold text-[#18332D]">Skill gap history</h2>
        <p className="mx-auto mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
          Your account does not have permission to view previous skill gap reports yet.
        </p>
      </section>
    );
  }

  return (
    <section className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm lg:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F]">History</p>
          <h2 className="mt-1 text-2xl font-extrabold text-[#18332D]">Previous skill gap reports</h2>
        </div>

        <button
          type="button"
          onClick={loadHistory}
          disabled={isLoading}
          className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isLoading ? <Loader2 className="animate-spin" size={16} /> : <RefreshCw size={16} />}
          Refresh
        </button>
      </div>

      {error && (
        <div className="mt-5 flex items-start gap-3 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-semibold leading-6 text-rose-700">
          <AlertTriangle className="mt-0.5 shrink-0" size={18} />
          <span>{error}</span>
        </div>
      )}

      {isLoading ? (
        <div className="mt-6 grid min-h-60 place-items-center rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 text-sm font-bold text-slate-600">
          <div className="flex items-center gap-2">
            <Loader2 className="animate-spin text-[#2FA084]" size={20} />
            Loading saved reports...
          </div>
        </div>
      ) : historyItems.length === 0 ? (
        <div className="mt-6 rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-8 text-center">
          <History className="mx-auto text-[#2FA084]" size={24} />
          <h3 className="mt-3 text-sm font-extrabold text-[#18332D]">No saved reports yet</h3>
          <p className="mx-auto mt-2 max-w-lg text-sm font-semibold leading-6 text-slate-600">
            Completed reports will appear here.
          </p>
        </div>
      ) : (
        <div className="mt-6 grid max-h-[62vh] gap-3 overflow-y-auto pr-1">
          {historyItems.map((item) => {
            const latestForPair = latestByRoleLevel.get(`${item.careerRoleName}::${item.levelName}`)?.historyId === item.historyId;
            const levelStyle = getAssessmentLevelStyle(item.levelName);
            const skillText = `${item.matchedSkills}/${item.totalSkills} matched`;
            const opening = activeHistoryId === item.historyId;

            return (
              <article
                key={item.historyId}
                className="rounded-2xl border border-[#B9D8CC]/80 bg-white p-4 shadow-sm transition hover:border-[#2FA084] hover:shadow-md"
              >
                <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="text-sm font-extrabold text-[#18332D]">{item.careerRoleName}</h3>
                      <span className={`rounded-full border px-2.5 py-1 text-[11px] font-extrabold ${levelStyle.badge}`}>
                        {item.levelName}
                      </span>
                      {latestForPair && (
                        <span className="rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-2.5 py-1 text-[11px] font-extrabold text-[#1F6F5F]">
                          Latest
                        </span>
                      )}
                    </div>

                    <div className="mt-3 grid gap-2 text-xs font-bold text-slate-600 sm:grid-cols-3">
                      <span className="rounded-xl bg-[#F7F1E8]/75 px-3 py-2">{skillText}</span>
                      <span className="rounded-xl bg-[#F7F1E8]/75 px-3 py-2">{item.missingSkills} skills to learn</span>
                      <span className="inline-flex items-center gap-1.5 rounded-xl bg-[#F7F1E8]/75 px-3 py-2">
                        <CalendarClock size={13} />
                        {formatDateTime(item.createdAt)}
                      </span>
                    </div>

                    {latestForPair && (
                      <p className="mt-2 text-[11px] font-semibold text-slate-500">
                        Next analyze: {getNextAnalyzeDate(item.createdAt)}.
                      </p>
                    )}
                  </div>

                  <button
                    type="button"
                    onClick={() => openHistoryResult(item.historyId)}
                    disabled={opening}
                    className="inline-flex shrink-0 items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600 disabled:hover:translate-y-0"
                  >
                    {opening ? <Loader2 className="animate-spin" size={16} /> : <Eye size={16} />}
                    View report
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}
