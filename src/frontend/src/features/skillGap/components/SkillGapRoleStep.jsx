import { BookOpenCheck, BriefcaseBusiness, Loader2, Map, UserRoundCheck } from "lucide-react";

function EmptyState({ title, description }) {
  return (
    <div className="rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-6 text-center">
      <p className="text-sm font-extrabold text-[#18332D]">{title}</p>
      <p className="mt-1 text-xs font-semibold text-slate-500">{description}</p>
    </div>
  );
}

export default function SkillGapRoleStep({
  roles,
  roadmaps,
  selectedRole,
  selectedRoadmap,
  isLoading,
  isLoadingRoadmaps,
  isLoadingAssessment,
  onSelectRole,
  onSelectRoadmap,
  onNext,
}) {
  const canContinue = Boolean(selectedRole?.slug && selectedRoadmap?.roadmapId);

  return (
    <section className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm sm:p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
            <UserRoundCheck size={14} /> Step 1
          </p>
          <h2 className="mt-3 text-2xl font-black text-[#18332D]">Choose your target roadmap</h2>
        </div>
      </div>

      <div className="mt-6 grid gap-5 lg:grid-cols-2">
        <div className="flex min-h-0 flex-col rounded-2xl border border-[#B9D8CC]/80 bg-[#F7F1E8]/50 p-4 lg:h-[25rem]">
          <div className="mb-3 flex shrink-0 items-center gap-2 text-sm font-extrabold text-[#18332D]">
            <BriefcaseBusiness size={17} className="text-[#2FA084]" /> Career role
          </div>

          <div className="skill-gap-selector-scroll min-h-0 flex-1 p-1">
            {isLoading ? (
              <div className="grid place-items-center rounded-xl border border-dashed border-[#B9D8CC] bg-white/70 py-10 text-sm font-bold text-slate-500">
                <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={22} /> Loading roles...
              </div>
            ) : roles.length === 0 ? (
              <EmptyState title="No career roles available" description="The backend did not return any role for skill gap analysis." />
            ) : (
              <div className="grid gap-2">
                {roles.map((role) => {
                  const active = selectedRole?.slug === role.slug;

                  return (
                    <button
                      key={role.careerRoleId || role.slug}
                      type="button"
                      onClick={() => onSelectRole(role)}
                      className={`rounded-xl border px-4 py-3 text-left transition hover:-translate-y-0.5 ${
                        active
                          ? "border-[#2FA084] bg-[#6FCF97]/20 shadow-sm"
                          : "border-slate-200 bg-white hover:border-[#2FA084] hover:bg-[#EAF7F1]/70"
                      }`}
                    >
                      <p className="text-sm font-extrabold text-[#18332D]">{role.name}</p>
                      <p className="mt-1 text-xs font-semibold text-slate-500">{role.slug}</p>
                    </button>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        <div className="flex min-h-0 flex-col rounded-2xl border border-[#B9D8CC]/80 bg-[#F7F1E8]/50 p-4 lg:h-[25rem]">
          <div className="mb-3 flex shrink-0 items-center gap-2 text-sm font-extrabold text-[#18332D]">
            <Map size={17} className="text-[#2FA084]" /> Published roadmap
          </div>

          <div className="skill-gap-selector-scroll min-h-0 flex-1 p-1">
            {!selectedRole ? (
              <EmptyState title="Choose a role first" description="Published roadmaps will appear after a career role is selected." />
            ) : isLoadingRoadmaps ? (
              <div className="grid place-items-center rounded-xl border border-dashed border-[#B9D8CC] bg-white/70 py-10 text-sm font-bold text-slate-500">
                <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={22} /> Loading roadmaps...
              </div>
            ) : roadmaps.length === 0 ? (
              <EmptyState title="No published roadmaps" description="This role does not have a published roadmap ready for analysis yet." />
            ) : (
              <div className="grid gap-2">
                {roadmaps.map((roadmap) => {
                  const active = selectedRoadmap?.roadmapId === roadmap.roadmapId;

                  return (
                    <button
                      key={roadmap.roadmapId}
                      type="button"
                      onClick={() => onSelectRoadmap(roadmap)}
                      className={`rounded-xl border px-4 py-3 text-left transition hover:-translate-y-0.5 ${
                        active
                          ? "border-[#2FA084] bg-[#6FCF97]/20 shadow-sm"
                          : "border-slate-200 bg-white hover:border-[#2FA084] hover:bg-[#EAF7F1]/70"
                      }`}
                    >
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <p className="text-sm font-extrabold text-[#18332D]">{roadmap.title}</p>
                        <span className="rounded-full border border-[#B9D8CC] bg-white px-2 py-0.5 text-[11px] font-extrabold text-[#1F6F5F]">
                          {roadmap.totalSkills} skills
                        </span>
                      </div>
                      <p className="mt-1 text-xs font-semibold text-slate-500">
                        Version {roadmap.versionNumber || roadmap.roadmapVersionNumber || "—"}
                        {roadmap.authorName ? ` · by ${roadmap.authorName}` : ""}
                      </p>
                    </button>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="mt-6 flex justify-end">
        <button
          type="button"
          disabled={!canContinue || isLoadingAssessment}
          onClick={onNext}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600 disabled:hover:translate-y-0"
        >
          {isLoadingAssessment ? <Loader2 className="animate-spin" size={16} /> : <BookOpenCheck size={16} />}
          Continue to skill checklist
        </button>
      </div>
    </section>
  );
}
