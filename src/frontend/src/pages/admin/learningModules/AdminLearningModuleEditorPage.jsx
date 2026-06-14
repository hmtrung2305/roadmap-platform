import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Circle,
  GripVertical,
  Minus,
  Plus,
  Save,
  Settings,
  Trash2,
  Upload,
} from "lucide-react";
import { toast } from "react-toastify";
import { counselorLearningModuleApi } from "../../../api/learningModuleApi";
import MarkdownRenderer, { titleFromMarkdown } from "../../../components/learningModules/MarkdownRenderer";
import SkillSearchPicker from "../../../components/learningModules/SkillSearchPicker";
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

export default function AdminLearningModuleEditorPage() {
  const { moduleSlug } = useParams();
  const navigate = useNavigate();
  const [detail, setDetail] = useState(null);
  const [activeTab, setActiveTab] = useState("overview");
  const [isLoading, setIsLoading] = useState(true);
  const [isPublishing, setIsPublishing] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [resolvedModuleId, setResolvedModuleId] = useState(null);

  const reload = () => setRefreshKey((key) => key + 1);

  useEffect(() => {
    let ignore = false;

    async function loadDetail() {
      try {
        setIsLoading(true);
        const moduleId = await counselorLearningModuleApi.resolveModuleIdFromRoute(moduleSlug);
        const data = await counselorLearningModuleApi.getModule(moduleId);

        if (!ignore) {
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
  }, [moduleSlug, refreshKey]);

  const module = detail?.module;
  const activeModuleId = module?.skillModuleId || resolvedModuleId;

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
        reload();
        return;
      }

      toast.success("Module published.");
      navigate("/admin/learning-modules?status=published");
    } catch (err) {
      toast.error(err?.message || "Unable to publish module.");
    } finally {
      setIsPublishing(false);
    }
  };

  if (isLoading) {
    return (
      <ModulePageShell>
        <ModuleCard className="p-10 text-center text-sm font-bold text-slate-600">
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
        <div>
          <button
            type="button"
            onClick={() => navigate("/admin/learning-modules")}
            className="inline-flex cursor-pointer items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to management
          </button>
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
                  onClick={() => setActiveTab(tab.key)}
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

        {activeTab === "overview" && <OverviewEditor module={module} onSaved={reload} />}
        {activeTab === "lessons" && <LessonsEditor module={module} lessons={detail.lessons} onChanged={reload} />}
        {activeTab === "quiz" && <QuizEditor module={module} quiz={detail.quiz} onChanged={reload} />}
        {activeTab === "preview" && <InlinePreview moduleId={activeModuleId} detail={detail} />}
        {activeTab === "publish" && (
          <PublishPanel
            detail={detail}
            isPublishing={isPublishing}
            onPublish={handlePublish}
          />
        )}
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
      await counselorLearningModuleApi.updateModule(module.skillModuleId, {
        skillId: form.skillId,
        title: form.title.trim(),
        slug: null,
        description: form.description.trim() || null,
        difficultyLevel: form.difficultyLevel || null,
        estimatedHours: form.estimatedHours === "" ? null : Number(form.estimatedHours),
      });

      toast.success("Overview saved.");
      onSaved();
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
          <h2 className="text-lg font-extrabold text-[#18332D]">Overview</h2>
          <p className="mt-1 text-sm font-semibold text-slate-600">Set the basic module information learners will see.</p>
        </div>

        <ModuleButton onClick={save} disabled={isSaving}>
          <Save size={14} /> {isSaving ? "Saving..." : "Save overview"}
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


function LessonsEditor({ module, lessons, onChanged }) {
  const [selectedFiles, setSelectedFiles] = useState([]);
  const [localLessons, setLocalLessons] = useState(lessons);
  const [activeLessonId, setActiveLessonId] = useState(lessons[0]?.skillModuleLessonId || null);
  const [preview, setPreview] = useState(null);
  const [previewRefreshKey, setPreviewRefreshKey] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const [isSavingOrder, setIsSavingOrder] = useState(false);
  const [draggedLessonId, setDraggedLessonId] = useState(null);

  useEffect(() => {
    setLocalLessons(lessons);
    setActiveLessonId((current) => {
      if (current && lessons.some((lesson) => lesson.skillModuleLessonId === current)) {
        return current;
      }

      return lessons[0]?.skillModuleLessonId || null;
    });
  }, [lessons]);

  useEffect(() => {
    let ignore = false;

    async function loadPreview() {
      if (!activeLessonId) {
        setPreview(null);
        return;
      }

      try {
        const data = await counselorLearningModuleApi.getLessonPreview(module.skillModuleId, activeLessonId);
        if (!ignore) setPreview(data);
      } catch {
        if (!ignore) setPreview(null);
      }
    }

    loadPreview();

    return () => {
      ignore = true;
    };
  }, [module.skillModuleId, activeLessonId, previewRefreshKey]);

  const orderedLessons = localLessons.slice().sort((a, b) => a.orderIndex - b.orderIndex);
  const activeLesson = orderedLessons.find((lesson) => lesson.skillModuleLessonId === activeLessonId) || null;

  const refreshPreview = () => setPreviewRefreshKey((key) => key + 1);

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

      await counselorLearningModuleApi.bulkUploadLessons(module.skillModuleId, lessonPayload, selectedFiles);

      toast.success("Lessons uploaded.");
      setSelectedFiles([]);
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to upload lessons.");
      onChanged();
    } finally {
      setIsUploading(false);
    }
  };

  const reorderLessons = (sourceId, targetId) => {
    if (!sourceId || !targetId || sourceId === targetId) return;

    const current = orderedLessons;
    const sourceIndex = current.findIndex((lesson) => lesson.skillModuleLessonId === sourceId);
    const targetIndex = current.findIndex((lesson) => lesson.skillModuleLessonId === targetId);

    if (sourceIndex < 0 || targetIndex < 0) return;

    const next = current.slice();
    const [moved] = next.splice(sourceIndex, 1);
    next.splice(targetIndex, 0, moved);

    setLocalLessons(next.map((lesson, index) => ({ ...lesson, orderIndex: index + 1 })));
  };

  const moveLesson = (lessonId, direction) => {
    const current = orderedLessons;
    const index = current.findIndex((lesson) => lesson.skillModuleLessonId === lessonId);
    const targetIndex = index + direction;

    if (index < 0 || targetIndex < 0 || targetIndex >= current.length) return;

    const next = current.slice();
    [next[index], next[targetIndex]] = [next[targetIndex], next[index]];

    setLocalLessons(next.map((lesson, nextIndex) => ({ ...lesson, orderIndex: nextIndex + 1 })));
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

  const deleteLesson = async (lesson) => {
    if (!window.confirm(`Delete lesson "${lesson.title}"?`)) return;

    try {
      await counselorLearningModuleApi.deleteLesson(module.skillModuleId, lesson.skillModuleLessonId);
      toast.success("Lesson deleted.");
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to delete lesson.");
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

    refreshPreview();
    onChanged();
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
            <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-bold leading-5 text-amber-800">
              Uploading and preparing lesson content. This can take a moment because the lessons are being indexed for module chat.
            </div>
          )}

          <ModuleButton className="mt-4 w-full" onClick={upload} disabled={isUploading}>
            {isUploading ? "Preparing lessons..." : "Upload lessons"}
          </ModuleButton>
        </ModuleCard>

        {orderedLessons.length === 0 ? (
          <ModuleEmptyState title="No lessons yet">Upload Markdown files to start building this module.</ModuleEmptyState>
        ) : (
          <ModuleCard className="grid min-h-0 grid-rows-[minmax(0,1fr)_auto] overflow-hidden">
            <div className="min-h-0 overflow-y-auto">
              {orderedLessons.map((lesson, index) => (
                <div
                  key={lesson.skillModuleLessonId}
                  draggable
                onDragStart={() => setDraggedLessonId(lesson.skillModuleLessonId)}
                onDragOver={(event) => event.preventDefault()}
                onDrop={() => {
                  reorderLessons(draggedLessonId, lesson.skillModuleLessonId);
                  setDraggedLessonId(null);
                }}
                className={`border-b border-[#B9D8CC]/60 p-3 last:border-b-0 ${
                  activeLessonId === lesson.skillModuleLessonId ? "bg-[#6FCF97]/10" : "bg-white"
                } ${draggedLessonId === lesson.skillModuleLessonId ? "opacity-60" : ""}`}
                >
                  <div className="flex items-center gap-3">
                    <GripVertical size={16} className="cursor-grab text-slate-500" />
                    <button
                      type="button"
                      onClick={() => setActiveLessonId(lesson.skillModuleLessonId)}
                      className="min-w-0 flex-1 text-left"
                    >
                      <div className="truncate text-sm font-extrabold text-[#18332D]">
                        {index + 1}. {lesson.title}
                      </div>
                      <LessonIndexingBadge lesson={lesson} />
                    </button>
                    <ModuleButton variant="secondary" size="icon" onClick={() => moveLesson(lesson.skillModuleLessonId, -1)}>↑</ModuleButton>
                    <ModuleButton variant="secondary" size="icon" onClick={() => moveLesson(lesson.skillModuleLessonId, 1)}>↓</ModuleButton>
                    <ModuleButton variant="danger" size="icon" onClick={() => deleteLesson(lesson)} aria-label="Delete lesson">
                      <Trash2 size={14} />
                    </ModuleButton>
                  </div>
                </div>
              ))}
            </div>

            <div className="border-t border-[#B9D8CC]/60 p-3">
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
          onContentReplaced={(updatedLesson) => {
            handleLessonSaved(updatedLesson);
            refreshPreview();
          }}
        />
      </div>

      <ModuleCard className="min-h-[560px] overflow-hidden">
        <div className="border-b border-[#B9D8CC] px-5 py-4">
          <h2 className="text-sm font-extrabold text-[#18332D]">{preview?.title || "Lesson preview"}</h2>
        </div>
        <div className="max-h-[720px] overflow-auto p-6">
          <MarkdownRenderer markdown={preview?.markdown || `# Lesson preview\n\nSelect a lesson to preview its content.`} />
        </div>
      </ModuleCard>
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

function LessonMetadataEditor({ module, lesson, onSaved, onContentReplaced }) {
  const [form, setForm] = useState({
    title: "",
    summary: "",
    estimatedHours: "",
  });
  const [replacementFile, setReplacementFile] = useState(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isReplacingContent, setIsReplacingContent] = useState(false);

  useEffect(() => {
    setForm({
      title: lesson?.title || "",
      summary: lesson?.summary || "",
      estimatedHours: lesson?.estimatedHours ?? "",
    });
    setReplacementFile(null);
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

  const replaceContent = async () => {
    if (!lesson || !replacementFile) return;

    try {
      setIsReplacingContent(true);
      const updated = await counselorLearningModuleApi.replaceLessonContent(
        module.skillModuleId,
        lesson.skillModuleLessonId,
        replacementFile,
      );

      toast.success("Lesson content replaced.");
      setReplacementFile(null);
      onContentReplaced(updated);
    } catch (err) {
      toast.error(err?.message || "Unable to replace lesson content.");
    } finally {
      setIsReplacingContent(false);
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
        <div>
          <h2 className="text-sm font-extrabold text-[#18332D]">Lesson details</h2>
          <p className="mt-1 text-xs font-semibold text-slate-500">
            Editing {lesson.title}
          </p>
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1.5">
          <ModuleBadge tone="slate">Lesson {lesson.orderIndex}</ModuleBadge>
          <LessonIndexingBadge lesson={lesson} />
        </div>
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

        <ModuleButton className="w-full" onClick={saveMetadata} disabled={isSaving}>
          <Save size={14} /> {isSaving ? "Saving..." : "Save lesson details"}
        </ModuleButton>
      </div>

      <div className="my-5 border-t border-[#B9D8CC]" />

      <div className="space-y-3">
        <div>
          <h3 className="text-sm font-extrabold text-[#18332D]">Replace Markdown file</h3>
          <p className="mt-1 text-xs font-semibold leading-5 text-slate-600">
            Replacing the file will update the preview and rebuild the lesson chunks for module chat.
          </p>
        </div>

        <label className="flex cursor-pointer flex-col items-center justify-center rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/50 px-4 py-5 text-center hover:bg-[#F7F1E8]">
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

        {isReplacingContent && (
          <div className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-bold leading-5 text-amber-800">
            Replacing and re-indexing lesson content. Please wait for this to finish.
          </div>
        )}

        <ModuleButton
          className="w-full"
          variant="secondary"
          onClick={replaceContent}
          disabled={!replacementFile || isReplacingContent}
        >
          {isReplacingContent ? "Replacing..." : "Replace content"}
        </ModuleButton>
      </div>
    </ModuleCard>
  );
}


function QuizEditor({ module, quiz, onChanged }) {
  const hasQuiz = Boolean(quiz);
  const [quizForm, setQuizForm] = useState({
    title: quiz?.title || "",
    passingScorePercent: quiz?.passingScorePercent ?? 70,
    maxAttempts: quiz?.maxAttempts ?? 3,
  });
  const [questions, setQuestions] = useState(quiz?.questions || []);
  const [activeQuestionId, setActiveQuestionId] = useState(quiz?.questions?.[0]?.skillModuleQuizQuestionId || null);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isSavingQuiz, setIsSavingQuiz] = useState(false);
  const [isSavingOrder, setIsSavingOrder] = useState(false);
  const [isQuestionOrderDirty, setIsQuestionOrderDirty] = useState(false);
  const [draggedQuestionId, setDraggedQuestionId] = useState(null);

  useEffect(() => {
    const nextQuestions = quiz?.questions || [];

    setQuizForm({
      title: quiz?.title || "",
      passingScorePercent: quiz?.passingScorePercent ?? 70,
      maxAttempts: quiz?.maxAttempts ?? 3,
    });
    setQuestions(nextQuestions);
    setActiveQuestionId((current) =>
      nextQuestions.some((question) => question.skillModuleQuizQuestionId === current)
        ? current
        : nextQuestions[0]?.skillModuleQuizQuestionId || null,
    );
    setIsQuestionOrderDirty(false);
  }, [quiz]);

  const orderedQuestions = questions.slice().sort((a, b) => a.orderIndex - b.orderIndex);
  const activeQuestion =
    orderedQuestions.find((question) => question.skillModuleQuizQuestionId === activeQuestionId)
    || orderedQuestions[0]
    || null;
  const activeQuestionIndex = activeQuestion
    ? orderedQuestions.findIndex((question) => question.skillModuleQuizQuestionId === activeQuestion.skillModuleQuizQuestionId)
    : -1;

  const hasUnsavedQuestions = orderedQuestions.some((question) =>
    String(question.skillModuleQuizQuestionId).startsWith("new-"),
  );
  const canSaveQuestionOrder = orderedQuestions.length > 1 && !hasUnsavedQuestions && isQuestionOrderDirty;
  const updateQuiz = (key, value) => setQuizForm((current) => ({ ...current, [key]: value }));

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
    const isNewQuestion = String(question.skillModuleQuizQuestionId).startsWith("new-");

    try {
      if (isNewQuestion) {
        await counselorLearningModuleApi.addQuestion(module.skillModuleId, toQuestionPayload(question));
        toast.success("Question added.");
      } else {
        await counselorLearningModuleApi.updateQuestion(module.skillModuleId, question.skillModuleQuizQuestionId, toQuestionPayload(question));
        toast.success("Question saved.");
      }

      setActiveQuestionId(question.skillModuleQuizQuestionId);
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to save question.");
    }
  };

  const deleteQuestion = async (question) => {
    const isNewQuestion = String(question.skillModuleQuizQuestionId).startsWith("new-");
    const nextActiveQuestion = orderedQuestions.find((item) => item.skillModuleQuizQuestionId !== question.skillModuleQuizQuestionId);

    if (isNewQuestion) {
      setQuestions((current) =>
        current.filter((item) => item.skillModuleQuizQuestionId !== question.skillModuleQuizQuestionId),
      );
      setActiveQuestionId(nextActiveQuestion?.skillModuleQuizQuestionId || null);
      return;
    }

    if (!window.confirm("Delete this question?")) return;

    try {
      await counselorLearningModuleApi.deleteQuestion(module.skillModuleId, question.skillModuleQuizQuestionId);
      toast.success("Question deleted.");
      setActiveQuestionId(nextActiveQuestion?.skillModuleQuizQuestionId || null);
      onChanged();
    } catch (err) {
      toast.error(err?.message || "Unable to delete question.");
    }
  };

  const reorderQuestions = (sourceId, targetId) => {
    if (!sourceId || !targetId || sourceId === targetId) return;

    const sourceIndex = orderedQuestions.findIndex((question) => question.skillModuleQuizQuestionId === sourceId);
    const targetIndex = orderedQuestions.findIndex((question) => question.skillModuleQuizQuestionId === targetId);

    if (sourceIndex < 0 || targetIndex < 0) return;

    const next = orderedQuestions.slice();
    const [moved] = next.splice(sourceIndex, 1);
    next.splice(targetIndex, 0, moved);

    setQuestions(next.map((question, index) => ({ ...question, orderIndex: index + 1 })));
    setIsQuestionOrderDirty(true);
  };

  const updateQuestion = (nextQuestion) => {
    setQuestions((current) =>
      current.map((item) =>
        item.skillModuleQuizQuestionId === nextQuestion.skillModuleQuizQuestionId ? nextQuestion : item,
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
            <h2 className="text-lg font-extrabold text-[#18332D]">Create quiz</h2>
          </div>

          {quizFields}

          <div className="mt-4 flex justify-end">
            <ModuleButton onClick={saveQuiz} disabled={isSavingQuiz}>
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
            <div className="flex items-center gap-2 text-sm font-extrabold text-[#18332D]">
              <Settings size={16} />
              Quiz settings
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
              <ModuleButton onClick={saveQuiz} disabled={isSavingQuiz}>
                {isSavingQuiz ? "Saving..." : "Save settings"}
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
                const questionTitle = question.questionText?.trim() || `Question ${index + 1}`;

                return (
                  <button
                    key={question.skillModuleQuizQuestionId}
                    type="button"
                    draggable
                    title={questionTitle}
                    onDragStart={() => setDraggedQuestionId(question.skillModuleQuizQuestionId)}
                    onDragOver={(event) => event.preventDefault()}
                    onDrop={() => {
                      reorderQuestions(draggedQuestionId, question.skillModuleQuizQuestionId);
                      setDraggedQuestionId(null);
                    }}
                    onClick={() => setActiveQuestionId(question.skillModuleQuizQuestionId)}
                    className={`w-full rounded-lg border px-3 py-3 text-left transition ${
                      isActive
                        ? "border-[#6FCF97] bg-[#6FCF97]/14 shadow-sm"
                        : "border-[#B9D8CC]/70 bg-white hover:border-[#6FCF97] hover:bg-[#F7F1E8]/55"
                    }`}
                  >
                    <div className="flex items-start gap-3">
                      <GripVertical size={15} className="mt-1 shrink-0 cursor-grab text-slate-400" />
                      <div className={`grid h-8 w-8 shrink-0 place-items-center rounded-full text-xs font-extrabold ${
                        isActive ? "bg-[#6FCF97]/24 text-[#1F6F5F]" : "bg-[#F7F1E8] text-slate-600"
                      }`}>
                        {index + 1}
                      </div>

                      <div className="min-w-0">
                        <div className="line-clamp-2 text-sm font-extrabold leading-5 text-[#18332D]">
                          {questionTitle}
                        </div>
                        <div className="mt-1 text-xs font-semibold text-slate-500">
                          Multiple choice
                        </div>
                      </div>
                    </div>
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
            onDelete={() => deleteQuestion(activeQuestion)}
          />
        ) : (
          <ModuleEmptyState title="No question selected">
            Add a question from the left panel to start building the quiz.
          </ModuleEmptyState>
        )}
      </div>
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
          <div className="text-sm font-extrabold text-[#18332D]">Question {questionNumber}</div>
          <div className="text-xs font-semibold text-slate-500">Multiple choice</div>
        </div>

        <ModuleButton variant="danger" size="icon" onClick={onDelete} aria-label="Delete question">
          <Trash2 size={14} />
        </ModuleButton>
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
                  <ModuleButton variant="danger" size="icon" onClick={() => removeOption(option.skillModuleQuizOptionId)} aria-label="Remove option">
                    <Trash2 size={14} />
                  </ModuleButton>
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
          <ModuleButton onClick={onSave}>Save question</ModuleButton>
        </div>
      </div>
    </ModuleCard>
  );
}

function InlinePreview({ moduleId, detail }) {
  return <PreviewShell moduleId={moduleId} detail={detail} />;
}

function PreviewShell({ moduleId, detail }) {
  const lessons = detail.lessons || [];
  const [activeLessonId, setActiveLessonId] = useState(lessons[0]?.skillModuleLessonId || null);
  const [preview, setPreview] = useState(null);

  useEffect(() => {
    setActiveLessonId((current) => {
      if (lessons.some((lesson) => lesson.skillModuleLessonId === current)) {
        return current;
      }

      return lessons[0]?.skillModuleLessonId || null;
    });
  }, [lessons]);

  useEffect(() => {
    let ignore = false;

    async function loadPreview() {
      if (!moduleId || !activeLessonId) {
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
        </div>
      </ModuleCard>

      <ModuleCard className="overflow-hidden">
        <div className="border-b border-[#B9D8CC] px-5 py-4">
          <h2 className="text-lg font-extrabold text-[#18332D]">
            {preview?.title || "Module preview"}
          </h2>
        </div>
        <div className="max-h-[620px] overflow-auto p-6">
          <MarkdownRenderer markdown={preview?.markdown || "# Empty preview\n\nUpload lessons to preview the learner reading experience."} />
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
