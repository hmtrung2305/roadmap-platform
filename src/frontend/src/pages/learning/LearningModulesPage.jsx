/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { useLearningModuleStore } from "../../stores/useLearningModuleStore";
import {
  getEnrollmentStatus,
  getProgress,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  prettyEnrollmentStatus,
} from "../../features/learningModules/components/learningModuleUi";

const moduleStatusTabs = ["in_progress", "completed"];

function getStatusFromSearchParams(searchParams) {
  const statusParam = searchParams.get("status");

  return moduleStatusTabs.includes(statusParam)
    ? statusParam
    : "in_progress";
}

export default function LearningModulesPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const modules = useLearningModuleStore((state) => state.enrolledModules);
  const isLoading = useLearningModuleStore((state) => state.isEnrolledModulesLoading);
  const isLoaded = useLearningModuleStore((state) => state.enrolledModulesLoaded);
  const error = useLearningModuleStore((state) => state.enrolledModulesError);
  const loadEnrolledModules = useLearningModuleStore((state) => state.loadEnrolledModules);
  const [status, setStatus] = useState(() => getStatusFromSearchParams(searchParams));

  useEffect(() => {
    const nextStatus = getStatusFromSearchParams(searchParams);

    setStatus((current) => (current === nextStatus ? current : nextStatus));
  }, [searchParams]);

  const updateStatus = (nextStatus) => {
    setStatus(nextStatus);
    setSearchParams((current) => {
      const nextParams = new URLSearchParams(current);

      if (nextStatus === "in_progress") {
        nextParams.delete("status");
      } else {
        nextParams.set("status", nextStatus);
      }

      return nextParams;
    }, { replace: true });
  };

  useEffect(() => {
    loadEnrolledModules().catch(() => {});
  }, [loadEnrolledModules]);

  const visibleModules = useMemo(() => {
    return modules.filter((module) => getEnrollmentStatus(module) === status);
  }, [modules, status]);

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">Quick modules</p>
            <h1 className="mt-1 text-4xl font-black tracking-[-0.045em] text-[#18332D]">Learning Modules</h1>
            <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
              Practice focused skills through short lessons, quizzes, and guided study.
            </p>
          </div>

          <ModuleButton size="md" onClick={() => navigate("/learning-modules/browse")}>
            Browse all modules <ArrowRight size={15} />
          </ModuleButton>
        </div>

        <ModuleCard className="p-4">
          <div className="flex flex-wrap gap-2">
            {["in_progress", "completed"].map((key) => (
              <button
                key={key}
                type="button"
                onClick={() => updateStatus(key)}
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
        </ModuleCard>

        {error && (
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            {error}
          </ModuleCard>
        )}

        {isLoading || (!isLoaded && !error) ? (
          <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">
            Loading learning modules...
          </ModuleCard>
        ) : visibleModules.length === 0 ? (
          <ModuleEmptyState
            title={status === "completed" ? "No completed modules yet" : "No modules in progress"}
            action={<ModuleButton onClick={() => navigate("/learning-modules/browse")}>Browse all modules</ModuleButton>}
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
  const actionLabel = getEnrollmentStatus(module) === "completed" ? "Review" : "Continue";
  const targetPath = `/learning-modules/${module.slug}/study`;

  const openModule = () => navigate(targetPath);
  const handleKeyDown = (event) => {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      openModule();
    }
  };

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={openModule}
      onKeyDown={handleKeyDown}
      className="grid cursor-pointer gap-4 rounded-xl border border-[#B9D8CC]/80 bg-white/95 px-5 py-4 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md focus:outline-none focus:ring-2 focus:ring-[#6FCF97]/30 md:grid-cols-[minmax(280px,1.6fr)_260px_140px] md:items-center"
    >
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="truncate text-base font-extrabold text-[#18332D]">{module.title}</h2>
          {module.difficultyLevel && <ModuleBadge tone="purple" className="capitalize">{module.difficultyLevel}</ModuleBadge>}
          {module.status === "archived" && <ModuleBadge tone="amber">Archived</ModuleBadge>}
        </div>
        <p className="mt-1 line-clamp-2 text-sm font-medium leading-6 text-slate-700">
          {module.description || "No description provided."}
        </p>
      </div>

      <div>
        <div className="mb-1 flex justify-between text-xs font-bold text-slate-700">
          <span>{Math.round(progress)}% complete</span>
        </div>
        <div className="h-3 overflow-hidden rounded-full bg-[#E4ECE8]">
          <div
            className="h-3 rounded-full bg-[#2FA084] transition-all"
            style={{ width: `${Math.max(progress, progress > 0 ? 8 : 0)}%` }}
          />
        </div>
      </div>

      <div className="md:text-right">
        <ModuleButton onClick={(event) => {
          event.stopPropagation();
          openModule();
        }}>
          {actionLabel} <ArrowRight size={15} />
        </ModuleButton>
      </div>
    </div>
  );
}
