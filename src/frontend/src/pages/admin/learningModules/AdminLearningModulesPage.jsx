import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  Archive,
  BookOpenText,
  Edit3,
  Eye,
  FileQuestion,
  MoreHorizontal,
  Plus,
  RotateCcw,
  Tag,
  Trash2,
} from "lucide-react";
import { toast } from "react-toastify";
import { counselorLearningModuleApi, getLearningModuleRouteSegment } from "../../../api/learningModuleApi";
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

  const [modulesByStatus, setModulesByStatus] = useState({
    draft: [],
    published: [],
    archived: [],
  });
  const [openMenuId, setOpenMenuId] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    let ignore = false;

    async function loadModules() {
      try {
        setIsLoading(true);
        setError(null);

        const entries = await Promise.all(
          statuses.map(async (status) => [status, await counselorLearningModuleApi.getModules(status)]),
        );

        if (!ignore) {
          setModulesByStatus(Object.fromEntries(entries));
        }
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
  }, [refreshKey]);

  const visibleModules = useMemo(() => modulesByStatus[activeStatus] || [], [activeStatus, modulesByStatus]);
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
            {statuses.map((status) => {
              const isActive = activeStatus === status;
              const count = modulesByStatus[status]?.length || 0;

              return (
                <button
                  key={status}
                  type="button"
                  onClick={() => setSearchParams({ status })}
                  className={`inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 text-xs font-extrabold transition ${
                    isActive
                      ? "border-[#2FA084] bg-[#6FCF97]/24 text-[#1F6F5F] shadow-sm"
                      : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-[#F7F1E8]"
                  }`}
                >
                  {prettyModuleStatus[status]}
                  <span
                    className={`rounded-full px-2 py-0.5 text-[11px] ${
                      isActive ? "bg-white/75 text-[#1F6F5F]" : "bg-slate-100 text-slate-600"
                    }`}
                  >
                    {count}
                  </span>
                </button>
              );
            })}
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
          <ModuleCard className="overflow-visible">
            <div className="hidden grid-cols-[minmax(340px,1.7fr)_190px_170px_120px_150px] gap-3 border-b border-[#B9D8CC] bg-[#F7F1E8]/70 px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-700 xl:grid">
              <span>Module</span>
              <span>Skill</span>
              <span>Content</span>
              <span>Status</span>
              <span className="text-right">Actions</span>
            </div>

            {visibleModules.map((module) => (
              <div
                key={module.skillModuleId}
                className="grid gap-3 border-b border-[#B9D8CC]/60 px-4 py-4 last:border-b-0 xl:grid-cols-[minmax(340px,1.7fr)_190px_170px_120px_150px] xl:items-center"
              >
                <div className="min-w-0">
                  <div className="truncate text-base font-bold text-[#18332D]">{module.title}</div>
                </div>

                <div>
                  <span className="inline-flex max-w-full items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-[#F7F1E8]/60 px-2.5 py-1 text-xs font-extrabold text-[#1F6F5F]">
                    <Tag size={13} />
                    <span className="truncate">{module.skillName || "No skill"}</span>
                  </span>
                </div>

                <div className="flex flex-wrap gap-2 text-sm font-bold text-slate-700">
                  <span className="inline-flex items-center gap-1.5 rounded-md bg-slate-100 px-2 py-1">
                    <BookOpenText size={14} /> {module.lessonCount}
                  </span>
                  <span className="inline-flex items-center gap-1.5 rounded-md bg-slate-100 px-2 py-1">
                    <FileQuestion size={14} /> {module.questionCount}
                  </span>
                </div>

                <div>
                  <ModuleBadge tone={getStatusTone(module.status)}>{prettyModuleStatus[module.status] || module.status}</ModuleBadge>
                </div>

                <div className="relative flex justify-start gap-2 xl:justify-end">
                  {module.status === "draft" ? (
                    <ModuleActionButton onClick={() => navigate(`/admin/learning-modules/${getLearningModuleRouteSegment(module)}/edit`)}>
                      <Edit3 size={14} strokeWidth={2.25} /> Edit
                    </ModuleActionButton>
                  ) : (
                    <ModuleActionButton onClick={() => navigate(`/admin/learning-modules/${getLearningModuleRouteSegment(module)}/preview`)}>
                      <Eye size={14} strokeWidth={2.25} /> Preview
                    </ModuleActionButton>
                  )}

                  <button
                    type="button"
                    onClick={() => setOpenMenuId((current) => current === module.skillModuleId ? null : module.skillModuleId)}
                    className="inline-grid h-8 w-8 shrink-0 place-items-center rounded-md border border-[#B9D8CC] bg-white text-slate-600 transition hover:border-[#2FA084] hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                    aria-label="More actions"
                  >
                    <MoreHorizontal size={16} strokeWidth={2.5} />
                  </button>

                  {openMenuId === module.skillModuleId && (
                    <div className="absolute right-0 top-9 z-20 w-40 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 shadow-lg">
                      {module.status === "draft" && (
                        <>
                          <OverflowAction onClick={() => {
                            setOpenMenuId(null);
                            navigate(`/admin/learning-modules/${getLearningModuleRouteSegment(module)}/preview`);
                          }}>
                            <Eye size={14} /> Preview
                          </OverflowAction>
                          <OverflowAction tone="danger" onClick={() => {
                            setOpenMenuId(null);
                            handleDelete(module);
                          }}>
                            <Trash2 size={14} /> Delete
                          </OverflowAction>
                        </>
                      )}

                      {module.status === "published" && (
                        <OverflowAction tone="danger" onClick={() => {
                          setOpenMenuId(null);
                          handleArchive(module);
                        }}>
                          <Archive size={14} /> Archive
                        </OverflowAction>
                      )}

                      {module.status === "archived" && (
                        <OverflowAction onClick={() => {
                          setOpenMenuId(null);
                          handleRestore(module);
                        }}>
                          <RotateCcw size={14} /> Restore
                        </OverflowAction>
                      )}
                    </div>
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


function ModuleActionButton({ children, onClick }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex h-8 shrink-0 items-center justify-center gap-1.5 rounded-md border border-[#B9D8CC] bg-white px-2.5 text-xs font-extrabold leading-none text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#F7F1E8]"
    >
      {children}
    </button>
  );
}

function OverflowAction({ children, tone = "default", onClick }) {
  const styles =
    tone === "danger"
      ? "text-rose-600 hover:bg-rose-50"
      : "text-slate-700 hover:bg-[#F7F1E8] hover:text-[#1F6F5F]";

  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex w-full items-center gap-2 px-3 py-2 text-left text-xs font-extrabold transition ${styles}`}
    >
      {children}
    </button>
  );
}
