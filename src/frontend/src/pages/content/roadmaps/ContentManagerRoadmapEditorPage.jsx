import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, CalendarDays, CheckCircle2, HelpCircle, Layers3, Loader2, Map as MapIcon, MessageSquare, Plus } from "lucide-react";

import ConfirmActionDialog from "../../../features/learningModules/components/ConfirmActionDialog";
import {
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  ModuleButton,
} from "../../../features/learningModules/components/learningModuleUi";
import DraftValidationModal from "../../../features/roadmapEditor/components/DraftValidationModal";
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
import { formatDate, getNextMajorVersionLabel } from "../../../features/roadmapEditor/roadmapEditorUtils";

export default function ContentManagerRoadmapEditorPage() {
  const navigate = useNavigate();
  const { roadmapId } = useParams();
  const editor = useContentRoadmapEditor(roadmapId);
  const [isCreateNodeOpen, setIsCreateNodeOpen] = useState(false);
  const [createNodeMode, setCreateNodeMode] = useState("child");
  const [isValidationOpen, setIsValidationOpen] = useState(false);
  const [reviewChangeLogDraft, setReviewChangeLogDraft] = useState({
    roadmapVersionId: "",
    value: "",
  });
  const [isCloneConfirmOpen, setIsCloneConfirmOpen] = useState(false);
  const [isGuideOpen, setIsGuideOpen] = useState(false);

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
    handleVersionChange,
    focusNodeFromSearch,
  } = editor;
  const activeVersionId = detail?.roadmapVersionId;
  const reviewChangeLog = reviewChangeLogDraft.roadmapVersionId === activeVersionId
    ? reviewChangeLogDraft.value
    : "";

  const handleBackToRoadmaps = () => {
    navigate("/content/roadmaps");
  };

  const handleEditorVersionChange = (versionId) => {
    handleVersionChange(versionId);
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
  const nextMajorVersionLabel = getNextMajorVersionLabel(detail?.versions);

  const openValidation = async () => {
    if (activeVersionId && reviewChangeLogDraft.roadmapVersionId !== activeVersionId) {
      setReviewChangeLogDraft({
        roadmapVersionId: activeVersionId,
        value: readReviewDraftCache(
          REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog,
          activeVersionId,
        ),
      });
    }

    await validateDraft();
    setIsValidationOpen(true);
  };

  const handleReviewChangeLogChange = (nextValue) => {
    setReviewChangeLogDraft({
      roadmapVersionId: activeVersionId || "",
      value: nextValue,
    });
    writeReviewDraftCache(
      REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog,
      activeVersionId,
      nextValue,
    );
  };

  const handleSubmitForReview = async () => {
    const didSubmit = await submitDraftForReview(reviewChangeLog);

    if (didSubmit) {
      clearReviewDraftCache(REVIEW_DRAFT_CACHE_TYPES.contentManagerChangelog, activeVersionId);
      setReviewChangeLogDraft({
        roadmapVersionId: activeVersionId || "",
        value: "",
      });
      setIsValidationOpen(false);
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
                <h1 className="truncate text-2xl font-extrabold text-[#18332D]">{detail.title}</h1>
                <p className="truncate text-sm font-semibold text-slate-600">
                  {detail.careerRole?.name || detail.slug || "Career role not set"}
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
          onSubmitDraftForReview={openValidation}
        />

        <ReviewHistoryPanel events={detail.reviewEvents || []} />

        <div className="rounded-xl border border-[#B9D8CC] bg-white p-2 shadow-sm">
          <div className="grid gap-2 sm:grid-cols-2">
            <button
              type="button"
              onClick={() => setWorkspaceMode("nodes")}
              className={[
                "rounded-lg px-4 py-2.5 text-center transition",
                workspaceMode === "nodes" ? "bg-[#1F6F5F] text-white" : "text-[#18332D] hover:bg-[#F7F1E8]",
              ].join(" ")}
            >
              <div className="text-sm font-extrabold">Node editor</div>
            </button>
            <button
              type="button"
              onClick={() => setWorkspaceMode("metadata")}
              className={[
                "rounded-lg px-4 py-2.5 text-center transition",
                workspaceMode === "metadata" ? "bg-[#1F6F5F] text-white" : "text-[#18332D] hover:bg-[#F7F1E8]",
              ].join(" ")}
            >
              <div className="text-sm font-extrabold">Roadmap metadata</div>
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
        ) : (
          <ModuleCard className="overflow-visible">
            <div className="border-b border-[#B9D8CC]/70 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
                    <Layers3 size={16} />
                  </div>
                  <h2 className="text-base font-extrabold text-[#18332D]">Node editor</h2>
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
                  <NodeSearchCombobox nodes={allNodes} onSelect={focusNodeFromSearch} />
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
                  onSelect={setSelectedNodeId}
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
                resourceSearch={resourceSearch}
                setResourceSearch={setResourceSearch}
                resourceResults={resourceResults}
                isSearchingResources={isSearchingResources}
                onSearchResources={searchResources}
                onOpenResourceSearch={loadResourceSuggestions}
                onAddResource={addResource}
                onRemoveResource={removeResource}
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

      <NodeEditorGuideModal isOpen={isGuideOpen} onClose={() => setIsGuideOpen(false)} />

      <DraftValidationModal
        isOpen={isValidationOpen}
        result={validationResult}
        changeLog={reviewChangeLog}
        onChangeLogChange={handleReviewChangeLogChange}
        cacheStatus={reviewChangeLog ? "Autosaved locally." : ""}
        isSubmitting={isMutatingDraft}
        onClose={() => {
          setIsValidationOpen(false);
          setValidationResult(null);
        }}
        onSubmitForReview={handleSubmitForReview}
      />
    </ModulePageShell>
  );
}

function ReviewHistoryPanel({ events }) {
  if (!events?.length) return null;

  return (
    <section className="rounded-xl border border-[#B9D8CC] bg-white shadow-sm">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
        <div className="flex items-center gap-2">
          <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
            <MessageSquare size={16} />
          </div>
          <div>
            <h2 className="text-sm font-extrabold text-[#18332D]">Review history</h2>
            <p className="text-xs font-semibold text-slate-600">Change logs and reviewer feedback for this version.</p>
          </div>
        </div>
      </div>

      <div className="space-y-3 p-4">
        {events.map((event) => (
          <article
            key={event.roadmapVersionReviewEventId || `${event.eventType}-${event.createdAt}`}
            className="grid gap-3 rounded-lg border border-[#B9D8CC]/70 bg-[#F4FBF8] p-3 md:grid-cols-[150px_minmax(0,1fr)]"
          >
            <div className="space-y-1">
              <span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-extrabold uppercase tracking-wide ${getReviewEventTone(event.eventType)}`}>
                {getReviewEventLabel(event.eventType)}
              </span>
              <div className="text-[11px] font-bold text-slate-500">
                {formatDate(event.createdAt)}
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
  if (normalized === "approved") return "border-emerald-200 bg-emerald-50 text-emerald-700";
  if (normalized === "rejected") return "border-amber-200 bg-amber-50 text-amber-700";
  return "border-[#B9D8CC] bg-white text-[#1F6F5F]";
}
