import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { toast } from "react-toastify";
import { counselorLearningModuleApi } from "../../../api/learningModuleApi";
import MarkdownRenderer from "../../../components/learningModules/MarkdownRenderer";
import {
  inputClass,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../../components/learningModules/learningModuleUi";

export default function AdminLearningModulePreviewPage() {
  const { moduleId } = useParams();
  const navigate = useNavigate();
  const [detail, setDetail] = useState(null);
  const [activeLessonId, setActiveLessonId] = useState(null);
  const [lessonPreview, setLessonPreview] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let ignore = false;

    async function loadDetail() {
      try {
        setIsLoading(true);
        const data = await counselorLearningModuleApi.getModule(moduleId);
        if (ignore) return;
        setDetail(data);
        setActiveLessonId(data.lessons?.[0]?.skillModuleLessonId || null);
      } catch (err) {
        if (!ignore) toast.error(err?.message || "Unable to load module preview.");
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    loadDetail();

    return () => {
      ignore = true;
    };
  }, [moduleId]);

  useEffect(() => {
    let ignore = false;

    async function loadLessonPreview() {
      if (!activeLessonId) {
        setLessonPreview(null);
        return;
      }

      try {
        const data = await counselorLearningModuleApi.getLessonPreview(moduleId, activeLessonId);
        if (!ignore) setLessonPreview(data);
      } catch {
        if (!ignore) setLessonPreview(null);
      }
    }

    loadLessonPreview();

    return () => {
      ignore = true;
    };
  }, [moduleId, activeLessonId]);

  if (isLoading) {
    return (
      <ModulePageShell compact>
        <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">Loading preview...</ModuleCard>
      </ModulePageShell>
    );
  }

  if (!detail?.module) {
    return (
      <ModulePageShell>
        <ModuleEmptyState title="Module not found">This preview could not be loaded.</ModuleEmptyState>
      </ModulePageShell>
    );
  }

  const module = detail.module;

  return (
    <ModulePageShell compact>
      <div className="space-y-4">
        <button
          type="button"
          onClick={() => navigate("/admin/learning-modules")}
          className="inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
        >
          <ArrowLeft size={16} /> Back to management
        </button>

        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[#B9D8CC] bg-[#F7F1E8]/70 px-4 py-3">
          <div>
            <h1 className="text-lg font-extrabold text-[#18332D]">Learner preview</h1>
            <p className="text-sm font-semibold text-slate-700">Check how the module reader feels before publishing.</p>
          </div>
          <ModuleBadge tone="slate">{module.status}</ModuleBadge>
        </div>

        <div className="grid min-h-[calc(100vh-12rem)] gap-4 lg:grid-cols-[280px_minmax(0,1fr)_340px]">
          <ModuleCard className="overflow-hidden">
            <div className="border-b border-[#B9D8CC] p-4">
              <ModuleBadge tone="green">{module.skillName}</ModuleBadge>
              <h1 className="mt-2 text-lg font-extrabold leading-6 text-[#18332D]">{module.title}</h1>
            </div>
            <div className="space-y-1 p-2">
              {detail.lessons.length === 0 && (
                <div className="rounded-lg bg-[#F7F1E8] px-3 py-3 text-sm font-semibold text-slate-700">
                  No lessons uploaded yet.
                </div>
              )}
              {detail.lessons.map((lesson) => (
                <button
                  key={lesson.skillModuleLessonId}
                  type="button"
                  onClick={() => setActiveLessonId(lesson.skillModuleLessonId)}
                  className={`flex w-full items-center gap-2 rounded-lg px-3 py-2.5 text-left text-sm font-bold ${
                    activeLessonId === lesson.skillModuleLessonId
                      ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
                      : "text-slate-700 hover:bg-[#F7F1E8]"
                  }`}
                >
                  <span>{lesson.orderIndex}</span>
                  {lesson.title}
                </button>
              ))}
              {detail.quiz && (
                <div className="mt-2 rounded-lg px-3 py-2.5 text-sm font-bold text-slate-700">
                  Quiz · {detail.quiz.questions?.length || 0} questions
                </div>
              )}
            </div>
          </ModuleCard>

          <ModuleCard className="overflow-hidden">
            <div className="border-b border-[#B9D8CC] px-5 py-4">
              <h2 className="text-lg font-extrabold text-[#18332D]">{lessonPreview?.title || "Module preview"}</h2>
            </div>
            <div className="max-h-[calc(100vh-17rem)] overflow-auto p-6">
              <MarkdownRenderer markdown={lessonPreview?.markdown || "# Empty preview\n\nUpload lessons to preview the learner reading experience."} />
            </div>
          </ModuleCard>

          <ModuleCard className="flex min-h-0 flex-col overflow-hidden">
            <div className="border-b border-[#B9D8CC] px-4 py-3">
              <h2 className="text-sm font-extrabold text-[#18332D]">Module chat preview</h2>
            </div>
            <div className="flex-1 space-y-3 overflow-auto p-4">
              <div className="rounded-lg bg-[#F7F1E8] p-3 text-sm font-semibold leading-6 text-slate-700">
                Chat becomes available when learners start the published module. Preview does not call the chat endpoint.
              </div>
            </div>
            <div className="border-t border-[#B9D8CC] p-3">
              <textarea disabled className={`${inputClass} min-h-20 resize-none bg-slate-50`} placeholder="Preview only" />
              <ModuleButton className="mt-2 w-full" disabled>Send</ModuleButton>
            </div>
          </ModuleCard>
        </div>
      </div>
    </ModulePageShell>
  );
}
