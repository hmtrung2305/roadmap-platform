/* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
import { useEffect, useMemo, useState } from "react";
import { Trophy } from "lucide-react";
import { toast } from "react-toastify";
import { useLearningModuleStore } from "../../stores/useLearningModuleStore";
import { ModuleBadge, ModuleButton } from "../learningModules/learningModuleUi";
import { isSubmittedToday } from "./studyRoomUtils";
import {
  clearQuizAttemptDraft,
  loadQuizAttemptDraft,
  saveQuizAttemptDraft,
} from "./quizAttemptDraftStorage";

function getAttemptId(attempt) {
  return (
    attempt?.skillModuleQuizAttemptId ||
    attempt?.SkillModuleQuizAttemptId ||
    attempt?.attemptId ||
    attempt?.AttemptId ||
    null
  );
}

function getAttemptStatus(attempt) {
  return String(attempt?.status || attempt?.Status || "").toLowerCase();
}

function getQuestionId(question) {
  return question?.skillModuleQuizQuestionId || question?.SkillModuleQuizQuestionId || question?.questionId || question?.QuestionId;
}

function getOptionId(option) {
  return option?.skillModuleQuizOptionId || option?.SkillModuleQuizOptionId || option?.optionId || option?.OptionId;
}

function getSelectedOptionId(answer) {
  return answer?.selectedOptionId || answer?.SelectedOptionId || answer?.skillModuleQuizOptionId || answer?.SkillModuleQuizOptionId;
}

function getInitialAnswers(attempt) {
  const attemptId = getAttemptId(attempt);
  const restoredAnswers = {
    ...loadQuizAttemptDraft(attemptId),
  };
  const answers = attempt?.answers || attempt?.Answers || [];

  answers.forEach((answer) => {
    const questionId = getQuestionId(answer);
    const optionId = getSelectedOptionId(answer);

    if (questionId && optionId) {
      restoredAnswers[questionId] = optionId;
    }
  });

  const validAnswers = {};

  (attempt?.quiz?.questions || []).forEach((question) => {
    const questionId = getQuestionId(question);
    const selectedOptionId = restoredAnswers[questionId];
    const optionExists = (question.options || []).some(
      (option) => getOptionId(option) === selectedOptionId,
    );

    if (questionId && optionExists) {
      validAnswers[questionId] = selectedOptionId;
    }
  });

  return validAnswers;
}

function hasAttemptQuestions(attempt) {
  return Array.isArray(attempt?.quiz?.questions) && attempt.quiz.questions.length > 0;
}

export default function StudyQuiz({ module, canStartQuiz = true, onProgressChanged }) {
  const loadQuizAttempts = useLearningModuleStore((state) => state.loadQuizAttempts);
  const startQuizAttempt = useLearningModuleStore((state) => state.startQuizAttempt);
  const loadQuizAttemptSession = useLearningModuleStore((state) => state.loadQuizAttemptSession);
  const loadQuizAttemptReview = useLearningModuleStore((state) => state.loadQuizAttemptReview);
  const submitQuizAttempt = useLearningModuleStore((state) => state.submitQuizAttempt);
  const attempts = useLearningModuleStore((state) => state.getQuizAttempts(module?.skillModuleId));
  const isLoadingAttempts = useLearningModuleStore((state) => state.getQuizAttemptsLoading(module?.skillModuleId));

  const [attempt, setAttempt] = useState(null);
  const [answers, setAnswers] = useState({});
  const [review, setReview] = useState(null);
  const [viewMode, setViewMode] = useState("overview");
  const [isStarting, setIsStarting] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoadingReview, setIsLoadingReview] = useState(false);

  const quiz = module.quiz;
  const submittedAttempts = attempts.filter((item) => getAttemptStatus(item) === "submitted");
  const submittedAttemptsToday = submittedAttempts.filter(isSubmittedToday);
  const inProgressAttempt = useMemo(
    () => attempts.find((item) => getAttemptStatus(item) === "in_progress") || null,
    [attempts],
  );
  const remainingAttempts = quiz?.maxAttempts
    ? Math.max(quiz.maxAttempts - submittedAttemptsToday.length, 0)
    : null;
  const questions = attempt?.quiz?.questions || [];

  useEffect(() => {
    setAttempt(null);
    setReview(null);
    setAnswers({});
    setViewMode("overview");

    if (module?.skillModuleId && module?.quiz) {
      loadQuizAttempts(module.skillModuleId).catch(() => {});
    }
  }, [module?.skillModuleId, loadQuizAttempts]);

  const resumeAttempt = async (attemptId) => {
    if (!attemptId) return false;

    const data = await loadQuizAttemptSession(module.skillModuleId, attemptId, { force: true });

    if (!hasAttemptQuestions(data)) {
      return false;
    }

    setAttempt(data);
    setReview(null);
    setAnswers(getInitialAnswers(data));
    setViewMode("attempt");
    return true;
  };

  const startAttempt = async () => {
    if (!quiz || isStarting || remainingAttempts === 0) return;

    if (!canStartQuiz) {
      toast.info("Complete all lessons before starting the quiz.");
      return;
    }

    try {
      setIsStarting(true);
      setReview(null);
      setAnswers({});

      const inProgressAttemptId = getAttemptId(inProgressAttempt);

      if (inProgressAttemptId) {
        const didResume = await resumeAttempt(inProgressAttemptId).catch(() => false);

        if (didResume) {
          return;
        }
      }

      const data = await startQuizAttempt(module.skillModuleId);
      setAttempt(data);
      setAnswers(getInitialAnswers(data));
      setViewMode("attempt");
    } catch (error) {
      toast.error(error?.message || "Unable to start quiz.");
    } finally {
      setIsStarting(false);
    }
  };

  const openReview = async (attemptId) => {
    try {
      setIsLoadingReview(true);
      const data = await loadQuizAttemptReview(module.skillModuleId, attemptId);
      clearQuizAttemptDraft(attemptId);
      setReview(data);
      setAttempt(null);
      setAnswers({});
      setViewMode("review");
    } catch (error) {
      toast.error(error?.message || "Unable to load quiz attempt.");
    } finally {
      setIsLoadingReview(false);
    }
  };

  const openAttemptFromHistory = async (item) => {
    const attemptId = getAttemptId(item);
    const status = getAttemptStatus(item);

    if (!attemptId || isLoadingReview) return;

    if (status === "submitted") {
      await openReview(attemptId);
      return;
    }

    if (status !== "in_progress") return;

    try {
      setIsLoadingReview(true);
      await resumeAttempt(attemptId);
    } catch (error) {
      toast.error(error?.message || "Unable to resume quiz attempt.");
    } finally {
      setIsLoadingReview(false);
    }
  };

  const handleSubmit = async () => {
    if (!attempt || isSubmitting) return;

    const payload = questions.map((question) => {
      const questionId = getQuestionId(question);

      return {
        skillModuleQuizQuestionId: questionId,
        selectedOptionId: answers[questionId],
      };
    });

    if (payload.some((item) => !item.selectedOptionId)) {
      toast.error("Answer every question before submitting.");
      return;
    }

    try {
      setIsSubmitting(true);
      const data = await submitQuizAttempt(
        module.skillModuleId,
        getAttemptId(attempt),
        payload,
      );

      clearQuizAttemptDraft(getAttemptId(attempt));
      setReview(data);
      setAttempt(null);
      setAnswers({});
      setViewMode("review");
      await onProgressChanged?.();
      toast.success("Quiz submitted.");
    } catch (error) {
      toast.error(error?.message || "Unable to submit quiz.");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!quiz) {
    return (
      <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 text-sm font-bold text-slate-600">
        Quiz is not available.
      </div>
    );
  }

  if (viewMode === "review" && review) {
    return (
      <QuizReview
        review={review}
        isLoadingReview={isLoadingReview}
        onBack={() => setViewMode("overview")}
      />
    );
  }

  if (viewMode === "attempt" && attempt) {
    return (
      <div className="space-y-4 rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="flex items-center gap-2 text-xl font-black text-[#18332D]">
              <Trophy size={20} /> {attempt.quiz?.title || quiz.title || "Module quiz"}
            </h2>
            <p className="mt-1 text-sm font-semibold text-slate-600">
              Attempt {attempt.attemptNo}. Answer every question before submitting.
            </p>
          </div>
          <ModuleButton variant="secondary" onClick={() => setViewMode("overview")}>Back to overview</ModuleButton>
        </div>

        {questions.map((question, index) => {
          const questionId = getQuestionId(question);

          return (
            <div key={questionId} className="rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/40 p-4">
              <div className="text-sm font-extrabold text-[#18332D]">
                {index + 1}. {question.questionText}
              </div>

              <div className="mt-3 space-y-2">
                {(question.options || []).map((option) => {
                  const optionId = getOptionId(option);

                  return (
                    <label
                      key={optionId}
                      className={`flex cursor-pointer items-center gap-3 rounded-lg border px-3 py-2 text-sm font-semibold transition ${
                        answers[questionId] === optionId
                          ? "border-[#2FA084] bg-[#6FCF97]/15 text-[#1F6F5F]"
                          : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-white/70"
                      }`}
                    >
                      <input
                        type="radio"
                        className="h-4 w-4 accent-[#2FA084]"
                        name={questionId}
                        checked={answers[questionId] === optionId}
                        onChange={() =>
                          setAnswers((current) => {
                            const nextAnswers = {
                              ...current,
                              [questionId]: optionId,
                            };

                            saveQuizAttemptDraft(getAttemptId(attempt), nextAnswers);
                            return nextAnswers;
                          })
                        }
                      />
                      {option.optionText}
                    </label>
                  );
                })}
              </div>
            </div>
          );
        })}

        <div className="flex justify-end">
          <ModuleButton onClick={handleSubmit} disabled={isSubmitting || questions.length === 0}>
            {isSubmitting ? "Submitting..." : "Submit quiz"}
          </ModuleButton>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h2 className="flex items-center gap-2 text-xl font-black text-[#18332D]">
              <Trophy size={20} /> {quiz.title || "Module quiz"}
            </h2>
            {quiz.description && (
              <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
                {quiz.description}
              </p>
            )}
          </div>
          <ModuleBadge tone={remainingAttempts === 0 ? "rose" : "green"}>
            {remainingAttempts === null ? "Unlimited attempts" : `${remainingAttempts} attempts left today`}
          </ModuleBadge>
        </div>

        <div className="mt-5 grid gap-3 sm:grid-cols-3">
          <QuizStat label="Questions" value={quiz.questionCount || 0} />
          <QuizStat label="Passing score" value={`${quiz.passingScorePercent ?? 0}%`} />
          <QuizStat label="Attempts per day" value={quiz.maxAttempts || "Unlimited"} />
        </div>

        <div className="mt-5 flex justify-end">
          <ModuleButton onClick={startAttempt} disabled={isStarting || remainingAttempts === 0 || !canStartQuiz}>
            {isStarting
              ? inProgressAttempt
                ? "Resuming..."
                : "Starting..."
              : !canStartQuiz
                ? "Locked"
                : inProgressAttempt
                  ? "Resume quiz"
                  : "Start quiz"}
          </ModuleButton>
        </div>
      </div>

      <div className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
        <div className="flex items-center justify-between gap-3">
          <h3 className="text-sm font-extrabold text-[#18332D]">Past attempts</h3>
          {isLoadingAttempts && <span className="text-xs font-bold text-slate-500">Loading...</span>}
        </div>

        {attempts.length === 0 ? (
          <p className="mt-3 text-sm font-semibold text-slate-600">No attempts yet.</p>
        ) : (
          <div className="mt-3 space-y-2">
            {attempts.map((item) => {
              const status = getAttemptStatus(item);
              const attemptId = getAttemptId(item);
              const canOpenAttempt = status === "submitted" || status === "in_progress";

              return (
                <button
                  key={attemptId}
                  type="button"
                  disabled={!canOpenAttempt || isLoadingReview}
                  onClick={() => openAttemptFromHistory(item)}
                  className="grid w-full gap-3 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/45 px-4 py-3 text-left transition hover:bg-[#F7F1E8] disabled:cursor-default disabled:opacity-70 sm:grid-cols-[120px_1fr_120px] sm:items-center"
                >
                  <div className="text-sm font-extrabold text-[#18332D]">Attempt {item.attemptNo}</div>
                  <div className="text-sm font-semibold text-slate-600">
                    {status === "submitted" ? `${item.scorePercent ?? 0}% score` : "In progress"}
                  </div>
                  <div className={`text-sm font-extrabold ${item.passed ? "text-[#1F6F5F]" : "text-rose-600"}`}>
                    {status !== "submitted" ? "Resume" : item.passed ? "Passed" : "Not passed"}
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

function QuizStat({ label, value }) {
  return (
    <div className="rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/60 px-4 py-3">
      <div className="text-xs font-extrabold uppercase tracking-[0.12em] text-[#1F6F5F]">{label}</div>
      <div className="mt-1 text-lg font-black text-[#18332D]">{value}</div>
    </div>
  );
}

function QuizReview({ review, isLoadingReview, onBack }) {
  return (
    <div className="space-y-4 rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/70 p-4">
        <div>
          <div className="text-lg font-extrabold text-[#18332D]">
            Attempt {review.attemptNo} · {review.scorePercent ?? 0}%
          </div>
          <div className="mt-1 text-sm font-bold text-slate-700">
            {review.earnedPoints}/{review.totalPoints} points · {review.passed ? "Passed" : "Not passed"}
          </div>
        </div>
        <ModuleButton variant="secondary" onClick={onBack} disabled={isLoadingReview}>Back to quiz</ModuleButton>
      </div>

      {(review.answers || []).map((answer, index) => (
        <div key={answer.skillModuleQuizAnswerId || `${answer.skillModuleQuizQuestionId}-${index}`} className="rounded-lg border border-[#B9D8CC] bg-white p-4">
          <div className="text-sm font-extrabold text-[#18332D]">
            {index + 1}. {answer.questionText}
          </div>
          <div className="mt-3 rounded-lg border border-slate-200 bg-[#F7F1E8]/50 px-3 py-2 text-sm font-bold text-slate-700">
            Your answer: {answer.selectedOptionText}
          </div>
          <div className={`mt-2 text-sm font-extrabold ${answer.isCorrect ? "text-[#1F6F5F]" : "text-rose-600"}`}>
            {answer.isCorrect ? "Correct" : "Incorrect"}
          </div>
        </div>
      ))}
    </div>
  );
}
