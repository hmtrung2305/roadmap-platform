import { CheckCircle2, Circle, Trophy, X } from "lucide-react";
import {
  formatHours,
  getLessonProgress,
  getProgress,
  ModuleBadge,
} from "../../learningModules/components/learningModuleUi";

export default function StudyLessonSidebar({
  module,
  activeLessonId,
  showQuiz,
  isOpen,
  width,
  topOffset,
  onStartResize,
  onClose,
  onSelectLesson,
  onShowQuiz,
  canStartQuiz,
}) {
  const progress = getProgress(module);

  return (
    <>
      {isOpen && (
        <div
          className="fixed inset-0 z-30 bg-slate-950/20 xl:hidden"
          onClick={onClose}
        />
      )}

      <aside
        style={{
          width,
          top: topOffset,
          height: `calc(100vh - ${topOffset}px)`,
        }}
        className={`fixed left-0 z-40 flex max-w-[calc(100vw-1rem)] flex-col overflow-hidden border-r border-[#B9D8CC] bg-white shadow-xl shadow-emerald-900/10 transition-[transform,top,height] duration-300 ${
          isOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div
          onMouseDown={onStartResize}
          className="absolute right-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-[#6FCF97]"
          title="Resize lessons panel"
        />

        <div className="flex h-24 shrink-0 items-start justify-between gap-3 border-b border-[#B9D8CC] px-5 py-4">
          <div className="min-w-0">
            <ModuleBadge>{module.skillName || "Module"}</ModuleBadge>
            <h2 className="mt-2 line-clamp-2 text-sm font-extrabold leading-5 text-[#18332D]">
              {module.title}
            </h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-slate-400 hover:bg-[#6FCF97]/20 hover:text-[#1F6F5F]"
            aria-label="Close lessons"
          >
            <X size={18} />
          </button>
        </div>

        <div className="border-b border-[#B9D8CC] px-5 py-4">
          <div className="mb-1 flex justify-between text-xs font-bold text-slate-600">
            <span>{Math.round(progress)}% complete</span>
            <span>{module.lessons?.length || 0} lessons</span>
          </div>
          <div className="h-2 rounded-full bg-slate-100">
            <div
              className="h-2 rounded-full bg-[#2FA084]"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <div className="space-y-2">
            {(module.lessons || []).map((lesson, index) => {
              const status = getLessonProgress(
                module.enrollment,
                lesson.skillModuleLessonId,
              );
              const isActive =
                activeLessonId === lesson.skillModuleLessonId && !showQuiz;

              return (
                <button
                  key={lesson.skillModuleLessonId}
                  type="button"
                  onClick={() => onSelectLesson(lesson.skillModuleLessonId)}
                  className={`w-full rounded-lg px-3 py-3 text-left text-sm transition ${
                    isActive
                      ? "bg-[#6FCF97]/20 text-[#1F6F5F] ring-1 ring-[#6FCF97]"
                      : "text-slate-600 hover:bg-[#F7F1E8] hover:text-[#18332D]"
                  }`}
                >
                  <div className="flex items-start gap-2">
                    {status === "completed" ? (
                      <CheckCircle2
                        size={16}
                        className="mt-0.5 shrink-0 text-[#2FA084]"
                      />
                    ) : status === "in_progress" ? (
                      <Circle
                        size={16}
                        className="mt-0.5 shrink-0 text-[#2FA084]"
                      />
                    ) : (
                      <span className="mt-0.5 grid h-4 w-4 shrink-0 place-items-center rounded-full border border-slate-300 text-[10px] font-black text-slate-500">
                        {lesson.orderIndex || index + 1}
                      </span>
                    )}

                    <div className="min-w-0">
                      <p className="line-clamp-2 font-extrabold">
                        {lesson.title || "Untitled lesson"}
                      </p>

                      <p className="mt-1 text-xs opacity-75">
                        {lesson.estimatedHours
                          ? formatHours(lesson.estimatedHours)
                          : "Lesson"}
                      </p>
                    </div>
                  </div>
                </button>
              );
            })}

            <button
              type="button"
              onClick={onShowQuiz}
              disabled={!canStartQuiz}
              className={`mt-3 flex w-full items-start gap-2 rounded-lg px-3 py-3 text-left text-sm font-extrabold transition ${
                showQuiz
                  ? "bg-[#6FCF97]/20 text-[#1F6F5F] ring-1 ring-[#6FCF97]"
                  : canStartQuiz
                    ? "text-slate-600 hover:bg-[#F7F1E8] hover:text-[#18332D]"
                    : "cursor-not-allowed bg-slate-50 text-slate-400"
              }`}
              title={
                canStartQuiz
                  ? "Open final quiz"
                  : "Complete all lessons before starting the quiz."
              }
            >
              <Trophy size={16} className="mt-0.5 shrink-0" />
              <span>
                Final quiz
                {!canStartQuiz && (
                  <span className="mt-1 block text-xs font-semibold">
                    Complete all lessons first
                  </span>
                )}
              </span>
            </button>
          </div>
        </div>
      </aside>
    </>
  );
}
