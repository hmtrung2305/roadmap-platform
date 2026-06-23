/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, CheckCircle2, ChevronDown, RefreshCw } from "lucide-react";
import {
  getAssessmentLevelStyle,
  getPriorityNumber,
  getPriorityStyle,
  getRuleDescription,
  toArray,
} from "./skillGapUtils";

function ResultMetric({ label, value }) {
  return (
    <div className="rounded-xl border border-[#B9D8CC]/80 bg-[#F7F1E8]/60 px-4 py-3 text-center">
      <div className="text-xl font-extrabold leading-tight text-[#18332D]">{value}</div>
      <div className="mt-1 text-[10px] font-extrabold uppercase tracking-wide text-slate-500">{label}</div>
    </div>
  );
}

function getSkillLabel(item) {
  if (typeof item === "string") return item;
  return item?.name || item?.Name || item?.title || item?.Title || item?.slug || item?.Slug || "Unnamed skill";
}

function getSkillKey(item, index) {
  if (typeof item === "string") return `${item}-${index}`;
  return item?.nodeId || item?.NodeId || item?.skillId || item?.SkillId || item?.id || item?.Id || item?.slug || item?.Slug || `${getSkillLabel(item)}-${index}`;
}

function SkillPills({ title, items, empty, soft = false, countFallback = 0 }) {
  const values = toArray(items);

  return (
    <div className="border-t border-slate-100 pt-3">
      <p className="text-[11px] font-extrabold uppercase tracking-wide text-slate-500">{title}</p>
      <div className="mt-2 flex flex-wrap gap-1.5">
        {values.length > 0 ? (
          values.map((item, index) => (
            <span
              key={getSkillKey(item, index)}
              className={`rounded-full border px-2.5 py-1 text-[11px] font-bold ${
                soft
                  ? "border-[#B9D8CC] bg-[#F7F1E8]/80 text-slate-700"
                  : "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
              }`}
            >
              {getSkillLabel(item)}
            </span>
          ))
        ) : countFallback > 0 ? (
          <span className="text-xs font-semibold text-slate-400">
            {countFallback} matched skills saved without detail names
          </span>
        ) : (
          <span className="text-xs font-semibold text-slate-400">{empty}</span>
        )}
      </div>
    </div>
  );
}

function RequirementComparisonSummary({ result, groups, completedGroups, totalGroups }) {
  const totalMatchedSkills = Number(result?.matchedSkills ?? groups.reduce((sum, group) => sum + Number(group.matchedSkillCount || 0), 0));
  const totalRequiredSkills = Number(result?.totalSkills ?? groups.reduce((sum, group) => sum + Number(group.totalSkillCount || 0), 0));
  const totalSuggestedSkills = Number(result?.missingSkills ?? groups.reduce((sum, group) => sum + toArray(group.suggestedSkills).length, 0));
  const matchedWidth = totalRequiredSkills ? Math.min((totalMatchedSkills / totalRequiredSkills) * 100, 100) : 0;
  const missingWidth = totalRequiredSkills ? Math.max(100 - matchedWidth, 0) : 0;
  const levelStyle = getAssessmentLevelStyle(result?.levelSlug || result?.levelName);

  return (
    <div className="mt-6 rounded-3xl border border-[#B9D8CC]/80 bg-white p-5 shadow-sm">
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_260px]">
        <div>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <h3 className="text-sm font-extrabold uppercase tracking-wide text-[#18332D]">
              Current skills vs level scope
            </h3>
            <span className={`inline-flex w-fit rounded-full border px-3 py-1 text-xs font-extrabold ${levelStyle.badge}`}>
              {result?.levelName || "Assessment level"}
            </span>
          </div>

          <div className="mt-4 rounded-2xl border border-slate-100 bg-[#F7F1E8]/60 p-4">
            <div className="flex items-center justify-between gap-3">
              <span className="text-xs font-extrabold uppercase tracking-wide text-slate-500">Skill coverage</span>
              <span className="text-sm font-extrabold text-[#18332D]">
                {totalMatchedSkills} of {totalRequiredSkills} skills matched
              </span>
            </div>

            <div className="mt-3 flex h-4 overflow-hidden rounded-full bg-[#E4ECE8]">
              <div
                className="h-full rounded-l-full bg-[#2FA084] transition-all"
                style={{ width: `${matchedWidth}%` }}
                title="Matched skills"
              />
              <div
                className="h-full bg-[#D9C7AA] transition-all"
                style={{ width: `${missingWidth}%` }}
                title="Missing skills"
              />
            </div>

            <div className="mt-3 flex flex-wrap gap-3 text-xs font-bold text-slate-600">
              <span className="inline-flex items-center gap-1.5">
                <span className="h-2.5 w-2.5 rounded-full bg-[#2FA084]" />
                Matched
              </span>
              <span className="inline-flex items-center gap-1.5">
                <span className="h-2.5 w-2.5 rounded-full bg-[#D9C7AA]" />
                Suggested next
              </span>
            </div>
          </div>
        </div>

        <div className="grid gap-2 sm:grid-cols-3 xl:grid-cols-1">
          <ResultMetric label="Level" value={result?.levelName || "—"} />
          <ResultMetric label="Groups" value={`${completedGroups}/${totalGroups}`} />
          <ResultMetric label="Skills to learn" value={totalSuggestedSkills} />
        </div>
      </div>
    </div>
  );
}

export default function SkillGapResultStep({
  result,
  selectedSkillCount = 0,
  canUpdateSelection = true,
  isHistoryView = false,
  onBack,
  onBackToHistory,
  onReset,
}) {
  const groups = useMemo(
    () =>
      toArray(result?.groups)
        .slice()
        .sort((a, b) => {
          if (a.isCompleted !== b.isCompleted) return a.isCompleted ? 1 : -1;
          return (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || getPriorityNumber(a.learningPriority) - getPriorityNumber(b.learningPriority);
        }),
    [result?.groups],
  );

  const [expandedGroupKey, setExpandedGroupKey] = useState("");

  useEffect(() => {
    const firstMissingGroup = groups.find((group) => !group.isCompleted) || groups[0];
    setExpandedGroupKey(firstMissingGroup ? String(firstMissingGroup.skillGroupId || firstMissingGroup.groupName) : "");
  }, [groups]);

  const completedGroups = result?.completedGroups ?? 0;
  const totalGroups = result?.totalGroups ?? groups.length;
  const suggestedSkillCount = result?.missingSkills ?? groups.reduce((sum, group) => sum + toArray(group.suggestedSkills).length, 0);

  return (
    <section className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm lg:p-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F]">
            {isHistoryView ? "History detail" : "Step 3"}
          </p>
          <h2 className="mt-1 text-2xl font-extrabold text-[#18332D]">
            {isHistoryView ? "Saved skill gap report" : "Your skill gap report"}
          </h2>
          <p className="mt-2 text-sm font-semibold leading-6 text-slate-600">
            {result?.careerRoleName} · {result?.levelName}
          </p>
        </div>
      </div>

      <RequirementComparisonSummary
        result={result}
        groups={groups}
        completedGroups={completedGroups}
        totalGroups={totalGroups}
      />

      <div className="mt-6">
        <div className="flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h3 className="text-sm font-extrabold uppercase tracking-wide text-[#18332D]">Group details</h3>
            <p className="mt-1 text-xs font-semibold text-slate-500">
              Review what you already have and what to learn next.
            </p>
          </div>
          <span className="text-xs font-extrabold text-[#1F6F5F]">
            {canUpdateSelection
              ? `${selectedSkillCount} skills marked · ${suggestedSkillCount} suggested next`
              : `${result?.matchedSkills ?? 0} matched · ${suggestedSkillCount} suggested next`}
          </span>
        </div>

        <div className="mt-3 grid gap-3">
          {groups.map((group) => {
            const groupKey = String(group.skillGroupId || group.groupName);
            const isExpanded = expandedGroupKey === groupKey;
            const priority = getPriorityStyle(group.learningPriority);
            const matched = Number(group.matchedSkillCount ?? group.matchedSkills ?? 0);
            const total = Number(group.totalSkillCount ?? group.totalSkills ?? 0);
            const groupFill = total ? Math.min((matched / total) * 100, 100) : 0;
            const alreadyHaveSkills = toArray(group.alreadyHaveSkills).length > 0
              ? group.alreadyHaveSkills
              : group.matchedSkillItems;
            const suggestedNextSkills = toArray(group.suggestedSkills).length > 0
              ? group.suggestedSkills
              : group.missingSkills;

            return (
              <article
                key={groupKey}
                className={`overflow-hidden rounded-2xl border transition ${
                  group.isCompleted ? "border-[#2FA084] bg-[#6FCF97]/10" : "border-slate-200 bg-white"
                }`}
              >
                <button
                  type="button"
                  onClick={() => setExpandedGroupKey((current) => (current === groupKey ? "" : groupKey))}
                  className="flex w-full items-start justify-between gap-4 p-4 text-left transition hover:bg-[#F7F1E8]/50"
                  aria-expanded={isExpanded}
                >
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <h4 className="text-sm font-extrabold text-[#18332D]">{group.groupName}</h4>
                      {group.isCompleted && <CheckCircle2 className="text-[#2FA084]" size={16} />}
                    </div>
                    <p className="mt-1 text-xs font-bold text-slate-500">
                      {matched}/{total} skills matched · {getRuleDescription(group)}
                    </p>
                    <div className="mt-3 h-2 max-w-4xl overflow-hidden rounded-full bg-[#E4ECE8]">
                      <div className={`h-full rounded-full transition-all ${priority.bar}`} style={{ width: `${groupFill}%` }} />
                    </div>
                  </div>

                  <ChevronDown
                    size={18}
                    className={`mt-1 shrink-0 text-slate-500 transition ${isExpanded ? "rotate-180" : ""}`}
                  />
                </button>

                <div
                  className={`grid transition-all duration-300 ease-out ${
                    isExpanded ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0"
                  }`}
                >
                  <div className="overflow-hidden">
                    <div className="grid gap-4 border-t border-slate-100 px-4 py-4 md:grid-cols-2">
                      <SkillPills
                        title="Already have"
                        items={alreadyHaveSkills}
                        empty="No selected skills in this group"
                        countFallback={matched}
                      />
                      <SkillPills title="Suggested next" items={suggestedNextSkills} empty="No missing skills" soft />
                    </div>
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      </div>

      <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
        {isHistoryView ? (
          <button
            type="button"
            onClick={onBackToHistory}
            className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8]"
          >
            <ArrowLeft size={16} /> Back to history
          </button>
        ) : canUpdateSelection ? (
          <button
            type="button"
            onClick={onBack}
            className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8]"
          >
            <ArrowLeft size={16} /> Update selected skills
          </button>
        ) : (
          <div />
        )}
        <button
          type="button"
          onClick={onReset}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
        >
          <RefreshCw size={16} /> Analyze another role
        </button>
      </div>
    </section>
  );
}
