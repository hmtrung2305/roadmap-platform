import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  AlertCircle,
  ArrowLeft,
  Eye,
  FileUp,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Circle,
  GripVertical,
  Loader2,
  Minus,
  MoreVertical,
  Plus,
  Save,
  Settings,
  Trash2,
  Upload,
} from "lucide-react";
import { toast } from "react-toastify";
import { counselorLearningModuleApi, getLearningModuleRouteSegment } from "../../../api/learningModuleApi";
import MarkdownRenderer, { titleFromMarkdown } from "../../../components/learningModules/MarkdownRenderer";
import SkillSearchPicker from "../../../components/learningModules/SkillSearchPicker";
import ConfirmActionDialog from "../../../components/learningModules/ConfirmActionDialog";
import {
  inputClass,
  numberInputClass,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModuleField,
  ModulePageShell,
  selectClass,
} from "../../../components/learningModules/learningModuleUi";

const editorTabs = [
  { key: "overview", label: "Overview" },
  { key: "lessons", label: "Lessons" },
  { key: "quiz", label: "Quiz" },
  { key: "preview", label: "Preview" },
  { key: "publish", label: "Publish" },
];

function getValidEditorTab(value) {
  return editorTabs.some((tab) => tab.key === value) ? value : null;
}

function getEditorStorageKey(moduleId, key) {
  return moduleId ? `learning-module-editor:${moduleId}:${key}` : "";
}

function readSessionValue(key) {
  if (!key) return null;

  try {
    return window.sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeSessionValue(key, value) {
  if (!key || value === null || value === undefined || value === "") return;

  try {
    window.sessionStorage.setItem(key, String(value));
  } catch {
    // Session persistence is a UX helper only.
  }
}

function removeSessionValue(key) {
  if (!key) return;

  try {
    window.sessionStorage.removeItem(key);
  } catch {
    // Session persistence is a UX helper only.
  }
}

function readSessionJson(key) {
  const value = readSessionValue(key);
  if (!value) return null;

  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

function writeSessionJson(key, value) {
  if (!key) return;

  try {
    window.sessionStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Session persistence is a UX helper only.
  }
}

function getQuestionId(question) {
  return question?.skillModuleQuizQuestionId;
}

function isUnsavedQuestion(question) {
  return String(getQuestionId(question) || "").startsWith("new-");
}

function getUnsavedQuizQuestions(questions = []) {
  return questions.filter(isUnsavedQuestion);
}

function mergeQuizQuestions(serverQuestions = [], draftQuestions = []) {
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

function getLessonIndexingStatus(lesson) {
  if (lesson?.indexingStatus) return lesson.indexingStatus;
  if (lesson?.chunkCount > 0 || lesson?.chunksGenerated > 0) return "indexed";
  return "pending";
}

function getLessonIndexingMeta(lesson) {
  return lessonIndexingMeta[getLessonIndexingStatus(lesson)] || lessonIndexingMeta.pending;
}

function isLessonIndexed(lesson) {
  return getLessonIndexingStatus(lesson) === "indexed";
}

function getIndexedLessonCount(lessons = []) {
  return lessons.filter(isLessonIndexed).length;
}

function shouldPollLessonIndexing(lessons = []) {
  return lessons.some((lesson) =>
    ["pending", "indexing", "needs_reindex"].includes(getLessonIndexingStatus(lesson)),
  );
}

function canRetryLessonIndexing(lesson) {
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

function getUploadedLessons(result) {
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

function hasOverviewDraftChanges(form, module) {
  if (!module) return false;

  return (
    normalizeFormValue(form.skillId).trim() !== normalizeFormValue(module.skillId).trim()
    || normalizeFormValue(form.title).trim() !== normalizeFormValue(module.title).trim()
    || normalizeOptionalTextValue(form.description) !== normalizeOptionalTextValue(module.description)
    || normalizeFormValue(form.difficultyLevel).trim() !== normalizeFormValue(module.difficultyLevel || "beginner").trim()
    || normalizeNumberFormValue(form.estimatedHours) !== normalizeNumberFormValue(module.estimatedHours)
  );
}

function hasLessonDraftChanges(form, lesson) {
  if (!lesson) return false;

  return (
    normalizeFormValue(form.title).trim() !== normalizeFormValue(lesson.title).trim()
    || normalizeOptionalTextValue(form.summary) !== normalizeOptionalTextValue(lesson.summary)
    || normalizeNumberFormValue(form.estimatedHours) !== normalizeNumberFormValue(lesson.estimatedHours)
  );
}

function hasQuizDraftChanges(form, quiz) {
  if (!quiz) return false;

  return (
    normalizeFormValue(form.title).trim() !== normalizeFormValue(quiz.title).trim()
    || normalizeNumberFormValue(form.passingScorePercent) !== normalizeNumberFormValue(quiz.passingScorePercent)
    || normalizeNumberFormValue(form.maxAttempts) !== normalizeNumberFormValue(quiz.maxAttempts)
  );
}

function hasQuestionDraftChanges(question) {
  return Boolean(question?.isDirty || isUnsavedQuestion(question));
}

function DirtyStateBadge({ isDirty, label = "Unsaved changes" }) {
  if (!isDirty) return null;

  return (
    <span className="inline-flex items-center gap-1 rounded-md border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-extrabold uppercase tracking-[0.12em] text-amber-700">
      <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
      {label}
    </span>
  );
}

function getModuleFromMutationResult(result) {
  return result?.module || result?.Module || result || null;
}

function showBulkUploadResultToast(result) {
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

function areLessonsIndexed(lessons = []) {
  return lessons.length > 0 && lessons.every(isLessonIndexed);
}

function isEditorTabComplete(tab, detail) {
  const module = detail?.module;
  const lessons = detail?.lessons || [];
  const lessonCount = lessons.length;
  const questionCount = detail?.quiz?.questions?.length || 0;
  const lessonsIndexed = areLessonsIndexed(lessons);

  if (tab === "overview") {
    return Boolean(module?.title && module?.skillId && module?.description);
  }

  if (tab === "lessons") {
    return lessonCount >= 3 && lessonsIndexed;
  }

  if (tab === "quiz") {
    return Boolean(detail?.quiz) && questionCount >= 10;
  }

  if (tab === "preview") {
    return Boolean(module?.title && lessonCount > 0);
  }

  if (tab === "publish") {
    return (
      Boolean(module?.title && module?.skillId && module?.description)
      && lessonCount >= 3
      && lessonsIndexed
      && Boolean(detail?.quiz)
      && questionCount >= 10
    );
  }

  return false;
}

export default function CounselorLearningModuleEditorPage() {
  const { moduleSlug } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const routeStateModuleId = location.state?.moduleId || null;
  const resolvedModuleIdRef = useRef(routeStateModuleId);
  const [detail, setDetail] = useState(null);
  const [activeTab, setActiveTab] = useState(() => getValidEditorTab(searchParams.get("tab")) || "overview");
  const [isLoading, setIsLoading] = useState(true);
  const [showLoadingState, setShowLoadingState] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [isPublishDialogOpen, setIsPublishDialogOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [resolvedModuleId, setResolvedModuleId] = useState(routeStateModuleId);
  const [hasUnsavedQuizDrafts, setHasUnsavedQuizDrafts] = useState(false);
  const [isDiscardDialogOpen, setIsDiscardDialogOpen] = useState(false);

  const reload = () => setRefreshKey((key) => key + 1);

  const selectTab = (tabKey) => {
    const nextTab = getValidEditorTab(tabKey) || "overview";

    setActiveTab(nextTab);
    setSearchParams((current) => {
      const next = new URLSearchParams(current);

      if (nextTab === "overview") {
        next.delete("tab");
      } else {
        next.set("tab", nextTab);
      }

      return next;
    }, { replace: true });
  };

  const leaveEditor = () => {
    if (hasUnsavedQuizDrafts) {
      setIsDiscardDialogOpen(true);
      return;
    }

    navigate("/counselor/learning-modules");
  };

  const discardQuizDraftsAndLeave = () => {
    removeSessionValue(getEditorStorageKey(activeModuleId, "quizDraftQuestions"));
    removeSessionValue(getEditorStorageKey(activeModuleId, "activeQuizQuestionId"));
    setHasUnsavedQuizDrafts(false);
    setIsDiscardDialogOpen(false);
    navigate("/counselor/learning-modules");
  };

  useEffect(() => {
    if (!routeStateModuleId) return;

    resolvedModuleIdRef.current = routeStateModuleId;
    setResolvedModuleId(routeStateModuleId);
  }, [routeStateModuleId]);

  useEffect(() => {
    let ignore = false;

    async function loadDetail() {
      try {
        setIsLoading(true);
        const knownModuleId = routeStateModuleId || resolvedModuleIdRef.current;
        const moduleId = await counselorLearningModuleApi.resolveModuleIdFromRoute(moduleSlug, knownModuleId);
        const data = await counselorLearningModuleApi.getModule(moduleId);

        if (!ignore) {
          resolvedModuleIdRef.current = moduleId;
          setResolvedModuleId(moduleId);
          setDetail(data);
        }
      } catch (err) {
        if (!ignore) {
          setResolvedModuleId(null);
          toast.error(err?.message || "Unable to load module.");
        }
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    loadDetail();

    return () => {
      ignore = true;
    };
  }, [moduleSlug, refreshKey, routeStateModuleId]);

  useEffect(() => {
    const nextTab = getValidEditorTab(searchParams.get("tab")) || "overview";
    setActiveTab((current) => current === nextTab ? current : nextTab);
  }, [searchParams]);

  useEffect(() => {
    if (!isLoading || detail) {
      setShowLoadingState(false);
      return undefined;
    }

    const timer = window.setTimeout(() => setShowLoadingState(true), 180);
    return () => window.clearTimeout(timer);
  }, [isLoading, detail]);

  useEffect(() => {
    if (!hasUnsavedQuizDrafts) return undefined;

    const handleBeforeUnload = (event) => {
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [hasUnsavedQuizDrafts]);

  const module = detail?.module;
  const activeModuleId = module?.skillModuleId || resolvedModuleId;

  const handleOverviewSaved = (result) => {
    const updatedModule = getModuleFromMutationResult(result);

    if (!updatedModule?.skillModuleId) {
      reload();
      return;
    }

    resolvedModuleIdRef.current = updatedModule.skillModuleId;
    setResolvedModuleId(updatedModule.skillModuleId);
    setDetail((current) =>
      current
        ? {
            ...current,
            module: {
              ...current.module,
              ...updatedModule,
            },
          }
        : current,
    );

    const nextRouteSegment = getLearningModuleRouteSegment(updatedModule);

    if (nextRouteSegment && nextRouteSegment !== moduleSlug) {
      const queryString = searchParams.toString();

      navigate(
        `/counselor/learning-modules/${nextRouteSegment}/edit${queryString ? `?${queryString}` : ""}`,
        {
          replace: true,
          state: { moduleId: updatedModule.skillModuleId },
        },
      );
    }
  };

  const refreshDetailSilently = async () => {
    const moduleId = activeModuleId;

    if (!moduleId) {
      return;
    }

    try {
      const data = await counselorLearningModuleApi.getModule(moduleId);
      setDetail(data);
    } catch {
      // Polling is best-effort. Manual refresh/save actions still surface errors.
    }
  };

  const handlePublish = async () => {
    try {
      setIsPublishing(true);
      const moduleId = activeModuleId;

      if (!moduleId) {
        toast.error("Learning module was not loaded yet.");
        return;
      }

      const result = await counselorLearningModuleApi.publishModule(moduleId);

      if (result?.readiness?.canPublish === false) {
        toast.error(result.readiness.errors?.[0] || "Module is not ready to publish.");
        setIsPublishDialogOpen(false);
        reload();
        return;
      }

      setIsPublishDialogOpen(false);
      toast.success("Module published.");
      navigate("/counselor/learning-modules?status=published");
    } catch (err) {
      toast.error(err?.message || "Unable to publish module.");
    } finally {
      setIsPublishing(false);
    }
  };

  if (isLoading && !detail) {
    if (!showLoadingState) {
      return (
        <ModulePageShell>
          <div className="min-h-[240px]" />
        </ModulePageShell>
      );
    }

    return (
      <ModulePageShell>
        <ModuleCard className="flex items-center justify-center gap-2 p-10 text-center text-sm font-bold text-slate-600">
          <Loader2 size={16} className="animate-spin text-[#1F6F5F]" />
          Loading module editor...
        </ModuleCard>
      </ModulePageShell>
    );
  }

  if (!detail || !module) {
    return (
      <ModulePageShell>
        <ModuleEmptyState title="Module not found">The module could not be loaded.</ModuleEmptyState>
      </ModulePageShell>
    );
  }

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <button
            type="button"
            onClick={leaveEditor}
            className="inline-flex cursor-pointer items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to management
          </button>

          {isLoading && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-white px-2.5 py-1 text-xs font-bold text-slate-600 shadow-sm">
              <Loader2 size={13} className="animate-spin text-[#1F6F5F]" />
              Updating...
            </span>
          )}
        </div>

        <div className="rounded-xl border border-[#B9D8CC] bg-white p-2 shadow-sm">
          <div className="grid gap-2 md:grid-cols-5">
            {editorTabs.map((tab, index) => {
              const isActive = activeTab === tab.key;
              const isComplete = isEditorTabComplete(tab.key, detail);

              return (
                <button
                  key={tab.key}
                  type="button"
                  onClick={() => selectTab(tab.key)}
                  className={`flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-left text-xs font-extrabold transition ${
                    isActive
                      ? "border-[#1F6F5F] bg-[#2FA084] text-white shadow-sm"
                      : isComplete
                        ? "border-[#B9D8CC] bg-white text-[#1F6F5F]"
                        : "border-transparent bg-white text-slate-600 hover:bg-[#F7F1E8]"
                  }`}
                >
                  <span className={`grid h-6 w-6 shrink-0 place-items-center rounded-full text-[11px] ${
                    isActive
                      ? "bg-white/20 text-white"
                      : isComplete
                        ? "border border-[#6FCF97] bg-[#6FCF97]/14 text-[#1F6F5F]"
                        : "bg-slate-100 text-slate-600"
                  }`}>
                    {isComplete && !isActive ? <CheckCircle2 size={14} /> : index + 1}
                  </span>
                  <span>{tab.label}</span>
                </button>
              );
            })}
          </div>
        </div>

        {activeTab === "overview" && <OverviewEditor module={module} onSaved={handleOverviewSaved} />}
        {activeTab === "lessons" && <LessonsEditor module={module} lessons={detail.lessons} onChanged={reload} onIndexingStatusPoll={refreshDetailSilently} />}
        {activeTab === "quiz" && <QuizEditor module={module} quiz={detail.quiz} onChanged={reload} onDraftStateChange={setHasUnsavedQuizDrafts} />}
        {activeTab === "preview" && <InlinePreview moduleId={activeModuleId} detail={detail} />}
        {activeTab === "publish" && (
          <PublishPanel
            detail={detail}
            isPublishing={isPublishing}
            onPublish={() => setIsPublishDialogOpen(true)}
          />
        )}

        <ConfirmActionDialog
          isOpen={isDiscardDialogOpen}
          tone="warning"
          title="Discard unsaved questions?"
          description="You have unsaved quiz questions. Leaving now will delete those draft questions."
          confirmLabel="Discard and leave"
          cancelLabel="Stay and save"
          onCancel={() => setIsDiscardDialogOpen(false)}
          onConfirm={discardQuizDraftsAndLeave}
        />

        <ConfirmActionDialog
          isOpen={isPublishDialogOpen}
          tone="success"
          title="Publish this module?"
          description="Learners will be able to find and start this module after it is published."
          confirmLabel="Publish module"
          cancelLabel="Keep editing"
          isConfirming={isPublishing}
          onCancel={() => setIsPublishDialogOpen(false)}
          onConfirm={handlePublish}
        />
      </div>
    </ModulePageShell>
  );
}




function CustomSelect({ value, onChange, options, placeholder = "Select an option" }) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedOption = options.find((option) => String(option.value) === String(value));

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="flex h-10 w-full cursor-pointer items-center justify-between gap-3 rounded-lg border border-[#B9D8CC] bg-white px-3 text-left text-sm font-semibold text-[#18332D] outline-none transition hover:border-[#2FA084] focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
      >
        <span className={selectedOption ? "truncate" : "truncate text-slate-400"}>
          {selectedOption?.label || placeholder}
        </span>
        <ChevronDown
          size={16}
          className={`shrink-0 text-[#1F6F5F] transition ${isOpen ? "rotate-180" : ""}`}
        />
      </button>

      {isOpen && (
        <div className="absolute left-0 right-0 top-[calc(100%+6px)] z-30 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 shadow-lg">
          {options.map((option) => {
            const isSelected = String(option.value) === String(value);

            return (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  onChange(option.value);
                  setIsOpen(false);
                }}
                className={`flex w-full cursor-pointer items-center justify-between gap-3 px-3 py-2 text-left text-sm font-bold transition ${
                  isSelected
                    ? "bg-[#6FCF97]/18 text-[#1F6F5F]"
                    : "text-slate-700 hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                }`}
              >
                <span>{option.label}</span>
                {isSelected && <CheckCircle2 size={15} className="text-[#1F6F5F]" />}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

function SelectShell({ children }) {
  return (
    <div className="relative">
      {children}
      <ChevronDown
        size={16}
        className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-[#1F6F5F]"
      />
    </div>
  );
}

function OverviewEditor({ module, onSaved }) {
  const [selectedSkill, setSelectedSkill] = useState({
    skillId: module.skillId,
    name: module.skillName,
    slug: module.skillSlug || "",
    category: null,
    description: null,
  });
  const [form, setForm] = useState({
    skillId: module.skillId || "",
    title: module.title || "",
    description: module.description || "",
    difficultyLevel: module.difficultyLevel || "beginner",
    estimatedHours: module.estimatedHours ?? "",
  });
  const [isSaving, setIsSaving] = useState(false);
  const isDirty = hasOverviewDraftChanges(form, module);

  useEffect(() => {
    setSelectedSkill({
      skillId: module.skillId,
      name: module.skillName,
      slug: module.skillSlug || "",
      category: null,
      description: null,
    });
    setForm({
      skillId: module.skillId || "",
      title: module.title || "",
      description: module.description || "",
      difficultyLevel: module.difficultyLevel || "beginner",
      estimatedHours: module.estimatedHours ?? "",
    });
  }, [
    module.skillModuleId,
    module.skillId,
    module.skillName,
    module.skillSlug,
    module.title,
    module.description,
    module.difficultyLevel,
    module.estimatedHours,
  ]);

  const update = (key, value) => setForm((current) => ({ ...current, [key]: value }));

  const adjustEstimatedHours = (delta) => {
    setForm((current) => {
      const currentValue = Number(current.estimatedHours || 0);
      const nextValue = Math.max(0, currentValue + delta);
      return { ...current, estimatedHours: nextValue };
    });
  };

  const updateSkill = (skillId, skill) => {
    setSelectedSkill(skill);
    update("skillId", skillId);
  };

  const save = async () => {
    if (!form.skillId) {
      toast.error("Choose a skill first.");
      return;
    }

    if (!form.title.trim()) {
      toast.error("Module title is required.");
      return;
    }

    try {
      setIsSaving(true);
      const updatedModule = await counselorLearningModuleApi.updateModule(module.skillModuleId, {
        skillId: form.skillId,
        title: form.title.trim(),
        slug: null,
        description: form.description.trim() || null,
        difficultyLevel: form.difficultyLevel || null,
        estimatedHours: form.estimatedHours === "" ? null : Number(form.estimatedHours),
      });

      toast.success("Overview saved.");
      onSaved(updatedModule);
    } catch (err) {
      toast.error(err?.message || "Unable to save overview.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <ModuleCard className="flex h-full min-h-0 flex-col overflow-hidden p-5">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b border-[#B9D8CC]/70 pb-4">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-lg font-extrabold text-[#18332D]">Overview</h2>
            <DirtyStateBadge isDirty={isDirty} />
          </div>
          <p className="mt-1 text-sm font-semibold text-slate-600">Set the basic module information learners will see.</p>
        </div>

        <ModuleButton onClick={save} disabled={isSaving || !isDirty}>
          <Save size={14} /> {isSaving ? "Saving..." : isDirty ? "Save overview" : "Saved"}
        </ModuleButton>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="lg:col-span-2">
          <ModuleField label="Skill">
            <SkillSearchPicker
              value={form.skillId}
              initialSkill={selectedSkill}
              onChange={updateSkill}
              placeholder="Search for a skill"
            />
          </ModuleField>
        </div>

        <ModuleField label="Title">
          <input
            value={form.title}
            onChange={(event) => update("title", event.target.value)}
            className={inputClass}
          />
        </ModuleField>

        <ModuleField label="Difficulty">
          <CustomSelect
            value={form.difficultyLevel}
            onChange={(value) => update("difficultyLevel", value)}
            options={[
              { value: "beginner", label: "Beginner" },
              { value: "intermediate", label: "Intermediate" },
              { value: "advanced", label: "Advanced" },
            ]}
          />
        </ModuleField>

        <ModuleField label="Estimated hours">
          <div className="flex h-10 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white">
            <button
              type="button"
              onClick={() => adjustEstimatedHours(-0.5)}
              className="grid w-10 place-items-center border-r border-[#B9D8CC] text-slate-600 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
              aria-label="Decrease estimated hours"
            >
              <Minus size={14} />
            </button>
            <input
              type="number"
              step="0.5"
              min="0"
              value={form.estimatedHours}
              onChange={(event) => update("estimatedHours", event.target.value)}
              className="min-w-0 flex-1 border-0 bg-white px-3 text-center text-sm font-semibold text-[#18332D] outline-none [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
            />
            <button
              type="button"
              onClick={() => adjustEstimatedHours(0.5)}
              className="grid w-10 place-items-center border-l border-[#B9D8CC] text-slate-600 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
              aria-label="Increase estimated hours"
            >
              <Plus size={14} />
            </button>
          </div>
        </ModuleField>

        <div className="lg:col-span-2">
          <ModuleField label="Description">
            <textarea
              value={form.description}
              onChange={(event) => update("description", event.target.value)}
              className={`${inputClass} min-h-32 resize-none`}
            />
          </ModuleField>
        </div>
      </div>


    </ModuleCard>
  );
}


function LessonsEditor({ module, lessons, onChanged, onIndexingStatusPoll }) {
  const activeLessonStorageKey = getEditorStorageKey(module.skillModuleId, "activeLessonId");
  const [selectedFiles, setSelectedFiles] = useState([]);
  const [localLessons, setLocalLessons] = useState(lessons);
  const [activeLessonId, setActiveLessonId] = useState(() =>
    readSessionValue(activeLessonStorageKey) || lessons[0]?.skillModuleLessonId || null,
  );
  const [isUploading, setIsUploading] = useState(false);
  const [isSavingOrder, setIsSavingOrder] = useState(false);
  const [draggedLessonId, setDraggedLessonId] = useState(null);
  const [dropTarget, setDropTarget] = useState(null);
  const [openLessonMenuId, setOpenLessonMenuId] = useState(null);
  const [previewLesson, setPreviewLesson] = useState(null);
  const [previewData, setPreviewData] = useState(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [lessonToReplace, setLessonToReplace] = useState(null);
  const [lessonToDelete, setLessonToDelete] = useState(null);
  const [isReplacingContent, setIsReplacingContent] = useState(false);
  const [reindexingLessonId, setReindexingLessonId] = useState(null);
  const [isDeletingLesson, setIsDeletingLesson] = useState(false);

  useEffect(() => {
    setLocalLessons(lessons);
    setActiveLessonId((current) => {
      if (current && lessons.some((lesson) => lesson.skillModuleLessonId === current)) {
        return current;
      }

      const storedLessonId = readSessionValue(activeLessonStorageKey);
      if (storedLessonId && lessons.some((lesson) => lesson.skillModuleLessonId === storedLessonId)) {
        return storedLessonId;
      }

      return lessons[0]?.skillModuleLessonId || null;
    });
  }, [lessons, activeLessonStorageKey]);

  useEffect(() => {
    if (activeLessonId) {
      writeSessionValue(activeLessonStorageKey, activeLessonId);
    } else {
      removeSessionValue(activeLessonStorageKey);
    }
  }, [activeLessonId, activeLessonStorageKey]);

  useEffect(() => {
    if (!shouldPollLessonIndexing(localLessons)) {
      return undefined;
    }

    const intervalId = window.setInterval(() => {
      onIndexingStatusPoll?.();
    }, 5000);

    return () => window.clearInterval(intervalId);
  }, [localLessons, onIndexingStatusPoll]);

  const orderedLessons = localLessons.slice().sort((a, b) => a.orderIndex - b.orderIndex);
  const activeLesson = orderedLessons.find((lesson) => lesson.skillModuleLessonId === activeLessonId) || null;

  const upload = async () => {
    if (selectedFiles.length === 0) {
      toast.error("Choose at least one Markdown file.");
      return;
    }

    try {
      setIsUploading(true);

      const lessonPayload = selectedFiles.map((file, index) => ({
        clientId: `lesson-${Date.now()}-${index}`,
        title: titleFromMarkdown(file.name),
        slug: null,
        summary: "",
        estimatedHours: null,
        orderIndex: localLessons.length + index + 1,
        fileName: file.name,
      }));

      const result = await counselorLearningModuleApi.bulkUploadLessons(module.skillModuleId, lessonPayload, selectedFiles);
      const uploadedLessons = getUploadedLessons(result);
      const firstUploadedLessonId = uploadedLessons[0]?.skillModuleLessonId;

      if (firstUploadedLessonId) {
        setActiveLessonId(firstUploadedLessonId);
      }

      showBulkUploadResultToast(result);
      setSelectedFiles([]);
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to upload lessons.");
      onChanged();
    } finally {
      setIsUploading(false);
    }
  };

  const reorderLessons = (sourceId, targetId, position = "before") => {
    if (!sourceId || !targetId || sourceId === targetId) return;

    const sourceIndex = orderedLessons.findIndex((lesson) => lesson.skillModuleLessonId === sourceId);
    const targetIndex = orderedLessons.findIndex((lesson) => lesson.skillModuleLessonId === targetId);

    if (sourceIndex < 0 || targetIndex < 0) return;

    const next = orderedLessons.slice();
    const [moved] = next.splice(sourceIndex, 1);
    const adjustedTargetIndex = next.findIndex((lesson) => lesson.skillModuleLessonId === targetId);

    if (adjustedTargetIndex < 0) return;

    const insertIndex = position === "after" ? adjustedTargetIndex + 1 : adjustedTargetIndex;
    next.splice(insertIndex, 0, moved);

    setLocalLessons(next.map((lesson, nextIndex) => ({ ...lesson, orderIndex: nextIndex + 1 })));
  };

  const updateDropTarget = (event, lessonId) => {
    if (!draggedLessonId || draggedLessonId === lessonId) {
      setDropTarget(null);
      return;
    }

    const bounds = event.currentTarget.getBoundingClientRect();
    const position = event.clientY > bounds.top + bounds.height / 2 ? "after" : "before";

    setDropTarget({ lessonId, position });
  };

  const clearDragState = () => {
    setDraggedLessonId(null);
    setDropTarget(null);
  };

  const saveOrder = async () => {
    try {
      setIsSavingOrder(true);
      await counselorLearningModuleApi.reorderLessons(
        module.skillModuleId,
        orderedLessons.map((lesson, index) => ({
          skillModuleLessonId: lesson.skillModuleLessonId,
          orderIndex: index + 1,
        })),
      );
      toast.success("Lesson order saved.");
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to save lesson order.");
    } finally {
      setIsSavingOrder(false);
    }
  };

  const handleLessonSaved = (updatedLesson) => {
    setLocalLessons((current) =>
      current.map((lesson) =>
        lesson.skillModuleLessonId === updatedLesson.skillModuleLessonId
          ? { ...lesson, ...updatedLesson }
          : lesson,
      ),
    );

    onChanged();
  };

  const openPreview = async (lesson) => {
    setOpenLessonMenuId(null);
    setPreviewLesson(lesson);
    setPreviewData(null);

    try {
      setIsPreviewLoading(true);
      const data = await counselorLearningModuleApi.getLessonPreview(module.skillModuleId, lesson.skillModuleLessonId);
      setPreviewData(data);
    } catch (err) {
      toast.error(err?.message || "Unable to load lesson preview.");
      setPreviewData(null);
    } finally {
      setIsPreviewLoading(false);
    }
  };

  const requestReplaceLesson = (lesson) => {
    setOpenLessonMenuId(null);
    setLessonToReplace(lesson);
  };

  const replaceLessonContent = async (file) => {
    if (!lessonToReplace || !file) return;

    try {
      setIsReplacingContent(true);
      const updated = await counselorLearningModuleApi.replaceLessonContent(
        module.skillModuleId,
        lessonToReplace.skillModuleLessonId,
        file,
      );

      toast.success("Lesson file replaced. Indexing started.");
      setLessonToReplace(null);
      handleLessonSaved(updated);
    } catch (err) {
      toast.error(err?.message || "Unable to replace lesson content.");
    } finally {
      setIsReplacingContent(false);
    }
  };

  const retryLessonIndexing = async (lesson) => {
    setOpenLessonMenuId(null);

    try {
      setReindexingLessonId(lesson.skillModuleLessonId);
      const updated = await counselorLearningModuleApi.reindexLesson(
        module.skillModuleId,
        lesson.skillModuleLessonId,
      );

      toast.success("Lesson queued for indexing.");
      handleLessonSaved(updated);
    } catch (err) {
      toast.error(err?.message || "Unable to retry lesson indexing.");
    } finally {
      setReindexingLessonId(null);
    }
  };

  const requestDeleteLesson = (lesson) => {
    setOpenLessonMenuId(null);
    setLessonToDelete(lesson);
  };

  const confirmDeleteLesson = async () => {
    if (!lessonToDelete) return;

    try {
      setIsDeletingLesson(true);
      await counselorLearningModuleApi.deleteLesson(module.skillModuleId, lessonToDelete.skillModuleLessonId);
      toast.success("Lesson deleted.");
      setLessonToDelete(null);
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to delete lesson.");
    } finally {
      setIsDeletingLesson(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="grid h-[710px] min-h-0 items-stretch gap-4 lg:grid-cols-[minmax(0,420px)_minmax(0,1fr)]">
        <div className="grid min-h-0 grid-rows-[auto_minmax(0,1fr)] gap-4">
          <ModuleCard className="shrink-0 p-5">
            <label className="flex cursor-pointer flex-col items-center justify-center rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/50 px-4 py-6 text-center hover:bg-[#F7F1E8]">
              <Upload size={22} className="mb-2 text-[#1F6F5F]" />
              <span className="text-sm font-extrabold text-[#18332D]">Choose Markdown files</span>
              <span className="mt-1 text-xs font-semibold text-slate-600">.md or .markdown</span>
              <input
                type="file"
                multiple
                accept=".md,.markdown,text/markdown,text/plain"
                className="hidden"
                onChange={(event) => setSelectedFiles(Array.from(event.target.files || []))}
              />
            </label>

            {selectedFiles.length > 0 && (
              <div className="mt-3 space-y-1">
                {selectedFiles.map((file) => (
                  <div key={file.name} className="rounded-lg bg-white px-3 py-2 text-xs font-bold text-slate-700">
                    {file.name}
                  </div>
                ))}
              </div>
            )}

            {isUploading && (
              <div className="mt-3 flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-bold leading-5 text-amber-800">
                <Loader2 size={14} className="shrink-0 animate-spin" />
                Uploading lessons...
              </div>
            )}

            <ModuleButton className="mt-4 w-full" onClick={upload} disabled={isUploading}>
              {isUploading ? "Uploading..." : "Upload lessons"}
            </ModuleButton>
          </ModuleCard>

          {orderedLessons.length === 0 ? (
            <ModuleEmptyState title="No lessons yet">Upload Markdown files to start building this module.</ModuleEmptyState>
          ) : (
            <ModuleCard className="grid min-h-0 grid-rows-[minmax(0,1fr)_auto] overflow-visible">
              <div className="min-h-0 overflow-y-auto">
                {orderedLessons.map((lesson, index) => {
                  const isDragging = draggedLessonId === lesson.skillModuleLessonId;
                  const isDropTarget = dropTarget?.lessonId === lesson.skillModuleLessonId;
                  const showDropBefore = isDropTarget && dropTarget.position === "before";
                  const showDropAfter = isDropTarget && dropTarget.position === "after";
                  const isMenuOpen = openLessonMenuId === lesson.skillModuleLessonId;

                  return (
                    <div
                      key={lesson.skillModuleLessonId}
                      draggable
                      onDragStart={(event) => {
                        event.dataTransfer.effectAllowed = "move";
                        setDraggedLessonId(lesson.skillModuleLessonId);
                      }}
                      onDragEnd={clearDragState}
                      onDragOver={(event) => {
                        event.preventDefault();
                        event.dataTransfer.dropEffect = "move";
                        updateDropTarget(event, lesson.skillModuleLessonId);
                      }}
                      onDragLeave={(event) => {
                        if (!event.currentTarget.contains(event.relatedTarget)) {
                          setDropTarget((current) =>
                            current?.lessonId === lesson.skillModuleLessonId ? null : current,
                          );
                        }
                      }}
                      onDrop={(event) => {
                        event.preventDefault();
                        reorderLessons(draggedLessonId, lesson.skillModuleLessonId, dropTarget?.position || "before");
                        clearDragState();
                      }}
                      className={`relative border-b border-[#B9D8CC]/60 p-3 transition-all duration-150 last:border-b-0 ${
                        activeLessonId === lesson.skillModuleLessonId ? "bg-[#6FCF97]/10" : "bg-white"
                      } ${isDragging ? "scale-[0.99] opacity-40" : "opacity-100"} ${
                        isDropTarget ? "bg-[#F7F1E8]/70" : ""
                      }`}
                    >
                      {showDropBefore && (
                        <div className="absolute left-3 right-3 top-0 z-10 h-0.5 rounded-full bg-[#1F6F5F] shadow-[0_0_0_3px_rgba(111,207,151,0.18)]" />
                      )}

                      <div className="flex items-center gap-3">
                        <GripVertical size={16} className="shrink-0 cursor-grab text-slate-500 active:cursor-grabbing" />
                        <button
                          type="button"
                          onClick={() => setActiveLessonId(lesson.skillModuleLessonId)}
                          className="min-w-0 flex-1 text-left"
                        >
                          <div className="truncate text-sm font-extrabold text-[#18332D]">
                            {index + 1}. {lesson.title}
                          </div>
                          <div className="mt-0.5 truncate text-xs font-semibold text-slate-500">
                            {lesson.markdownFileName || lesson.fileName || "Markdown file"}
                          </div>
                        </button>

                        <div className="relative flex shrink-0 items-center gap-2">
                          <LessonIndexingBadge lesson={lesson} />
                          <button
                            type="button"
                            onClick={() =>
                              setOpenLessonMenuId((current) =>
                                current === lesson.skillModuleLessonId ? null : lesson.skillModuleLessonId,
                              )
                            }
                            className="grid h-8 w-8 place-items-center rounded-md border border-[#B9D8CC] bg-white text-slate-600 transition hover:border-[#6FCF97] hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                            aria-label="Lesson actions"
                          >
                            <MoreVertical size={16} />
                          </button>

                          {isMenuOpen && (
                            <div className="absolute right-0 top-9 z-30 w-44 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 shadow-xl">
                              <button
                                type="button"
                                onClick={() => openPreview(lesson)}
                                className="flex w-full items-center gap-2 px-3 py-2 text-left text-xs font-extrabold text-[#18332D] hover:bg-[#F7F1E8]"
                              >
                                <Eye size={14} /> Preview lesson
                              </button>
                              <button
                                type="button"
                                onClick={() => requestReplaceLesson(lesson)}
                                className="flex w-full items-center gap-2 px-3 py-2 text-left text-xs font-extrabold text-[#18332D] hover:bg-[#F7F1E8]"
                              >
                                <FileUp size={14} /> Replace file
                              </button>
                              {canRetryLessonIndexing(lesson) && (
                                <button
                                  type="button"
                                  onClick={() => retryLessonIndexing(lesson)}
                                  disabled={reindexingLessonId === lesson.skillModuleLessonId}
                                  className="flex w-full items-center gap-2 px-3 py-2 text-left text-xs font-extrabold text-[#1F6F5F] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:text-slate-400"
                                >
                                  {reindexingLessonId === lesson.skillModuleLessonId ? (
                                    <Loader2 size={14} className="animate-spin" />
                                  ) : (
                                    <Loader2 size={14} />
                                  )}
                                  Retry indexing
                                </button>
                              )}
                              <div className="my-1 border-t border-[#B9D8CC]/70" />
                              <button
                                type="button"
                                onClick={() => requestDeleteLesson(lesson)}
                                className="flex w-full items-center gap-2 px-3 py-2 text-left text-xs font-extrabold text-rose-700 hover:bg-rose-50"
                              >
                                <Trash2 size={14} /> Delete lesson
                              </button>
                            </div>
                          )}
                        </div>
                      </div>

                      {showDropAfter && (
                        <div className="absolute bottom-0 left-3 right-3 z-10 h-0.5 rounded-full bg-[#1F6F5F] shadow-[0_0_0_3px_rgba(111,207,151,0.18)]" />
                      )}
                    </div>
                  );
                })}
              </div>

              <div className="space-y-2 border-t border-[#B9D8CC]/60 p-3">
                {shouldPollLessonIndexing(localLessons) && (
                  <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-bold text-amber-800">
                    <Loader2 size={13} className="shrink-0 animate-spin" />
                    Indexing in background...
                  </div>
                )}

                <ModuleButton className="w-full" onClick={saveOrder} disabled={isSavingOrder}>
                  <Save size={14} /> {isSavingOrder ? "Saving..." : "Save order"}
                </ModuleButton>
              </div>
            </ModuleCard>
          )}
        </div>

        <LessonMetadataEditor
          module={module}
          lesson={activeLesson}
          onSaved={handleLessonSaved}
        />
      </div>

      <LessonPreviewDialog
        isOpen={Boolean(previewLesson)}
        lesson={previewLesson}
        preview={previewData}
        isLoading={isPreviewLoading}
        onClose={() => {
          setPreviewLesson(null);
          setPreviewData(null);
        }}
      />

      <ReplaceLessonContentDialog
        isOpen={Boolean(lessonToReplace)}
        lesson={lessonToReplace}
        isReplacing={isReplacingContent}
        onClose={() => setLessonToReplace(null)}
        onReplace={replaceLessonContent}
      />

      <ConfirmActionDialog
        isOpen={Boolean(lessonToDelete)}
        tone="danger"
        title="Delete this lesson?"
        description="This lesson file and its indexed chunks will be removed from the draft module."
        confirmLabel="Delete lesson"
        cancelLabel="Keep lesson"
        isConfirming={isDeletingLesson}
        onCancel={() => setLessonToDelete(null)}
        onConfirm={confirmDeleteLesson}
      />
    </div>
  );
}


function LessonIndexingBadge({ lesson }) {
  const meta = getLessonIndexingMeta(lesson);

  return (
    <ModuleBadge tone={meta.tone} className="shrink-0">
      {meta.label}
    </ModuleBadge>
  );
}

function LessonMetadataEditor({ module, lesson, onSaved }) {
  const [form, setForm] = useState({
    title: "",
    summary: "",
    estimatedHours: "",
  });
  const [isSaving, setIsSaving] = useState(false);
  const isDirty = hasLessonDraftChanges(form, lesson);

  useEffect(() => {
    setForm({
      title: lesson?.title || "",
      summary: lesson?.summary || "",
      estimatedHours: lesson?.estimatedHours ?? "",
    });
  }, [lesson?.skillModuleLessonId, lesson?.title, lesson?.summary, lesson?.estimatedHours]);

  const update = (key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const saveMetadata = async () => {
    if (!lesson) return;

    if (!form.title.trim()) {
      toast.error("Lesson title is required.");
      return;
    }

    try {
      setIsSaving(true);
      const updated = await counselorLearningModuleApi.updateLesson(
        module.skillModuleId,
        lesson.skillModuleLessonId,
        {
          title: form.title.trim(),
          slug: null,
          summary: form.summary.trim() || null,
          estimatedHours: form.estimatedHours === "" ? null : Number(form.estimatedHours),
        },
      );

      toast.success("Lesson details saved.");
      onSaved(updated);
    } catch (err) {
      toast.error(err?.message || "Unable to save lesson details.");
    } finally {
      setIsSaving(false);
    }
  };

  if (!lesson) {
    return (
      <ModuleCard className="p-5">
        <h2 className="text-sm font-extrabold text-[#18332D]">Lesson details</h2>
        <p className="mt-2 text-sm font-semibold leading-6 text-slate-600">
          Select a lesson to edit its details.
        </p>
      </ModuleCard>
    );
  }

  return (
    <ModuleCard className="flex h-full min-h-0 flex-col overflow-hidden p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-sm font-extrabold text-[#18332D]">Lesson details</h2>
            <DirtyStateBadge isDirty={isDirty} />
          </div>
          <p
            className="mt-1 truncate text-xs font-semibold text-slate-500"
            title={lesson.markdownFileName || lesson.fileName || "Markdown file"}
          >
            {lesson.markdownFileName || lesson.fileName || "Markdown file"}
          </p>
        </div>
        <span className="inline-flex shrink-0 items-center rounded-md border border-[#6FCF97] bg-[#6FCF97]/18 px-3 py-1.5 text-xs font-extrabold uppercase tracking-[0.08em] text-[#1F6F5F] shadow-sm">
          Lesson {lesson.orderIndex}
        </span>
      </div>

      {lesson.indexingError && (
        <div className="mt-3 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-xs font-bold leading-5 text-rose-700">
          {lesson.indexingError}
        </div>
      )}

      <div className="mt-4 space-y-4">
        <ModuleField label="Lesson title">
          <input
            value={form.title}
            onChange={(event) => update("title", event.target.value)}
            className={inputClass}
            placeholder="Lesson title"
          />
        </ModuleField>

        <ModuleField label="Summary">
          <textarea
            value={form.summary}
            onChange={(event) => update("summary", event.target.value)}
            className={`${inputClass} min-h-20 resize-none`}
            placeholder="Short summary shown in module lists and lesson details"
          />
        </ModuleField>

        <ModuleField label="Estimated hours">
          <input
            type="number"
            step="0.5"
            min="0"
            value={form.estimatedHours}
            onChange={(event) => update("estimatedHours", event.target.value)}
            className={inputClass}
            placeholder="1.5"
          />
        </ModuleField>

        <ModuleButton className="w-full" onClick={saveMetadata} disabled={isSaving || !isDirty}>
          <Save size={14} /> {isSaving ? "Saving..." : isDirty ? "Save lesson details" : "Saved"}
        </ModuleButton>
      </div>
    </ModuleCard>
  );
}


function LessonPreviewDialog({ isOpen, lesson, preview, isLoading, onClose }) {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm"
      onMouseDown={onClose}
    >
      <div
        className="flex h-[82vh] w-full max-w-4xl flex-col overflow-hidden rounded-xl border border-[#B9D8CC] bg-white shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-4 border-b border-[#B9D8CC] px-5 py-4">
          <div className="min-w-0">
            <h2 className="text-base font-extrabold text-[#18332D]">Lesson preview</h2>
            <p className="mt-1 truncate text-xs font-semibold text-slate-500">
              {lesson?.markdownFileName || lesson?.fileName || lesson?.title || "Markdown file"}
            </p>
          </div>

          <ModuleButton variant="secondary" size="xs" onClick={onClose}>
            Close
          </ModuleButton>
        </div>

        <div className="min-h-0 flex-1 overflow-auto p-6">
          {isLoading ? (
            <div className="flex h-full items-center justify-center gap-2 text-sm font-bold text-slate-600">
              <Loader2 size={16} className="animate-spin text-[#1F6F5F]" />
              Loading preview...
            </div>
          ) : (
            <MarkdownRenderer markdown={preview?.markdown || "# Empty preview\n\nNo lesson content was returned."} />
          )}
        </div>
      </div>
    </div>
  );
}

function ReplaceLessonContentDialog({ isOpen, lesson, isReplacing, onClose, onReplace }) {
  const [replacementFile, setReplacementFile] = useState(null);

  useEffect(() => {
    if (isOpen) {
      setReplacementFile(null);
    }
  }, [isOpen, lesson?.skillModuleLessonId]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm"
      onMouseDown={isReplacing ? undefined : onClose}
    >
      <div
        className="w-full max-w-lg rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="flex items-start gap-3">
          <div className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-[#6FCF97]/20 text-[#1F6F5F]">
            <FileUp size={20} />
          </div>

          <div className="min-w-0">
            <h2 className="text-base font-extrabold text-[#18332D]">Replace lesson file</h2>
            <p className="mt-1 text-sm font-semibold leading-6 text-slate-600">
              Upload a new Markdown file for {lesson?.title || "this lesson"}. Indexing will run in the background after the file is replaced.
            </p>
          </div>
        </div>

        <label className="mt-5 flex cursor-pointer flex-col items-center justify-center rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/50 px-4 py-6 text-center hover:bg-[#F7F1E8]">
          <Upload size={20} className="mb-2 text-[#1F6F5F]" />
          <span className="text-sm font-extrabold text-[#18332D]">
            {replacementFile ? replacementFile.name : "Choose replacement file"}
          </span>
          <span className="mt-1 text-xs font-semibold text-slate-600">.md or .markdown</span>
          <input
            type="file"
            accept=".md,.markdown,text/markdown,text/plain"
            className="hidden"
            onChange={(event) => setReplacementFile(event.target.files?.[0] || null)}
          />
        </label>

        {isReplacing && (
          <div className="mt-3 flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-bold leading-5 text-amber-800">
            <Loader2 size={14} className="shrink-0 animate-spin" />
            Replacing file...
          </div>
        )}

        <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <ModuleButton variant="secondary" onClick={onClose} disabled={isReplacing}>
            Cancel
          </ModuleButton>
          <ModuleButton onClick={() => onReplace(replacementFile)} disabled={!replacementFile || isReplacing}>
            {isReplacing ? "Replacing..." : "Replace file"}
          </ModuleButton>
        </div>
      </div>
    </div>
  );
}


function QuizEditor({ module, quiz, onChanged, onDraftStateChange }) {
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
  const hasQuizDraftWork = hasUnsavedQuestions || hasDirtyQuestions || hasQuizSettingsChanges || isQuestionOrderDirty;
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

    onDraftStateChange?.(unsavedQuestions.length > 0);
  }, [questions, draftStorageKey, onDraftStateChange]);

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
      await counselorLearningModuleApi.upsertQuiz(module.skillModuleId, {
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
        const savedQuestion = await counselorLearningModuleApi.addQuestion(module.skillModuleId, toQuestionPayload(question));

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
        const savedQuestion = await counselorLearningModuleApi.updateQuestion(
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
      await counselorLearningModuleApi.deleteQuestion(module.skillModuleId, questionToDelete.skillModuleQuizQuestionId);
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
      await counselorLearningModuleApi.reorderQuestions(
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
                    className={`relative w-full rounded-lg border px-3 py-3 text-left transition-all duration-150 ${
                      isActive
                        ? "border-[#6FCF97] bg-[#6FCF97]/14 shadow-sm"
                        : "border-[#B9D8CC]/70 bg-white hover:border-[#6FCF97] hover:bg-[#F7F1E8]/55"
                    } ${isDragging ? "scale-[0.99] opacity-40" : "opacity-100"} ${
                      isDropTarget ? "bg-[#F7F1E8]/70" : ""
                    }`}
                  >
                    {showDropBefore && (
                      <div className="absolute left-3 right-3 top-0 z-10 h-0.5 rounded-full bg-[#1F6F5F] shadow-[0_0_0_3px_rgba(111,207,151,0.18)]" />
                    )}

                    <div className="flex items-start gap-3">
                      <GripVertical size={15} className="mt-1 shrink-0 cursor-grab text-slate-400 active:cursor-grabbing" />
                      <div className={`grid h-8 w-8 shrink-0 place-items-center rounded-full text-xs font-extrabold ${
                        isActive ? "bg-[#6FCF97]/24 text-[#1F6F5F]" : "bg-[#F7F1E8] text-slate-600"
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
        isOpen={Boolean(questionToDelete)}
        tone="danger"
        title="Delete this question?"
        description="This question and its options will be removed from the quiz."
        confirmLabel="Delete question"
        cancelLabel="Keep question"
        onCancel={() => setQuestionToDelete(null)}
        onConfirm={confirmDeleteQuestion}
      />

      <ConfirmActionDialog
        isOpen={Boolean(unsavedQuestionToDelete)}
        tone="warning"
        title="Discard this unsaved question?"
        description="This question has not been saved yet. Discarding it will remove the draft question from this editor."
        confirmLabel="Discard question"
        cancelLabel="Keep editing"
        onCancel={() => setUnsavedQuestionToDelete(null)}
        onConfirm={confirmDeleteUnsavedQuestion}
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
                  className={`grid h-7 w-7 place-items-center rounded-full border text-xs font-extrabold transition ${
                    option.isCorrect
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

function InlinePreview({ moduleId, detail }) {
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
        const data = await counselorLearningModuleApi.getLessonPreview(moduleId, activeLessonId);
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


function PublishPanel({ detail, isPublishing, onPublish }) {
  const module = detail.module;
  const lessons = detail.lessons || [];
  const lessonCount = lessons.length;
  const indexedLessonCount = getIndexedLessonCount(lessons);
  const lessonsIndexed = areLessonsIndexed(lessons);
  const questionCount = detail.quiz?.questions?.length || 0;
  const requiredLessons = 3;
  const requiredQuestions = 10;

  const checks = useMemo(
    () => [
      {
        label: "Module overview",
        description: "Title, skill, and description are ready.",
        complete: Boolean(module.title && module.skillId && module.description),
      },
      {
        label: "Lessons",
        description:
          lessonCount >= requiredLessons
            ? `${lessonCount} lessons added.`
            : `${lessonCount}/${requiredLessons} lessons added.`,
        complete: lessonCount >= requiredLessons,
      },
      {
        label: "Lesson indexing",
        description:
          lessonCount === 0
            ? "Upload lessons before indexing."
            : lessonsIndexed
              ? "All lessons are indexed for module chat."
              : `${indexedLessonCount}/${lessonCount} lessons indexed.`,
        complete: lessonCount > 0 && lessonsIndexed,
      },
      {
        label: "Quiz",
        description: !detail.quiz
          ? "Create a quiz for this module."
          : questionCount >= requiredQuestions
            ? `${questionCount} questions ready.`
            : `${questionCount}/${requiredQuestions} questions added.`,
        complete: Boolean(detail.quiz) && questionCount >= requiredQuestions,
      },
    ],
    [
      detail.quiz,
      indexedLessonCount,
      lessonCount,
      lessonsIndexed,
      module.description,
      module.skillId,
      module.title,
      questionCount,
    ],
  );

  const frontendReady = checks.every((check) => check.complete);
  const backendReady = detail.publishReadiness?.canPublish ?? true;
  const canPublish = frontendReady && backendReady;
  const backendErrors = detail.publishReadiness?.errors || [];

  if (module.status !== "draft") {
    return (
      <ModuleCard className="p-8 text-center">
        <CheckCircle2 size={34} className="mx-auto text-[#1F6F5F]" />
        <h2 className="mt-3 text-lg font-extrabold text-[#18332D]">
          This module is {module.status}
        </h2>
        <p className="mx-auto mt-2 max-w-md text-sm font-semibold leading-6 text-slate-600">
          Publishing is only available while the module is still a draft.
        </p>
      </ModuleCard>
    );
  }

  return (
    <ModuleCard className="flex h-full min-h-0 flex-col overflow-hidden p-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-extrabold text-[#18332D]">Publish module</h2>
        </div>

        <ModuleBadge tone={canPublish ? "green" : "rose"}>
          {canPublish ? "Ready" : "Not ready"}
        </ModuleBadge>
      </div>

      <div className="mt-5 grid gap-3">
        {checks.map((check) => (
          <div
            key={check.label}
            className={`flex items-start gap-3 rounded-xl border p-4 ${
              check.complete
                ? "border-[#B9D8CC] bg-[#6FCF97]/10"
                : "border-rose-200 bg-rose-50"
            }`}
          >
            {check.complete ? (
              <CheckCircle2 size={20} className="mt-0.5 shrink-0 text-[#1F6F5F]" />
            ) : (
              <Circle size={20} className="mt-0.5 shrink-0 text-rose-600" />
            )}
            <div>
              <div className="text-sm font-extrabold text-[#18332D]">{check.label}</div>
              <div className="mt-1 text-sm font-semibold text-slate-600">{check.description}</div>
            </div>
          </div>
        ))}
      </div>

      {backendErrors.length > 0 && (
        <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 p-4">
          <div className="flex items-center gap-2 text-sm font-extrabold text-rose-700">
            <AlertCircle size={16} /> Needs attention
          </div>
          <ul className="mt-2 space-y-1 text-sm font-semibold text-rose-700">
            {backendErrors.map((error) => (
              <li key={error}>• {error}</li>
            ))}
          </ul>
        </div>
      )}

      <div className="mt-5 flex justify-end">
        <ModuleButton onClick={onPublish} disabled={!canPublish || isPublishing}>
          {isPublishing ? "Publishing..." : "Publish module"}
        </ModuleButton>
      </div>
    </ModuleCard>
  );
}

function createEmptyQuestionPayload(number) {
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

function toQuestionPayload(question) {
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
