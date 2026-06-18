import { ArrowLeft, CheckCircle2, Clock, FileText } from "lucide-react";
import { formatHours } from "../learningModules/learningModuleUi";

export default function StudyRoomHeader({
  module,
  activeLesson,
  showQuiz,
  activeLessonStatus,
  isCompletingLesson = false,
  onBack,
  onCompleteLesson,
}) {
  return (
    <header className="h-[72px] border-b border-[#B9D8CC] bg-white/95 shadow-sm shadow-emerald-900/5 backdrop-blur-xl">
      <div className="flex h-full items-center justify-between gap-4 px-4 sm:px-6">
        <div className="flex min-w-0 items-center gap-4">
          <button
            type="button"
            onClick={onBack}
            className="inline-flex shrink-0 items-center gap-2 rounded-lg px-3 py-2 text-sm font-bold text-slate-600 transition hover:bg-[#6FCF97]/20 hover:text-[#1F6F5F]"
          >
            <ArrowLeft size={18} />
            Back to modules
          </button>

          <div className="hidden h-8 w-px bg-[#B9D8CC] sm:block" />

          <div className="min-w-0">
            <h1 className="line-clamp-1 text-lg font-extrabold text-[#1F6F5F]">
              {showQuiz ? `${module.title} Quiz` : activeLesson?.title || module.title}
            </h1>

            <div className="mt-1 flex flex-wrap items-center gap-3 text-xs font-medium text-slate-500">
              <span className="inline-flex items-center gap-1">
                <FileText size={13} />
                {module.skillName || "Learning module"}
              </span>

              {!showQuiz && activeLesson?.estimatedHours && (
                <span className="inline-flex items-center gap-1">
                  <Clock size={13} />
                  {formatHours(activeLesson.estimatedHours)}
                </span>
              )}
            </div>
          </div>
        </div>

        {!showQuiz && activeLesson && (
          <button
            type="button"
            onClick={onCompleteLesson}
            disabled={activeLessonStatus === "completed" || isCompletingLesson}
            className="inline-flex shrink-0 items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2 text-sm font-bold text-white shadow-sm transition hover:bg-[#1F6F5F] disabled:bg-slate-300 disabled:text-slate-600"
          >
            <CheckCircle2 size={16} />
            {activeLessonStatus === "completed"
              ? "Completed"
              : isCompletingLesson
                ? "Saving..."
                : "Mark as Completed"}
          </button>
        )}
      </div>
    </header>
  );
}
