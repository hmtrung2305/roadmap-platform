import { ArrowLeft, BookOpen, CheckCircle2, History, Map, RotateCcw, XCircle } from "lucide-react";
import { Link } from "react-router-dom";
import { normalizeSkillGapResult, toArray } from "../utils/skillGapUtils";

function StatCard({ label, value, tone = "default" }) {
  const toneClass =
    tone === "good"
      ? "border-[#B9D8CC] bg-[#EAF7F1] text-[#1F6F5F]"
      : tone === "warn"
        ? "border-[#F1D9A8] bg-[#FFF7E6] text-[#8A5A12]"
        : "border-[#B9D8CC] bg-[#F7F1E8] text-[#18332D]";

  return (
    <div className={`rounded-2xl border px-4 py-3 ${toneClass}`}>
      <p className="text-[11px] font-extrabold uppercase tracking-[0.16em] opacity-75">{label}</p>
      <p className="mt-1 text-2xl font-black">{value}</p>
    </div>
  );
}

function SkillLearningLink({ skill, isMatched }) {
  const className = isMatched
    ? "inline-flex items-center gap-1.5 rounded-full border border-[#2FA084] bg-[#6FCF97]/20 px-3 py-1.5 text-xs font-bold text-[#1F6F5F] transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/30 focus:outline-none focus:ring-2 focus:ring-[#6FCF97]/40"
    : "inline-flex items-center gap-1.5 rounded-full border border-[#E4B95F] bg-[#FFF7E6] px-3 py-1.5 text-xs font-bold text-[#8A5A12] transition hover:-translate-y-0.5 hover:border-[#C98A18] hover:bg-[#FFEFCB] focus:outline-none focus:ring-2 focus:ring-[#E4B95F]/40";
  const content = (
    <>
      {isMatched ? <CheckCircle2 size={12} /> : <BookOpen size={12} />}
      {skill.skillName}
    </>
  );

  if (!skill.skillSlug) {
    return <span className={className}>{content}</span>;
  }

  return (
    <Link
      to={`/learning-modules/skills/${encodeURIComponent(skill.skillSlug)}`}
      className={className}
      aria-label={`View learning modules for ${skill.skillName}`}
    >
      {content}
    </Link>
  );
}

export default function SkillGapResultStep({
  result,
  canUpdateSelection = true,
  isHistoryView = false,
  onBack,
  onBackToHistory,
  onReset,
}) {
  const normalizedResult = normalizeSkillGapResult(result);

  if (!normalizedResult) return null;

  return (
    <section className="skill-gap-result-shell flex min-h-0 flex-col overflow-hidden rounded-3xl border border-[#B9D8CC]/80 bg-white/95 shadow-sm">
      <div className="skill-gap-content-scroll min-h-0 flex-1 p-5 sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="min-w-0">
            <p className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
              {isHistoryView ? <History size={14} /> : <CheckCircle2 size={14} />} {isHistoryView ? "Saved result" : "Step 3"}
            </p>
            <h2 className="mt-3 text-2xl font-black text-[#18332D]">Skill gap result</h2>
            <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
              Roadmap: <span className="text-[#1F6F5F]">{normalizedResult.roadmapName}</span>
              {normalizedResult.careerRoleName ? ` · Role: ${normalizedResult.careerRoleName}` : ""}
              {normalizedResult.authorName ? ` · Author: ${normalizedResult.authorName}` : ""}
            </p>
          </div>

          {normalizedResult.roadmapSlug && (
            <Link
              to={`/roadmaps/${encodeURIComponent(normalizedResult.roadmapSlug)}`}
              className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#2FA084] bg-[#2FA084] px-4 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] focus:outline-none focus:ring-2 focus:ring-[#6FCF97]/40"
            >
              <Map size={16} /> {isHistoryView ? "View current roadmap" : "View roadmap"}
            </Link>
          )}
        </div>

        <div className="mt-6 grid gap-3 sm:grid-cols-3">
          <StatCard label="Skills you have" value={normalizedResult.matchedSkills} tone="good" />
          <StatCard label="Missing skills" value={normalizedResult.missingSkills} tone="warn" />
          <StatCard label="Total roadmap skills" value={normalizedResult.totalSkills} />
        </div>

        <div className="mt-6 space-y-4 pb-1">
          {normalizedResult.categories.map((category) => {
            const skills = toArray(category.skills);
            const matchedSkills = skills.filter((skill) => skill.isMatched);
            const missingSkills = skills.filter((skill) => !skill.isMatched);

            return (
              <article key={category.categoryName} className="overflow-hidden rounded-2xl border border-[#B9D8CC]/80 bg-white shadow-sm">
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#E4ECE8] bg-[#F7F1E8]/70 px-4 py-3">
                  <div>
                    <h3 className="text-sm font-black text-[#18332D]">{category.categoryName}</h3>
                    <p className="mt-1 text-xs font-semibold text-slate-500">
                      {category.matchedSkills}/{category.totalSkills} matched · {category.missingSkills} missing
                    </p>
                  </div>
                </div>

                <div className="grid gap-4 p-4 lg:grid-cols-2">
                  <div>
                    <div className="mb-2 flex items-center gap-2 text-xs font-extrabold uppercase tracking-[0.14em] text-[#1F6F5F]">
                      <CheckCircle2 size={14} /> Skills you have
                    </div>
                    {matchedSkills.length === 0 ? (
                      <p className="rounded-xl border border-dashed border-[#B9D8CC] bg-[#EAF7F1]/50 px-3 py-3 text-xs font-semibold text-slate-500">
                        No skill selected in this category.
                      </p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {matchedSkills.map((skill) => (
                          <SkillLearningLink
                            key={skill.skillId}
                            skill={skill}
                            isMatched
                          />
                        ))}
                      </div>
                    )}
                  </div>

                  <div>
                    <div className="mb-2 flex items-center gap-2 text-xs font-extrabold uppercase tracking-[0.14em] text-[#8A5A12]">
                      <XCircle size={14} /> Missing skills
                    </div>
                    {missingSkills.length === 0 ? (
                      <p className="rounded-xl border border-dashed border-[#B9D8CC] bg-[#EAF7F1]/50 px-3 py-3 text-xs font-semibold text-slate-500">
                        No missing skills in this category.
                      </p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {missingSkills.map((skill) => (
                          <SkillLearningLink
                            key={skill.skillId}
                            skill={skill}
                            isMatched={false}
                          />
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      </div>

      <div className="z-10 flex shrink-0 flex-col-reverse gap-3 border-t border-[#D9E7E1] bg-white px-5 py-5 shadow-[0_-10px_24px_rgba(24,51,45,0.10)] sm:flex-row sm:items-center sm:justify-between sm:px-6">
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
          <span />
        )}

        <button
          type="button"
          onClick={onReset}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
        >
          <RotateCcw size={16} /> Start new analysis
        </button>
      </div>
    </section>
  );
}
