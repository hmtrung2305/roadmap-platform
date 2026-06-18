import { useEffect, useState } from "react";
import { contentManagerLearningModuleApi } from "../../../api/learningModuleApi";
import MarkdownRenderer from "../../../components/learningModules/MarkdownRenderer";
import { inputClass, ModuleBadge, ModuleButton, ModuleCard, ModuleEmptyState } from "../../../components/learningModules/learningModuleUi";

export default function ModulePreviewTab({ moduleId, detail }) {
  return <PreviewShell moduleId={moduleId} detail={detail} />;
}


function QuizPreviewPanel({ quiz }) {
  if (!quiz) {
    return (
      <ModuleEmptyState title="No quiz yet">
        Create a quiz to preview the final assessment.
      </ModuleEmptyState>
    );
  }

  const questions = (quiz.questions || [])
    .slice()
    .sort((a, b) => a.orderIndex - b.orderIndex);

  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-[#B9D8CC] bg-[#F7F1E8]/70 p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <ModuleBadge tone="green">Final quiz</ModuleBadge>
            <h2 className="mt-2 text-xl font-extrabold text-[#18332D]">
              {quiz.title || "Untitled quiz"}
            </h2>
            {quiz.description && (
              <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">
                {quiz.description}
              </p>
            )}
          </div>

          <div className="flex flex-wrap gap-2">
            <ModuleBadge tone="slate">
              {questions.length} {questions.length === 1 ? "question" : "questions"}
            </ModuleBadge>
            <ModuleBadge tone="amber">
              Pass {quiz.passingScorePercent ?? 0}%
            </ModuleBadge>
            <ModuleBadge tone="slate">
              {quiz.maxAttempts ? `${quiz.maxAttempts} attempts/day` : "Unlimited attempts"}
            </ModuleBadge>
          </div>
        </div>

      </div>

      {questions.length === 0 ? (
        <ModuleEmptyState title="No questions yet">
          Add questions in the Quiz tab to preview them here.
        </ModuleEmptyState>
      ) : (
        <div className="space-y-3">
          {questions.map((question, index) => {
            const options = (question.options || [])
              .slice()
              .sort((a, b) => a.orderIndex - b.orderIndex);

            return (
              <ModuleCard key={question.skillModuleQuizQuestionId || index} className="p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <div className="text-xs font-extrabold uppercase tracking-[0.12em] text-[#1F6F5F]">
                      Question {index + 1}
                    </div>
                    <h3 className="mt-1 text-base font-extrabold leading-6 text-[#18332D]">
                      {question.questionText || "Untitled question"}
                    </h3>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <ModuleBadge tone="slate">
                      {question.points || 1} {(question.points || 1) === 1 ? "point" : "points"}
                    </ModuleBadge>
                    <ModuleBadge tone="slate">
                      {question.questionType === "multiple_choice" ? "Multiple choice" : "Single choice"}
                    </ModuleBadge>
                  </div>
                </div>

                <div className="mt-4 space-y-2">
                  {options.map((option, optionIndex) => (
                    <div
                      key={option.skillModuleQuizOptionId || optionIndex}
                      className={`flex items-start gap-3 rounded-lg border px-3 py-2.5 text-sm font-semibold leading-6 ${
                        option.isCorrect
                          ? "border-[#6FCF97] bg-[#6FCF97]/15 text-[#18332D]"
                          : "border-[#B9D8CC] bg-white text-slate-700"
                      }`}
                    >
                      <span
                        className={`mt-0.5 inline-grid h-6 w-6 shrink-0 place-items-center rounded-full text-xs font-extrabold ${
                          option.isCorrect
                            ? "bg-[#1F6F5F] text-white"
                            : "bg-[#F7F1E8] text-slate-600"
                        }`}
                      >
                        {String.fromCharCode(65 + optionIndex)}
                      </span>

                      <div className="min-w-0 flex-1">
                        <div>{option.optionText || "Untitled option"}</div>
                        {option.explanation && (
                          <div className="mt-1 text-xs font-semibold leading-5 text-slate-600">
                            {option.explanation}
                          </div>
                        )}
                      </div>

                      {option.isCorrect && (
                        <ModuleBadge tone="green" className="shrink-0">
                          Correct
                        </ModuleBadge>
                      )}
                    </div>
                  ))}
                </div>

                {question.explanation && (
                  <div className="mt-3 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-2 text-sm font-semibold leading-6 text-slate-700">
                    <span className="font-extrabold text-[#18332D]">Explanation:</span>{" "}
                    {question.explanation}
                  </div>
                )}
              </ModuleCard>
            );
          })}
        </div>
      )}
    </div>
  );
}


function PreviewShell({ moduleId, detail }) {
  const lessons = detail.lessons || [];
  const [activeLessonId, setActiveLessonId] = useState(lessons[0]?.skillModuleLessonId || (detail.quiz ? "quiz" : null));
  const [preview, setPreview] = useState(null);

  useEffect(() => {
    setActiveLessonId((current) => {
      if (lessons.some((lesson) => lesson.skillModuleLessonId === current)) {
        return current;
      }

      if (current === "quiz" && detail.quiz) {
        return current;
      }

      return lessons[0]?.skillModuleLessonId || (detail.quiz ? "quiz" : null);
    });
  }, [lessons, detail.quiz]);

  useEffect(() => {
    let ignore = false;

    async function loadPreview() {
      if (!moduleId || !activeLessonId || activeLessonId === "quiz") {
        setPreview(null);
        return;
      }

      try {
        const data = await contentManagerLearningModuleApi.getLessonPreview(moduleId, activeLessonId);
        if (!ignore) setPreview(data);
      } catch {
        if (!ignore) setPreview(null);
      }
    }

    loadPreview();

    return () => {
      ignore = true;
    };
  }, [moduleId, activeLessonId]);

  const module = detail.module;
  const isQuizActive = activeLessonId === "quiz";

  return (
    <div className="grid min-h-[680px] gap-4 lg:grid-cols-[260px_minmax(0,1fr)_320px]">
      <ModuleCard className="overflow-hidden">
        <div className="border-b border-[#B9D8CC] p-4">
          <h1 className="text-lg font-extrabold text-[#18332D]">{module.title}</h1>
          <div className="mt-2 text-sm font-bold text-[#1F6F5F]">{module.skillName}</div>
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
            <button
              type="button"
              onClick={() => setActiveLessonId("quiz")}
              className={`mt-2 flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2.5 text-left text-sm font-bold ${
                isQuizActive
                  ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
                  : "text-slate-700 hover:bg-[#F7F1E8]"
              }`}
            >
              <span>Final quiz</span>
              <ModuleBadge tone="slate">
                {detail.quiz.questions?.length || 0} questions
              </ModuleBadge>
            </button>
          )}
        </div>
      </ModuleCard>

      <ModuleCard className="overflow-hidden">
        <div className="border-b border-[#B9D8CC] px-5 py-4">
          <h2 className="text-lg font-extrabold text-[#18332D]">
            {isQuizActive ? "Final quiz preview" : preview?.title || "Module preview"}
          </h2>
        </div>
        <div className="max-h-[620px] overflow-auto p-6">
          {isQuizActive ? (
            <QuizPreviewPanel quiz={detail.quiz} />
          ) : (
            <MarkdownRenderer markdown={preview?.markdown || "# Empty preview\n\nUpload lessons to preview the learner reading experience."} />
          )}
        </div>
      </ModuleCard>

      <ModuleCard className="flex min-h-0 flex-col overflow-hidden">
        <div className="border-b border-[#B9D8CC] px-4 py-3">
          <h2 className="text-sm font-extrabold text-[#18332D]">Module chat preview</h2>
        </div>
        <div className="flex-1 space-y-3 overflow-auto p-4">
          <div className="rounded-lg bg-[#F7F1E8] p-3 text-sm font-semibold leading-6 text-slate-700">
            Chat becomes available when learners start the published module.
          </div>
        </div>
        <div className="border-t border-[#B9D8CC] p-3">
          <textarea disabled className={`${inputClass} min-h-20 resize-none bg-slate-50`} placeholder="Preview only" />
          <ModuleButton className="mt-2 w-full" disabled>Send</ModuleButton>
        </div>
      </ModuleCard>
    </div>
  );
}


