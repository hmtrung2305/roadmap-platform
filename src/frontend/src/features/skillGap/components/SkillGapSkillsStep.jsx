import { ArrowLeft, BarChart3, Check, Loader2, SearchCheck } from "lucide-react";
import { normalizeId, toArray } from "../utils/skillGapUtils";

export default function SkillGapSkillsStep({
  role,
  roadmap,
  categories,
  selectedSkillIds,
  isLoading,
  isAnalyzing,
  onToggleSkill,
  onBack,
  onAnalyze,
}) {
  const selectedSet = new Set(toArray(selectedSkillIds).map(normalizeId));
  const totalSkills = toArray(categories).reduce(
    (sum, category) => sum + toArray(category.skills).length,
    0,
  );

  return (
    <section className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm sm:p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
            <SearchCheck size={14} /> Step 2
          </p>
          <h2 className="mt-3 text-2xl font-black text-[#18332D]">Mark the skills you already have</h2>
          <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
            Roadmap: <span className="text-[#1F6F5F]">{roadmap?.title || roadmap?.roadmapName || "Selected roadmap"}</span>
            {role?.name ? ` · Role: ${role.name}` : ""}
          </p>
        </div>
        <div className="rounded-2xl border border-[#B9D8CC] bg-[#F7F1E8] px-4 py-3 text-right">
          <p className="text-[11px] font-extrabold uppercase tracking-[0.16em] text-slate-500">Selected</p>
          <p className="mt-1 text-2xl font-black text-[#18332D]">
            {selectedSet.size}<span className="text-sm text-slate-500">/{totalSkills}</span>
          </p>
        </div>
      </div>

      {isLoading ? (
        <div className="mt-6 grid place-items-center rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 py-14 text-sm font-bold text-slate-500">
          <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={24} /> Loading skill checklist...
        </div>
      ) : categories.length === 0 ? (
        <div className="mt-6 rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 p-8 text-center">
          <p className="text-sm font-extrabold text-[#18332D]">No skills found for this roadmap</p>
          <p className="mt-1 text-xs font-semibold text-slate-500">
            Ask the content manager to add skills and publish/generate category configuration.
          </p>
        </div>
      ) : (
        <div className="mt-6 space-y-4">
          {categories.map((category) => {
            const skills = toArray(category.skills);
            const matched = skills.filter((skill) => selectedSet.has(skill.skillId)).length;

            return (
              <article
                key={category.categoryName}
                className="overflow-hidden rounded-2xl border border-[#B9D8CC]/80 bg-white shadow-sm"
              >
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#E4ECE8] bg-[#F7F1E8]/70 px-4 py-3">
                  <div>
                    <h3 className="text-sm font-black text-[#18332D]">{category.categoryName}</h3>
                    <p className="mt-1 text-xs font-semibold text-slate-500">Display order {category.displayOrder || "—"}</p>
                  </div>
                  <span className="rounded-full border border-[#B9D8CC] bg-white px-3 py-1 text-xs font-extrabold text-[#1F6F5F]">
                    {matched}/{skills.length} selected
                  </span>
                </div>

                <div className="flex flex-wrap gap-2 px-4 py-4">
                  {skills.map((skill) => {
                    const selected = selectedSet.has(skill.skillId);

                    return (
                      <button
                        key={skill.skillId}
                        type="button"
                        onClick={() => onToggleSkill(skill.skillId)}
                        disabled={isAnalyzing || isLoading}
                        className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-bold transition hover:-translate-y-0.5 ${
                          selected
                            ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                            : "border-slate-200 bg-slate-50 text-slate-600 hover:border-[#2FA084] hover:bg-white"
                        } disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0`}
                      >
                        {selected && <Check size={12} />}
                        {skill.skillName}
                      </button>
                    );
                  })}
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
          disabled={isLoading || isAnalyzing}
          className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-60"
        >
          <ArrowLeft size={16} /> Back to roadmap selection
        </button>
        <button
          type="button"
          disabled={isLoading || isAnalyzing || totalSkills === 0}
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
