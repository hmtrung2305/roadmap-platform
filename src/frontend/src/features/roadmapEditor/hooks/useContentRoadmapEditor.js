import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { toast } from "react-toastify";

import { contentManagerRoadmapApi } from "../../../api/contentRoadmapApi";
import { skillApi } from "../../../api/skillApi";
import {
  countMissingMappings,
  fromFormNumber,
  isCanceledRequest,
  isSameNullableNumber,
  normalizeComparableText,
  normalizeNodes,
  prettyStatus,
  toFormNumber,
} from "../roadmapEditorUtils";
import { canEditLearningFields, canEditMappings } from "../nodeRules";

export default function useContentRoadmapEditor(roadmapId) {
  const [searchParams, setSearchParams] = useSearchParams();
  const selectedVersionId = searchParams.get("versionId") || "";

  const [detail, setDetail] = useState(null);
  const [selectedNodeId, setSelectedNodeId] = useState("");
  const [metadataForm, setMetadataForm] = useState({ title: "", description: "", estimatedTotalHours: "" });
  const [nodeForm, setNodeForm] = useState({ title: "", description: "", estimatedHours: "", difficultyLevel: "" });
  const [graphFocusRequest, setGraphFocusRequest] = useState(null);
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
    setNodeForm({
      title: selectedNode?.title || "",
      description: selectedNode?.description || "",
      estimatedHours: toFormNumber(selectedNode?.estimatedHours),
      difficultyLevel: selectedNode?.difficultyLevel || "",
    });
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
    );

    if (!canEditLearningFields(selectedNode)) {
      return baseChanged;
    }

    return (
      baseChanged
      || !isSameNullableNumber(nodeForm.estimatedHours, toFormNumber(selectedNode.estimatedHours))
      || normalizeComparableText(nodeForm.difficultyLevel) !== normalizeComparableText(selectedNode.difficultyLevel)
    );
  }, [nodeForm.description, nodeForm.difficultyLevel, nodeForm.estimatedHours, nodeForm.title, selectedNode]);

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
    };

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
