import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
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


const lessonIndexingMeta = {
  pending: { label: "Pending", tone: "amber" },
  indexing: { label: "Indexing", tone: "amber" },
  indexed: { label: "Indexed", tone: "green" },
  failed: { label: "Failed", tone: "rose" },
  needs_reindex: { label: "Reindex", tone: "amber" },
};

function getLessonIndexingStatus(lesson) {
  if (lesson?.indexingStatus) return lesson.indexingStatus;
  if (lesson?.chunkCount > 0 || lesson?.chunksGenerated > 0) return "indexed";
  return "pending";
}

function LessonIndexingBadge({ lesson }) {
  const meta = lessonIndexingMeta[getLessonIndexingStatus(lesson)] || lessonIndexingMeta.pending;

  return (
    <ModuleBadge tone={meta.tone} className="shrink-0">
      {meta.label}
    </ModuleBadge>
  );
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


export default function AdminLearningModulePreviewPage() {
  const { moduleSlug } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const routeStateModuleId = location.state?.moduleId || null;
  const resolvedModuleIdRef = useRef(routeStateModuleId);
  const [detail, setDetail] = useState(null);
  const [activeLessonId, setActiveLessonId] = useState(null);
  const [lessonPreview, setLessonPreview] = useState(null);
  const [resolvedModuleId, setResolvedModuleId] = useState(routeStateModuleId);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let ignore = false;

    async function loadDetail() {
      try {
        setIsLoading(true);
        const knownModuleId = routeStateModuleId || resolvedModuleIdRef.current;
        const moduleId = await counselorLearningModuleApi.resolveModuleIdFromRoute(moduleSlug, knownModuleId);
        const data = await counselorLearningModuleApi.getModule(moduleId);

        if (ignore) return;

        resolvedModuleIdRef.current = moduleId;
        setResolvedModuleId(moduleId);
        setDetail(data);
        setActiveLessonId(data.lessons?.[0]?.skillModuleLessonId || (data.quiz ? "quiz" : null));
      } catch (err) {
        if (!ignore) {
          setResolvedModuleId(null);
          toast.error(err?.message || "Unable to load module preview.");
        }
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    loadDetail();

    return () => {
      ignore = true;
    };
  }, [moduleSlug, routeStateModuleId]);

  useEffect(() => {
    let ignore = false;

    async function loadLessonPreview() {
      if (!activeLessonId || activeLessonId === "quiz") {
        setLessonPreview(null);
        return;
      }

      if (!resolvedModuleId) {
        setLessonPreview(null);
        return;
      }

      try {
        const data = await counselorLearningModuleApi.getLessonPreview(resolvedModuleId, activeLessonId);
        if (!ignore) setLessonPreview(data);
      } catch {
        if (!ignore) setLessonPreview(null);
      }
    }

    loadLessonPreview();

    return () => {
      ignore = true;
    };
  }, [resolvedModuleId, activeLessonId]);

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
  const isQuizActive = activeLessonId === "quiz";

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
                  <span className="min-w-0 flex-1 truncate">{lesson.title}</span>
                  <LessonIndexingBadge lesson={lesson} />
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
                {isQuizActive ? "Final quiz preview" : lessonPreview?.title || "Module preview"}
              </h2>
            </div>
            <div className="max-h-[calc(100vh-17rem)] overflow-auto p-6">
              {isQuizActive ? (
                <QuizPreviewPanel quiz={detail.quiz} />
              ) : (
                <MarkdownRenderer markdown={lessonPreview?.markdown || "# Empty preview\n\nUpload lessons to preview the learner reading experience."} />
              )}
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
