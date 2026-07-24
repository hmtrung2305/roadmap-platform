import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  AlertCircle,
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  Clock3,
  HelpCircle,
  Layers3,
  Loader2,
  Map as MapIcon,
  MessageSquare,
  Plus,
  Send,
  X,
} from "lucide-react";

import ConfirmActionDialog from "../../../components/common/ConfirmActionDialog";
import {
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  ModuleButton,
  inputClass,
} from "../../../features/learningModules/components/learningModuleUi";
import DraftVersionActions from "../../../features/roadmapEditor/components/DraftVersionActions";
import MetadataEditor from "../../../features/roadmapEditor/components/MetadataEditor";
import NodeCreateModal from "../../../features/roadmapEditor/components/NodeCreateModal";
import NodeDetailsPanel from "../../../features/roadmapEditor/components/NodeDetailsPanel";
import NodeEditorGuideModal from "../../../features/roadmapEditor/components/NodeEditorGuideModal";
import NodeSearchCombobox from "../../../features/roadmapEditor/components/NodeSearchCombobox";
import RoadmapGraphCanvas from "../../../features/roadmapEditor/components/RoadmapGraphCanvas";
import useContentRoadmapEditor from "../../../features/roadmapEditor/hooks/useContentRoadmapEditor";
import {
  clearReviewDraftCache,
  readReviewDraftCache,
  REVIEW_DRAFT_CACHE_TYPES,
  writeReviewDraftCache,
} from "../../../features/roadmapEditor/reviewDraftCache";
import SkillFormModal from "../../../features/contentCatalog/SkillFormModal";
import LearningResourceFormModal from "../../../features/contentCatalog/LearningResourceFormModal";
import { skillApi } from "../../../api/skillApi";
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";
import { PERMISSIONS } from "../../../constants/permissions";
import { hasPermission } from "../../../utils/authorizationUtils";
import { useAuthStore } from "../../../stores/useAuthStore";
import {
  formatDate,
  formatVersionLabel,
  getNextMajorVersionLabel,
} from "../../../features/roadmapEditor/roadmapEditorUtils";

export default function ContentManagerRoadmapEditorPage() {
  const navigate = useNavigate();
  const { roadmapId } = useParams();
  const user = useAuthStore((state) => state.user);
  const editor = useContentRoadmapEditor(roadmapId);
  const [isCreateNodeOpen, setIsCreateNodeOpen] = useState(false);
  const [createNodeMode, setCreateNodeMode] = useState("child");
  const [reviewChangeLogDraft, setReviewChangeLogDraft] = useState({
    roadmapVersionId: "",
    value: "",
  });
  const [isCloneConfirmOpen, setIsCloneConfirmOpen] = useState(false);
  const [isGuideOpen, setIsGuideOpen] = useState(false);
  const [isCreateSkillOpen, setIsCreateSkillOpen] = useState(false);
  const [skillCreateInitialName, setSkillCreateInitialName] = useState("");
  const [isCreateResourceOpen, setIsCreateResourceOpen] = useState(false);
  const [resourceCreateInitialTitle, setResourceCreateInitialTitle] =
    useState("");
  const [resourceCreateInitialUrl, setResourceCreateInitialUrl] = useState("");
  const [editingResource, setEditingResource] = useState(null);
  const [isValidationModalOpen, setIsValidationModalOpen] = useState(false);
  const [pendingNodeSelection, setPendingNodeSelection] = useState(null);
  const [catalogFormError, setCatalogFormError] = useState("");
  const [skillCategories, setSkillCategories] = useState([]);

  const {
    detail,
    selectedNodeId,
    setSelectedNodeId,
    selectedNode,
    metadataForm,
    setMetadataForm,
    nodeForm,
    setNodeForm,
    graphFocusRequest,
    newNodeIds,
    skillSearch,
    setSkillSearch,
    skillResults,
    resourceSearch,
    setResourceSearch,
    resourceResults,
    isLoading,
    showLoadingState,
    isSavingMetadata,
    isSavingNode,
    isSearchingSkills,
    isSearchingResources,
    isMutatingDraft,
    isSavingCatalogItem,
    validationResult,
    setValidationResult,
    selectedNodeChildCount,
    error,
    workspaceMode,
    setWorkspaceMode,
    allNodes,
    missingMappingCount,
    versionOptions,
    hasMetadataChanges,
    hasNodeDetailsChanges,
    saveMetadata,
    saveNode,
    cloneDraft,
    createPatchDraft,
    createMinorDraft,
    validateDraft,
    submitDraftForReview,
    createNode,
    moveNode,
    updateGroupRule,
    updateNodeRequirement,
    deleteNode,
    loadSkillSuggestions,
    loadResourceSuggestions,
    searchSkills,
    searchResources,
    addSkill,
    removeSkill,
    addResource,
    removeResource,
    createAndMapSkill,
    createAndMapResource,
    updateMappedResource,
    handleVersionChange,
    focusNodeFromSearch,
  } = editor;
  const activeVersionId = detail?.roadmapVersionId;
  const reviewChangeLog =
    reviewChangeLogDraft.roadmapVersionId === activeVersionId
      ? reviewChangeLogDraft.value
      : readReviewDraftCache(
          REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog,
          activeVersionId,
        );

  useEffect(() => {
    if (!isCreateSkillOpen) return;

    skillApi
      .getCategories()
      .then(setSkillCategories)
      .catch(() => setSkillCategories([]));
  }, [isCreateSkillOpen]);

  const openCreateSkillFromSearch = (searchText = "") => {
    setCatalogFormError("");
    setSkillCreateInitialName(searchText.trim());
    setIsCreateSkillOpen(true);
  };

  const openCreateResourceFromSearch = (searchText = "") => {
    const trimmedSearch = searchText.trim();
    setCatalogFormError("");
    setResourceCreateInitialTitle(
      trimmedSearch.startsWith("http") ? "" : trimmedSearch,
    );
    setResourceCreateInitialUrl(
      trimmedSearch.startsWith("http") ? trimmedSearch : "",
    );
    setIsCreateResourceOpen(true);
  };

  const closeCatalogForms = () => {
    if (isSavingCatalogItem) return;
    setIsCreateSkillOpen(false);
    setIsCreateResourceOpen(false);
    setEditingResource(null);
    setCatalogFormError("");
  };

  const handleCreateSkill = async (payload) => {
    try {
      setCatalogFormError("");
      await createAndMapSkill(payload);
      closeCatalogForms();
    } catch (actionError) {
      setCatalogFormError(
        getFriendlyApiErrorMessage(actionError, "Unable to create skill."),
      );
    }
  };

  const handleCreateResource = async (payload) => {
    try {
      setCatalogFormError("");
      await createAndMapResource(payload);
      closeCatalogForms();
    } catch (actionError) {
      setCatalogFormError(
        getFriendlyApiErrorMessage(actionError, "Unable to create resource."),
      );
    }
  };

  const handleUpdateResource = async (payload) => {
    const resourceId =
      editingResource?.resourceId || editingResource?.learningResourceId;

    try {
      setCatalogFormError("");
      await updateMappedResource(resourceId, payload);
      closeCatalogForms();
    } catch (actionError) {
      setCatalogFormError(
        getFriendlyApiErrorMessage(actionError, "Unable to update resource."),
      );
    }
  };

  const handleBackToRoadmaps = () => {
    navigate("/content/roadmaps");
  };

  const handleEditorVersionChange = (versionId) => {
    setReviewChangeLogDraft({ roadmapVersionId: "", value: "" });
    setValidationResult(null);
    setIsValidationModalOpen(false);
    handleVersionChange(versionId);
  };

  const applyNodeSelection = (selection) => {
    if (!selection?.roadmapNodeId) return;

    if (selection.shouldFocus) {
      focusNodeFromSearch(selection.roadmapNodeId);
      return;
    }

    setSelectedNodeId(selection.roadmapNodeId);
  };

  const requestNodeSelection = (roadmapNodeId, options = {}) => {
    if (!roadmapNodeId || roadmapNodeId === selectedNodeId) return;

    const nextSelection = {
      roadmapNodeId,
      shouldFocus: Boolean(options.shouldFocus),
    };

    if (hasNodeDetailsChanges) {
      setPendingNodeSelection(nextSelection);
      return;
    }

    applyNodeSelection(nextSelection);
  };

  const handleDiscardAndSwitchNode = () => {
    applyNodeSelection(pendingNodeSelection);
    setPendingNodeSelection(null);
  };

  const handleSaveAndSwitchNode = async () => {
    const didSave = await saveNode();

    if (!didSave) return;

    applyNodeSelection(pendingNodeSelection);
    setPendingNodeSelection(null);
  };

  if (isLoading && !detail) {
    if (!showLoadingState) {
      return (
        <ModulePageShell compact>
          <div className="min-h-[280px]" />
        </ModulePageShell>
      );
    }

    return (
      <ModulePageShell compact>
        <ModuleCard className="flex items-center justify-center gap-2 p-10 text-center text-sm font-bold text-slate-600">
          <Loader2 size={16} className="animate-spin text-[#1F6F5F]" />
          Loading roadmap editor...
        </ModuleCard>
      </ModulePageShell>
    );
  }

  if (error) {
    return (
      <ModulePageShell compact>
        <div className="space-y-4">
          <button
            type="button"
            onClick={handleBackToRoadmaps}
            className="inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to roadmaps
          </button>
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            {error}
          </ModuleCard>
        </div>
      </ModulePageShell>
    );
  }

  if (!detail) {
    return (
      <ModulePageShell compact>
        <ModuleEmptyState title="Roadmap not found">
          The roadmap could not be loaded.
        </ModuleEmptyState>
      </ModulePageShell>
    );
  }

  const status = String(detail.status || "").toLowerCase();
  const isDraft = status === "draft" || status === "changes_requested";
  const releaseType = String(detail.releaseType || "").toLowerCase();
  const isPatchDraft = isDraft && releaseType === "patch";
  const isMinorDraft = isDraft && releaseType === "minor";
  const canEditStructure = isDraft && !isPatchDraft;
  const canAddPhaseNode = canEditStructure && !isMinorDraft;
  const canCreateSkillCatalog = hasPermission(
    user,
    PERMISSIONS.SKILL_CREATE_CATALOG,
  );
  const canCreateResourceCatalog = hasPermission(
    user,
    PERMISSIONS.LEARNING_RESOURCE_CREATE_CATALOG,
  );
  const canUpdateResourceCatalog = hasPermission(
    user,
    PERMISSIONS.LEARNING_RESOURCE_UPDATE_CATALOG,
  );
  const nextMajorVersionLabel = getNextMajorVersionLabel(detail?.versions);
  const latestReviewerFeedback = getLatestReviewEvent(
    detail.reviewEvents,
    "rejected",
  );
  const canSubmitReviewWorkflow = isDraft;

  const openReviewWorkflow = () => {
    if (activeVersionId) {
      const cachedChangeLog = readReviewDraftCache(
        REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog,
        activeVersionId,
      );
      const latestSubmittedChangeLog =
        getLatestReviewEvent(detail.reviewEvents, "submitted")?.message || "";
      const nextChangeLog = cachedChangeLog || latestSubmittedChangeLog;

      setReviewChangeLogDraft({
        roadmapVersionId: activeVersionId,
        value: nextChangeLog,
      });

      if (nextChangeLog) {
        writeReviewDraftCache(
          REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog,
          activeVersionId,
          nextChangeLog,
        );
      }
    }

    setWorkspaceMode("review");
  };

  const handleReviewChangeLogChange = (nextValue) => {
    if (!activeVersionId) return;

    setReviewChangeLogDraft({
      roadmapVersionId: activeVersionId,
      value: nextValue,
    });
    writeReviewDraftCache(
      REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog,
      activeVersionId,
      nextValue,
    );
  };

  const handleRunReviewValidation = async () => {
    await validateDraft();
    setIsValidationModalOpen(true);
  };

  const handleSubmitForReview = async () => {
    const currentErrors = validationResult?.errors || [];
    const isCurrentValidationValid =
      validationResult?.isValid && currentErrors.length === 0;
    let canSubmit = isCurrentValidationValid;

    if (!canSubmit) {
      const result = await validateDraft();
      canSubmit = Boolean(result?.isValid && !(result?.errors || []).length);
    }

    if (!canSubmit) {
      setIsValidationModalOpen(true);
      return;
    }

    const didSubmit = await submitDraftForReview(reviewChangeLog);

    if (didSubmit) {
      clearReviewDraftCache(
        REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog,
        activeVersionId,
      );
      setReviewChangeLogDraft({
        roadmapVersionId: activeVersionId || "",
        value: "",
      });
    }
  };

  return (
    <ModulePageShell compact>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <button
            type="button"
            onClick={handleBackToRoadmaps}
            className="inline-flex cursor-pointer items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to roadmap selection
          </button>

          {isLoading && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-white px-2.5 py-1 text-xs font-bold text-slate-600 shadow-sm">
              <Loader2 size={13} className="animate-spin text-[#1F6F5F]" />
              Updating...
            </span>
          )}
        </div>

        <section className="rounded-xl border border-[#B9D8CC] bg-white shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4 p-5">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <MapIcon size={22} />
              </div>
              <div className="min-w-0">
                <h1 className="truncate text-2xl font-extrabold text-[#18332D]">
                  {detail.title}
                </h1>
                <p className="truncate text-sm font-semibold text-slate-600">
                  {detail.careerRole?.name ||
                    detail.slug ||
                    "Career role not set"}
                </p>
              </div>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-4 border-t border-[#B9D8CC]/70 px-5 py-3 text-xs font-bold text-slate-600">
            <span className="inline-flex items-center gap-2">
              <CheckCircle2 size={14} className="text-[#1F6F5F]" />
              {missingMappingCount} missing mappings
            </span>
            <span className="inline-flex items-center gap-2">
              <CalendarDays size={14} className="text-[#1F6F5F]" />
              Last edited {formatDate(detail.updatedAt || detail.createdAt)}
            </span>
          </div>
        </section>

        <DraftVersionActions
          detail={detail}
          versionOptions={versionOptions}
          onVersionChange={handleEditorVersionChange}
          isBusy={isMutatingDraft}
          onCloneDraft={() => setIsCloneConfirmOpen(true)}
          onCreatePatchDraft={createPatchDraft}
          onCreateMinorDraft={createMinorDraft}
        />

        {status === "changes_requested" && latestReviewerFeedback ? (
          <LatestReviewerFeedbackAlert event={latestReviewerFeedback} />
        ) : null}

        <div className="rounded-xl border border-[#B9D8CC] bg-white p-2 shadow-sm">
          <div className="grid gap-2 sm:grid-cols-3">
            <button
              type="button"
              onClick={() => setWorkspaceMode("nodes")}
              className={[
                "rounded-lg px-4 py-2.5 text-center transition",
                workspaceMode === "nodes"
                  ? "bg-[#1F6F5F] text-white"
                  : "text-[#18332D] hover:bg-[#F7F1E8]",
              ].join(" ")}
            >
              <div className="text-sm font-extrabold">Node editor</div>
            </button>
            <button
              type="button"
              onClick={() => setWorkspaceMode("metadata")}
              className={[
                "rounded-lg px-4 py-2.5 text-center transition",
                workspaceMode === "metadata"
                  ? "bg-[#1F6F5F] text-white"
                  : "text-[#18332D] hover:bg-[#F7F1E8]",
              ].join(" ")}
            >
              <div className="text-sm font-extrabold">Roadmap metadata</div>
            </button>
            <button
              type="button"
              onClick={openReviewWorkflow}
              className={[
                "rounded-lg px-4 py-2.5 text-center transition",
                workspaceMode === "review"
                  ? "bg-[#1F6F5F] text-white"
                  : "text-[#18332D] hover:bg-[#F7F1E8]",
              ].join(" ")}
            >
              <div className="text-sm font-extrabold">Submission</div>
            </button>
          </div>
        </div>

        {workspaceMode === "metadata" ? (
          <MetadataEditor
            detail={detail}
            form={metadataForm}
            setForm={setMetadataForm}
            isSaving={isSavingMetadata}
            isDirty={hasMetadataChanges}
            onSave={saveMetadata}
            isEditable={isDraft}
          />
        ) : workspaceMode === "review" ? (
          <ReviewWorkflowPanel
            detail={detail}
            status={status}
            releaseType={releaseType}
            changeLog={reviewChangeLog}
            validationResult={validationResult}
            reviewEvents={detail.reviewEvents || []}
            isSubmitting={isMutatingDraft}
            canSubmit={canSubmitReviewWorkflow}
            onChangeLogChange={handleReviewChangeLogChange}
            onValidate={handleRunReviewValidation}
            onSubmitForReview={handleSubmitForReview}
            isValidationModalOpen={isValidationModalOpen}
            onCloseValidationModal={() => setIsValidationModalOpen(false)}
          />
        ) : (
          <ModuleCard className="overflow-visible">
            <div className="border-b border-[#B9D8CC]/70 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
                    <Layers3 size={16} />
                  </div>
                  <h2 className="text-base font-extrabold text-[#18332D]">
                    Node editor
                  </h2>
                  <button
                    type="button"
                    onClick={() => setIsGuideOpen(true)}
                    className="grid h-8 w-8 place-items-center rounded-full text-[#1F6F5F] transition hover:bg-[#F7F1E8] hover:text-[#18332D]"
                    aria-label="Open roadmap editor guide"
                    title="Guide"
                  >
                    <HelpCircle size={18} />
                  </button>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  {canAddPhaseNode && (
                    <ModuleButton
                      size="xs"
                      variant="secondary"
                      className="border-[#028C7C] bg-[#03A791]/12 text-[#026B60] hover:border-[#026B60] hover:bg-[#03A791]/20"
                      onClick={() => {
                        setCreateNodeMode("phase");
                        setIsCreateNodeOpen(true);
                      }}
                      disabled={isMutatingDraft}
                    >
                      <Plus size={14} /> Add Phase Node
                    </ModuleButton>
                  )}
                  <NodeSearchCombobox
                    nodes={allNodes}
                    onSelect={(roadmapNodeId) =>
                      requestNodeSelection(roadmapNodeId, { shouldFocus: true })
                    }
                  />
                </div>
              </div>
            </div>

            <div className="grid gap-4 p-4 xl:grid-cols-[minmax(340px,0.72fr)_minmax(560px,1.15fr)]">
              <div className="min-w-0">
                <RoadmapGraphCanvas
                  detail={detail}
                  nodes={allNodes}
                  selectedNodeId={selectedNodeId}
                  focusNodeRequest={graphFocusRequest}
                  newNodeIds={newNodeIds}
                  onSelect={(roadmapNodeId) =>
                    requestNodeSelection(roadmapNodeId)
                  }
                />
              </div>

              <NodeDetailsPanel
                selectedNode={selectedNode}
                allNodes={allNodes}
                nodeForm={nodeForm}
                setNodeForm={setNodeForm}
                isSavingNode={isSavingNode}
                isDirty={hasNodeDetailsChanges}
                onSaveNode={saveNode}
                skillSearch={skillSearch}
                setSkillSearch={setSkillSearch}
                skillResults={skillResults}
                isSearchingSkills={isSearchingSkills}
                onSearchSkills={searchSkills}
                onOpenSkillSearch={loadSkillSuggestions}
                onAddSkill={addSkill}
                onRemoveSkill={removeSkill}
                onCreateSkillFromSearch={openCreateSkillFromSearch}
                canCreateSkills={canCreateSkillCatalog}
                resourceSearch={resourceSearch}
                setResourceSearch={setResourceSearch}
                resourceResults={resourceResults}
                isSearchingResources={isSearchingResources}
                onSearchResources={searchResources}
                onOpenResourceSearch={loadResourceSuggestions}
                onAddResource={addResource}
                onRemoveResource={removeResource}
                onCreateResourceFromSearch={openCreateResourceFromSearch}
                canCreateResources={canCreateResourceCatalog}
                canUpdateResources={canUpdateResourceCatalog}
                onEditResource={(resource) => {
                  setCatalogFormError("");
                  setEditingResource(resource);
                }}
                isDraft={isDraft}
                isPatchDraft={isPatchDraft}
                isMinorDraft={isMinorDraft}
                canEditStructure={canEditStructure}
                isMutatingDraft={isMutatingDraft}
                onMoveNode={moveNode}
                onUpdateGroupRule={updateGroupRule}
                onUpdateNodeRequirement={updateNodeRequirement}
                onDeleteNode={deleteNode}
                childNodeCount={selectedNodeChildCount}
                onOpenCreateChild={() => {
                  setCreateNodeMode("child");
                  setIsCreateNodeOpen(true);
                }}
              />
            </div>
          </ModuleCard>
        )}
      </div>

      <ConfirmActionDialog
        isOpen={isCloneConfirmOpen}
        tone="success"
        title="Create major draft"
        description={`Create ${nextMajorVersionLabel} from the current version.`}
        confirmLabel="Create draft"
        cancelLabel="Cancel"
        isConfirming={isMutatingDraft}
        onCancel={() => setIsCloneConfirmOpen(false)}
        onConfirm={async () => {
          await cloneDraft();
          setIsCloneConfirmOpen(false);
        }}
      />

      <UnsavedNodeChangesDialog
        isOpen={Boolean(pendingNodeSelection)}
        isSaving={isSavingNode}
        onCancel={() => setPendingNodeSelection(null)}
        onDiscard={handleDiscardAndSwitchNode}
        onSave={handleSaveAndSwitchNode}
      />

      <NodeCreateModal
        isOpen={isCreateNodeOpen}
        nodes={allNodes}
        selectedNode={selectedNode}
        createMode={createNodeMode}
        isSaving={isMutatingDraft}
        onClose={() => setIsCreateNodeOpen(false)}
        isMinorDraft={isMinorDraft}
        onCreate={createNode}
      />

      <NodeEditorGuideModal
        isOpen={isGuideOpen}
        onClose={() => setIsGuideOpen(false)}
      />

      <SkillFormModal
        isOpen={isCreateSkillOpen}
        initialName={skillCreateInitialName}
        categories={skillCategories}
        isSaving={isSavingCatalogItem}
        error={catalogFormError}
        onClose={closeCatalogForms}
        onSubmit={handleCreateSkill}
      />

      <LearningResourceFormModal
        isOpen={isCreateResourceOpen}
        initialTitle={resourceCreateInitialTitle}
        initialUrl={resourceCreateInitialUrl}
        isSaving={isSavingCatalogItem}
        error={catalogFormError}
        onClose={closeCatalogForms}
        onSubmit={handleCreateResource}
      />

      <LearningResourceFormModal
        isOpen={Boolean(editingResource)}
        resource={editingResource}
        isSaving={isSavingCatalogItem}
        error={catalogFormError}
        onClose={closeCatalogForms}
        onSubmit={handleUpdateResource}
      />

    </ModulePageShell>
  );
}

function UnsavedNodeChangesDialog({
  isOpen,
  isSaving,
  onCancel,
  onDiscard,
  onSave,
}) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm">
      <div className="w-full max-w-md rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-2xl">
        <div className="flex items-start gap-3">
          <div className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-amber-50 text-amber-700">
            <AlertCircle size={20} />
          </div>
          <div className="min-w-0">
            <h2 className="text-base font-extrabold text-[#18332D]">
              Unsaved node changes
            </h2>
            <p className="mt-1 text-sm font-semibold leading-6 text-slate-600">
              Save or discard the current node edits before switching nodes.
            </p>
          </div>
        </div>

        <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <ModuleButton
            variant="secondary"
            onClick={onCancel}
            disabled={isSaving}
          >
            Cancel
          </ModuleButton>
          <ModuleButton
            variant="danger"
            onClick={onDiscard}
            disabled={isSaving}
          >
            Discard
          </ModuleButton>
          <ModuleButton onClick={onSave} disabled={isSaving}>
            {isSaving ? <Loader2 size={14} className="animate-spin" /> : null}
            Save changes
          </ModuleButton>
        </div>
      </div>
    </div>
  );
}

function ReviewWorkflowPanel({
  detail,
  status,
  releaseType,
  changeLog,
  validationResult,
  reviewEvents,
  isSubmitting,
  canSubmit,
  onChangeLogChange,
  onValidate,
  onSubmitForReview,
  isValidationModalOpen,
  onCloseValidationModal,
}) {
  const errors = validationResult?.errors || [];
  const isValid = Boolean(validationResult?.isValid && errors.length === 0);
  const hasChangeLog = String(changeLog || "").trim().length > 0;
  const latestSubmittedChangeLog = getLatestReviewEvent(reviewEvents, "submitted");
  const isPendingReview = status === "pending_review";
  const isReleased = status === "published" || status === "archived";
  const submitLabel =
    status === "changes_requested" ? "Resubmit for review" : "Submit for review";

  return (
    <ModuleCard className="overflow-hidden p-0">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
        <div className="flex items-center gap-2">
          <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
            <MessageSquare size={16} />
          </div>
          <h2 className="text-base font-extrabold text-[#18332D]">
            Submission
          </h2>
        </div>
      </div>

      <div className="grid gap-4 p-4 xl:grid-cols-[minmax(0,1fr)_minmax(360px,0.72fr)] xl:items-start">
        <div className="flex min-h-[560px] flex-col gap-4 xl:h-[560px] xl:min-h-0">
          {canSubmit ? (
            <section className="rounded-xl border border-[#B9D8CC]/80 bg-[#F4FBF8] p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h3 className="text-sm font-extrabold text-[#18332D]">
                  Checks
                </h3>
                <ModuleButton
                  variant="secondary"
                  onClick={onValidate}
                  disabled={isSubmitting}
                >
                  {isSubmitting ? (
                    <Loader2 size={14} className="animate-spin" />
                  ) : (
                    <CheckCircle2 size={14} />
                  )}
                  Run check
                </ModuleButton>
              </div>

              <ValidationSummary result={validationResult} />
            </section>
          ) : null}

          {canSubmit ? (
            <section className="flex min-h-0 flex-1 flex-col rounded-xl border border-[#B9D8CC]/80 bg-white p-4">
              <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
                <h3 className="text-sm font-extrabold text-[#18332D]">
                  Changelog
                </h3>
                {hasChangeLog ? (
                  <span className="rounded-full border border-[#B9D8CC] bg-[#F4FBF8] px-2.5 py-1 text-[11px] font-bold text-[#1F6F5F]">
                    Autosaved
                  </span>
                ) : null}
              </div>

              <textarea
                className={`${inputClass} min-h-[220px] flex-1 resize-none`}
                value={changeLog}
                onChange={(event) => onChangeLogChange?.(event.target.value)}
                placeholder="Summarize the final roadmap changes learners should understand in this version."
                maxLength={4000}
              />
              <p className="mt-2 text-xs font-semibold leading-5 text-slate-600">
                This is the learner-facing changelog for the version. Keep reviewer discussion in feedback, not here.
              </p>

              <div className="mt-4 flex justify-end">
                <ModuleButton
                  onClick={onSubmitForReview}
                  disabled={!isValid || !hasChangeLog || isSubmitting}
                >
                  {isSubmitting ? (
                    <Loader2 size={14} className="animate-spin" />
                  ) : (
                    <Send size={14} />
                  )}
                  {submitLabel}
                </ModuleButton>
              </div>
            </section>
          ) : (
            <ReadOnlyReviewSummary
              detail={detail}
              releaseType={releaseType}
              latestSubmittedChangeLog={latestSubmittedChangeLog}
              isPendingReview={isPendingReview}
              isReleased={isReleased}
            />
          )}
        </div>

        <ReviewHistoryPanel events={reviewEvents} />
      </div>

      <ValidationDetailsModal
        isOpen={isValidationModalOpen}
        result={validationResult}
        onClose={onCloseValidationModal}
      />
    </ModuleCard>
  );
}

function ValidationSummary({ result }) {
  if (!result) {
    return (
      <div className="mt-4 rounded-lg border border-dashed border-[#B9D8CC] bg-white px-3 py-2 text-sm font-semibold text-slate-600">
        Not checked yet.
      </div>
    );
  }

  const errors = result.errors || [];
  const warnings = result.warnings || [];
  const isValid = result.isValid && errors.length === 0;

  if (isValid && warnings.length === 0) {
    return (
      <div className="mt-4 flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-sm font-bold text-[#1F6F5F]">
        <CheckCircle2 size={16} />
        Ready to submit.
      </div>
    );
  }

  return (
    <div className="mt-4 flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-bold text-amber-800">
      <AlertCircle size={16} />
      {errors.length > 0
        ? `${errors.length} error${errors.length === 1 ? "" : "s"}`
        : `${warnings.length} warning${warnings.length === 1 ? "" : "s"}`}
    </div>
  );
}

function ValidationDetailsModal({ isOpen, result, onClose }) {
  if (!isOpen) return null;

  const errors = result?.errors || [];
  const warnings = result?.warnings || [];
  const isValid = Boolean(result?.isValid && errors.length === 0);

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-slate-950/40 p-4"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="flex max-h-[82vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl border border-[#B9D8CC] bg-white shadow-xl"
        onClick={(event) => event.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-labelledby="validation-dialog-title"
      >
        <div className="flex items-center justify-between gap-3 border-b border-[#B9D8CC]/70 px-5 py-4">
          <h2
            id="validation-dialog-title"
            className="text-base font-extrabold text-[#18332D]"
          >
            Checks
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="grid h-8 w-8 place-items-center rounded-full text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D]"
            aria-label="Close checks"
          >
            <X size={18} />
          </button>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-5">
          {!result ? (
            <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-white px-3 py-2 text-sm font-semibold text-slate-600">
              Not checked yet.
            </div>
          ) : isValid && warnings.length === 0 ? (
            <div className="flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-[#F4FBF8] px-3 py-2 text-sm font-bold text-[#1F6F5F]">
              <CheckCircle2 size={16} />
              Ready to submit.
            </div>
          ) : errors.length > 0 || warnings.length > 0 ? (
            <div className="space-y-4">
              <ValidationModalList title="Errors" items={errors} tone="error" />
              <ValidationModalList
                title="Warnings"
                items={warnings}
                tone="warning"
              />
            </div>
          ) : (
            <div className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm font-bold text-amber-800">
              Checks did not pass. No detailed message was returned.
            </div>
          )}
        </div>

        <div className="flex justify-end border-t border-[#B9D8CC]/70 px-5 py-4">
          <ModuleButton variant="secondary" onClick={onClose}>
            Close
          </ModuleButton>
        </div>
      </div>
    </div>
  );
}

function ValidationModalList({ title, items, tone }) {
  if (!items?.length) return null;

  const toneClass =
    tone === "error"
      ? "border-red-200 bg-red-50 text-red-700"
      : "border-amber-200 bg-amber-50 text-amber-800";

  return (
    <section className="space-y-2">
      <h3 className="text-sm font-extrabold text-[#18332D]">{title}</h3>
      <div className="space-y-2">
        {items.map((item, index) => (
          <div
            key={`${item.code}-${item.roadmapNodeId || index}`}
            className={`rounded-lg border px-3 py-2 text-sm font-semibold ${toneClass}`}
          >
            <div>{item.message}</div>
            {item.nodeTitle ? (
              <div className="mt-1 text-xs opacity-75">{item.nodeTitle}</div>
            ) : null}
          </div>
        ))}
      </div>
    </section>
  );
}

function ReadOnlyReviewSummary({
  detail,
  releaseType,
  latestSubmittedChangeLog,
  isPendingReview,
  isReleased,
}) {
  return (
    <section className="flex min-h-0 flex-1 flex-col rounded-xl border border-[#B9D8CC]/80 bg-[#F4FBF8] p-4">
      <div className="mb-3 flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-white text-[#1F6F5F]">
            {isPendingReview ? <Clock3 size={18} /> : <CheckCircle2 size={18} />}
          </div>
          <h3 className="truncate text-sm font-extrabold text-[#18332D]">
            {isPendingReview
              ? "Awaiting review"
              : isReleased
                ? "Released version"
                : "Review summary"}
          </h3>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <span className="rounded-full border border-[#B9D8CC] bg-white px-2.5 py-1 text-[11px] font-black text-[#18332D]">
            {formatVersionLabel(detail)}
          </span>
          <span className="rounded-full border border-[#B9D8CC] bg-white px-2.5 py-1 text-[11px] font-black capitalize text-[#1F6F5F]">
            {releaseType || "Roadmap"}
          </span>
        </div>
      </div>

      {latestSubmittedChangeLog ? (
        <div className="min-h-0 flex-1 overflow-y-auto rounded-lg border border-[#B9D8CC]/70 bg-white p-3">
          <div className="mb-1 text-[11px] font-bold uppercase tracking-wide text-slate-500">
            Latest changelog
          </div>
          <p className="whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
            {latestSubmittedChangeLog.message || "No changelog recorded."}
          </p>
        </div>
      ) : (
        <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-white px-3 py-2 text-sm font-semibold text-slate-600">
          No changelog recorded.
        </div>
      )}
    </section>
  );
}

function LatestReviewerFeedbackAlert({ event }) {
  return (
    <section className="rounded-xl border border-amber-200 bg-amber-50 p-4 shadow-sm">
      <div className="flex items-start gap-3">
        <div className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-white text-amber-700">
          <AlertCircle size={17} />
        </div>
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-sm font-extrabold text-amber-900">
              Latest reviewer feedback
            </h2>
            <span className="text-[11px] font-bold text-amber-700">
              {formatDate(event.createdAt)}
            </span>
          </div>
          <p className="mt-1 text-xs font-bold text-amber-800">
            Reviewer: {formatReviewActor(event)}
          </p>
          <p className="mt-1 whitespace-pre-wrap text-sm font-semibold leading-6 text-amber-900">
            {event.message || "No feedback message recorded."}
          </p>
        </div>
      </div>
    </section>
  );
}

function ReviewHistoryPanel({ events }) {
  return (
    <section className="flex h-[560px] min-h-0 flex-col overflow-hidden rounded-xl border border-[#B9D8CC] bg-white shadow-sm">
      <div className="flex items-center gap-2 border-b border-[#B9D8CC]/70 p-4">
        <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
          <MessageSquare size={16} />
        </div>
        <h2 className="text-sm font-extrabold text-[#18332D]">
          Review history
        </h2>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-4">
        {!events?.length ? (
          <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-white px-3 py-2 text-sm font-semibold text-slate-600">
            No review events recorded yet.
          </div>
        ) : (
          <div className="space-y-3">
            {events.map((event) => (
              <article
                key={
                  event.roadmapVersionReviewEventId ||
                  `${event.eventType}-${event.createdAt}`
                }
                className="grid gap-3 rounded-lg border border-[#B9D8CC]/70 bg-[#F4FBF8] p-3 md:grid-cols-[132px_minmax(0,1fr)]"
              >
                <div className="space-y-1">
                  <span
                    className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-extrabold uppercase tracking-wide ${getReviewEventTone(event.eventType)}`}
                  >
                    {getReviewEventLabel(event.eventType)}
                  </span>
                  <div className="text-[11px] font-bold text-slate-500">
                    {formatDate(event.createdAt)}
                  </div>
                  <div className="text-[11px] font-semibold leading-4 text-slate-600">
                    {getReviewEventActorLabel(event)}: {formatReviewActor(event)}
                  </div>
                </div>
                <div className="min-w-0">
                  <p className="whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
                    {event.message || "No message recorded."}
                  </p>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

function getReviewEventLabel(eventType) {
  const normalized = String(eventType || "").toLowerCase();
  if (normalized === "submitted") return "Changelog";
  if (normalized === "rejected") return "Rejected";
  if (normalized === "approved") return "Approved";
  return eventType || "Review note";
}

function getReviewEventTone(eventType) {
  const normalized = String(eventType || "").toLowerCase();
  if (normalized === "approved")
    return "border-emerald-200 bg-emerald-50 text-emerald-700";
  if (normalized === "rejected")
    return "border-amber-200 bg-amber-50 text-amber-700";
  return "border-[#B9D8CC] bg-white text-[#1F6F5F]";
}

function getReviewEventActorLabel(event) {
  const normalized = String(event?.eventType || "").toLowerCase();
  if (normalized === "submitted") return "Author";
  if (normalized === "approved" || normalized === "rejected") return "Reviewer";
  return "Actor";
}

function formatReviewActor(event) {
  const displayName = String(event?.actorDisplayName || "").trim();
  const username = String(event?.actorUsername || "").trim();
  const userId = String(event?.actorUserId || "").trim();
  const primary = displayName || username || "Unknown user";
  const details = [];

  if (username && username.toLowerCase() !== primary.toLowerCase()) {
    details.push(`@${username}`);
  }

  if (userId) {
    details.push(`ID ${userId.slice(0, 8)}`);
  }

  return details.length > 0 ? `${primary} (${details.join(", ")})` : primary;
}

function getLatestReviewEvent(events = [], eventType) {
  const normalizedType = String(eventType || "").toLowerCase();

  return [...(events || [])]
    .filter(
      (event) => String(event.eventType || "").toLowerCase() === normalizedType,
    )
    .sort((first, second) => {
      const firstTime = Date.parse(first.createdAt || "") || 0;
      const secondTime = Date.parse(second.createdAt || "") || 0;
      return secondTime - firstTime;
    })[0] || null;
}
