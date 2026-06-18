import { ArrowLeft, Loader2 } from "lucide-react";
import ConfirmActionDialog from "../../../components/learningModules/ConfirmActionDialog";
import {
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../../components/learningModules/learningModuleUi";
import ModuleEditorTabs from "../../../features/learningModuleEditor/ModuleEditorTabs";
import useLearningModuleEditor from "../../../features/learningModuleEditor/hooks/useLearningModuleEditor";
import ModuleLessonsTab from "../../../features/learningModuleEditor/tabs/ModuleLessonsTab";
import ModuleOverviewTab from "../../../features/learningModuleEditor/tabs/ModuleOverviewTab";
import ModulePreviewTab from "../../../features/learningModuleEditor/tabs/ModulePreviewTab";
import ModuleQuizTab from "../../../features/learningModuleEditor/tabs/ModuleQuizTab";
import ReviewPublishTab from "../../../features/learningModuleEditor/tabs/ReviewPublishTab";

export default function ContentManagerLearningModuleEditorPage() {
  const editor = useLearningModuleEditor();

  if (editor.isLoading && !editor.detail) {
    if (!editor.showLoadingState) {
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

  if (!editor.detail || !editor.module) {
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
            onClick={editor.leaveEditor}
            className="inline-flex cursor-pointer items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to management
          </button>

          {editor.isLoading && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-white px-2.5 py-1 text-xs font-bold text-slate-600 shadow-sm">
              <Loader2 size={13} className="animate-spin text-[#1F6F5F]" />
              Updating...
            </span>
          )}
        </div>

        <ModuleEditorTabs
          activeTab={editor.activeTab}
          detail={editor.detail}
          onSelect={editor.selectTab}
        />

        {editor.activeTab === "overview" && (
          <ModuleOverviewTab
            module={editor.module}
            onSaved={editor.handleOverviewSaved}
            onDirtyStateChange={editor.setDirtyState}
          />
        )}

        {editor.activeTab === "lessons" && (
          <ModuleLessonsTab
            module={editor.module}
            lessons={editor.detail.lessons}
            onChanged={editor.reload}
            onIndexingStatusPoll={editor.refreshDetailSilently}
            onDirtyStateChange={editor.setDirtyState}
          />
        )}

        {editor.activeTab === "quiz" && (
          <ModuleQuizTab
            module={editor.module}
            quiz={editor.detail.quiz}
            onChanged={editor.reload}
            onDirtyStateChange={editor.setDirtyState}
          />
        )}

        {editor.activeTab === "preview" && (
          <ModulePreviewTab
            moduleId={editor.activeModuleId}
            detail={editor.detail}
          />
        )}

        {editor.activeTab === "publish" && (
          <ReviewPublishTab
            detail={editor.detail}
            isPublishing={editor.isPublishing}
            onPublish={editor.openPublishDialog}
          />
        )}

        <ConfirmActionDialog
          isOpen={editor.isDiscardDialogOpen}
          tone="warning"
          title="Discard unsaved changes?"
          description="Changes that have not been saved will be lost."
          confirmLabel="Discard changes"
          cancelLabel="Keep editing"
          onCancel={editor.cancelDiscard}
          onConfirm={editor.discardChangesAndContinue}
        />

        <ConfirmActionDialog
          isOpen={editor.isPublishDialogOpen}
          tone="success"
          title="Publish this module?"
          description="Learners will be able to find and start this module after it is published."
          confirmLabel="Publish module"
          cancelLabel="Keep editing"
          isConfirming={editor.isPublishing}
          onCancel={editor.closePublishDialog}
          onConfirm={editor.handlePublish}
        />
      </div>
    </ModulePageShell>
  );
}
