import {
  ArrowRight,
  Check,
  GraduationCap,
  Layers3,
  Loader2,
  Target,
} from "lucide-react";
import { getAssessmentLevelStyle } from "./skillGapUtils";

export default function SkillGapRoleStep({
  roles,
  levels,
  selectedRole,
  selectedLevel,
  isLoading,
  isLoadingLevels,
  isLoadingGroups,
  onSelectRole,
  onSelectLevel,
  onNext,
}) {
  return (
    <section className="rounded-2xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F]">Step 1</p>
          <h2 className="mt-1 text-xl font-extrabold text-[#18332D]">Choose target role and assessment level</h2>
        </div>
        <Target className="hidden text-[#2FA084] sm:block" size={30} />
      </div>

      {isLoading ? (
        <div className="mt-6 grid min-h-48 place-items-center rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 text-sm font-bold text-slate-600">
          <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={22} />
          Loading career roles...
        </div>
      ) : roles.length === 0 ? (
        <div className="mt-6 rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-8 text-center text-sm font-bold text-slate-600">
          No career roles were returned by the backend. Check seed data for CareerRoles.
        </div>
      ) : (
        <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {roles.map((role) => {
            const isSelected = selectedRole?.slug === role.slug;

            return (
              <button
                key={role.careerRoleId || role.slug}
                type="button"
                onClick={() => onSelectRole(role)}
                className={`group rounded-2xl border p-4 text-left transition hover:-translate-y-0.5 hover:shadow-md ${
                  isSelected
                    ? "border-[#2FA084] bg-[#6FCF97]/20 shadow-sm"
                    : "border-[#B9D8CC]/75 bg-white hover:border-[#2FA084]"
                }`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="grid h-10 w-10 place-items-center rounded-xl bg-[#F7F1E8] text-[#1F6F5F] transition group-hover:bg-[#6FCF97]/20">
                    <GraduationCap size={20} />
                  </div>
                  {isSelected && (
                    <span className="grid h-6 w-6 place-items-center rounded-full bg-[#2FA084] text-white">
                      <Check size={14} />
                    </span>
                  )}
                </div>
                <h3 className="mt-4 text-sm font-extrabold text-[#18332D]">{role.name}</h3>
              </button>
            );
          })}
        </div>
      )}

      {selectedRole && (
        <div className="mt-6 rounded-2xl border border-[#B9D8CC]/80 bg-[#F7F1E8]/55 p-4">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <div className="flex items-center gap-2 text-sm font-extrabold text-[#18332D]">
                <Layers3 size={17} className="text-[#2FA084]" />
                Assessment level
              </div>
            </div>
            {isLoadingLevels && (
              <span className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-1 text-xs font-extrabold text-[#1F6F5F]">
                <Loader2 className="animate-spin" size={13} /> Loading levels
              </span>
            )}
          </div>

          {!isLoadingLevels && levels.length === 0 ? (
            <div className="mt-4 rounded-xl border border-dashed border-[#B9D8CC] bg-white/70 p-4 text-sm font-bold text-slate-600">
              No assessment levels were configured for this role yet.
            </div>
          ) : (
            <div className="mt-4 grid gap-3 sm:grid-cols-3">
              {levels.map((level) => {
                const isSelected = selectedLevel?.slug === level.slug;
                const style = getAssessmentLevelStyle(level.slug);

                return (
                  <button
                    key={level.levelId || level.slug}
                    type="button"
                    onClick={() => onSelectLevel(level)}
                    disabled={isLoadingLevels}
                    className={`rounded-2xl border p-4 text-left transition hover:-translate-y-0.5 hover:shadow-sm disabled:cursor-not-allowed disabled:opacity-60 ${
                      isSelected
                        ? `${style.panel} border-[#2FA084] shadow-sm`
                        : "border-[#B9D8CC]/80 bg-white hover:border-[#2FA084]"
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <span className={`rounded-full border px-2.5 py-1 text-[11px] font-extrabold ${style.badge}`}>
                        {level.levelName}
                      </span>
                      {isSelected && (
                        <span className="grid h-6 w-6 place-items-center rounded-full bg-[#2FA084] text-white">
                          <Check size={14} />
                        </span>
                      )}
                    </div>
                    <p className="mt-3 text-xs font-bold leading-5 text-slate-600">
                      {level.groupCount === null || level.groupCount === undefined
                        ? "Groups will be loaded after selection."
                        : `${level.groupCount} configured groups`}
                    </p>
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}

      <div className="mt-6 flex justify-end">
        <button
          type="button"
          disabled={!selectedRole || !selectedLevel || isLoading || isLoadingLevels || isLoadingGroups}
          onClick={onNext}
          className="inline-flex items-center gap-2 rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600 disabled:hover:translate-y-0"
        >
          {isLoadingGroups ? <Loader2 className="animate-spin" size={16} /> : null}
          Continue to skills <ArrowRight size={16} />
        </button>
      </div>
    </section>
  );
}
