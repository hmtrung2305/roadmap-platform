/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from "react";
import { ChevronDown, ChevronUp, GripVertical, Minus, Plus, Save, Settings, Trash2 } from "lucide-react";
import { toast } from "react-toastify";
import { contentManagerLearningModuleApi } from "../../../api/learningModuleApi";
import ConfirmActionDialog from "../../../components/common/ConfirmActionDialog";
import { inputClass, numberInputClass, ModuleBadge, ModuleButton, ModuleCard, ModuleEmptyState, ModuleField } from "../../learningModules/components/learningModuleUi";
import { DirtyStateBadge } from "../EditorControls";
import { createEmptyQuestionPayload, getEditorStorageKey, getUnsavedQuizQuestions, hasQuestionDraftChanges, hasQuizDraftChanges, isUnsavedQuestion, mergeQuizQuestions, readSessionJson, readSessionValue, removeSessionValue, toQuestionPayload, writeSessionJson, writeSessionValue } from "../editorUtils";

export default function ModuleQuizTab({ module, quiz, onChanged, onDirtyStateChange }) {
  const hasQuiz = Boolean(quiz);
  const draftStorageKey = getEditorStorageKey(module.skillModuleId, "quizDraftQuestions");
  const activeQuestionStorageKey = getEditorStorageKey(module.skillModuleId, "activeQuizQuestionId");
  const initialQuestions = mergeQuizQuestions(
    quiz?.questions || [],
    readSessionJson(draftStorageKey)?.questions || [],
  );

  const [quizForm, setQuizForm] = useState({
    title: quiz?.title || "",
    passingScorePercent: quiz?.passingScorePercent ?? 70,
    maxAttempts: quiz?.maxAttempts ?? 3,
  });
  const [questions, setQuestions] = useState(initialQuestions);
  const [activeQuestionId, setActiveQuestionId] = useState(() =>
    readSessionValue(activeQuestionStorageKey) || initialQuestions[0]?.skillModuleQuizQuestionId || null,
  );
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isSavingQuiz, setIsSavingQuiz] = useState(false);
  const [isSavingOrder, setIsSavingOrder] = useState(false);
  const [isQuestionOrderDirty, setIsQuestionOrderDirty] = useState(false);
  const [draggedQuestionId, setDraggedQuestionId] = useState(null);
  const [questionDropTarget, setQuestionDropTarget] = useState(null);
  const [questionToDelete, setQuestionToDelete] = useState(null);
  const [unsavedQuestionToDelete, setUnsavedQuestionToDelete] = useState(null);

  useEffect(() => {
    const serverQuestions = quiz?.questions || [];
    const draftQuestions = readSessionJson(draftStorageKey)?.questions || [];
    const nextQuestions = mergeQuizQuestions(serverQuestions, draftQuestions);

    setQuizForm({
      title: quiz?.title || "",
      passingScorePercent: quiz?.passingScorePercent ?? 70,
      maxAttempts: quiz?.maxAttempts ?? 3,
    });
    setQuestions(nextQuestions);
    setActiveQuestionId((current) => {
      if (nextQuestions.some((question) => question.skillModuleQuizQuestionId === current)) {
        return current;
      }

      const storedQuestionId = readSessionValue(activeQuestionStorageKey);
      if (storedQuestionId && nextQuestions.some((question) => question.skillModuleQuizQuestionId === storedQuestionId)) {
        return storedQuestionId;
      }

      return nextQuestions[0]?.skillModuleQuizQuestionId || null;
    });
    setIsQuestionOrderDirty(false);
  }, [quiz, draftStorageKey, activeQuestionStorageKey]);

  const orderedQuestions = questions.slice().sort((a, b) => a.orderIndex - b.orderIndex);
  const activeQuestion =
    orderedQuestions.find((question) => question.skillModuleQuizQuestionId === activeQuestionId)
    || orderedQuestions[0]
    || null;
  const activeQuestionIndex = activeQuestion
    ? orderedQuestions.findIndex((question) => question.skillModuleQuizQuestionId === activeQuestion.skillModuleQuizQuestionId)
    : -1;

  const hasUnsavedQuestions = orderedQuestions.some(isUnsavedQuestion);
  const hasDirtyQuestions = orderedQuestions.some((question) => question.isDirty);
  const hasQuizSettingsChanges = hasQuizDraftChanges(quizForm, quiz);
  const hasNewQuizDraft = !hasQuiz && (
    Boolean(quizForm.title.trim())
    || Number(quizForm.passingScorePercent) !== 70
    || Number(quizForm.maxAttempts) !== 3
  );
  const hasQuizDraftWork = hasUnsavedQuestions || hasDirtyQuestions || hasQuizSettingsChanges || hasNewQuizDraft || isQuestionOrderDirty;
  const canSaveQuestionOrder = orderedQuestions.length > 1 && !hasUnsavedQuestions && isQuestionOrderDirty;
  const updateQuiz = (key, value) => setQuizForm((current) => ({ ...current, [key]: value }));

  useEffect(() => {
    const unsavedQuestions = getUnsavedQuizQuestions(questions);

    if (unsavedQuestions.length > 0) {
      writeSessionJson(draftStorageKey, {
        questions: unsavedQuestions,
        updatedAt: Date.now(),
      });
    } else {
      removeSessionValue(draftStorageKey);
    }

  }, [questions, draftStorageKey]);

  useEffect(() => {
    onDirtyStateChange?.("quiz", hasQuizDraftWork);
  }, [hasQuizDraftWork, onDirtyStateChange]);

  useEffect(() => () => {
    onDirtyStateChange?.("quiz", false);
  }, [onDirtyStateChange]);

  useEffect(() => {
    if (activeQuestionId) {
      writeSessionValue(activeQuestionStorageKey, activeQuestionId);
    } else {
      removeSessionValue(activeQuestionStorageKey);
    }
  }, [activeQuestionId, activeQuestionStorageKey]);

  const adjustMaxAttempts = (delta) => {
    setQuizForm((current) => {
      const currentValue = Number(current.maxAttempts || 1);
      return { ...current, maxAttempts: Math.max(1, currentValue + delta) };
    });
  };

  const saveQuiz = async () => {
    if (!quizForm.title.trim()) {
      toast.error("Quiz title is required.");
      return;
    }

    try {
      setIsSavingQuiz(true);
      await contentManagerLearningModuleApi.upsertQuiz(module.skillModuleId, {
        title: quizForm.title.trim(),
        description: null,
        passingScorePercent: Number(quizForm.passingScorePercent),
        maxAttempts: Number(quizForm.maxAttempts),
      });
      toast.success(hasQuiz ? "Quiz settings saved." : "Quiz created.");
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to save quiz.");
    } finally {
      setIsSavingQuiz(false);
    }
  };

  const addQuestion = () => {
    const nextQuestion = createEmptyQuestionPayload(orderedQuestions.length + 1);

    setQuestions((current) => [...current, nextQuestion]);
    setActiveQuestionId(nextQuestion.skillModuleQuizQuestionId);
  };

  const saveQuestion = async (question) => {
    const isNewQuestion = isUnsavedQuestion(question);

    try {
      if (isNewQuestion) {
        const savedQuestion = await contentManagerLearningModuleApi.addQuestion(module.skillModuleId, toQuestionPayload(question));

        setQuestions((current) =>
          savedQuestion?.skillModuleQuizQuestionId
            ? current.map((item) =>
              item.skillModuleQuizQuestionId === question.skillModuleQuizQuestionId ? savedQuestion : item,
            )
            : current.filter((item) => item.skillModuleQuizQuestionId !== question.skillModuleQuizQuestionId),
        );

        setActiveQuestionId(savedQuestion?.skillModuleQuizQuestionId || null);
        toast.success("Question added.");
      } else {
        const savedQuestion = await contentManagerLearningModuleApi.updateQuestion(
          module.skillModuleId,
          question.skillModuleQuizQuestionId,
          toQuestionPayload(question),
        );

        if (savedQuestion?.skillModuleQuizQuestionId) {
          setQuestions((current) =>
            current.map((item) =>
              item.skillModuleQuizQuestionId === savedQuestion.skillModuleQuizQuestionId ? savedQuestion : item,
            ),
          );
        }

        setActiveQuestionId(savedQuestion?.skillModuleQuizQuestionId || question.skillModuleQuizQuestionId);
        toast.success("Question saved.");
      }

      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to save question.");
    }
  };

  const requestDeleteQuestion = (question) => {
    if (isUnsavedQuestion(question)) {
      setUnsavedQuestionToDelete(question);
      return;
    }

    setQuestionToDelete(question);
  };

  const confirmDeleteUnsavedQuestion = () => {
    if (!unsavedQuestionToDelete) return;

    const nextActiveQuestion = orderedQuestions.find(
      (item) => item.skillModuleQuizQuestionId !== unsavedQuestionToDelete.skillModuleQuizQuestionId,
    );

    setQuestions((current) =>
      current.filter((item) => item.skillModuleQuizQuestionId !== unsavedQuestionToDelete.skillModuleQuizQuestionId),
    );
    setActiveQuestionId(nextActiveQuestion?.skillModuleQuizQuestionId || null);
    setUnsavedQuestionToDelete(null);
  };

  const confirmDeleteQuestion = async () => {
    if (!questionToDelete) return;

    const nextActiveQuestion = orderedQuestions.find((item) => item.skillModuleQuizQuestionId !== questionToDelete.skillModuleQuizQuestionId);

    try {
      await contentManagerLearningModuleApi.deleteQuestion(module.skillModuleId, questionToDelete.skillModuleQuizQuestionId);
      toast.success("Question deleted.");
      setActiveQuestionId(nextActiveQuestion?.skillModuleQuizQuestionId || null);
      setQuestionToDelete(null);
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to delete question.");
    }
  };

  const reorderQuestions = (sourceId, targetId, position = "before") => {
    if (!sourceId || !targetId || sourceId === targetId) return;

    const sourceIndex = orderedQuestions.findIndex((question) => question.skillModuleQuizQuestionId === sourceId);
    const targetIndex = orderedQuestions.findIndex((question) => question.skillModuleQuizQuestionId === targetId);

    if (sourceIndex < 0 || targetIndex < 0) return;

    const next = orderedQuestions.slice();
    const [moved] = next.splice(sourceIndex, 1);
    const adjustedTargetIndex = next.findIndex((question) => question.skillModuleQuizQuestionId === targetId);

    if (adjustedTargetIndex < 0) return;

    const insertIndex = position === "after" ? adjustedTargetIndex + 1 : adjustedTargetIndex;
    next.splice(insertIndex, 0, moved);

    setQuestions(next.map((question, index) => ({ ...question, orderIndex: index + 1 })));
    setIsQuestionOrderDirty(true);
  };

  const updateQuestionDropTarget = (event, questionId) => {
    if (!draggedQuestionId || draggedQuestionId === questionId) {
      setQuestionDropTarget(null);
      return;
    }

    const bounds = event.currentTarget.getBoundingClientRect();
    const position = event.clientY > bounds.top + bounds.height / 2 ? "after" : "before";

    setQuestionDropTarget({ questionId, position });
  };

  const clearQuestionDragState = () => {
    setDraggedQuestionId(null);
    setQuestionDropTarget(null);
  };

  const updateQuestion = (nextQuestion) => {
    const nextQuestionWithDirtyState = isUnsavedQuestion(nextQuestion)
      ? nextQuestion
      : { ...nextQuestion, isDirty: true };

    setQuestions((current) =>
      current.map((item) =>
        item.skillModuleQuizQuestionId === nextQuestion.skillModuleQuizQuestionId ? nextQuestionWithDirtyState : item,
      ),
    );
  };

  const saveQuestionOrder = async () => {
    try {
      setIsSavingOrder(true);
      await contentManagerLearningModuleApi.reorderQuestions(
        module.skillModuleId,
        orderedQuestions.map((question, index) => ({
          skillModuleQuizQuestionId: question.skillModuleQuizQuestionId,
          orderIndex: index + 1,
        })),
      );
      toast.success("Question order saved.");
      setIsQuestionOrderDirty(false);
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to save question order.");
    } finally {
      setIsSavingOrder(false);
    }
  };

  const quizFields = (
    <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_180px_180px]">
      <ModuleField label="Quiz title">
        <input
          value={quizForm.title}
          onChange={(event) => updateQuiz("title", event.target.value)}
          className={inputClass}
          placeholder="Enter quiz title..."
        />
      </ModuleField>

      <ModuleField label="Passing score">
        <div className="relative">
          <input
            type="number"
            min="1"
            max="100"
            value={quizForm.passingScorePercent}
            onChange={(event) => updateQuiz("passingScorePercent", event.target.value)}
            className={`${numberInputClass} pr-9`}
          />
          <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-sm font-extrabold text-slate-500">
            %
          </span>
        </div>
      </ModuleField>

      <ModuleField label="Max attempts">
        <div className="flex h-10 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white">
          <button
            type="button"
            onClick={() => adjustMaxAttempts(-1)}
            className="grid w-10 place-items-center border-r border-[#B9D8CC] text-slate-600 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
            aria-label="Decrease max attempts"
          >
            <Minus size={14} />
          </button>
          <input
            type="number"
            min="1"
            value={quizForm.maxAttempts}
            onChange={(event) => updateQuiz("maxAttempts", event.target.value)}
            className="min-w-0 flex-1 border-0 bg-white px-2 text-center text-sm font-semibold text-[#18332D] outline-none [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
          />
          <button
            type="button"
            onClick={() => adjustMaxAttempts(1)}
            className="grid w-10 place-items-center border-l border-[#B9D8CC] text-slate-600 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
            aria-label="Increase max attempts"
          >
            <Plus size={14} />
          </button>
        </div>
      </ModuleField>
    </div>
  );

  if (!hasQuiz) {
    return (
      <div className="space-y-4">
        <ModuleCard className="p-5">
          <div className="mb-4">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-lg font-extrabold text-[#18332D]">Create quiz</h2>
              <DirtyStateBadge isDirty={Boolean(quizForm.title.trim()) || Number(quizForm.passingScorePercent) !== 70 || Number(quizForm.maxAttempts) !== 3} />
            </div>
          </div>

          {quizFields}

          <div className="mt-4 flex justify-end">
            <ModuleButton
              onClick={saveQuiz}
              disabled={isSavingQuiz || !quizForm.title.trim()}
            >
              {isSavingQuiz ? "Creating..." : "Create quiz"}
            </ModuleButton>
          </div>
        </ModuleCard>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <ModuleCard className="overflow-hidden">
        <button
          type="button"
          onClick={() => setIsSettingsOpen((current) => !current)}
          className="flex w-full items-center justify-between gap-4 border-b border-[#B9D8CC]/70 px-5 py-4 text-left"
        >
          <div>
            <div className="flex flex-wrap items-center gap-2 text-sm font-extrabold text-[#18332D]">
              <Settings size={16} />
              Quiz settings
              <DirtyStateBadge isDirty={hasQuizSettingsChanges} />
            </div>
            <div className="mt-1 text-xs font-semibold text-slate-500">
              {quiz.title} · {quiz.passingScorePercent}% passing · {quiz.maxAttempts} attempts
            </div>
          </div>

          {isSettingsOpen ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
        </button>

        {isSettingsOpen && (
          <div className="p-5">
            {quizFields}

            <div className="mt-5 flex justify-end">
              <ModuleButton onClick={saveQuiz} disabled={isSavingQuiz || !hasQuizSettingsChanges}>
                {isSavingQuiz ? "Saving..." : hasQuizSettingsChanges ? "Save settings" : "Saved"}
              </ModuleButton>
            </div>
          </div>
        )}
      </ModuleCard>

      <div className="grid h-[680px] min-h-0 gap-4 lg:grid-cols-[360px_minmax(0,1fr)]">
        <ModuleCard className="flex min-h-0 flex-col overflow-hidden">
          <div className="flex items-center justify-between gap-3 border-b border-[#B9D8CC] px-4 py-3">
            <div>
              <div className="text-sm font-extrabold text-[#18332D]">Questions</div>
              <div className="text-xs font-semibold text-slate-500">
                {orderedQuestions.length} question{orderedQuestions.length === 1 ? "" : "s"}
              </div>
              {hasUnsavedQuestions && (
                <div className="mt-1 text-xs font-bold text-amber-700">
                  There are unsaved questions.
                </div>
              )}
            </div>
          </div>

          <div className="min-h-0 flex-1 space-y-2 overflow-y-auto p-3 scrollbar-thin scrollbar-track-[#F7F1E8] scrollbar-thumb-[#B9D8CC] hover:scrollbar-thumb-[#2FA084] [scrollbar-color:#B9D8CC_#F7F1E8] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:rounded-full [&::-webkit-scrollbar-track]:bg-[#F7F1E8] [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-[#B9D8CC] [&::-webkit-scrollbar-thumb:hover]:bg-[#2FA084]">
            {orderedQuestions.length === 0 ? (
              <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/40 p-4 text-center text-sm font-semibold text-slate-600">
                No questions yet.
              </div>
            ) : (
              orderedQuestions.map((question, index) => {
                const isActive = activeQuestion?.skillModuleQuizQuestionId === question.skillModuleQuizQuestionId;
                const isDragging = draggedQuestionId === question.skillModuleQuizQuestionId;
                const isDropTarget = questionDropTarget?.questionId === question.skillModuleQuizQuestionId;
                const showDropBefore = isDropTarget && questionDropTarget.position === "before";
                const showDropAfter = isDropTarget && questionDropTarget.position === "after";
                const questionTitle = question.questionText?.trim() || `Question ${index + 1}`;

                return (
                  <button
                    key={question.skillModuleQuizQuestionId}
                    type="button"
                    draggable
                    title={questionTitle}
                    onDragStart={(event) => {
                      event.dataTransfer.effectAllowed = "move";
                      setDraggedQuestionId(question.skillModuleQuizQuestionId);
                    }}
                    onDragEnd={clearQuestionDragState}
                    onDragOver={(event) => {
                      event.preventDefault();
                      event.dataTransfer.dropEffect = "move";
                      updateQuestionDropTarget(event, question.skillModuleQuizQuestionId);
                    }}
                    onDragLeave={(event) => {
                      if (!event.currentTarget.contains(event.relatedTarget)) {
                        setQuestionDropTarget((current) =>
                          current?.questionId === question.skillModuleQuizQuestionId ? null : current,
                        );
                      }
                    }}
                    onDrop={(event) => {
                      event.preventDefault();
                      reorderQuestions(
                        draggedQuestionId,
                        question.skillModuleQuizQuestionId,
                        questionDropTarget?.position || "before",
                      );
                      clearQuestionDragState();
                    }}
                    onClick={() => setActiveQuestionId(question.skillModuleQuizQuestionId)}
                    className={`relative w-full rounded-lg border px-3 py-3 text-left transition-all duration-150 ${isActive
                        ? "border-[#6FCF97] bg-[#6FCF97]/14 shadow-sm"
                        : "border-[#B9D8CC]/70 bg-white hover:border-[#6FCF97] hover:bg-[#F7F1E8]/55"
                      } ${isDragging ? "scale-[0.99] opacity-40" : "opacity-100"} ${isDropTarget ? "bg-[#F7F1E8]/70" : ""
                      }`}
                  >
                    {showDropBefore && (
                      <div className="absolute left-3 right-3 top-0 z-10 h-0.5 rounded-full bg-[#1F6F5F] shadow-[0_0_0_3px_rgba(111,207,151,0.18)]" />
                    )}

                    <div className="flex items-start gap-3">
                      <GripVertical size={15} className="mt-1 shrink-0 cursor-grab text-slate-400 active:cursor-grabbing" />
                      <div className={`grid h-8 w-8 shrink-0 place-items-center rounded-full text-xs font-extrabold ${isActive ? "bg-[#6FCF97]/24 text-[#1F6F5F]" : "bg-[#F7F1E8] text-slate-600"
                        }`}>
                        {index + 1}
                      </div>

                      <div className="min-w-0">
                        <div className="line-clamp-2 text-sm font-extrabold leading-5 text-[#18332D]">
                          {questionTitle}
                        </div>
                        <div className="mt-1 flex flex-wrap items-center gap-2 text-xs font-semibold text-slate-500">
                          <span>Multiple choice</span>
                          {isUnsavedQuestion(question) && (
                            <ModuleBadge tone="amber" className="px-2 py-0 text-[10px] leading-4">
                              New
                            </ModuleBadge>
                          )}
                          {!isUnsavedQuestion(question) && question.isDirty && (
                            <DirtyStateBadge isDirty label="Edited" />
                          )}
                        </div>
                      </div>
                    </div>

                    {showDropAfter && (
                      <div className="absolute bottom-0 left-3 right-3 z-10 h-0.5 rounded-full bg-[#1F6F5F] shadow-[0_0_0_3px_rgba(111,207,151,0.18)]" />
                    )}
                  </button>
                );
              })
            )}
          </div>

          <div className="space-y-2 border-t border-[#B9D8CC] p-3">
            <ModuleButton variant="secondary" className="w-full" onClick={addQuestion}>
              <Plus size={14} /> Add question
            </ModuleButton>

            {canSaveQuestionOrder && (
              <ModuleButton variant="secondary" className="w-full" onClick={saveQuestionOrder} disabled={isSavingOrder}>
                <Save size={14} /> {isSavingOrder ? "Saving..." : "Save order"}
              </ModuleButton>
            )}
          </div>
        </ModuleCard>

        {activeQuestion ? (
          <QuestionEditorCard
            question={activeQuestion}
            index={activeQuestionIndex}
            onChange={updateQuestion}
            onSave={() => saveQuestion(activeQuestion)}
            onDelete={() => requestDeleteQuestion(activeQuestion)}
          />
        ) : (
          <ModuleEmptyState title="No question selected">
            Add a question from the left panel to start building the quiz.
          </ModuleEmptyState>
        )}
      </div>

      <ConfirmActionDialog
        isOpen={Boolean(deleteQuestionTarget)}
        title="Delete question?"
        description="This question and its answers will be permanently removed."
        confirmLabel="Delete question"
        cancelLabel="Cancel"
        isConfirming={isDeletingQuestion}
        onCancel={handleCancelDeleteQuestion}
        onConfirm={handleConfirmDeleteQuestion}
      />

      <ConfirmActionDialog
        isOpen={Boolean(deleteOptionTarget)}
        title="Delete answer option?"
        description="This answer option will be permanently removed."
        confirmLabel="Delete option"
        cancelLabel="Cancel"
        isConfirming={isDeletingOption}
        onCancel={handleCancelDeleteOption}
        onConfirm={handleConfirmDeleteOption}
      />
    </div>
  );
}


function QuestionEditorCard({
  question,
  index,
  onChange,
  onSave,
  onDelete,
}) {
  const questionNumber = index >= 0 ? index + 1 : 1;

  const setOption = (optionId, updater) => {
    onChange({
      ...question,
      options: question.options.map((option) =>
        option.skillModuleQuizOptionId === optionId ? updater(option) : option,
      ),
    });
  };

  const addOption = () => {
    onChange({
      ...question,
      options: [
        ...question.options,
        {
          skillModuleQuizOptionId: `new-${Date.now()}`,
          optionText: "",
          isCorrect: question.options.length === 0,
          explanation: "",
          orderIndex: question.options.length + 1,
        },
      ],
    });
  };

  const removeOption = (optionId) => {
    if (question.options.length <= 2) {
      toast.error("A question needs at least two options.");
      return;
    }

    onChange({
      ...question,
      options: question.options.filter((option) => option.skillModuleQuizOptionId !== optionId),
    });
  };

  return (
    <ModuleCard className="flex h-full min-h-0 flex-col overflow-hidden">
      <div className="flex flex-wrap items-center gap-3 border-b border-[#B9D8CC]/70 px-5 py-4">
        <div className="grid h-8 w-8 place-items-center rounded-full bg-[#6FCF97]/20 text-xs font-extrabold text-[#1F6F5F]">
          {questionNumber}
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <div className="text-sm font-extrabold text-[#18332D]">Question {questionNumber}</div>
            <DirtyStateBadge isDirty={hasQuestionDraftChanges(question)} />
          </div>
          <div className="text-xs font-semibold text-slate-500">Multiple choice</div>
        </div>

        <button
          type="button"
          onClick={onDelete}
          className="inline-flex h-8 items-center gap-1.5 rounded-md border border-rose-200 bg-rose-50 px-2.5 text-xs font-extrabold text-rose-700 transition hover:border-rose-300 hover:bg-rose-100"
          aria-label="Delete question"
        >
          <Trash2 size={15} strokeWidth={2.25} />
          Delete
        </button>
      </div>

      <div className="min-h-0 flex-1 space-y-4 overflow-y-auto p-5 scrollbar-thin scrollbar-track-[#F7F1E8] scrollbar-thumb-[#B9D8CC] hover:scrollbar-thumb-[#2FA084] [scrollbar-color:#B9D8CC_#F7F1E8] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:rounded-full [&::-webkit-scrollbar-track]:bg-[#F7F1E8] [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-[#B9D8CC] [&::-webkit-scrollbar-thumb:hover]:bg-[#2FA084]">
        <ModuleField label="Question text">
          <textarea
            value={question.questionText}
            onChange={(event) => onChange({ ...question, questionText: event.target.value })}
            className={`${inputClass} min-h-24 resize-none`}
            placeholder="Enter question..."
          />
        </ModuleField>

        <div className="overflow-hidden rounded-lg border border-[#B9D8CC]/70">
          {question.options
            .slice()
            .sort((a, b) => a.orderIndex - b.orderIndex)
            .map((option, optionIndex) => (
              <div
                key={option.skillModuleQuizOptionId}
                className="grid grid-cols-[36px_1fr_36px] items-center gap-3 border-b border-[#B9D8CC]/60 bg-white px-3 py-2.5 last:border-b-0"
              >
                <button
                  type="button"
                  onClick={() =>
                    onChange({
                      ...question,
                      options: question.options.map((item) => ({
                        ...item,
                        isCorrect: item.skillModuleQuizOptionId === option.skillModuleQuizOptionId,
                      })),
                    })
                  }
                  className={`grid h-7 w-7 place-items-center rounded-full border text-xs font-extrabold transition ${option.isCorrect
                      ? "border-[#6FCF97] bg-[#6FCF97]/24 text-[#1F6F5F]"
                      : "border-[#B9D8CC] bg-white text-slate-500"
                    }`}
                  aria-label={`Mark option ${optionIndex + 1} as correct`}
                >
                  {String.fromCharCode(65 + optionIndex)}
                </button>

                <input
                  value={option.optionText}
                  onChange={(event) => setOption(option.skillModuleQuizOptionId, (old) => ({ ...old, optionText: event.target.value }))}
                  className="w-full bg-transparent py-1 text-sm font-semibold text-[#18332D] outline-none placeholder:text-slate-400"
                  placeholder="Enter option..."
                />

                {question.options.length > 2 ? (
                  <button
                    type="button"
                    onClick={() => removeOption(option.skillModuleQuizOptionId)}
                    className="grid h-7 w-7 place-items-center rounded-md border border-rose-200 bg-rose-50 text-rose-700 transition hover:border-rose-300 hover:bg-rose-100"
                    aria-label="Remove option"
                  >
                    <Trash2 size={14} strokeWidth={2.25} />
                  </button>
                ) : (
                  <div />
                )}
              </div>
            ))}
        </div>

        <div className="flex justify-start">
          <ModuleButton size="xs" variant="secondary" onClick={addOption}>
            <Plus size={13} /> Add option
          </ModuleButton>
        </div>

        <ModuleField label="Explanation">
          <textarea
            value={question.explanation || ""}
            onChange={(event) => onChange({ ...question, explanation: event.target.value })}
            className={`${inputClass} min-h-20 resize-none`}
            placeholder="Explanation shown after review"
          />
        </ModuleField>

        <div className="flex justify-end">
          <ModuleButton onClick={onSave} disabled={!hasQuestionDraftChanges(question)}>
            {hasQuestionDraftChanges(question) ? "Save question" : "Saved"}
          </ModuleButton>
        </div>
      </div>
    </ModuleCard>
  );
}

