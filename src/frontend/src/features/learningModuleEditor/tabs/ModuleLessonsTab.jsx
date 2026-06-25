/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from "react";
import { Eye, FileUp, GripVertical, Loader2, MoreVertical, Save, Trash2, Upload } from "lucide-react";
import { toast } from "react-toastify";
import { contentManagerLearningModuleApi } from "../../../api/learningModuleApi";
import { LEARNING_MODULE_AUTHORING_LIMITS, formatFileSize } from "../../../constants/learningModuleAuthoringLimits";
import MarkdownRenderer from "../../../features/learningModules/components/MarkdownRenderer";
import { titleFromMarkdown } from "../../../utils/markdownUtils";
import ConfirmActionDialog from "../../../features/learningModules/components/ConfirmActionDialog";
import { inputClass, ModuleBadge, ModuleButton, ModuleCard, ModuleEmptyState, ModuleField } from "../../../features/learningModules/components/learningModuleUi";
import { DirtyStateBadge } from "../EditorControls";
import { canRetryLessonIndexing, getEditorStorageKey, getLessonIndexingMeta, getUploadedLessons, hasLessonDraftChanges, hasLessonOrderChanges, readSessionValue, removeSessionValue, shouldPollLessonIndexing, showBulkUploadResultToast, writeSessionValue } from "../editorUtils";

const MARKDOWN_EXTENSIONS = [".md", ".markdown"];

function isMarkdownFile(file) {
  const name = file?.name?.toLowerCase() || "";
  return MARKDOWN_EXTENSIONS.some((extension) => name.endsWith(extension));
}

function getSelectedFileErrors(files) {
  const errors = [];

  if (files.length > LEARNING_MODULE_AUTHORING_LIMITS.maxBulkLessonCount) {
    errors.push(`Choose up to ${LEARNING_MODULE_AUTHORING_LIMITS.maxBulkLessonCount} files.`);
  }

  const totalBytes = files.reduce((sum, file) => sum + file.size, 0);
  if (totalBytes > LEARNING_MODULE_AUTHORING_LIMITS.maxBulkMarkdownUploadBytes) {
    errors.push(`Total upload must be ${formatFileSize(LEARNING_MODULE_AUTHORING_LIMITS.maxBulkMarkdownUploadBytes)} or less.`);
  }

  const unsupported = files.filter((file) => !isMarkdownFile(file));
  if (unsupported.length > 0) {
    errors.push("Only Markdown files are supported.");
  }

  const oversized = files.filter((file) => file.size > LEARNING_MODULE_AUTHORING_LIMITS.maxMarkdownFileBytes);
  if (oversized.length > 0) {
    errors.push(`Each file must be ${formatFileSize(LEARNING_MODULE_AUTHORING_LIMITS.maxMarkdownFileBytes)} or less.`);
  }

  const fileNames = new Set();
  const hasDuplicateNames = files.some((file) => {
    const normalizedName = file.name.trim().toLowerCase();
    if (fileNames.has(normalizedName)) return true;
    fileNames.add(normalizedName);
    return false;
  });

  if (hasDuplicateNames) {
    errors.push("File names must be unique.");
  }

  return errors;
}

function validateFilesForSelection(files) {
  const errors = getSelectedFileErrors(files);

  if (errors.length > 0) {
    errors.forEach((error) => toast.error(error));
    return false;
  }

  return true;
}

export default function ModuleLessonsTab({ module, lessons, onChanged, onIndexingStatusPoll, onDirtyStateChange }) {
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
  const [isLessonMetadataDirty, setIsLessonMetadataDirty] = useState(false);

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
  const isOrderDirty = hasLessonOrderChanges(localLessons, lessons);
  const hasLessonDraftWork = selectedFiles.length > 0 || isOrderDirty || isLessonMetadataDirty;

  const handleFileSelection = (fileList) => {
    const files = Array.from(fileList || []);

    if (files.length === 0) {
      setSelectedFiles([]);
      return;
    }

    if (!validateFilesForSelection(files)) {
      return;
    }

    setSelectedFiles(files);
  };

  useEffect(() => {
    onDirtyStateChange?.("lessons", hasLessonDraftWork);
  }, [hasLessonDraftWork, onDirtyStateChange]);

  useEffect(() => () => {
    onDirtyStateChange?.("lessons", false);
  }, [onDirtyStateChange]);

  const upload = async () => {
    if (selectedFiles.length === 0) {
      toast.error("Choose at least one Markdown file.");
      return;
    }

    if (!validateFilesForSelection(selectedFiles)) {
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

      const result = await contentManagerLearningModuleApi.bulkUploadLessons(module.skillModuleId, lessonPayload, selectedFiles);
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
      await contentManagerLearningModuleApi.reorderLessons(
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
      const data = await contentManagerLearningModuleApi.getLessonPreview(module.skillModuleId, lesson.skillModuleLessonId);
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

    if (!validateFilesForSelection([file])) {
      return;
    }

    try {
      setIsReplacingContent(true);
      const updated = await contentManagerLearningModuleApi.replaceLessonContent(
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
      const updated = await contentManagerLearningModuleApi.reindexLesson(
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
      await contentManagerLearningModuleApi.deleteLesson(module.skillModuleId, lessonToDelete.skillModuleLessonId);
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
              <span className="mt-1 text-xs font-semibold text-slate-600">.md or .markdown, 2 MB each</span>
              <input
                type="file"
                multiple
                accept=".md,.markdown,text/markdown,text/plain"
                className="hidden"
                onChange={(event) => handleFileSelection(event.target.files)}
              />
            </label>

            {selectedFiles.length > 0 && (
              <div className="mt-3 space-y-1">
                {selectedFiles.map((file) => (
                  <div key={file.name} className="flex items-center justify-between gap-3 rounded-lg bg-white px-3 py-2 text-xs font-bold text-slate-700">
                    <span className="min-w-0 truncate">{file.name}</span>
                    <span className="shrink-0 text-slate-500">{formatFileSize(file.size)}</span>
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
          onDirtyStateChange={setIsLessonMetadataDirty}
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

function LessonMetadataEditor({ module, lesson, onSaved, onDirtyStateChange }) {
  const [form, setForm] = useState({
    title: "",
    summary: "",
    estimatedHours: "",
  });
  const [isSaving, setIsSaving] = useState(false);
  const isDirty = hasLessonDraftChanges(form, lesson);

  useEffect(() => {
    onDirtyStateChange?.(isDirty);
  }, [isDirty, onDirtyStateChange]);

  useEffect(() => () => {
    onDirtyStateChange?.(false);
  }, [onDirtyStateChange]);

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
      const updated = await contentManagerLearningModuleApi.updateLesson(
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
          <span className="mt-1 text-xs font-semibold text-slate-600">.md or .markdown, 2 MB max</span>
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


