import { useMemo } from "react";
import { CheckCircle2, Target } from "lucide-react";
import {
  getAssessmentLevelStyle,
  isGroupCompleted,
  normalizeId,
  toArray,
} from "../utils/skillGapUtils";

function SnapshotMetric({ label, value, align = "center" }) {
  return (
    <div
      className={`rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/70 px-3 py-3 ${align === "left" ? "text-left" : "text-center"}`}
    >
      <div className="truncate text-sm font-extrabold text-[#18332D]">
        {value}
      </div>
      <div className="mt-0.5 text-[10px] font-extrabold uppercase tracking-wide text-slate-500">
        {label}
      </div>
    </div>
  );
}

export default function SkillGapInsightPanel({
  step,
  roles,
  selectedRole,
  selectedLevel,
  groups,
  selectedNodeIds,
  result,
}) {
  const selectedSet = useMemo(
    () => new Set(toArray(selectedNodeIds).map(normalizeId)),
    [selectedNodeIds],
  );
  const resultGroups = toArray(result?.groups);
  const checklistGroups =
    step === 3 && resultGroups.length > 0 ? resultGroups : groups;
  const completedPreview = groups.filter((group) =>
    isGroupCompleted(group, selectedNodeIds),
  ).length;
  const completedGroups =
    step === 3 ? (result?.completedGroups ?? 0) : completedPreview;
  const totalGroups =
    step === 3 ? (result?.totalGroups ?? resultGroups.length) : groups.length;
  const levelStyle = getAssessmentLevelStyle(
    selectedLevel?.slug || result?.levelSlug,
  );

  return (
    <aside className="grid gap-4 self-start lg:sticky lg:top-24">
      <div className="rounded-2xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm">
        <div className="flex items-center gap-2 text-sm font-extrabold text-[#18332D]">
          <Target className="text-[#2FA084]" size={18} />
          Assessment snapshot
        </div>

        <div
          className={`mt-4 grid gap-2 ${selectedRole ? "grid-cols-[minmax(0,1.35fr)_0.8fr_0.8fr]" : "grid-cols-3"}`}
        >
          {selectedRole ? (
            <SnapshotMetric
              label="Role"
              value={
                selectedRole.name || result?.careerRoleName || "Selected role"
              }
              align="left"
            />
          ) : (
            <SnapshotMetric
              label="Available roles"
              value={roles.length || "—"}
            />
          )}
          <SnapshotMetric
            label="Skill groups"
            value={selectedRole ? totalGroups || 0 : "After role"}
          />
          <SnapshotMetric
            label="Marked skills"
            value={selectedNodeIds.length}
          />
        </div>

        <div className="mt-4 grid gap-3 rounded-2xl bg-[#F7F1E8]/80 p-4">
          <div className="flex items-center justify-between gap-3">
            <span className="text-xs font-extrabold uppercase tracking-wide text-slate-500">
              Assessment level
            </span>
            <span
              className={`rounded-full border px-2.5 py-1 text-xs font-extrabold ${levelStyle.badge}`}
            >
              {selectedLevel?.levelName || result?.levelName || "Not selected"}
            </span>
          </div>
          <div className="flex items-center justify-between gap-3">
            <span className="text-xs font-extrabold uppercase tracking-wide text-slate-500">
              Groups completed
            </span>
            <span className="rounded-full bg-white px-2.5 py-1 text-xs font-extrabold text-[#1F6F5F]">
              {completedGroups}/{totalGroups || 0}
            </span>
          </div>
        </div>
      </div>

      {step === 3 && checklistGroups.length > 0 && (
        <div className="rounded-2xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm">
          <div className="flex items-center justify-between gap-3">
            <div className="flex items-center gap-2 text-sm font-extrabold text-[#18332D]">
              <CheckCircle2 className="text-[#2FA084]" size={18} />
              Group checklist
            </div>
            <span className="rounded-full bg-[#F7F1E8] px-2.5 py-1 text-[10px] font-extrabold uppercase tracking-wide text-[#1F6F5F]">
              Level groups
            </span>
          </div>

          <div className="mt-4 grid max-h-[48vh] gap-2 overflow-y-auto pr-1">
            {checklistGroups.map((group) => {
              const total =
                group.totalSkillCount ?? toArray(group.skills).length;
              const selectedCount =
                group.matchedSkillCount ??
                toArray(group.skills).filter((skill) =>
                  selectedSet.has(
                    normalizeId(skill.nodeId || skill.skillId || skill.slug),
                  ),
                ).length;
              const done =
                group.isCompleted ?? isGroupCompleted(group, selectedNodeIds);

              return (
                <div
                  key={group.skillGroupId || group.groupName}
                  className="flex items-center justify-between gap-3 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2"
                >
                  <span className="min-w-0 flex-1 truncate text-xs font-bold text-slate-700">
                    {group.groupName}
                  </span>
                  <span
                    className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-extrabold ${done ? "bg-[#6FCF97]/20 text-[#1F6F5F]" : "bg-white text-slate-500"}`}
                  >
                    {selectedCount}/{total}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </aside>
  );
}
