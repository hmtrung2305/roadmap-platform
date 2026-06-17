import { ArrowRight, Check, GraduationCap, Loader2, Target } from "lucide-react";

export default function SkillGapRoleStep({
  roles,
  selectedRole,
  isLoading,
  isLoadingGroups,
  onSelectRole,
  onNext,
}) {
  return (
    <section className="rounded-2xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F]">Step 1</p>
          <h2 className="mt-1 text-xl font-extrabold text-[#18332D]">Choose your target career role</h2>
          <p className="mt-2 text-sm font-semibold leading-6 text-slate-600">
            The role decides which skill groups the backend will use for the assessment.
          </p>
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

      <div className="mt-6 flex justify-end">
        <button
          type="button"
          disabled={!selectedRole || isLoading || isLoadingGroups}
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
