import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { toast } from "react-toastify";

import { contentManagerRoadmapApi } from "../../../api/contentRoadmapApi";
import { skillApi } from "../../../api/skillApi";
import {
  areTextListsEqual,
  countMissingMappings,
  fromFormNumber,
  isCanceledRequest,
  getFirstMetadataList,
  getFirstMetadataText,
  isSameNullableNumber,
  listToText,
  normalizeComparableText,
  normalizeNodes,
  prettyStatus,
  textToList,
  toFormNumber,
} from "../roadmapEditorUtils";
import { canEditGuideMetadata, canEditLearningFields, canEditMappings, normalizeNodeType } from "../nodeRules";

function createNodeGuideForm(node) {
  const metadata = node?.metadata || {};

  return {
    focus: getFirstMetadataText(metadata, ["focus", "topicFocus", "purpose", "summary"]),
    practiceText: listToText(getFirstMetadataList(metadata, ["practice", "practiceIdeas", "suggestedPractice"])),
    projectBrief: getFirstMetadataText(metadata, ["projectBrief", "brief", "goal", "objective", "purpose"]),
    suggestedStepsText: listToText(getFirstMetadataList(metadata, ["suggestedSteps", "buildSteps", "implementationSteps", "steps", "practiceSteps"])),
    expectedEvidenceText: listToText(getFirstMetadataList(metadata, ["expectedEvidence", "evidence", "deliverables", "artifacts", "outputs", "reviewEvidence"])),
    reviewFocus: getFirstMetadataText(metadata, ["reviewFocus", "checkpointFocus", "focus", "purpose", "reviewPurpose"]),
    reviewQuestionsText: listToText(getFirstMetadataList(metadata, ["reviewQuestions", "questions", "reflectionQuestions", "selfCheckQuestions"])),
    nextActionsText: listToText(getFirstMetadataList(metadata, ["nextActions", "followUpActions", "followUps", "nextSteps"])),
    whenToChoose: getFirstMetadataText(metadata, ["whenToChoose", "optionUseCase", "useCase", "purpose", "focus"]),
    choiceGuidance: getFirstMetadataText(metadata, ["choiceGuidance", "guidance", "purpose", "selectionGuidance"]),
    selectionNotesText: listToText(getFirstMetadataList(metadata, ["selectionNotes", "options", "considerations"])),
    phaseFocus: getFirstMetadataText(metadata, ["phaseFocus", "focus", "purpose", "summary"]),
    milestonesText: listToText(getFirstMetadataList(metadata, ["milestones", "phaseMilestones", "reviewPoints"])),
    purpose: getFirstMetadataText(metadata, ["purpose", "summary", "resourcePurpose", "focus"]),
  };
}

function createNodeForm(node = null) {
  return {
    title: node?.title || "",
    description: node?.description || "",
    estimatedHours: toFormNumber(node?.estimatedHours),
    difficultyLevel: node?.difficultyLevel || "",
    learningOutcomesText: listToText(node?.learningOutcomes),
    completionCriteriaText: listToText(node?.completionCriteria),
    guide: createNodeGuideForm(node),
  };
}


function getNodeId(node) {
  return node?.roadmapNodeId || node?.id || "";
}

function getDescendantIds(rootId, nodes = []) {
  if (!rootId) return [];

  const childrenByParent = new Map();
  normalizeNodes(nodes).forEach((node) => {
    const parentId = node.parentNodeId ? String(node.parentNodeId) : "";
    if (!parentId) return;
    const list = childrenByParent.get(parentId) || [];
    list.push(node);
    childrenByParent.set(parentId, list);
  });

  const orderedIds = [];
  const visit = (nodeId) => {
    const normalizedId = String(nodeId);
    if (orderedIds.includes(normalizedId)) return;
    orderedIds.push(normalizedId);
    (childrenByParent.get(normalizedId) || []).forEach((child) => visit(getNodeId(child)));
  };

  visit(rootId);
  return orderedIds;
}

function getEdgeNodeIds(edge = {}) {
  return {
    sourceId: edge.fromNodeId || edge.sourceNodeId || edge.source || edge.sourceId,
    targetId: edge.toNodeId || edge.targetNodeId || edge.target || edge.targetId,
  };
}

function removeNodesFromDetail(detail, deletedIds = []) {
  if (!detail) return detail;

  const deletedSet = new Set(deletedIds.map(String));
  return {
    ...detail,
    nodes: normalizeNodes(detail.nodes).filter((node) => !deletedSet.has(String(getNodeId(node)))),
    edges: Array.isArray(detail.edges)
      ? detail.edges.filter((edge) => {
        const { sourceId, targetId } = getEdgeNodeIds(edge);
        return !deletedSet.has(String(sourceId)) && !deletedSet.has(String(targetId));
      })
      : detail.edges,
  };
}

function restoreDeletedItems(detail, deletedNodes = [], deletedEdges = []) {
  if (!detail) return detail;

  const existingNodeIds = new Set(normalizeNodes(detail.nodes).map((node) => String(getNodeId(node))));
  const restoredNodes = deletedNodes.filter((node) => !existingNodeIds.has(String(getNodeId(node))));
  const existingEdgeIds = new Set((detail.edges || []).map((edge) => String(edge.roadmapEdgeId || edge.id || `${edge.fromNodeId || edge.sourceNodeId}-${edge.toNodeId || edge.targetNodeId}-${edge.edgeType || edge.type}`)));
  const restoredEdges = deletedEdges.filter((edge) => {
    const edgeId = String(edge.roadmapEdgeId || edge.id || `${edge.fromNodeId || edge.sourceNodeId}-${edge.toNodeId || edge.targetNodeId}-${edge.edgeType || edge.type}`);
    return !existingEdgeIds.has(edgeId);
  });

  return {
    ...detail,
    nodes: [...normalizeNodes(detail.nodes), ...restoredNodes],
    edges: [...(Array.isArray(detail.edges) ? detail.edges : []), ...restoredEdges],
  };
}

function areGuideFormsEqual(left = {}, right = {}, node = null) {
  const nodeType = normalizeNodeType(node);

  if (nodeType === "project") {
    return (
      normalizeComparableText(left.projectBrief) === normalizeComparableText(right.projectBrief)
      && areTextListsEqual(left.suggestedStepsText, right.suggestedStepsText)
      && areTextListsEqual(left.expectedEvidenceText, right.expectedEvidenceText)
    );
  }

  if (nodeType === "checkpoint") {
    return (
      normalizeComparableText(left.reviewFocus) === normalizeComparableText(right.reviewFocus)
      && areTextListsEqual(left.expectedEvidenceText, right.expectedEvidenceText)
      && areTextListsEqual(left.reviewQuestionsText, right.reviewQuestionsText)
      && areTextListsEqual(left.nextActionsText, right.nextActionsText)
    );
  }

  return true;
}

function buildGuidePayload(guide = {}, node = null) {
  const nodeType = normalizeNodeType(node);

  if (nodeType === "project") {
    return {
      projectBrief: guide.projectBrief || null,
      suggestedSteps: textToList(guide.suggestedStepsText),
      expectedEvidence: textToList(guide.expectedEvidenceText),
    };
  }

  if (nodeType === "checkpoint") {
    return {
      reviewFocus: guide.reviewFocus || null,
      expectedEvidence: textToList(guide.expectedEvidenceText),
      reviewQuestions: textToList(guide.reviewQuestionsText),
      nextActions: textToList(guide.nextActionsText),
    };
  }

  return null;
}

export default function useContentRoadmapEditor(roadmapId) {
  const [searchParams, setSearchParams] = useSearchParams();
  const selectedVersionId = searchParams.get("versionId") || "";

  const [detail, setDetail] = useState(null);
  const [selectedNodeId, setSelectedNodeId] = useState("");
  const [metadataForm, setMetadataForm] = useState({ title: "", description: "", estimatedTotalHours: "" });
  const [nodeForm, setNodeForm] = useState(createNodeForm());
  const [graphFocusRequest, setGraphFocusRequest] = useState(null);
  const [newNodeIds, setNewNodeIds] = useState([]);
  const [skillSearch, setSkillSearch] = useState("");
  const [skillResults, setSkillResults] = useState([]);
  const [resourceSearch, setResourceSearch] = useState("");
  const [resourceResults, setResourceResults] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showLoadingState, setShowLoadingState] = useState(false);
  const [isSavingMetadata, setIsSavingMetadata] = useState(false);
  const [isSavingNode, setIsSavingNode] = useState(false);
  const [isSearchingSkills, setIsSearchingSkills] = useState(false);
  const [isSearchingResources, setIsSearchingResources] = useState(false);
  const [error, setError] = useState("");
  const [workspaceMode, setWorkspaceMode] = useState("nodes");
  const [isMutatingDraft, setIsMutatingDraft] = useState(false);
  const [validationResult, setValidationResult] = useState(null);
  const [pendingNodeDeletions, setPendingNodeDeletions] = useState([]);

  useEffect(() => {
    if (!roadmapId) return undefined;

    const controller = new AbortController();
    const loadingTimer = window.setTimeout(() => setShowLoadingState(true), 180);

    async function loadDetail() {
      try {
        setIsLoading(true);
        setError("");

        const data = await contentManagerRoadmapApi.getRoadmapDetail({
          roadmapId,
          versionId: selectedVersionId,
          signal: controller.signal,
        });

        setDetail(data);
        setMetadataForm({
          title: data?.title || "",
          description: data?.description || "",
          estimatedTotalHours: toFormNumber(data?.estimatedTotalHours),
        });

        const availableNodes = normalizeNodes(data?.nodes);
        if (availableNodes.length === 0) {
          setSelectedNodeId("");
        } else {
          setSelectedNodeId((current) => {
            if (availableNodes.some((node) => node.roadmapNodeId === current)) return current;
            const firstLearningNode = availableNodes.find(canEditLearningFields);
            return (firstLearningNode || availableNodes[0]).roadmapNodeId;
          });
        }
      } catch (requestError) {
        if (!isCanceledRequest(requestError)) {
          setError(requestError?.message || "Unable to load roadmap editor.");
        }
      } finally {
        window.clearTimeout(loadingTimer);
        if (!controller.signal.aborted) {
          setIsLoading(false);
          setShowLoadingState(false);
        }
      }
    }

    loadDetail();

    return () => {
      window.clearTimeout(loadingTimer);
      controller.abort();
    };
  }, [roadmapId, selectedVersionId]);

  const allNodes = useMemo(() => normalizeNodes(detail?.nodes), [detail?.nodes]);
  const selectedNode = useMemo(
    () => allNodes.find((node) => node.roadmapNodeId === selectedNodeId) || null,
    [allNodes, selectedNodeId],
  );

  useEffect(() => {
    setNodeForm(createNodeForm(selectedNode));
    setSkillResults([]);
    setResourceResults([]);
    setSkillSearch("");
    setResourceSearch("");
  }, [selectedNode?.roadmapNodeId]);

  useEffect(() => {
    if (!selectedNode && allNodes.length > 0) {
      setSelectedNodeId(allNodes[0].roadmapNodeId);
    }
  }, [allNodes, selectedNode]);
  const selectedNodeChildCount = useMemo(() => {
    if (!selectedNode?.roadmapNodeId) return 0;
    return getDescendantIds(selectedNode.roadmapNodeId, allNodes).length - 1;
  }, [allNodes, selectedNode?.roadmapNodeId]);

  const missingMappingCount = useMemo(() => countMissingMappings(allNodes.filter(canEditMappings)), [allNodes]);
  const versionOptions = useMemo(() => {
    if (!detail?.versions?.length) return [];

    return detail.versions.map((version) => ({
      value: version.roadmapVersionId,
      label: `v${version.versionNumber} · ${prettyStatus(version.status)}`,
    }));
  }, [detail?.versions]);

  const hasMetadataChanges = useMemo(() => {
    if (!detail) return false;

    return (
      normalizeComparableText(metadataForm.title) !== normalizeComparableText(detail.title)
      || normalizeComparableText(metadataForm.description) !== normalizeComparableText(detail.description)
      || !isSameNullableNumber(metadataForm.estimatedTotalHours, toFormNumber(detail.estimatedTotalHours))
    );
  }, [detail, metadataForm.description, metadataForm.estimatedTotalHours, metadataForm.title]);

  const hasNodeDetailsChanges = useMemo(() => {
    if (!selectedNode) return false;

    const baseChanged = (
      normalizeComparableText(nodeForm.title) !== normalizeComparableText(selectedNode.title)
      || normalizeComparableText(nodeForm.description) !== normalizeComparableText(selectedNode.description)
      || !areTextListsEqual(nodeForm.learningOutcomesText, selectedNode.learningOutcomes)
      || !areTextListsEqual(nodeForm.completionCriteriaText, selectedNode.completionCriteria)
      || !areGuideFormsEqual(nodeForm.guide, createNodeGuideForm(selectedNode), selectedNode)
    );

    if (!canEditLearningFields(selectedNode)) {
      return baseChanged;
    }

    return (
      baseChanged
      || !isSameNullableNumber(nodeForm.estimatedHours, toFormNumber(selectedNode.estimatedHours))
      || normalizeComparableText(nodeForm.difficultyLevel) !== normalizeComparableText(selectedNode.difficultyLevel)
    );
  }, [nodeForm, selectedNode]);

  const updateNodeInDetail = (updatedNode) => {
    setDetail((current) => {
      if (!current) return current;

      return {
        ...current,
        nodes: normalizeNodes(current.nodes).map((node) => (
          node.roadmapNodeId === updatedNode.roadmapNodeId ? updatedNode : node
        )),
      };
    });
  };

  const applyRoadmapDetail = (updatedDetail, focusNodeId = null) => {
    if (!updatedDetail) return;

    setDetail(updatedDetail);
    setMetadataForm({
      title: updatedDetail?.title || "",
      description: updatedDetail?.description || "",
      estimatedTotalHours: toFormNumber(updatedDetail?.estimatedTotalHours),
    });

    if (updatedDetail?.roadmapVersionId) {
      const next = new URLSearchParams(searchParams.toString());
      next.set("versionId", updatedDetail.roadmapVersionId);
      setSearchParams(next, { replace: true });
    }

    const availableNodes = normalizeNodes(updatedDetail?.nodes);
    if (focusNodeId && availableNodes.some((node) => node.roadmapNodeId === focusNodeId)) {
      setSelectedNodeId(focusNodeId);
      setGraphFocusRequest({ id: focusNodeId, requestId: Date.now() });
    } else if (!availableNodes.some((node) => node.roadmapNodeId === selectedNodeId)) {
      setSelectedNodeId(availableNodes[0]?.roadmapNodeId || "");
    }
  };

  const saveMetadata = async () => {
    if (!detail) return;

    try {
      setIsSavingMetadata(true);
      const updated = await contentManagerRoadmapApi.updateVersionMetadata(
        detail.roadmapVersionId,
        {
          title: metadataForm.title,
          description: metadataForm.description,
          estimatedTotalHours: fromFormNumber(metadataForm.estimatedTotalHours),
        },
      );

      setDetail(updated);
      setMetadataForm({
        title: updated?.title || "",
        description: updated?.description || "",
        estimatedTotalHours: toFormNumber(updated?.estimatedTotalHours),
      });
      toast.success("Roadmap metadata saved.");
    } catch (saveError) {
      toast.error(saveError?.message || "Unable to save roadmap metadata.");
    } finally {
      setIsSavingMetadata(false);
    }
  };

  const saveNode = async () => {
    if (!selectedNode) return;

    const payload = {
      title: nodeForm.title,
      description: nodeForm.description,
      learningOutcomes: textToList(nodeForm.learningOutcomesText),
      completionCriteria: textToList(nodeForm.completionCriteriaText),
    };

    if (canEditGuideMetadata(selectedNode)) {
      payload.guide = buildGuidePayload(nodeForm.guide, selectedNode);
    }

    if (canEditLearningFields(selectedNode)) {
      payload.estimatedHours = fromFormNumber(nodeForm.estimatedHours);
      payload.difficultyLevel = nodeForm.difficultyLevel || null;
    }

    try {
      setIsSavingNode(true);
      const updatedNode = await contentManagerRoadmapApi.updateNodeMetadata(
        selectedNode.roadmapNodeId,
        payload,
      );

      updateNodeInDetail(updatedNode);
      toast.success("Node updated.");
    } catch (saveError) {
      toast.error(saveError?.message || "Unable to update node.");
    } finally {
      setIsSavingNode(false);
    }
  };

  const loadSkillSuggestions = async () => {
    if (isSearchingSkills || (skillResults.length > 0 && !skillSearch.trim())) return;

    try {
      setIsSearchingSkills(true);
      const suggestions = await skillApi.getSuggestions({ limit: 12 });
      setSkillResults(Array.isArray(suggestions) ? suggestions : []);
    } catch (searchError) {
      toast.error(searchError?.message || "Unable to load skill suggestions.");
    } finally {
      setIsSearchingSkills(false);
    }
  };

  const loadResourceSuggestions = async () => {
    if (isSearchingResources || (resourceResults.length > 0 && !resourceSearch.trim())) return;

    try {
      setIsSearchingResources(true);
      const resources = await contentManagerRoadmapApi.searchLearningResources({
        search: "",
        limit: 12,
      });
      setResourceResults(resources);
    } catch (searchError) {
      toast.error(searchError?.message || "Unable to load resource suggestions.");
    } finally {
      setIsSearchingResources(false);
    }
  };

  const searchSkills = async () => {
    try {
      setIsSearchingSkills(true);
      const response = await skillApi.searchSkills({
        search: skillSearch,
        limit: 12,
      });
      setSkillResults(Array.isArray(response?.items) ? response.items : []);
    } catch (searchError) {
      toast.error(searchError?.message || "Unable to search skills.");
    } finally {
      setIsSearchingSkills(false);
    }
  };

  const searchResources = async () => {
    try {
      setIsSearchingResources(true);
      const resources = await contentManagerRoadmapApi.searchLearningResources({
        search: resourceSearch,
        limit: 12,
      });
      setResourceResults(resources);
    } catch (searchError) {
      toast.error(searchError?.message || "Unable to search resources.");
    } finally {
      setIsSearchingResources(false);
    }
  };

  const addSkill = async (skillId) => {
    if (!selectedNode || !skillId || !canEditMappings(selectedNode)) return;

    try {
      const updatedNode = await contentManagerRoadmapApi.addNodeSkill(selectedNode.roadmapNodeId, skillId);
      updateNodeInDetail(updatedNode);
      toast.success("Skill mapped.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to map skill.");
    }
  };

  const removeSkill = async (skillId) => {
    if (!selectedNode || !skillId || !canEditMappings(selectedNode)) return;

    try {
      const updatedNode = await contentManagerRoadmapApi.removeNodeSkill(selectedNode.roadmapNodeId, skillId);
      updateNodeInDetail(updatedNode);
      toast.success("Skill removed.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to remove skill.");
    }
  };

  const addResource = async (resourceId) => {
    if (!selectedNode || !resourceId || !canEditMappings(selectedNode)) return;

    try {
      const updatedNode = await contentManagerRoadmapApi.addNodeResource(selectedNode.roadmapNodeId, resourceId);
      updateNodeInDetail(updatedNode);
      toast.success("Resource mapped.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to map resource.");
    }
  };

  const removeResource = async (resourceId) => {
    if (!selectedNode || !resourceId || !canEditMappings(selectedNode)) return;

    try {
      const updatedNode = await contentManagerRoadmapApi.removeNodeResource(selectedNode.roadmapNodeId, resourceId);
      updateNodeInDetail(updatedNode);
      toast.success("Resource removed.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to remove resource.");
    }
  };

  const cloneDraft = async () => {
    if (!detail?.roadmapVersionId) return;

    try {
      setIsMutatingDraft(true);
      const updated = await contentManagerRoadmapApi.cloneVersionToDraft(detail.roadmapVersionId, {});
      applyRoadmapDetail(updated);
      setWorkspaceMode("nodes");
      toast.success("Draft opened.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to create draft.");
    } finally {
      setIsMutatingDraft(false);
    }
  };

  const validateDraft = async () => {
    if (!detail?.roadmapVersionId) return null;

    try {
      setIsMutatingDraft(true);
      const result = await contentManagerRoadmapApi.validateVersion(detail.roadmapVersionId);
      setValidationResult(result);
      return result;
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to validate draft.");
      return null;
    } finally {
      setIsMutatingDraft(false);
    }
  };

  const publishDraft = async () => {
    if (!detail?.roadmapVersionId) return;

    try {
      setIsMutatingDraft(true);
      const updated = await contentManagerRoadmapApi.publishVersion(detail.roadmapVersionId);
      applyRoadmapDetail(updated);
      setValidationResult(null);
      toast.success("Draft published.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to publish draft.");
    } finally {
      setIsMutatingDraft(false);
    }
  };

  const createNode = async (payload) => {
    if (!detail?.roadmapVersionId) return;

    try {
      setIsMutatingDraft(true);
      const result = await contentManagerRoadmapApi.createNode(detail.roadmapVersionId, payload);
      const focusNodeId = result?.focusNodeId;
      applyRoadmapDetail(result?.roadmap, focusNodeId);
      if (focusNodeId) {
        setNewNodeIds((current) => Array.from(new Set([...current, focusNodeId])));
        window.setTimeout(() => {
          setNewNodeIds((current) => current.filter((id) => id !== focusNodeId));
        }, 12000);
      }
      toast.success("Node added.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to add node.");
      throw actionError;
    } finally {
      setIsMutatingDraft(false);
    }
  };

  const moveNode = async (direction) => {
    if (!selectedNode?.roadmapNodeId) return;

    try {
      setIsMutatingDraft(true);
      const result = await contentManagerRoadmapApi.moveNode(selectedNode.roadmapNodeId, direction);
      applyRoadmapDetail(result?.roadmap, result?.focusNodeId || selectedNode.roadmapNodeId);
      toast.success("Node moved.");
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to move node.");
    } finally {
      setIsMutatingDraft(false);
    }
  };

  const deleteNode = async () => {
    if (!selectedNode?.roadmapNodeId || !detail) return;

    const deletedNode = selectedNode;
    const deletedNodeId = selectedNode.roadmapNodeId;
    const deletedIds = getDescendantIds(deletedNodeId, allNodes);
    const deletedIdSet = new Set(deletedIds.map(String));
    const deletedNodes = allNodes.filter((node) => deletedIdSet.has(String(getNodeId(node))));
    const deletedEdges = Array.isArray(detail.edges)
      ? detail.edges.filter((edge) => {
        const { sourceId, targetId } = getEdgeNodeIds(edge);
        return deletedIdSet.has(String(sourceId)) || deletedIdSet.has(String(targetId));
      })
      : [];

    const remainingNodes = allNodes.filter((node) => !deletedIdSet.has(String(getNodeId(node))));
    const siblingFallback = remainingNodes.find((node) => node.parentNodeId === deletedNode.parentNodeId)?.roadmapNodeId;
    const parentFallback = remainingNodes.find((node) => node.roadmapNodeId === deletedNode.parentNodeId)?.roadmapNodeId;
    const focusNodeId = siblingFallback || parentFallback || remainingNodes[0]?.roadmapNodeId || "";

    setDetail((current) => removeNodesFromDetail(current, deletedIds));
    setSelectedNodeId(focusNodeId);

    setPendingNodeDeletions((current) => [
      ...current,
      {
        actionId: `${deletedNodeId}-${Date.now()}`,
        rootNodeId: deletedNodeId,
        nodeLabel: deletedNode.title || "Node",
        deletedCount: deletedNodes.length,
        deletedNodes,
        deletedEdges,
        restoreSelectedNodeId: deletedNodeId,
      },
    ]);
    toast.info("Node staged for deletion.");
  };

  const undoNodeDelete = (actionId) => {
    const action = pendingNodeDeletions.find((item) => item.actionId === actionId);
    if (!action) return;

    setDetail((current) => restoreDeletedItems(current, action.deletedNodes, action.deletedEdges));
    setSelectedNodeId(action.restoreSelectedNodeId);
    setPendingNodeDeletions((current) => current.filter((item) => item.actionId !== actionId));
    toast.info("Node restored.");
  };

  const restoreAllPendingNodeDeletions = () => {
    if (pendingNodeDeletions.length === 0) return;

    setDetail((current) => pendingNodeDeletions.reduce(
      (nextDetail, action) => restoreDeletedItems(nextDetail, action.deletedNodes, action.deletedEdges),
      current,
    ));
    const lastRestoredNodeId = pendingNodeDeletions[pendingNodeDeletions.length - 1]?.restoreSelectedNodeId || "";
    if (lastRestoredNodeId) {
      setSelectedNodeId(lastRestoredNodeId);
    }
    setPendingNodeDeletions([]);
    toast.info("Nodes restored.");
  };

  const commitPendingNodeDeletions = async () => {
    if (pendingNodeDeletions.length === 0) return true;

    try {
      setIsMutatingDraft(true);
      let latestResult = null;

      for (const action of pendingNodeDeletions) {
        latestResult = await contentManagerRoadmapApi.deleteNode(action.rootNodeId);
      }

      setPendingNodeDeletions([]);
      if (latestResult?.roadmap) {
        applyRoadmapDetail(latestResult.roadmap, latestResult.focusNodeId);
      }
      toast.success("Deletions applied.");
      return true;
    } catch (actionError) {
      toast.error(actionError?.message || "Unable to apply deletions.");
      return false;
    } finally {
      setIsMutatingDraft(false);
    }
  };

  const handleVersionChange = (versionId) => {
    const next = new URLSearchParams(searchParams.toString());
    if (versionId) {
      next.set("versionId", versionId);
    } else {
      next.delete("versionId");
    }
    setSearchParams(next);
  };

  const focusNodeFromSearch = (roadmapNodeId) => {
    if (!roadmapNodeId) return;

    setSelectedNodeId(roadmapNodeId);
    setGraphFocusRequest({ id: roadmapNodeId, requestId: Date.now() });
  };

  return {
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
    pendingNodeDeletions,
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
    validateDraft,
    publishDraft,
    createNode,
    moveNode,
    deleteNode,
    undoNodeDelete,
    restoreAllPendingNodeDeletions,
    commitPendingNodeDeletions,
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
  };
}
