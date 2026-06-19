import { toast } from "react-toastify";

export const editorTabs = [
  { key: "overview", label: "Overview" },
  { key: "lessons", label: "Lessons" },
  { key: "quiz", label: "Quiz" },
  { key: "preview", label: "Preview" },
  { key: "publish", label: "Publish" },
];

export function getValidEditorTab(value) {
  return editorTabs.some((tab) => tab.key === value) ? value : null;
}

export function getEditorStorageKey(moduleId, key) {
  return moduleId ? `learning-module-editor:${moduleId}:${key}` : "";
}

export function readSessionValue(key) {
  if (!key) return null;

  try {
    return window.sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

export function writeSessionValue(key, value) {
  if (!key || value === null || value === undefined || value === "") return;

  try {
    window.sessionStorage.setItem(key, String(value));
  } catch {
    // Session persistence is best-effort.
  }
}

export function removeSessionValue(key) {
  if (!key) return;

  try {
    window.sessionStorage.removeItem(key);
  } catch {
    // Session persistence is best-effort.
  }
}

export function readSessionJson(key) {
  const value = readSessionValue(key);
  if (!value) return null;

  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

export function writeSessionJson(key, value) {
  if (!key) return;

  try {
    window.sessionStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Session persistence is best-effort.
  }
}

function getQuestionId(question) {
  return question?.skillModuleQuizQuestionId;
}

export function isUnsavedQuestion(question) {
  return String(getQuestionId(question) || "").startsWith("new-");
}

export function getUnsavedQuizQuestions(questions = []) {
  return questions.filter(isUnsavedQuestion);
}

export function mergeQuizQuestions(serverQuestions = [], draftQuestions = []) {
  const serverIds = new Set(serverQuestions.map((question) => String(getQuestionId(question))));
  const unsavedDraftQuestions = draftQuestions.filter((question) =>
    isUnsavedQuestion(question) && !serverIds.has(String(getQuestionId(question))),
  );

  return [...serverQuestions, ...unsavedDraftQuestions]
    .slice()
    .sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0));
}

const lessonIndexingMeta = {
  pending: {
    label: "Pending index",
    tone: "amber",
    description: "Waiting to be indexed for module chat.",
  },
  indexing: {
    label: "Indexing",
    tone: "amber",
    description: "Preparing lesson chunks for module chat.",
  },
  indexed: {
    label: "Indexed",
    tone: "green",
    description: "Ready for module chat and publishing.",
  },
  failed: {
    label: "Index failed",
    tone: "rose",
    description: "Indexing failed. Replace the lesson file or retry indexing later.",
  },
  needs_reindex: {
    label: "Needs reindex",
    tone: "amber",
    description: "This lesson needs to be indexed again.",
  },
};

export function getLessonIndexingStatus(lesson) {
  if (lesson?.indexingStatus) return lesson.indexingStatus;
  if (lesson?.chunkCount > 0 || lesson?.chunksGenerated > 0) return "indexed";
  return "pending";
}

export function getLessonIndexingMeta(lesson) {
  return lessonIndexingMeta[getLessonIndexingStatus(lesson)] || lessonIndexingMeta.pending;
}

export function shouldPollLessonIndexing(lessons = []) {
  return lessons.some((lesson) =>
    ["pending", "indexing", "needs_reindex"].includes(getLessonIndexingStatus(lesson)),
  );
}

export function canRetryLessonIndexing(lesson) {
  return ["failed", "needs_reindex"].includes(getLessonIndexingStatus(lesson));
}

function getQueuedLessonUploads(lessons = []) {
  return lessons.filter((lesson) =>
    ["pending", "indexing", "needs_reindex"].includes(getLessonIndexingStatus(lesson)),
  );
}

function getFailedLessonUploads(result) {
  return Array.isArray(result?.failedLessons) ? result.failedLessons : [];
}

export function getUploadedLessons(result) {
  return Array.isArray(result?.lessons) ? result.lessons : [];
}

function getIndexFailedUploads(lessons = []) {
  return lessons.filter((lesson) => getLessonIndexingStatus(lesson) === "failed");
}

function pluralizeLesson(count) {
  return count === 1 ? "lesson" : "lessons";
}

function normalizeFormValue(value) {
  return value === null || value === undefined ? "" : String(value);
}

function normalizeOptionalTextValue(value) {
  return normalizeFormValue(value).trim();
}

function normalizeNumberFormValue(value) {
  if (value === null || value === undefined || value === "") return "";
  return String(Number(value));
}

export function hasOverviewDraftChanges(form, module) {
  if (!module) return false;

  return (
    normalizeFormValue(form.skillId).trim() !== normalizeFormValue(module.skillId).trim()
    || normalizeFormValue(form.title).trim() !== normalizeFormValue(module.title).trim()
    || normalizeOptionalTextValue(form.description) !== normalizeOptionalTextValue(module.description)
    || normalizeFormValue(form.difficultyLevel).trim() !== normalizeFormValue(module.difficultyLevel || "beginner").trim()
    || normalizeNumberFormValue(form.estimatedHours) !== normalizeNumberFormValue(module.estimatedHours)
  );
}

export function hasLessonDraftChanges(form, lesson) {
  if (!lesson) return false;

  return (
    normalizeFormValue(form.title).trim() !== normalizeFormValue(lesson.title).trim()
    || normalizeOptionalTextValue(form.summary) !== normalizeOptionalTextValue(lesson.summary)
    || normalizeNumberFormValue(form.estimatedHours) !== normalizeNumberFormValue(lesson.estimatedHours)
  );
}

export function hasLessonOrderChanges(localLessons = [], serverLessons = []) {
  if (localLessons.length !== serverLessons.length) return true;

  const localOrder = localLessons
    .slice()
    .sort((a, b) => a.orderIndex - b.orderIndex)
    .map((lesson) => String(lesson.skillModuleLessonId));
  const serverOrder = serverLessons
    .slice()
    .sort((a, b) => a.orderIndex - b.orderIndex)
    .map((lesson) => String(lesson.skillModuleLessonId));

  return localOrder.some((lessonId, index) => lessonId !== serverOrder[index]);
}

export function hasQuizDraftChanges(form, quiz) {
  if (!quiz) return false;

  return (
    normalizeFormValue(form.title).trim() !== normalizeFormValue(quiz.title).trim()
    || normalizeNumberFormValue(form.passingScorePercent) !== normalizeNumberFormValue(quiz.passingScorePercent)
    || normalizeNumberFormValue(form.maxAttempts) !== normalizeNumberFormValue(quiz.maxAttempts)
  );
}

export function hasQuestionDraftChanges(question) {
  return Boolean(question?.isDirty || isUnsavedQuestion(question));
}

export function getModuleFromMutationResult(result) {
  return result?.module || result?.Module || result || null;
}

export function showBulkUploadResultToast(result) {
  const uploadedLessons = getUploadedLessons(result);
  const failedLessons = getFailedLessonUploads(result);
  const indexingFailures = getIndexFailedUploads(uploadedLessons);
  const queuedLessons = getQueuedLessonUploads(uploadedLessons);

  if (uploadedLessons.length === 0 && failedLessons.length > 0) {
    const firstFailure = failedLessons[0];
    const fileLabel = firstFailure?.fileName ? ` (${firstFailure.fileName})` : "";
    toast.error(`No lessons uploaded. ${failedLessons.length} failed${fileLabel}.`);
    return;
  }

  if (failedLessons.length > 0) {
    toast(`Uploaded ${uploadedLessons.length} ${pluralizeLesson(uploadedLessons.length)}. ${failedLessons.length} failed.`);
    return;
  }

  if (indexingFailures.length > 0) {
    toast(`Uploaded ${uploadedLessons.length} ${pluralizeLesson(uploadedLessons.length)}. ${indexingFailures.length} need reindexing.`);
    return;
  }

  if (queuedLessons.length > 0) {
    toast.success(`Uploaded ${uploadedLessons.length} ${pluralizeLesson(uploadedLessons.length)}. Indexing started.`);
    return;
  }

  toast.success(`Uploaded ${uploadedLessons.length} ${pluralizeLesson(uploadedLessons.length)}.`);
}

function getReadinessCheck(detail, key) {
  return detail?.publishReadiness?.checks?.find((check) => check.key === key) || null;
}

function isReadinessCheckComplete(detail, key) {
  return Boolean(getReadinessCheck(detail, key)?.isComplete);
}

export function isEditorTabComplete(tab, detail) {
  if (tab === "overview") {
    return isReadinessCheckComplete(detail, "overview");
  }

  if (tab === "lessons") {
    return (
      isReadinessCheckComplete(detail, "lessons")
      && isReadinessCheckComplete(detail, "lesson_indexing")
    );
  }

  if (tab === "quiz") {
    return isReadinessCheckComplete(detail, "quiz");
  }

  if (tab === "preview") {
    return (
      isReadinessCheckComplete(detail, "overview")
      && isReadinessCheckComplete(detail, "lessons")
    );
  }

  if (tab === "publish") {
    return Boolean(detail?.publishReadiness?.canPublish);
  }

  return false;
}

export function createEmptyQuestionPayload(number) {
  const timestamp = Date.now();

  return {
    skillModuleQuizQuestionId: `new-question-${timestamp}`,
    questionText: "",
    questionType: "single_choice",
    explanation: "",
    points: 1,
    orderIndex: number,
    options: [
      {
        skillModuleQuizOptionId: `new-option-${timestamp}-1`,
        optionText: "",
        isCorrect: true,
        explanation: "",
        orderIndex: 1,
      },
      {
        skillModuleQuizOptionId: `new-option-${timestamp}-2`,
        optionText: "",
        isCorrect: false,
        explanation: "",
        orderIndex: 2,
      },
    ],
  };
}

export function toQuestionPayload(question) {
  return {
    questionText: question.questionText,
    questionType: question.questionType || "single_choice",
    explanation: question.explanation || null,
    points: question.points || 1,
    orderIndex: question.orderIndex || 1,
    options: question.options.map((option, index) => ({
      skillModuleQuizOptionId: String(option.skillModuleQuizOptionId).startsWith("new-")
        ? null
        : option.skillModuleQuizOptionId,
      optionText: option.optionText,
      isCorrect: option.isCorrect,
      explanation: option.explanation || null,
      orderIndex: option.orderIndex || index + 1,
    })),
  };
}
