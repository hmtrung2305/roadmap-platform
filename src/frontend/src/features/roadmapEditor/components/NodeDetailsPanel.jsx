import { useState } from "react";
import { ArrowDown, ArrowUp, BookOpenText, Plus, Save, Tag, Trash2 } from "lucide-react";
import ConfirmActionDialog from "../../learningModules/components/ConfirmActionDialog";
import AppSelect from "../../../components/common/AppSelect";

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
  normalizeNodeType,
} from "../nodeRules";
import MappingList from "./MappingList";
import MappingSearchModal from "./MappingSearchModal";
import NodeContentFields from "./NodeContentFields";


const groupRuleOptions = [
  { value: "complete_all", label: "Complete all required children" },
  { value: "choose_one", label: "Complete one required child" },
  { value: "choose_many", label: "Complete a set number" },
];

function requirementOptionClass(isActive, isDisabled, activeClass) {
  return [
    "rounded-md px-2.5 py-1 text-xs font-extrabold transition",
    isActive ? activeClass : "text-slate-600 hover:bg-white",
    isDisabled && isActive ? "cursor-default" : "",
    isDisabled && !isActive ? "cursor-not-allowed opacity-50 hover:bg-transparent" : "",
  ].join(" ");
}

function RequirementTooltip({ message, align = "right" }) {
  if (!message) return null;

  const alignmentClass = align === "left" ? "left-0" : "right-0";

  return (
    <span className="group/requirement-tooltip relative inline-flex shrink-0">
      <span
        tabIndex={0}
        aria-label={message}
        className="grid h-5 w-5 cursor-help place-items-center rounded-full border border-[#B9D8CC] bg-white text-[11px] font-extrabold text-[#1F6F5F] shadow-sm outline-none transition hover:border-[#1F6F5F] focus-visible:border-[#1F6F5F]"
      >
        ?
      </span>
      <span
        role="tooltip"
        className={`pointer-events-none absolute ${alignmentClass} top-full z-[95] mt-2 hidden w-64 rounded-lg border border-[#B9D8CC] bg-[#18332D] px-3 py-2 text-xs font-semibold leading-5 text-white shadow-xl group-hover/requirement-tooltip:block group-focus-within/requirement-tooltip:block`}
      >
        {message}
      </span>
    </span>
  );
}

function isRequirementEditableNode(node) {
  const nodeType = normalizeNodeType(node);
  return ["topic", "project", "checkpoint", "choice_option"].includes(nodeType);
}

function getNodeById(nodes = [], nodeId = "") {
  if (!nodeId) return null;
  return nodes.find((node) => node.roadmapNodeId === nodeId) || null;
}

function getDirectGroupChildren(nodes = [], parentId = "") {
  if (!parentId) return [];

  return nodes
    .filter((node) => node.parentNodeId === parentId)
    .sort((left, right) => (
      (left.orderIndex ?? 0) - (right.orderIndex ?? 0)
      || String(left.title || "").localeCompare(String(right.title || ""))
    ));
}

function toPositiveInteger(value) {
  const number = Number.parseInt(value, 10);
  return Number.isInteger(number) && number > 0 ? number : null;
}

function clampRequiredCount(value, requiredChildCount) {
  if (requiredChildCount < 1) return "0";

  const parsed = toPositiveInteger(value);
  if (!parsed) return "1";

  return String(Math.min(Math.max(parsed, 1), requiredChildCount));
}

function getRequiredCountDisplayValue(selectionType, formRequiredCount, requiredChildCount) {
  if (requiredChildCount < 1) return "0";
  if (selectionType === "choose_many") return clampRequiredCount(formRequiredCount, requiredChildCount);
  if (selectionType === "choose_one") return "1";
  return String(requiredChildCount);
}

function getRequiredCountOptions(requiredChildCount) {
  if (requiredChildCount < 1) return [{ value: "0", label: "0" }];
  return Array.from({ length: requiredChildCount }, (_, index) => {
    const value = String(index + 1);
    return { value, label: value };
  });
}

function createGroupRuleForm(node = null, nodes = []) {
  const selectionType = node?.selectionType || "complete_all";
  const requiredChildCount = getDirectGroupChildren(nodes, node?.roadmapNodeId)
    .filter((child) => child.isRequired).length;

  return {
    selectionType,
    requiredCount: getRequiredCountDisplayValue(selectionType, node?.requiredCount, requiredChildCount),
  };
}

function getNodeDetailsPanelKey(selectedNode, allNodes = []) {
  const groupChildFingerprint = getDirectGroupChildren(allNodes, selectedNode?.roadmapNodeId)
    .map((child) => [
      child.roadmapNodeId,
      child.isRequired ? "required" : "optional",
      child.orderIndex ?? "",
    ].join(":"))
    .join("|");

  return [
    selectedNode?.roadmapNodeId || "",
    selectedNode?.selectionType || "",
    selectedNode?.requiredCount ?? "",
    groupChildFingerprint,
  ].join("::");
}

function getGroupRuleSummary(selectionType, requiredCount, requiredChildCount, optionalChildCount) {
  if (requiredChildCount === 0) {
    return "Mark at least one child as required.";
  }

  const optionalText = optionalChildCount > 0
    ? ` ${optionalChildCount} optional ${optionalChildCount === 1 ? "child" : "children"} will not block progress.`
    : "";

  if (selectionType === "choose_many") {
    return `Complete ${requiredCount || "?"} of ${requiredChildCount} required children.${optionalText}`;
  }

  if (selectionType === "choose_one") {
    return `Complete 1 of ${requiredChildCount} required children.${optionalText}`;
  }

  return `Complete all ${requiredChildCount} required children.${optionalText}`;
}

export default function NodeDetailsPanel(props) {
  const { selectedNode, allNodes = [] } = props;

  if (!selectedNode) {
    return (
      <ModuleCard className="h-[640px] p-6">
        <ModuleEmptyState title="Select a node" />
      </ModuleCard>
    );
  }

  return (
    <NodeDetailsPanelContent
      key={getNodeDetailsPanelKey(selectedNode, allNodes)}
      {...props}
      allNodes={allNodes}
    />
  );
}

function NodeDetailsPanelContent({
  selectedNode,
  allNodes = [],
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
  onCreateSkillFromSearch,
  canCreateSkills = false,
  resourceSearch,
  setResourceSearch,
  resourceResults,
  isSearchingResources,
  onSearchResources,
  onOpenResourceSearch,
  onAddResource,
  onRemoveResource,
  onCreateResourceFromSearch,
  onEditResource,
  canCreateResources = false,
  canUpdateResources = false,
  isDraft = false,
  isPatchDraft = false,
  isMinorDraft = false,
  canEditStructure = false,
  isMutatingDraft = false,
  onMoveNode,
  onUpdateGroupRule,
  onUpdateNodeRequirement,
  onDeleteNode,
  childNodeCount = 0,
  onOpenCreateChild,
}) {
  const [panelMode, setPanelMode] = useState(
    () => normalizeNodeType(selectedNode) === "choice_group" ? "requirements" : "details",
  );
  const [mappingModal, setMappingModal] = useState(null);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [groupRuleForm, setGroupRuleForm] = useState(() => createGroupRuleForm(selectedNode, allNodes));

  const mappedSkillIds = new Set((selectedNode.skills || []).map(getSkillId));
  const mappedResourceIds = new Set((selectedNode.resources || []).map(getResourceId));
  const availableSkillResults = skillResults.filter((skill) => !mappedSkillIds.has(getSkillId(skill)));
  const availableResourceResults = resourceResults.filter((resource) => (
    !mappedResourceIds.has(getResourceId(resource))
  ));
  const canEditLearning = canEditLearningFields(selectedNode);
  const canMapLearning = canEditMappings(selectedNode);
  const canShowContent = canEditLearning;
  const canShowSkills = canMapLearning;
  const canShowResources = canMapLearning;
  const canManageSkills = isDraft && !isPatchDraft && canMapLearning;
  const canManageResources = isDraft && canMapLearning;
  const canAddChildNode = canEditStructure && canCreateChildNodes(selectedNode);
  const canMutateSelectedStructure = canEditStructure && !(isMinorDraft && selectedNode.isRequired);
  const skillCount = selectedNode.skills?.length || 0;
  const resourceCount = selectedNode.resources?.length || 0;
  const selectedNodeType = normalizeNodeType(selectedNode);
  const isGroupNode = selectedNodeType === "choice_group";
  const canEditRequirementStatus = isRequirementEditableNode(selectedNode);
  const parentNode = getNodeById(allNodes, selectedNode.parentNodeId);
  const parentNodeType = normalizeNodeType(parentNode);
  const isParentGroupNode = parentNodeType === "choice_group";
  const parentGroupChildren = isParentGroupNode ? getDirectGroupChildren(allNodes, parentNode.roadmapNodeId) : [];
  const parentRequiredChildCount = parentGroupChildren.filter((node) => node.isRequired).length;
  const parentSelectionType = parentNode?.selectionType || "complete_all";
  const parentRequiredCount = toPositiveInteger(parentNode?.requiredCount);
  const groupChildren = getDirectGroupChildren(allNodes, selectedNode.roadmapNodeId);
  const requiredGroupChildren = groupChildren.filter((node) => node.isRequired);
  const requiredChildCount = requiredGroupChildren.length;
  const optionalChildCount = Math.max(groupChildren.length - requiredChildCount, 0);
  const normalizedSelectionType = groupRuleForm.selectionType || "complete_all";
  const isChooseMany = normalizedSelectionType === "choose_many";
  const requiredCountDisplayValue = getRequiredCountDisplayValue(
    normalizedSelectionType,
    groupRuleForm.requiredCount,
    requiredChildCount,
  );
  const requiredCountNumber = toPositiveInteger(requiredCountDisplayValue);
  const requiredCountOptions = getRequiredCountOptions(requiredChildCount);
  const isRequiredCountLocked = !isChooseMany;
  const savedSelectionType = selectedNode.selectionType || "complete_all";
  const savedRequiredCount = savedSelectionType === "choose_many" ? Number(selectedNode.requiredCount || 0) : null;
  const groupRuleChanged = isGroupNode && (
    normalizedSelectionType !== savedSelectionType
    || (isChooseMany
      ? requiredCountNumber !== savedRequiredCount
      : Boolean(selectedNode.requiredCount))
  );
  const groupRuleInvalid = isGroupNode && (
    requiredChildCount < 1
    || (isChooseMany && (!requiredCountNumber || requiredCountNumber < 1 || requiredCountNumber > requiredChildCount))
  );
  const canManageGroupRule = canEditStructure && isGroupNode && !isMinorDraft;
  const canManageSelectedRequirement = canEditStructure && !isMinorDraft && canEditRequirementStatus;

  const handleSaveGroupRule = async () => {
    if (!selectedNode?.roadmapNodeId || groupRuleInvalid) return;

    await onUpdateGroupRule?.(selectedNode.roadmapNodeId, {
      selectionType: normalizedSelectionType,
      requiredCount: isChooseMany ? requiredCountNumber : null,
    });
  };

  const getChildRequirementBlockReason = (child, nextIsRequired) => {
    if (!canManageGroupRule) return "Requirement changes are only available in an initial draft or a major draft.";
    if (isMutatingDraft) return "Wait for the current change to finish.";
    if (child.isRequired === nextIsRequired) return "";
    if (nextIsRequired) return "";

    const nextRequiredChildCount = requiredChildCount - 1;
    if (nextRequiredChildCount < 1) return "Group node must have at least one required child.";
    if (isChooseMany && requiredCountNumber && requiredCountNumber > nextRequiredChildCount) {
      return "Set a lower required count before making this child optional.";
    }

    return "";
  };

  const canToggleChildRequirement = (child, nextIsRequired) => (
    child.isRequired !== nextIsRequired
    && getChildRequirementBlockReason(child, nextIsRequired) === ""
  );

  const getSelectedRequirementBlockReason = (nextIsRequired) => {
    if (!canManageSelectedRequirement) return "Requirement changes are only available in an initial draft or a major draft.";
    if (isMutatingDraft) return "Wait for the current change to finish.";
    if (selectedNode.isRequired === nextIsRequired) return "";
    if (nextIsRequired || !isParentGroupNode) return "";

    const nextRequiredChildCount = parentRequiredChildCount - 1;
    if (nextRequiredChildCount < 1) return "Parent group must have at least one required child.";
    if (parentSelectionType === "choose_many" && parentRequiredCount && parentRequiredCount > nextRequiredChildCount) {
      return "Set a lower parent group required count before making this node optional.";
    }

    return "";
  };

  const canToggleSelectedRequirement = (nextIsRequired) => (
    selectedNode.isRequired !== nextIsRequired
    && getSelectedRequirementBlockReason(nextIsRequired) === ""
  );

  const handleSelectionTypeChange = (nextSelectionType) => {
    setGroupRuleForm((current) => ({
      ...current,
      selectionType: nextSelectionType,
      requiredCount: getRequiredCountDisplayValue(
        nextSelectionType,
        current.requiredCount || selectedNode.requiredCount,
        requiredChildCount,
      ),
    }));
  };

  const handleChildRequirementChange = async (child, nextIsRequired) => {
    if (!canToggleChildRequirement(child, nextIsRequired)) return;
    await onUpdateNodeRequirement?.(child.roadmapNodeId, nextIsRequired, selectedNode.roadmapNodeId);
  };

  const handleSelectedRequirementChange = async (nextIsRequired) => {
    if (!canToggleSelectedRequirement(nextIsRequired)) return;
    await onUpdateNodeRequirement?.(selectedNode.roadmapNodeId, nextIsRequired, selectedNode.roadmapNodeId);
  };

  const selectedRequirementBlockReason = canEditRequirementStatus
    ? getSelectedRequirementBlockReason(Boolean(!selectedNode.isRequired))
    : "";

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
              {canEditStructure && (
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
              <ModuleButton onClick={onSaveNode} disabled={!isDraft || isSavingNode || !isDirty}>
                <Save size={14} /> {isSavingNode ? "Saving" : "Save"}
              </ModuleButton>
            </div>
          </div>

          {canEditStructure && (
            <div className="mt-3 flex flex-wrap items-center gap-2 rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-2">
              <ModuleButton size="xs" variant="secondary" onClick={() => onMoveNode?.("up")} disabled={isMutatingDraft || !canMutateSelectedStructure}>
                <ArrowUp size={13} /> Move up
              </ModuleButton>
              <ModuleButton size="xs" variant="secondary" onClick={() => onMoveNode?.("down")} disabled={isMutatingDraft || !canMutateSelectedStructure}>
                <ArrowDown size={13} /> Move down
              </ModuleButton>
              <ModuleButton size="xs" variant="danger" className="hover:border-rose-500 hover:bg-rose-100 hover:text-rose-800" onClick={() => setIsDeleteConfirmOpen(true)} disabled={isMutatingDraft || !canMutateSelectedStructure}>
                <Trash2 size={13} /> Delete
              </ModuleButton>
            </div>
          )}
        </div>

        <div className="border-b border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-2">
          <div
            className="grid gap-2 rounded-xl bg-white p-1 ring-1 ring-[#B9D8CC]/70"
            style={{ gridTemplateColumns: `repeat(${1 + (isGroupNode ? 1 : 0) + (canShowContent ? 1 : 0) + (canShowSkills ? 1 : 0) + (canShowResources ? 1 : 0)}, minmax(0, 1fr))` }}
          >
            <button
              type="button"
              onClick={() => setPanelMode("details")}
              className={tabButtonClass("details")}
            >
              Details
            </button>
            {isGroupNode && (
              <button
                type="button"
                onClick={() => setPanelMode("requirements")}
                className={tabButtonClass("requirements")}
              >
                Requirements
              </button>
            )}
            {canShowContent && (
              <button
                type="button"
                onClick={() => setPanelMode("content")}
                className={tabButtonClass("content")}
              >
                Content
              </button>
            )}
            {canShowSkills && (
              <button
                type="button"
                onClick={() => setPanelMode("skills")}
                className={tabButtonClass("skills")}
              >
                Skills
              </button>
            )}
            {canShowResources && (
              <button
                type="button"
                onClick={() => setPanelMode("resources")}
                className={tabButtonClass("resources")}
              >
                Resources
              </button>
            )}
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
                    disabled={!isDraft}
                  />
                </ModuleField>

                <ModuleField label="Description">
                  <textarea
                    value={nodeForm.description}
                    onChange={(event) => setNodeForm((current) => ({ ...current, description: event.target.value }))}
                    rows={5}
                    className={`${inputClass} min-h-32 resize-y`}
                    disabled={!isDraft}
                  />
                </ModuleField>
              </section>

              {canEditRequirementStatus && (
                <section className="space-y-3 rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-3">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="text-xs font-extrabold uppercase tracking-wide text-slate-600">Requirement</div>
                      <div className="mt-1 text-xs font-bold text-slate-500">
                        {selectedNode.isRequired ? "Counts toward required progress." : "Optional content does not block progress."}
                      </div>
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      <div className="flex rounded-lg bg-white p-1 ring-1 ring-[#B9D8CC]/70">
                        <button
                          type="button"
                          className={requirementOptionClass(
                            selectedNode.isRequired,
                            !canToggleSelectedRequirement(true),
                            "bg-[#1F6F5F] text-white",
                          )}
                          onClick={() => handleSelectedRequirementChange(true)}
                          disabled={!canToggleSelectedRequirement(true)}
                        >
                          Required
                        </button>
                        <button
                          type="button"
                          className={requirementOptionClass(
                            !selectedNode.isRequired,
                            !canToggleSelectedRequirement(false),
                            "bg-slate-700 text-white",
                          )}
                          onClick={() => handleSelectedRequirementChange(false)}
                          disabled={!canToggleSelectedRequirement(false)}
                        >
                          Optional
                        </button>
                      </div>
                      <RequirementTooltip message={selectedRequirementBlockReason} />
                    </div>
                  </div>
                </section>
              )}

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
                        disabled={!isDraft}
                      />
                    </ModuleField>
                    <ModuleField label="Difficulty">
                      <select
                        value={nodeForm.difficultyLevel}
                        onChange={(event) => setNodeForm((current) => ({ ...current, difficultyLevel: event.target.value }))}
                        className={inputClass}
                        disabled={!isDraft}
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

          {panelMode === "requirements" && isGroupNode && (
            <section className="space-y-4">
              <div className="rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-3">
                <div className="mb-3 text-xs font-extrabold uppercase tracking-wide text-slate-600">Child requirements</div>
                {groupChildren.length === 0 ? (
                  <ModuleEmptyState title="No child nodes" />
                ) : (
                  <div className="space-y-2">
                    {groupChildren.map((child) => {
                      const childRequired = Boolean(child.isRequired);
                      const targetBlockReason = childRequired
                        ? getChildRequirementBlockReason(child, false)
                        : getChildRequirementBlockReason(child, true);

                      return (
                        <div key={child.roadmapNodeId} className="rounded-lg bg-white p-3 ring-1 ring-[#B9D8CC]/70">
                          <div className="flex items-center justify-between gap-3">
                            <div className="min-w-0">
                              <div className="truncate text-sm font-extrabold text-[#18332D]">{getNodeLabel(child)}</div>
                              <div className="text-[11px] font-bold uppercase tracking-wide text-slate-500">{normalizeNodeType(child)}</div>
                            </div>
                            <div className="flex shrink-0 items-center gap-2">
                              <div className="flex rounded-lg bg-[#F7F1E8] p-1 ring-1 ring-[#B9D8CC]/70">
                                <button
                                  type="button"
                                  className={requirementOptionClass(
                                    childRequired,
                                    !canToggleChildRequirement(child, true),
                                    "bg-[#1F6F5F] text-white",
                                  )}
                                  onClick={() => handleChildRequirementChange(child, true)}
                                  disabled={!canToggleChildRequirement(child, true)}
                                >
                                  Required
                                </button>
                                <button
                                  type="button"
                                  className={requirementOptionClass(
                                    !childRequired,
                                    !canToggleChildRequirement(child, false),
                                    "bg-slate-700 text-white",
                                  )}
                                  onClick={() => handleChildRequirementChange(child, false)}
                                  disabled={!canToggleChildRequirement(child, false)}
                                >
                                  Optional
                                </button>
                              </div>
                              <RequirementTooltip message={targetBlockReason} />
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>

              <div className="space-y-3 rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-3">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <div className="text-xs font-extrabold uppercase tracking-wide text-slate-600">Completion rule</div>
                    <div className="mt-1 text-xs font-bold text-slate-500">
                      {groupRuleChanged ? "Unsaved rule changes" : "Rule is saved"}
                    </div>
                  </div>
                  <ModuleButton
                    size="xs"
                    variant={groupRuleChanged && !groupRuleInvalid ? "primary" : "secondary"}
                    onClick={handleSaveGroupRule}
                    disabled={!canManageGroupRule || isMutatingDraft || !groupRuleChanged || groupRuleInvalid}
                  >
                    {groupRuleChanged ? "Save changes" : "Saved"}
                  </ModuleButton>
                </div>

                <ModuleField label="Rule">
                  <AppSelect
                    value={normalizedSelectionType}
                    options={groupRuleOptions}
                    onChange={handleSelectionTypeChange}
                    ariaLabel="Completion rule"
                    dropdownMode="fixed"
                    disabled={!canManageGroupRule || isMutatingDraft}
                  />
                </ModuleField>

                <ModuleField label="Required count">
                  <AppSelect
                    value={requiredCountDisplayValue}
                    options={requiredCountOptions}
                    onChange={(value) => setGroupRuleForm((current) => ({ ...current, requiredCount: value }))}
                    ariaLabel="Required count"
                    dropdownMode="fixed"
                    disabled={!canManageGroupRule || isMutatingDraft || requiredChildCount < 1 || isRequiredCountLocked}
                  />
                  {isRequiredCountLocked && (
                    <div className="mt-1 text-[11px] font-bold text-slate-500">
                      Required count is controlled by the selected rule.
                    </div>
                  )}
                </ModuleField>

                <div className="rounded-lg bg-white p-3 text-xs font-bold text-slate-600 ring-1 ring-[#B9D8CC]/70">
                  {getGroupRuleSummary(normalizedSelectionType, requiredCountNumber, requiredChildCount, optionalChildCount)}
                </div>

                {!canManageGroupRule && (
                  <div className="rounded-lg bg-white p-3 text-xs font-bold text-slate-500 ring-1 ring-[#B9D8CC]/70">
                    Requirement rules can only be changed in an initial draft or a major draft.
                  </div>
                )}
              </div>
            </section>
          )}

          {panelMode === "content" && canShowContent && (
            <NodeContentFields
              selectedNode={selectedNode}
              nodeForm={nodeForm}
              setNodeForm={setNodeForm}
              isEditable={isDraft}
            />
          )}

          {panelMode === "skills" && canShowSkills && (
            <section className="space-y-4">
              <div className="flex items-center justify-between gap-3">
                <div className="text-xs font-extrabold uppercase tracking-wide text-slate-600">
                  {skillCount} mapped skills
                </div>
                {canManageSkills && (
                  <ModuleButton size="xs" variant="secondary" onClick={() => {
                    setMappingModal("skills");
                    onOpenSkillSearch();
                  }}>
                    <Plus size={14} /> Add skill
                  </ModuleButton>
                )}
              </div>
              <MappingList
                title="Skills"
                icon={Tag}
                items={selectedNode.skills || []}
                getId={getSkillId}
                getLabel={getSkillName}
                emptyText="No skills mapped yet."
                onRemove={canManageSkills ? onRemoveSkill : null}
              />
            </section>
          )}

          {panelMode === "resources" && canShowResources && (
            <section className="space-y-4">
              <div className="flex items-center justify-between gap-3">
                <div className="text-xs font-extrabold uppercase tracking-wide text-slate-600">
                  {resourceCount} mapped resources
                </div>
                {canManageResources && (
                  <ModuleButton size="xs" variant="secondary" onClick={() => {
                    setMappingModal("resources");
                    onOpenResourceSearch();
                  }}>
                    <Plus size={14} /> Add resource
                  </ModuleButton>
                )}
              </div>
              <MappingList
                title="Resources"
                icon={BookOpenText}
                items={selectedNode.resources || []}
                getId={getResourceId}
                getLabel={getResourceTitle}
                emptyText="No resources mapped yet."
                onRemove={canManageResources ? onRemoveResource : null}
                onEdit={canManageResources && canUpdateResources ? onEditResource : null}
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
        emptyBody="Search for another skill or create a new one."
        emptyActionLabel={canCreateSkills ? "Create skill" : ""}
        onEmptyAction={canCreateSkills ? () => onCreateSkillFromSearch?.(skillSearch) : null}
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
        emptyBody="Search for another resource or create a new one."
        emptyActionLabel={canCreateResources ? "Create resource" : ""}
        onEmptyAction={canCreateResources ? () => onCreateResourceFromSearch?.(resourceSearch) : null}
      />
    </>
  );
}
