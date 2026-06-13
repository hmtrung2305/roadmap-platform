import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowLeft, Search } from "lucide-react";
import { learningModuleApi } from "../../api/learningModuleApi";
import {
  formatHours,
  getEnrollmentStatus,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../components/learningModules/learningModuleUi";

export default function BrowseLearningModulesPage() {
  const navigate = useNavigate();
  const [modules, setModules] = useState([]);
  const [search, setSearch] = useState("");
  const [difficulty, setDifficulty] = useState("all");
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
      .filter((module) => getEnrollmentStatus(module) === "not_started")
      .filter((module) => difficulty === "all" || module.difficultyLevel === difficulty)
      .filter((module) => {
        if (!term) return true;
        return `${module.title} ${module.skillName} ${module.description || ""}`
          .toLowerCase()
          .includes(term);
      });
  }, [modules, difficulty, search]);

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <button
              type="button"
              onClick={() => navigate("/learning-modules")}
              className="mb-2 inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
            >
              <ArrowLeft size={16} /> Back to my modules
            </button>
            <h1 className="text-3xl font-black tracking-[-0.035em] text-[#18332D]">Browse modules</h1>
          </div>
        </div>

        <ModuleCard className="p-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <label className="relative w-full md:max-w-xl">
              <Search size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                className="w-full rounded-lg border border-[#B9D8CC] bg-white py-2.5 pl-9 pr-3 text-sm font-semibold outline-none transition focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
                placeholder="Search modules by title, skill, or topic"
              />
            </label>

            <div className="flex flex-wrap gap-2">
              {["all", "beginner", "intermediate", "advanced"].map((key) => (
                <button
                  key={key}
                  type="button"
                  onClick={() => setDifficulty(key)}
                  className={`rounded-lg border px-3.5 py-1.5 text-xs font-bold capitalize transition ${
                    difficulty === key
                      ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                      : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-[#F7F1E8]"
                  }`}
                >
                  {key}
                </button>
              ))}
            </div>
          </div>
        </ModuleCard>

        {error && <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">{error}</ModuleCard>}

        {isLoading ? (
          <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">Loading modules...</ModuleCard>
        ) : visibleModules.length === 0 ? (
          <ModuleEmptyState title="No available modules found">
            Try another search or check back after more modules are published.
          </ModuleEmptyState>
        ) : (
          <div className="space-y-3">
            {visibleModules.map((module) => (
              <BrowseModuleRow key={module.skillModuleId} module={module} />
            ))}
          </div>
        )}
      </div>
    </ModulePageShell>
  );
}

function BrowseModuleRow({ module }) {
  const navigate = useNavigate();

  return (
    <div className="grid gap-4 rounded-xl border border-[#B9D8CC]/80 bg-white/95 px-5 py-4 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md md:grid-cols-[minmax(280px,1.6fr)_170px_160px_120px] md:items-center">
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

      <div className="text-sm font-bold text-slate-700">
        {module.lessonCount} lessons
        <div className="text-xs font-semibold text-slate-500">
          {module.questionCount} quiz questions · {formatHours(module.estimatedHours)}
        </div>
      </div>

      <div className="md:text-right">
        <ModuleButton onClick={() => navigate(`/learning-modules/${module.slug}/overview`)}>
          Open
        </ModuleButton>
      </div>
    </div>
  );
}
