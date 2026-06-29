import { useEffect, useState } from "react";
import { ArrowDown, ArrowUp, BookOpenText, Plus, Save, Tag, Trash2 } from "lucide-react";
import ConfirmActionDialog from "../../learningModules/components/ConfirmActionDialog";

import { DirtyStateBadge } from "../../learningModuleEditor/EditorControls";
import {
  inputClass,
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModuleField,
  numberInputClass,
} from "../../learningModules/components/learningModuleUi";
import { difficultyOptions } from "../roadmapEditorConstants";
import { getNodeTone } from "../roadmapEditorUtils";
import {
  canEditLearningFields,
  canCreateChildNodes,
  canEditMappings,
  getNodeKindLabel,
  getNodeLabel,
  getResourceId,
  getResourceTitle,
  getSkillId,
  getSkillName,
} from "../nodeRules";
import MappingList from "./MappingList";
import MappingSearchModal from "./MappingSearchModal";
import NodeContentFields from "./NodeContentFields";

export default function NodeDetailsPanel({
  selectedNode,
  nodeForm,
  setNodeForm,
  isSavingNode,
  isDirty,
  onSaveNode,
  skillSearch,
  setSkillSearch,
  skillResults,
  isSearchingSkills,
  onSearchSkills,
  onOpenSkillSearch,
  onAddSkill,
  onRemoveSkill,
  resourceSearch,
  setResourceSearch,
  resourceResults,
  isSearchingResources,
  onSearchResources,
  onOpenResourceSearch,
  onAddResource,
  onRemoveResource,
  isDraft = false,
  isMutatingDraft = false,
  onMoveNode,
  onDeleteNode,
  childNodeCount = 0,
  onOpenCreateChild,
}) {
  const [panelMode, setPanelMode] = useState("details");
  const [mappingModal, setMappingModal] = useState(null);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);

  useEffect(() => {
    setPanelMode("details");
    setMappingModal(null);
    setIsDeleteConfirmOpen(false);
  }, [selectedNode?.roadmapNodeId]);

  if (!selectedNode) {
    return (
      <ModuleCard className="h-[640px] p-6">
        <ModuleEmptyState title="Select a node" />
      </ModuleCard>
    );
  }

  const mappedSkillIds = new Set((selectedNode.skills || []).map(getSkillId));
  const mappedResourceIds = new Set((selectedNode.resources || []).map(getResourceId));
  const availableSkillResults = skillResults.filter((skill) => !mappedSkillIds.has(getSkillId(skill)));
  const availableResourceResults = resourceResults.filter((resource) => (
    !mappedResourceIds.has(getResourceId(resource))
  ));
  const canEditLearning = canEditLearningFields(selectedNode);
  const canMapLearning = canEditMappings(selectedNode);
  const canAddChildNode = isDraft && canCreateChildNodes(selectedNode);
  const skillCount = selectedNode.skills?.length || 0;
  const resourceCount = selectedNode.resources?.length || 0;

  const tabButtonClass = (tabKey, disabled = false) => [
    "rounded-lg px-3 py-2 text-xs font-extrabold transition",
    panelMode === tabKey ? "bg-[#1F6F5F] text-white" : "text-slate-600 hover:bg-[#F7F1E8]",
    disabled ? "cursor-not-allowed opacity-45" : "",
  ].join(" ");

  return (
    <>
      <ModuleCard className="sticky top-24 flex h-[640px] w-full max-h-[calc(100vh-7.5rem)] flex-col overflow-hidden p-0">
        <div className="border-b border-[#B9D8CC]/70 p-4">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <h3 className="truncate text-base font-extrabold text-[#18332D]">{getNodeLabel(selectedNode)}</h3>
                <DirtyStateBadge isDirty={isDirty} />
              </div>
              <div className="mt-1 flex flex-wrap items-center gap-2">
                <ModuleBadge tone={getNodeTone(selectedNode.nodeType)}>{selectedNode.nodeType}</ModuleBadge>
                <span className="rounded-full bg-[#F7F1E8] px-2.5 py-1 text-[11px] font-bold text-slate-600">
                  {getNodeKindLabel(selectedNode)}
                </span>
              </div>
            </div>
            <div className="flex shrink-0 items-center gap-2">
              {isDraft && (
                <ModuleButton
                  variant="primary"
                  size="xs"
                  className="bg-[#2FA084] text-white hover:bg-[#1F6F5F]"
                  onClick={onOpenCreateChild}
                  disabled={isMutatingDraft || !canAddChildNode}
                >
                  <Plus size={14} /> Add node
                </ModuleButton>
              )}
              <ModuleButton onClick={onSaveNode} disabled={isSavingNode || !isDirty}>
                <Save size={14} /> {isSavingNode ? "Saving" : "Save"}
              </ModuleButton>
            </div>
          </div>

          {isDraft && (
            <div className="mt-3 flex flex-wrap items-center gap-2 rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-2">
              <ModuleButton size="xs" variant="secondary" onClick={() => onMoveNode?.("up")} disabled={isMutatingDraft}>
                <ArrowUp size={13} /> Move up
              </ModuleButton>
              <ModuleButton size="xs" variant="secondary" onClick={() => onMoveNode?.("down")} disabled={isMutatingDraft}>
                <ArrowDown size={13} /> Move down
              </ModuleButton>
              <ModuleButton size="xs" variant="danger" className="hover:border-rose-500 hover:bg-rose-100 hover:text-rose-800" onClick={() => setIsDeleteConfirmOpen(true)} disabled={isMutatingDraft}>
                <Trash2 size={13} /> Delete
              </ModuleButton>
            </div>
          )}
        </div>

        <div className="border-b border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-2">
          <div className="grid grid-cols-4 gap-2 rounded-xl bg-white p-1 ring-1 ring-[#B9D8CC]/70">
            <button
              type="button"
              onClick={() => setPanelMode("details")}
              className={tabButtonClass("details")}
            >
              Details
            </button>
            <button
              type="button"
              onClick={() => setPanelMode("content")}
              className={tabButtonClass("content")}
            >
              Content
            </button>
            <button
              type="button"
              onClick={() => setPanelMode("skills")}
              disabled={!canMapLearning}
              className={tabButtonClass("skills", !canMapLearning)}
            >
              Skills
            </button>
            <button
              type="button"
              onClick={() => setPanelMode("resources")}
              disabled={!canMapLearning}
              className={tabButtonClass("resources", !canMapLearning)}
            >
              Resources
            </button>
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-4">
          {panelMode === "details" && (
            <div className="space-y-4">
              <section className="space-y-3">
                <ModuleField label="Title">
                  <input
                    type="text"
                    value={nodeForm.title}
                    onChange={(event) => setNodeForm((current) => ({ ...current, title: event.target.value }))}
                    className={inputClass}
                  />
                </ModuleField>

                <ModuleField label="Description">
                  <textarea
                    value={nodeForm.description}
                    onChange={(event) => setNodeForm((current) => ({ ...current, description: event.target.value }))}
                    rows={5}
                    className={`${inputClass} min-h-32 resize-y`}
                  />
                </ModuleField>
              </section>

              {canEditLearning ? (
                <section className="space-y-3">
                  <div className="grid gap-3 sm:grid-cols-2">
                    <ModuleField label="Estimated hours">
                      <input
                        type="number"
                        min="0"
                        value={nodeForm.estimatedHours}
                        onChange={(event) => setNodeForm((current) => ({ ...current, estimatedHours: event.target.value }))}
                        className={numberInputClass}
                      />
                    </ModuleField>
                    <ModuleField label="Difficulty">
                      <select
                        value={nodeForm.difficultyLevel}
                        onChange={(event) => setNodeForm((current) => ({ ...current, difficultyLevel: event.target.value }))}
                        className={inputClass}
                      >
                        {difficultyOptions.map((option) => (
                          <option key={option.value || "empty"} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </ModuleField>
                  </div>
                </section>
              ) : null}

            </div>
          )}

          {panelMode === "content" && (
            <NodeContentFields
              selectedNode={selectedNode}
              nodeForm={nodeForm}
              setNodeForm={setNodeForm}
            />
          )}

          {panelMode === "skills" && canMapLearning && (
            <section className="space-y-4">
              <div className="flex items-center justify-between gap-3">
                <div className="text-xs font-extrabold uppercase tracking-wide text-slate-600">
                  {skillCount} mapped skills
                </div>
                <ModuleButton size="xs" variant="secondary" onClick={() => {
                  setMappingModal("skills");
                  onOpenSkillSearch();
                }}>
                  <Plus size={14} /> Add skill
                </ModuleButton>
              </div>
              <MappingList
                title="Skills"
                icon={Tag}
                items={selectedNode.skills || []}
                getId={getSkillId}
                getLabel={getSkillName}
                emptyText="No skills mapped yet."
                onRemove={onRemoveSkill}
              />
            </section>
          )}

          {panelMode === "resources" && canMapLearning && (
            <section className="space-y-4">
              <div className="flex items-center justify-between gap-3">
                <div className="text-xs font-extrabold uppercase tracking-wide text-slate-600">
                  {resourceCount} mapped resources
                </div>
                <ModuleButton size="xs" variant="secondary" onClick={() => {
                  setMappingModal("resources");
                  onOpenResourceSearch();
                }}>
                  <Plus size={14} /> Add resource
                </ModuleButton>
              </div>
              <MappingList
                title="Resources"
                icon={BookOpenText}
                items={selectedNode.resources || []}
                getId={getResourceId}
                getLabel={getResourceTitle}
                emptyText="No resources mapped yet."
                onRemove={onRemoveResource}
              />
            </section>
          )}
        </div>
      </ModuleCard>

      <ConfirmActionDialog
        isOpen={isDeleteConfirmOpen}
        title="Delete node"
        description={childNodeCount > 0
          ? `This permanently deletes this node and ${childNodeCount} child ${childNodeCount === 1 ? "node" : "nodes"}. This action cannot be undone.`
          : "This permanently deletes the selected draft node. This action cannot be undone."}
        confirmLabel="Delete permanently"
        cancelLabel="Keep node"
        isConfirming={isMutatingDraft}
        onCancel={() => setIsDeleteConfirmOpen(false)}
        onConfirm={async () => {
          await onDeleteNode?.();
          setIsDeleteConfirmOpen(false);
        }}
      />

      <MappingSearchModal
        isOpen={mappingModal === "skills"}
        title="Add skill"
        searchValue={skillSearch}
        setSearchValue={setSkillSearch}
        isSearching={isSearchingSkills}
        onSearch={onSearchSkills}
        items={availableSkillResults}
        getId={getSkillId}
        getTitle={getSkillName}
        onChoose={onAddSkill}
        onClose={() => setMappingModal(null)}
        emptyTitle="No skills found"
        emptyBody="Search for another skill."
      />

      <MappingSearchModal
        isOpen={mappingModal === "resources"}
        title="Add resource"
        searchValue={resourceSearch}
        setSearchValue={setResourceSearch}
        isSearching={isSearchingResources}
        onSearch={onSearchResources}
        items={availableResourceResults}
        getId={getResourceId}
        getTitle={getResourceTitle}
        getSubtitle={(resource) => resource.provider || resource.resourceType || "Resource"}
        onChoose={onAddResource}
        onClose={() => setMappingModal(null)}
        emptyTitle="No resources found"
        emptyBody="Search for another resource."
      />
    </>
  );
}
