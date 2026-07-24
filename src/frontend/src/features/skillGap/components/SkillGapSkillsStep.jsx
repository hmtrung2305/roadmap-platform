import { useMemo } from "react";
import {
  ArrowLeft,
  BarChart3,
  Check,
  Loader2,
  SearchCheck,
  Sparkles,
} from "lucide-react";
import {
  getSuggestedSkillIds,
  normalizeId,
  toArray,
} from "../utils/skillGapUtils";

function getSuggestionLabel(skill) {
  if (!skill?.isSuggestedFromCompletedNodes) {
    return "";
  }

  const completedNodeCount =
    Number(skill.completedNodeCount) || 0;

  if (completedNodeCount > 1) {
    return `Completed in ${completedNodeCount} nodes`;
  }

  return "Learned in roadmap";
}

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
  const normalizedCategories = useMemo(
    () => toArray(categories),
    [categories],
  );

  const selectedSet = useMemo(
    () =>
      new Set(
        toArray(selectedSkillIds)
          .map(normalizeId)
          .filter(Boolean),
      ),
    [selectedSkillIds],
  );

  const totalSkills = useMemo(
    () =>
      normalizedCategories.reduce(
        (total, category) =>
          total +
          toArray(category?.skills).length,
        0,
      ),
    [normalizedCategories],
  );

  const suggestedSkillIds = useMemo(
    () =>
      getSuggestedSkillIds(
        normalizedCategories,
      ),
    [normalizedCategories],
  );

  const suggestedSkillCount =
    suggestedSkillIds.length;

  return (
    <section className="flex min-h-0 flex-col rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm sm:p-6 lg:h-full lg:overflow-hidden">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
            <SearchCheck size={14} />
            Step 2
          </p>

          <h2 className="mt-3 text-2xl font-black text-[#18332D]">
            Mark the skills you already have
          </h2>

          <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
            Roadmap:{" "}
            <span className="text-[#1F6F5F]">
              {roadmap?.title ||
                roadmap?.roadmapName ||
                "Selected roadmap"}
            </span>

            {role?.name
              ? ` · Role: ${role.name}`
              : ""}
          </p>
        </div>

        <div className="rounded-2xl border border-[#B9D8CC] bg-[#F7F1E8] px-4 py-3 text-right">
          <p className="text-[11px] font-extrabold uppercase tracking-[0.16em] text-slate-500">
            Selected
          </p>

          <p className="mt-1 text-2xl font-black text-[#18332D]">
            {selectedSet.size}

            <span className="text-sm text-slate-500">
              /{totalSkills}
            </span>
          </p>
        </div>
      </div>

      {!isLoading &&
        suggestedSkillCount > 0 && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl border border-[#B9D8CC] bg-[#EAF7F1] px-4 py-3">
            <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-white text-[#1F6F5F] shadow-sm">
              <Sparkles size={16} />
            </div>

            <div>
              <p className="text-sm font-extrabold text-[#18332D]">
                We pre-selected{" "}
                {suggestedSkillCount}{" "}
                {suggestedSkillCount === 1
                  ? "skill"
                  : "skills"}{" "}
                based on your completed roadmap
                nodes.
              </p>

              <p className="mt-1 text-xs font-semibold leading-5 text-slate-600">
                You can change these selections
                before analyzing.
              </p>
            </div>
          </div>
        )}

      {isLoading ? (
        <div className="mt-6 grid min-h-0 flex-1 place-items-center rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 py-14 text-sm font-bold text-slate-500">
          <div className="flex flex-col items-center">
            <Loader2
              className="mb-2 animate-spin text-[#2FA084]"
              size={24}
            />

            <span>
              Loading skill checklist...
            </span>
          </div>
        </div>
      ) : normalizedCategories.length === 0 ? (
        <div className="mt-6 min-h-0 flex-1 rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 p-8 text-center">
          <p className="text-sm font-extrabold text-[#18332D]">
            No skills found for this roadmap
          </p>

          <p className="mt-1 text-xs font-semibold text-slate-500">
            Ask the content manager to add skills
            and publish/generate category
            configuration.
          </p>
        </div>
      ) : (
        <div className="skill-gap-content-scroll mt-6 min-h-0 flex-1 space-y-4 pr-1">
          {normalizedCategories.map(
            (category) => {
              const skills = toArray(
                category?.skills,
              );

              const selectedCount =
                skills.filter((skill) =>
                  selectedSet.has(
                    normalizeId(
                      skill?.skillId,
                    ),
                  ),
                ).length;

              return (
                <article
                  key={category.categoryName}
                  className="overflow-hidden rounded-2xl border border-[#B9D8CC]/80 bg-white shadow-sm"
                >
                  <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#E4ECE8] bg-[#F7F1E8]/70 px-4 py-3">
                    <div>
                      <h3 className="text-sm font-black text-[#18332D]">
                        {category.categoryName}
                      </h3>

                      <p className="mt-1 text-xs font-semibold text-slate-500">
                        Display order{" "}
                        {category.displayOrder ||
                          "—"}
                      </p>
                    </div>

                    <span className="rounded-full border border-[#B9D8CC] bg-white px-3 py-1 text-xs font-extrabold text-[#1F6F5F]">
                      {selectedCount}/
                      {skills.length} selected
                    </span>
                  </div>

                  <div className="flex flex-wrap gap-2 px-4 py-4">
                    {skills.map((skill) => {
                      const skillId =
                        normalizeId(
                          skill?.skillId,
                        );

                      const selected =
                        selectedSet.has(
                          skillId,
                        );

                      const suggestionLabel =
                        getSuggestionLabel(
                          skill,
                        );

                      return (
                        <button
                          key={skillId}
                          type="button"
                          aria-pressed={selected}
                          onClick={() =>
                            onToggleSkill(
                              skillId,
                            )
                          }
                          disabled={
                            isAnalyzing ||
                            isLoading
                          }
                          className={`inline-flex min-w-[140px] flex-col items-start gap-1 rounded-2xl border px-3 py-2 text-left transition hover:-translate-y-0.5 ${
                            selected
                              ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                              : "border-slate-200 bg-slate-50 text-slate-600 hover:border-[#2FA084] hover:bg-white"
                          } disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0`}
                        >
                          <span className="inline-flex items-center gap-1.5 text-xs font-bold">
                            <span
                              className={`inline-flex h-4 w-4 shrink-0 items-center justify-center rounded border ${
                                selected
                                  ? "border-[#2FA084] bg-[#2FA084] text-white"
                                  : "border-slate-300 bg-white text-transparent"
                              }`}
                            >
                              <Check
                                size={11}
                              />
                            </span>

                            <span>
                              {skill.skillName}
                            </span>
                          </span>

                          {suggestionLabel && (
                            <span className="inline-flex items-center gap-1 pl-[22px] text-[10px] font-extrabold text-[#1F6F5F]">
                              <Sparkles
                                size={10}
                              />

                              {
                                suggestionLabel
                              }
                            </span>
                          )}
                        </button>
                      );
                    })}
                  </div>
                </article>
              );
            },
          )}
        </div>
      )}

      <div className="mt-6 flex shrink-0 flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
        <button
          type="button"
          onClick={onBack}
          disabled={
            isLoading || isAnalyzing
          }
          className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-60"
        >
          <ArrowLeft size={16} />
          Back to roadmap selection
        </button>

        <button
          type="button"
          disabled={
            isLoading ||
            isAnalyzing ||
            totalSkills === 0
          }
          onClick={onAnalyze}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600 disabled:hover:translate-y-0"
        >
          {isAnalyzing ? (
            <Loader2
              className="animate-spin"
              size={16}
            />
          ) : (
            <BarChart3 size={16} />
          )}

          Analyze gap
        </button>
      </div>
    </section>
  );
}