import { useEffect, useMemo, useState } from "react";
import {
  ArrowLeft,
  BarChart3,
  Check,
  CheckCircle2,
  ChevronDown,
  Loader2,
} from "lucide-react";
import {
  getPriorityStyle,
  getRuleDescription,
  isGroupCompleted,
  toArray,
} from "./skillGapUtils";

export default function SkillGapSkillsStep({
  role,
  groups,
  selectedSkillSlugs,
  isLoading,
  isAnalyzing,
  onToggleSkill,
  onBack,
  onAnalyze,
}) {
  const [expandedGroupKey, setExpandedGroupKey] = useState("");
  const selectedSet = useMemo(() => new Set(selectedSkillSlugs), [selectedSkillSlugs]);

  useEffect(() => {
    if (groups.length === 0) {
      setExpandedGroupKey("");
      return;
    }

    const firstGroup = groups[0];
    setExpandedGroupKey(String(firstGroup.skillGroupId || firstGroup.groupName));
  }, [groups]);

  const toggleGroup = (groupKey) => {
    setExpandedGroupKey((current) => (current === groupKey ? "" : groupKey));
  };

  return (
    <section className="rounded-2xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F]">Step 2</p>
          <h2 className="mt-1 text-xl font-extrabold text-[#18332D]">Mark the skills you already have</h2>
          <p className="mt-2 text-sm font-semibold leading-6 text-slate-600">
            Target role: <span className="text-[#1F6F5F]">{role?.name}</span>. Open one group at a time so the checklist stays focused.
          </p>
        </div>
        <div className="rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-1.5 text-xs font-extrabold text-[#1F6F5F]">
          {selectedSkillSlugs.length} skills marked
        </div>
      </div>

      {isLoading ? (
        <div className="mt-6 grid min-h-64 place-items-center rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 text-sm font-bold text-slate-600">
          <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={22} />
          Loading assessment skills...
        </div>
      ) : (
        <div className="mt-6 grid gap-3">
          {groups.map((group) => {
            const groupKey = String(group.skillGroupId || group.groupName);
            const skills = toArray(group.skills);
            const matchedCount = skills.filter((skill) => selectedSet.has(skill.slug)).length;
            const total = skills.length;
            const percent = total ? Math.round((matchedCount / total) * 100) : 0;
            const isCompleted = isGroupCompleted(group, selectedSkillSlugs);
            const isExpanded = expandedGroupKey === groupKey;
            const priority = getPriorityStyle(group.priority);

            return (
              <article
                key={groupKey}
                className={`overflow-hidden rounded-2xl border bg-white shadow-sm transition ${
                  isCompleted ? "border-[#2FA084] bg-[#6FCF97]/10" : "border-[#B9D8CC]/80"
                }`}
              >
                <button
                  type="button"
                  onClick={() => toggleGroup(groupKey)}
                  className="flex w-full items-start justify-between gap-4 p-4 text-left transition hover:bg-[#F7F1E8]/50"
                  aria-expanded={isExpanded}
                >
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className={`h-2.5 w-2.5 rounded-full ${priority.dot}`} />
                      <h3 className="text-sm font-extrabold text-[#18332D]">{group.groupName}</h3>
                      {isCompleted && <CheckCircle2 className="text-[#2FA084]" size={16} />}
                    </div>
                    <p className="mt-1 text-xs font-semibold leading-5 text-slate-500">
                      {group.requirementDescription || getRuleDescription(group)}
                    </p>
                  </div>

                  <div className="flex shrink-0 items-center gap-2">
                    <span className="rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-2.5 py-1 text-[11px] font-extrabold text-[#1F6F5F]">
                      {matchedCount}/{total} selected
                    </span>
                    <ChevronDown
                      size={18}
                      className={`text-slate-500 transition ${isExpanded ? "rotate-180" : ""}`}
                    />
                  </div>
                </button>

                <div className="px-4 pb-3">
                  <div className="h-2 overflow-hidden rounded-full bg-[#E4ECE8]">
                    <div className={`h-full rounded-full transition-all ${priority.bar}`} style={{ width: `${percent}%` }} />
                  </div>
                </div>

                <div
                  className={`grid transition-all duration-300 ease-out ${
                    isExpanded ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0"
                  }`}
                >
                  <div className="overflow-hidden">
                    <div className="flex flex-wrap gap-2 border-t border-slate-100 px-4 py-4">
                      {skills.map((skill) => {
                        const selected = selectedSet.has(skill.slug);

                        return (
                          <button
                            key={skill.skillId || skill.slug}
                            type="button"
                            onClick={() => onToggleSkill(skill.slug)}
                            className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-bold transition hover:-translate-y-0.5 ${
                              selected
                                ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                                : "border-slate-200 bg-slate-50 text-slate-600 hover:border-[#2FA084] hover:bg-white"
                            }`}
                          >
                            {selected && <Check size={12} />}
                            {skill.name}
                          </button>
                        );
                      })}
                    </div>
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      )}

      <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
        <button
          type="button"
          onClick={onBack}
          className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8]"
        >
          <ArrowLeft size={16} /> Back to roles
        </button>
        <button
          type="button"
          disabled={isLoading || isAnalyzing || groups.length === 0}
          onClick={onAnalyze}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600 disabled:hover:translate-y-0"
        >
          {isAnalyzing ? <Loader2 className="animate-spin" size={16} /> : <BarChart3 size={16} />}
          Analyze gap
        </button>
      </div>
    </section>
  );
}
