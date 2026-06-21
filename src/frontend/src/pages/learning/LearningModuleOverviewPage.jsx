import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { AlertCircle, ArrowLeft, CheckCircle2, Clock, FileText } from "lucide-react";
import { toast } from "react-toastify";
import { useLearningModuleStore } from "../../stores/useLearningModuleStore";
import { useStreakStore } from "../../stores/useStreakStore";
import {
  formatHours,
  getEnrollmentStatus,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../components/learningModules/learningModuleUi";

export default function LearningModuleOverviewPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const module = useLearningModuleStore((state) => state.getModuleSnapshot(slug));
  const moduleLoaded = useLearningModuleStore((state) => state.getModuleLoaded(slug));
  const isLoading = useLearningModuleStore((state) => state.getModuleLoading(slug));
  const error = useLearningModuleStore((state) => state.getModuleError(slug));
  const loadModuleBySlug = useLearningModuleStore((state) => state.loadModuleBySlug);
  const enrollModule = useLearningModuleStore((state) => state.enrollModule);
  const trackStreakIfNeeded = useStreakStore((state) => state.trackStreakIfNeeded);
  const [isStarting, setIsStarting] = useState(false);

  useEffect(() => {
    loadModuleBySlug(slug).catch(() => {});
  }, [slug, loadModuleBySlug]);

  const totalEstimatedHours = useMemo(() => {
    if (!module?.lessons?.length) return module?.estimatedHours;
    const sum = module.lessons.reduce((total, lesson) => total + Number(lesson.estimatedHours || 0), 0);
    return sum > 0 ? sum : module.estimatedHours;
  }, [module]);

  const handleStart = async () => {
    if (!module || isStarting) return;

    if (getEnrollmentStatus(module) !== "not_started") {
      trackStreakIfNeeded().catch(() => {
        // Streak tracking should not block opening a learning module.
      });
      navigate(`/learning-modules/${module.slug}/study`);
      return;
    }

    try {
      setIsStarting(true);
      await enrollModule(module.skillModuleId, { slug });
      trackStreakIfNeeded().catch(() => {
        // Streak tracking should not block module enrollment.
      });
      toast.success("Module started.");
      navigate(`/learning-modules/${module.slug}/study`);
    } catch (err) {
      toast.error(err?.message || "Unable to start module.");
    } finally {
      setIsStarting(false);
    }
  };

  if (isLoading || (!moduleLoaded && !error)) {
    return (
      <ModulePageShell>
        <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">
          Loading module...
        </ModuleCard>
      </ModulePageShell>
    );
  }

  if (error || !module) {
    return (
      <ModulePageShell>
        <ModuleEmptyState title="Module not found">{error || "This module could not be loaded."}</ModuleEmptyState>
      </ModulePageShell>
    );
  }

  const isArchived = module.status === "archived";
  const enrollmentStatus = getEnrollmentStatus(module);
  const actionLabel = enrollmentStatus === "not_started" ? "Start module" : "Continue module";

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <button
          type="button"
          onClick={() => navigate("/learning-modules/browse")}
          className="inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
        >
          <ArrowLeft size={16} /> Back to browse
        </button>

        {isArchived && (
          <ModuleCard className="border-amber-200 bg-amber-50 p-4 text-sm font-bold leading-6 text-amber-800">
            <div className="flex gap-3">
              <AlertCircle size={18} className="mt-0.5 shrink-0" />
              <div>
                This module is archived. You can still access it because you are enrolled, but it is no longer available to new learners.
              </div>
            </div>
          </ModuleCard>
        )}

        <ModuleCard className="p-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <div className="flex flex-wrap gap-2">
                <ModuleBadge tone="green">{module.skillName}</ModuleBadge>
                {module.difficultyLevel && <ModuleBadge tone="slate">{module.difficultyLevel}</ModuleBadge>}
                {isArchived && <ModuleBadge tone="amber">Archived</ModuleBadge>}
              </div>

              <h1 className="mt-3 text-3xl font-extrabold text-[#18332D]">{module.title}</h1>
              <p className="mt-3 max-w-3xl text-sm font-medium leading-6 text-slate-700">
                {module.description || "No description provided."}
              </p>

              <div className="mt-4 flex flex-wrap gap-3 text-xs font-bold text-slate-700">
                <span className="inline-flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/70 px-3 py-2">
                  <FileText size={14} /> {module.lessons.length} lessons
                </span>
                <span className="inline-flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/70 px-3 py-2">
                  <Clock size={14} /> {formatHours(totalEstimatedHours)} estimated
                </span>
              </div>
            </div>

            <ModuleButton
              size="md"
              onClick={handleStart}
              disabled={isStarting || (isArchived && enrollmentStatus === "not_started")}
            >
              {isStarting ? "Starting..." : actionLabel}
            </ModuleButton>
          </div>
        </ModuleCard>

        <div className="grid gap-5 lg:grid-cols-[1.2fr_.8fr]">
          <ModuleCard className="p-5">
            <h2 className="text-lg font-extrabold text-[#18332D]">What's inside</h2>
            <div className="mt-4 space-y-3">
              {module.lessons.map((lesson) => (
                <div key={lesson.skillModuleLessonId} className="rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/45 p-4">
                  <div className="text-sm font-extrabold text-[#18332D]">
                    {lesson.orderIndex}. {lesson.title}
                  </div>
                  <p className="mt-1 text-sm font-medium leading-6 text-slate-700">
                    {lesson.summary || "Lesson content available after starting the module."}
                  </p>
                  {lesson.estimatedHours && (
                    <div className="mt-2 text-xs font-bold text-[#1F6F5F]">
                      {formatHours(lesson.estimatedHours)}
                    </div>
                  )}
                </div>
              ))}

              {module.quiz && (
                <div className="rounded-lg border border-[#B9D8CC] bg-white p-4">
                  <div className="text-sm font-extrabold text-[#18332D]">{module.quiz.title || "Final quiz"}</div>
                  <p className="mt-1 text-sm font-medium text-slate-700">
                    {module.quiz.questionCount} questions · passing score {module.quiz.passingScorePercent}%
                  </p>
                </div>
              )}
            </div>
          </ModuleCard>

          <ModuleCard className="p-5">
            <h2 className="text-lg font-extrabold text-[#18332D]">What you will learn</h2>
            <ul className="mt-4 space-y-3">
              {deriveOutcomes(module).map((item) => (
                <li key={item} className="flex gap-3 text-sm font-medium leading-6 text-slate-700">
                  <span className="mt-0.5 grid h-5 w-5 shrink-0 place-items-center rounded-full bg-[#6FCF97]/25 text-xs font-bold text-[#1F6F5F]">
                    <CheckCircle2 size={13} />
                  </span>
                  {item}
                </li>
              ))}
            </ul>
          </ModuleCard>
        </div>
      </div>
    </ModulePageShell>
  );
}

function deriveOutcomes(module) {
  const skill = module.skillName || "this skill";
  const lessons = module.lessons?.slice(0, 4).map((lesson) => `Understand ${lesson.title.toLowerCase()}.`) || [];

  if (lessons.length > 0) return lessons;

  return [
    `Understand the core ideas behind ${skill}.`,
    "Practice the topic through structured lessons.",
    "Check your understanding with the module quiz.",
  ];
}
