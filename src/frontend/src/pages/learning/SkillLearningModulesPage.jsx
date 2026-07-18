import { useEffect } from "react";
import { ArrowLeft, ArrowRight, BookOpen } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { CreatorByline } from "../../features/creatorProfile/components/CreatorProfileDisplay";
import {
  formatHours,
  getEnrollmentStatus,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../features/learningModules/components/learningModuleUi";
import { useLearningModuleStore } from "../../stores/useLearningModuleStore";

export default function SkillLearningModulesPage() {
  const navigate = useNavigate();
  const { skillSlug = "" } = useParams();
  const skillData = useLearningModuleStore(
    (state) => state.skillModulesBySlug[skillSlug] || null,
  );
  const isLoading = useLearningModuleStore(
    (state) => Boolean(state.skillModulesLoadingBySlug[skillSlug]),
  );
  const isLoaded = useLearningModuleStore(
    (state) => Boolean(state.skillModulesLoadedBySlug[skillSlug]),
  );
  const error = useLearningModuleStore(
    (state) => state.skillModulesErrorBySlug[skillSlug] || null,
  );
  const loadModulesBySkillSlug = useLearningModuleStore(
    (state) => state.loadModulesBySkillSlug,
  );

  useEffect(() => {
    loadModulesBySkillSlug(skillSlug).catch(() => {});
  }, [loadModulesBySkillSlug, skillSlug]);

  const modules = Array.isArray(skillData?.modules) ? skillData.modules : [];
  const skillName = skillData?.skillName || skillSlug.replaceAll("-", " ");

  const goBack = () => {
    if (window.history.length > 1) {
      navigate(-1);
      return;
    }

    navigate("/skill-gap");
  };

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <div>
          <button
            type="button"
            onClick={goBack}
            className="mb-3 inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back
          </button>
          <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
            Skill learning path
          </p>
          <h1 className="mt-1 text-3xl font-black capitalize tracking-[-0.035em] text-[#18332D]">
            {skillName}
          </h1>
          <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
            Choose a focused module to build this skill through lessons, quizzes, and guided study.
          </p>
        </div>

        {error ? (
          <ModuleCard className="border-red-200 bg-red-50 p-5 text-sm font-bold text-red-700">
            <p>{error}</p>
            <ModuleButton
              className="mt-4"
              onClick={() => loadModulesBySkillSlug(skillSlug, { force: true }).catch(() => {})}
            >
              Try again
            </ModuleButton>
          </ModuleCard>
        ) : isLoading || !isLoaded ? (
          <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">
            Loading modules for this skill...
          </ModuleCard>
        ) : !skillData?.isActive ? (
          <ModuleEmptyState
            title="This skill is currently unavailable"
            action={<ModuleButton onClick={() => navigate("/learning-modules/browse")}>Browse all modules</ModuleButton>}
          >
            The skill is no longer active in the learning catalog.
          </ModuleEmptyState>
        ) : modules.length === 0 ? (
          <ModuleEmptyState
            title="Learning modules for this skill are not available yet"
            action={<ModuleButton onClick={() => navigate("/learning-modules/browse")}>Browse all modules</ModuleButton>}
          >
            Learning content for this skill is still being prepared. Please check back later.
          </ModuleEmptyState>
        ) : (
          <div className="space-y-3">
            {modules.map((module) => (
              <SkillModuleRow key={module.skillModuleId} module={module} />
            ))}
          </div>
        )}
      </div>
    </ModulePageShell>
  );
}

function SkillModuleRow({ module }) {
  const navigate = useNavigate();
  const enrollmentStatus = getEnrollmentStatus(module);
  const targetPath = enrollmentStatus === "not_started"
    ? `/learning-modules/${module.slug}/overview`
    : `/learning-modules/${module.slug}/study`;
  const actionLabel = enrollmentStatus === "not_started"
    ? "Start learning"
    : enrollmentStatus === "completed"
      ? "Review"
      : "Continue";

  return (
    <ModuleCard className="grid gap-4 p-5 md:grid-cols-[minmax(280px,1.6fr)_210px_150px] md:items-center">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <BookOpen size={17} className="text-[#1F6F5F]" />
          <h2 className="truncate text-base font-extrabold text-[#18332D]">
            {module.title}
          </h2>
          {module.difficultyLevel && (
            <ModuleBadge tone="purple" className="capitalize">
              {module.difficultyLevel}
            </ModuleBadge>
          )}
        </div>
        <p className="mt-1 line-clamp-2 text-sm font-medium leading-6 text-slate-700">
          {module.description || "No description provided."}
        </p>
        <CreatorByline creatorProfile={module.creatorProfile} className="mt-2" />
      </div>

      <div className="text-sm font-bold text-slate-700">
        {module.lessonCount} lessons
        <div className="text-xs font-semibold text-slate-500">
          {module.questionCount} quiz questions · {formatHours(module.estimatedHours)}
        </div>
      </div>

      <div className="md:text-right">
        <ModuleButton onClick={() => navigate(targetPath)}>
          {actionLabel} <ArrowRight size={15} />
        </ModuleButton>
      </div>
    </ModuleCard>
  );
}
