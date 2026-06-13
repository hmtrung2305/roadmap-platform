import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Archive, BookOpenText, Edit3, Eye, Plus, RotateCcw, Trash2 } from "lucide-react";
import { toast } from "react-toastify";
import { counselorLearningModuleApi } from "../../../api/learningModuleApi";
import {
  getStatusTone,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  prettyModuleStatus,
} from "../../../components/learningModules/learningModuleUi";

const statuses = ["draft", "published", "archived"];

export default function AdminLearningModulesPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const activeStatus = searchParams.get("status") || "draft";

  const [modules, setModules] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    let ignore = false;

    async function loadModules() {
      try {
        setIsLoading(true);
        setError(null);
        const data = await counselorLearningModuleApi.getModules(activeStatus);
        if (!ignore) setModules(data);
      } catch (err) {
        if (!ignore) setError(err?.message || "Unable to load modules.");
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    loadModules();

    return () => {
      ignore = true;
    };
  }, [activeStatus, refreshKey]);

  const visibleModules = useMemo(() => modules, [modules]);
  const reload = () => setRefreshKey((key) => key + 1);

  const handleDelete = async (module) => {
    if (!window.confirm(`Delete draft module "${module.title}"?`)) return;

    try {
      await counselorLearningModuleApi.deleteDraftModule(module.skillModuleId);
      toast.success("Draft module deleted.");
      reload();
    } catch (err) {
      toast.error(err?.message || "Unable to delete module.");
    }
  };

  const handleArchive = async (module) => {
    try {
      await counselorLearningModuleApi.archiveModule(module.skillModuleId);
      toast.success("Module archived.");
      reload();
    } catch (err) {
      toast.error(err?.message || "Unable to archive module.");
    }
  };

  const handleRestore = async (module) => {
    try {
      await counselorLearningModuleApi.restoreModule(module.skillModuleId);
      toast.success("Module restored to draft.");
      reload();
    } catch (err) {
      toast.error(err?.message || "Unable to restore module.");
    }
  };

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center gap-3">
            <div className="grid h-11 w-11 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
              <BookOpenText size={22} />
            </div>
            <div>
              <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">Workspace</p>
              <h1 className="text-2xl font-extrabold text-[#18332D]">Learning module management</h1>
            </div>
          </div>
        </section>

        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap gap-2">
            {statuses.map((status) => (
              <button
                key={status}
                type="button"
                onClick={() => setSearchParams({ status })}
                className={`rounded-lg border px-3.5 py-1.5 text-xs font-bold transition ${
                  activeStatus === status
                    ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                    : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-[#F7F1E8]"
                }`}
              >
                {prettyModuleStatus[status]}
              </button>
            ))}
          </div>

          <ModuleButton onClick={() => navigate("/admin/learning-modules/create")}>
            <Plus size={15} /> Add module
          </ModuleButton>
        </div>

        {error && <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">{error}</ModuleCard>}

        {isLoading ? (
          <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">Loading modules...</ModuleCard>
        ) : visibleModules.length === 0 ? (
          <ModuleEmptyState
            title={`No ${prettyModuleStatus[activeStatus].toLowerCase()} modules`}
            action={<ModuleButton onClick={() => navigate("/admin/learning-modules/create")}>Add module</ModuleButton>}
          >
            Modules will appear here after they are created.
          </ModuleEmptyState>
        ) : (
          <ModuleCard className="overflow-hidden">
            <div className="hidden grid-cols-[minmax(360px,1.7fr)_170px_150px_120px_380px] gap-3 border-b border-[#B9D8CC] bg-[#F7F1E8]/70 px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-700 xl:grid">
              <span>Module</span>
              <span>Skill</span>
              <span>Content</span>
              <span>Status</span>
              <span className="text-right">Actions</span>
            </div>

            {visibleModules.map((module) => (
              <div
                key={module.skillModuleId}
                className="grid gap-3 border-b border-[#B9D8CC]/60 px-4 py-4 last:border-b-0 xl:grid-cols-[minmax(360px,1.7fr)_170px_150px_120px_380px] xl:items-center"
              >
                <div className="min-w-0">
                  <div className="truncate font-extrabold text-[#18332D]">{module.title}</div>
                </div>

                <div className="text-sm font-bold text-slate-700">{module.skillName}</div>

                <div className="text-sm font-bold text-slate-700">
                  {module.lessonCount} lessons
                  <div className="text-xs font-semibold text-slate-500">{module.questionCount} questions</div>
                </div>

                <div>
                  <ModuleBadge tone={getStatusTone(module.status)}>{prettyModuleStatus[module.status] || module.status}</ModuleBadge>
                </div>

                <div className="flex flex-nowrap justify-start gap-1.5 overflow-x-auto pb-1 xl:justify-end xl:overflow-visible xl:pb-0">
                  {module.status === "draft" && (
                    <ModuleActionButton onClick={() => navigate(`/admin/learning-modules/${module.skillModuleId}/edit`)}>
                      <Edit3 size={14} strokeWidth={2.25} /> Edit
                    </ModuleActionButton>
                  )}

                  <ModuleActionButton onClick={() => navigate(`/admin/learning-modules/${module.skillModuleId}/preview`)}>
                    <Eye size={14} strokeWidth={2.25} /> Preview
                  </ModuleActionButton>

                  {module.status === "draft" && (
                    <ModuleActionButton tone="danger" onClick={() => handleDelete(module)}>
                      <Trash2 size={14} strokeWidth={2.25} /> Delete
                    </ModuleActionButton>
                  )}

                  {module.status === "published" && (
                    <ModuleActionButton tone="danger" onClick={() => handleArchive(module)}>
                      <Archive size={14} strokeWidth={2.25} /> Archive
                    </ModuleActionButton>
                  )}

                  {module.status === "archived" && (
                    <ModuleActionButton onClick={() => handleRestore(module)}>
                      <RotateCcw size={14} strokeWidth={2.25} /> Restore
                    </ModuleActionButton>
                  )}
                </div>
              </div>
            ))}
          </ModuleCard>
        )}
      </div>
    </ModulePageShell>
  );
}

function ModuleActionButton({ children, tone = "default", onClick }) {
  const styles =
    tone === "danger"
      ? "border-rose-200 bg-white text-rose-600 hover:border-rose-300 hover:bg-rose-50"
      : "border-[#B9D8CC] bg-white text-[#1F6F5F] hover:border-[#2FA084] hover:bg-[#F7F1E8]";

  return (
    <button
      type="button"
      onClick={onClick}
      className={`inline-flex h-8 shrink-0 items-center justify-center gap-1.5 rounded-md border px-2.5 text-xs font-extrabold leading-none transition ${styles}`}
    >
      {children}
    </button>
  );
}
