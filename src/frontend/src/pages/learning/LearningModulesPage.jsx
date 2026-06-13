import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowRight, Search } from "lucide-react";
import { learningModuleApi } from "../../api/learningModuleApi";
import {
  getEnrollmentStatus,
  getProgress,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  prettyEnrollmentStatus,
} from "../../components/learningModules/learningModuleUi";

export default function LearningModulesPage() {
  const navigate = useNavigate();
  const [modules, setModules] = useState([]);
  const [status, setStatus] = useState("in_progress");
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    let ignore = false;

    async function loadModules() {
      try {
        setIsLoading(true);
        setError(null);
        const data = await learningModuleApi.getPublishedModules();
        if (!ignore) setModules(data);
      } catch (err) {
        if (!ignore) setError(err?.message || "Unable to load learning modules.");
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    loadModules();

    return () => {
      ignore = true;
    };
  }, []);

  const visibleModules = useMemo(() => {
    const term = search.trim().toLowerCase();

    return modules
      .filter((module) => getEnrollmentStatus(module) === status)
      .filter((module) => {
        if (!term) return true;
        return `${module.title} ${module.skillName} ${module.description || ""}`
          .toLowerCase()
          .includes(term);
      });
  }, [modules, status, search]);

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h1 className="text-3xl font-black tracking-[-0.035em] text-[#18332D]">Learning modules</h1>
          <ModuleButton size="md" onClick={() => navigate("/learning-modules/browse")}>
            Browse modules <ArrowRight size={15} />
          </ModuleButton>
        </div>

        <ModuleCard className="p-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="flex flex-wrap gap-2">
              {["in_progress", "completed"].map((key) => (
                <button
                  key={key}
                  type="button"
                  onClick={() => setStatus(key)}
                  className={`rounded-lg border px-3.5 py-1.5 text-xs font-bold transition ${
                    status === key
                      ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                      : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-[#F7F1E8]"
                  }`}
                >
                  {prettyEnrollmentStatus[key]}
                </button>
              ))}
            </div>

            <label className="relative w-full md:w-96">
              <Search size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search your modules"
                className="w-full rounded-lg border border-[#B9D8CC] bg-white py-2 pl-9 pr-3 text-sm font-semibold outline-none transition focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
              />
            </label>
          </div>
        </ModuleCard>

        {error && (
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            {error}
          </ModuleCard>
        )}

        {isLoading ? (
          <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">
            Loading learning modules...
          </ModuleCard>
        ) : visibleModules.length === 0 ? (
          <ModuleEmptyState
            title={status === "completed" ? "No completed modules yet" : "No modules in progress"}
            action={<ModuleButton onClick={() => navigate("/learning-modules/browse")}>Browse modules</ModuleButton>}
          >
            Start a module to see it here.
          </ModuleEmptyState>
        ) : (
          <div className="space-y-3">
            {visibleModules.map((module) => (
              <LearningModuleListRow key={module.skillModuleId} module={module} />
            ))}
          </div>
        )}
      </div>
    </ModulePageShell>
  );
}

function LearningModuleListRow({ module }) {
  const navigate = useNavigate();
  const progress = getProgress(module);
  const completedLessons = Object.values(module.enrollment?.lessonProgress || {}).filter(
    (value) => value === "completed",
  ).length;
  const actionLabel = getEnrollmentStatus(module) === "completed" ? "Review" : "Continue";

  return (
    <div className="grid gap-4 rounded-xl border border-[#B9D8CC]/80 bg-white/95 px-5 py-4 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md md:grid-cols-[minmax(280px,1.6fr)_160px_260px_120px] md:items-center">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="truncate text-base font-extrabold text-[#18332D]">{module.title}</h2>
          {module.difficultyLevel && <ModuleBadge tone="slate">{module.difficultyLevel}</ModuleBadge>}
        </div>
        <p className="mt-1 line-clamp-2 text-sm font-medium leading-6 text-slate-700">
          {module.description || "No description provided."}
        </p>
      </div>

      <div className="text-sm font-bold text-slate-700">{module.skillName}</div>

      <div>
        <div className="mb-1 flex justify-between text-xs font-bold text-slate-700">
          <span>{Math.round(progress)}%</span>
          <span>{completedLessons}/{module.lessonCount} lessons</span>
        </div>
        <div className="h-2 rounded-full bg-slate-100">
          <div className="h-2 rounded-full bg-[#2FA084]" style={{ width: `${progress}%` }} />
        </div>
      </div>

      <div className="md:text-right">
        <ModuleButton onClick={() => navigate(`/learning-modules/${module.slug}/study`)}>
          {actionLabel}
        </ModuleButton>
      </div>
    </div>
  );
}
