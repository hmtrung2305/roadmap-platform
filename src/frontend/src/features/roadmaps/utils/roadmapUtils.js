import { MarkerType, Position } from "@xyflow/react";

export const NODE_WIDTH = 285;
export const NODE_HEIGHT = 102;

export const ROADMAP_LAYOUT = {
  trunkX: 1000,
  checkpointGapX: 80,
  centerToGroupGapX: 56,
  groupToOptionGapX: 52,
  rightCenterToGroupGapX: 300,
  rightGroupToOptionGapX: 52,
  hiddenColumnGapX: 430,
  minPhaseBlockHeight: 620,
  phaseBlockGapY: 210,
  groupRowGapY: 148,
  optionGapY: 164,
  checkpointGapY: 182,
  checkpointBelowPhaseOffsetY: 164,
  nodeVerticalGapY: 62,
  sideColumnOffsetY: 0,
};

export const TRUNK_X = ROADMAP_LAYOUT.trunkX;
export const CHECKPOINT_X = TRUNK_X - NODE_WIDTH - ROADMAP_LAYOUT.checkpointGapX;
export const LEFT_GROUP_X = CHECKPOINT_X - NODE_WIDTH - ROADMAP_LAYOUT.centerToGroupGapX;
export const LEFT_OPTION_X = LEFT_GROUP_X - NODE_WIDTH - ROADMAP_LAYOUT.groupToOptionGapX;
export const RIGHT_GROUP_X = TRUNK_X + NODE_WIDTH + ROADMAP_LAYOUT.rightCenterToGroupGapX;
export const RIGHT_OPTION_X = RIGHT_GROUP_X + NODE_WIDTH + ROADMAP_LAYOUT.rightGroupToOptionGapX;
export const HIDDEN_X = RIGHT_OPTION_X + NODE_WIDTH + ROADMAP_LAYOUT.hiddenColumnGapX;

export const MIN_PHASE_BLOCK_HEIGHT = ROADMAP_LAYOUT.minPhaseBlockHeight;
export const PHASE_BLOCK_GAP_Y = ROADMAP_LAYOUT.phaseBlockGapY;
export const GROUP_ROW_GAP_Y = ROADMAP_LAYOUT.groupRowGapY;
export const OPTION_GAP_Y = ROADMAP_LAYOUT.optionGapY;
export const CHECKPOINT_GAP_Y = ROADMAP_LAYOUT.checkpointGapY;

export const MANUAL_NODE_TYPES = new Set(["topic", "choice_option", "checkpoint", "project"]);
export const COMPUTED_NODE_TYPES = new Set(["phase", "choice_group", "resource_group"]);
export const VISIBLE_EDGE_TYPES = new Set(["sequence", "contains", "choice"]);



export function focusRoadmapStart(flowInstance, flowNodes, containerElement) {
  const startNode = getStartFlowNode(flowNodes);

  if (!startNode) return;

  const zoom = 0.54;
  const bounds = containerElement?.getBoundingClientRect?.();
  const viewportWidth = bounds?.width || window.innerWidth || 1200;
  const startCenterX = startNode.position.x + NODE_WIDTH / 2;
  const topPadding = 406;

  flowInstance.setViewport(
    {
      x: viewportWidth / 2 - startCenterX * zoom,
      y: topPadding - startNode.position.y * zoom,
      zoom,
    },
    { duration: 0 }
  );
}

export function getStartFlowNode(flowNodes) {
  const visibleNodes = flowNodes.filter((node) => node?.position);

  if (visibleNodes.length === 0) return null;

  const startCandidates = visibleNodes.filter((node) =>
    node.data?.nodeType === "phase" || node.data?.layoutRole === "trunk"
  );

  const candidates = startCandidates.length > 0 ? startCandidates : visibleNodes;

  return [...candidates].sort((a, b) => {
    if (a.position.y !== b.position.y) return a.position.y - b.position.y;
    return a.position.x - b.position.x;
  })[0];
}

export function mergeGraphNodeWithDetail(graphNode, detailNode) {
  const detailLearningModules =
    detailNode.learningModules ||
    detailNode.LearningModules ||
    graphNode.learningModules ||
    graphNode.LearningModules ||
    [];
  const detailResources =
    detailNode.resources ||
    detailNode.Resources ||
    graphNode.resources ||
    graphNode.Resources ||
    [];
  const detailSkills =
    detailNode.skills ||
    detailNode.Skills ||
    graphNode.skills ||
    graphNode.Skills ||
    [];
  const detailLearningOutcomes =
    detailNode.learningOutcomes ||
    detailNode.LearningOutcomes ||
    graphNode.learningOutcomes ||
    graphNode.LearningOutcomes ||
    [];
  const detailCompletionCriteria =
    detailNode.completionCriteria ||
    detailNode.CompletionCriteria ||
    graphNode.completionCriteria ||
    graphNode.CompletionCriteria ||
    [];
  const detailMetadata =
    detailNode.metadata ??
    detailNode.Metadata ??
    graphNode.metadata ??
    graphNode.Metadata ??
    null;

  return {
    ...graphNode,
    ...detailNode,
    metadata: detailMetadata,
    learningModules: detailLearningModules,
    resources: detailResources,
    skills: detailSkills,
    learningOutcomes: detailLearningOutcomes,
    completionCriteria: detailCompletionCriteria,
    progress: {
      ...(graphNode.progress || {}),
      ...(detailNode.progress || {}),
    },
  };
}

export function applyProgressUpdateResult(roadmap, result) {
  if (!roadmap || !result) return roadmap;

  const changedNodes = result.changedNodes || result.changedProgress || [];
  const changedByNodeId = new Map(
    changedNodes
      .map((progress) => [String(progress.roadmapNodeId || progress.nodeId || progress.id), progress])
      .filter(([nodeId]) => nodeId && nodeId !== "undefined")
  );

  return {
    ...roadmap,
    enrollment: result.enrollment || roadmap.enrollment,
    trackableNodeCount: result.trackableNodeCount ?? roadmap.trackableNodeCount,
    completedNodeCount: result.completedNodeCount ?? roadmap.completedNodeCount,
    progressPercent: result.progressPercent ?? roadmap.progressPercent,
    nodes: (roadmap.nodes || []).map((node) => {
      const nodeId = getNodeId(node);
      const changedProgress = changedByNodeId.get(nodeId);

      if (!changedProgress) return node;

      return {
        ...node,
        progress: {
          ...(node.progress || {}),
          ...changedProgress,
        },
      };
    }),
  };
}

export function findChangedProgress(result, nodeId) {
  const changedNodes = result?.changedNodes || result?.changedProgress || [];
  const targetNodeId = String(nodeId);

  return changedNodes.find((progress) =>
    String(progress.roadmapNodeId || progress.nodeId || progress.id) === targetNodeId
  );
}

export function patchCachedNodeProgress(cache, result) {
  if (!cache || !result) return cache;

  const changedNodes = result.changedNodes || result.changedProgress || [];

  if (changedNodes.length === 0) {
    return cache;
  }

  const nextCache = { ...cache };

  changedNodes.forEach((progress) => {
    const nodeId = String(progress.roadmapNodeId || progress.nodeId || progress.id);

    if (!nodeId || !nextCache[nodeId]) return;

    nextCache[nodeId] = {
      ...nextCache[nodeId],
      progress: {
        ...(nextCache[nodeId].progress || {}),
        ...progress,
      },
    };
  });

  return nextCache;
}

export function getBalancedEdgePath(sourceX, sourceY, targetX, targetY, sourcePosition) {
  const sameRow = Math.abs(sourceY - targetY) < 8;
  const sameColumn = Math.abs(sourceX - targetX) < 8;

  if (sameRow || sourcePosition === Position.Right || sourcePosition === Position.Left) {
    const midX = sourceX + (targetX - sourceX) / 2;

    return [
      `M ${sourceX},${sourceY}`,
      `L ${midX},${sourceY}`,
      `L ${midX},${targetY}`,
      `L ${targetX},${targetY}`,
    ].join(" ");
  }

  if (sameColumn || sourcePosition === Position.Bottom || sourcePosition === Position.Top) {
    const midY = sourceY + (targetY - sourceY) / 2;

    return [
      `M ${sourceX},${sourceY}`,
      `L ${sourceX},${midY}`,
      `L ${targetX},${midY}`,
      `L ${targetX},${targetY}`,
    ].join(" ");
  }

  const midX = sourceX + (targetX - sourceX) / 2;

  return [
    `M ${sourceX},${sourceY}`,
    `L ${midX},${sourceY}`,
    `L ${midX},${targetY}`,
    `L ${targetX},${targetY}`,
  ].join(" ");
}

export function groupResourcesByType(resources) {
  const grouped = new Map();

  resources.forEach((resource) => {
    const type = resource.resourceType || resource.type || "link";
    const label = splitCamelCase(String(type)).trim() || "Resource";
    const key = label.toUpperCase();
    const list = grouped.get(key) || [];
    list.push(resource);
    grouped.set(key, list);
  });

  return [...grouped.entries()].map(([type, groupedResources]) => ({
    type,
    resources: groupedResources,
  }));
}

export function getResourceUrl(resource) {
  return resource.url || resource.link || resource.href || resource.htmlUrl || resource.fileUrl || null;
}

export function formatResourceDuration(value) {
  if (typeof value === "number") return `${value} min`;
  return String(value);
}

export function formatStatusLabel(status) {
  if (!status || status === "pending" || status === "available") return "Available";
  return formatReadableLabel(status);
}

export function formatNodeTypeLabel(nodeType) {
  return formatReadableLabel(nodeType || "node");
}

export function formatReadableLabel(value) {
  if (value == null) return "";

  return String(value)
    .replace(/_/g, " ")
    .replace(/-/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .trim()
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

export function formatResourceType(value) {
  const normalized = String(value || "link").toLowerCase();

  const labels = {
    documentation: "Documentation",
    official_docs: "Official Docs",
    course: "Course",
    youtube: "YouTube",
    video: "Video",
    article: "Article",
    tutorial: "Tutorial",
    book: "Book",
    practice: "Practice",
    repository: "Repository",
  };

  return labels[normalized] || formatReadableLabel(value);
}

export function formatSkillLabel(skill) {
  if (!skill || typeof skill !== "object") return getDisplayText(skill);

  const name = skill.name || skill.title || skill.label || "Skill";
  const category = skill.category || skill.skillCategory || null;

  return category ? `${category}: ${name}` : name;
}

export function getProgressMessage(status, isComputedNode) {
  if (isComputedNode) return "Computed from child nodes.";
  if (status === "locked") return "Finish the prerequisite work first.";
  if (status === "completed") return "This item is completed.";
  if (status === "in_progress") return "Continue working on this item.";
  if (status === "skipped") return "This optional item was skipped.";
  return "This item is available but not started.";
}

export function getProgressSummary(node) {
  const completed = node.completedCount ?? node.completedUnits ?? node.completedRequiredCount ?? null;
  const total = node.totalCount ?? node.totalUnits ?? node.requiredCountTotal ?? node.requiredTrackableCount ?? null;
  const required = node.requiredCount ?? node.selectionRequiredCount ?? null;

  if (completed != null && total != null) return `${completed} / ${total} completed`;

  if (node.nodeType === "choice_group" && required != null) {
    return `Rule target: ${required} required option${required === 1 ? "" : "s"}`;
  }

  return null;
}

export function getParentChoiceRule(node) {
  return node.parentChoiceRule || node.parentRule || node.choiceRule || null;
}

export function getLockedReason(node) {
  return node.lockedReason || node.progress?.reason || node.progress?.lockedReason || null;
}

export function getUsefulMetadata(metadata) {
  if (!metadata || typeof metadata !== "object") return [];

  const keys = [
    ["deliverables", "Deliverables"],
    ["tools", "Tools"],
    ["recommendedBefore", "Recommended before"],
    ["warning", "Warning"],
    ["estimatedMinutes", "Estimated time"],
  ];

  return keys
    .filter(([key]) => metadata[key] != null && metadata[key] !== "")
    .map(([key, label]) => ({
      label,
      value: key === "estimatedMinutes" ? formatResourceDuration(metadata[key]) : metadata[key],
    }));
}

export function getSortedResources(resources) {
  return [...resources].sort((a, b) => {
    const orderA = a.orderIndex ?? a.order ?? 999;
    const orderB = b.orderIndex ?? b.order ?? 999;
    return orderA - orderB;
  });
}

export function normalizeRoadmap(data) {
  return {
    ...data,
    nodes: data.nodes || data.roadmapNodes || data.roadmapVersionNodes || [],
    edges: data.edges || data.roadmapEdges || data.dependencies || [],
    layoutDirection: data.layoutDirection || "TB",
    layoutAlgorithm: data.layoutAlgorithm || "custom",
  };
}

export function buildRoadmapFlow(roadmap, selectedNodeId) {
  const sourceNodes = roadmap.nodes || [];
  const sourceEdges = roadmap.edges || [];
  const nodeLookup = new Map();

  sourceNodes.forEach((node) => {
    nodeLookup.set(getNodeId(node), node);
  });

  const nodesById = toNodeMap(sourceNodes);
  const normalizedEdges = sourceEdges.length > 0 ? sourceEdges : buildFallbackParentEdges(sourceNodes);
  const positionMap = calculateSchemaAwarePositions(sourceNodes, normalizedEdges);
  const flowEdges = buildFlowEdges(normalizedEdges, positionMap, nodesById);
  const flowNodes = sourceNodes.map((node) => {
    const nodeId = getNodeId(node);
    const fallbackPosition = positionMap.get(nodeId) || { x: 0, y: 0 };

    return toFlowNode(node, fallbackPosition.x, fallbackPosition.y, selectedNodeId);
  });

  return {
    flowNodes,
    flowEdges,
    nodeLookup,
  };
}

export function isCenterSpineChildNode(node) {
  const nodeType = getNodeType(node);
  const layoutRole = getLayoutRole(node);
  const checkpointType = node?.checkpointType || node?.checkpoint_type || null;
  const isRequired = node?.isRequired ?? node?.is_required ?? false;

  if (nodeType === "phase") return true;
  if (nodeType === "checkpoint") return true;
  if (layoutRole === "checkpoint" || layoutRole === "gate" || layoutRole === "validation") return true;
  if (checkpointType === "gate" || checkpointType === "assessment" || checkpointType === "review") return true;

  if (nodeType === "project") {
    return isRequired === true || layoutRole === "required_project" || layoutRole === "validation_project";
  }

  return false;
}

export function isSideContentNode(node) {
  return !isCenterSpineChildNode(node);
}

export function calculateSchemaAwarePositions(sourceNodes, sourceEdges) {
  const positions = new Map();
  const nodesById = toNodeMap(sourceNodes);
  const childrenByParent = buildChildrenByParent(sourceNodes, sourceEdges);
  const choiceChildrenByParent = buildChildrenByEdgeType(sourceEdges, "choice");
  const nestedChildrenByParent = buildNestedChildrenByParent(sourceNodes, sourceEdges);
  const trunkNodes = getOrderedTrunkNodes(sourceNodes, sourceEdges, nodesById);

  let phaseStartY = 0;

  trunkNodes.forEach((trunkNode) => {
    const trunkId = getNodeId(trunkNode);
    const children = getSortedChildren(childrenByParent.get(trunkId) || [])
      .filter((child) => getNodeId(child) !== trunkId);

    const centerSpineChildren = children.filter(isCenterSpineChildNode);
    const sideChildren = children.filter((child) => !centerSpineChildren.includes(child));
    const sideColumns = splitSideChildrenIntoColumns(sideChildren, choiceChildrenByParent, nestedChildrenByParent);
    const phaseHeight = calculatePhaseBlockHeight(
      sideColumns,
      centerSpineChildren,
      choiceChildrenByParent,
      nestedChildrenByParent
    );

    const trunkY = phaseStartY + phaseHeight / 2 - NODE_HEIGHT / 2;

    positions.set(trunkId, { x: TRUNK_X, y: trunkY });

    placeCenterSpineChildren(centerSpineChildren, phaseStartY, phaseHeight, positions);
    placeSideChildren(
      sideColumns,
      phaseStartY,
      phaseHeight,
      positions,
      choiceChildrenByParent,
      nestedChildrenByParent,
      nodesById
    );

    phaseStartY += phaseHeight + PHASE_BLOCK_GAP_Y;
  });

  const unplacedNodes = sourceNodes.filter((node) => !positions.has(getNodeId(node)));
  unplacedNodes
    .sort((a, b) => getLayoutSortValue(a) - getLayoutSortValue(b))
    .forEach((node, index) => {
      positions.set(getNodeId(node), {
        x: HIDDEN_X,
        y: phaseStartY + index * (NODE_HEIGHT + GROUP_ROW_GAP_Y),
      });
    });

  resolveColumnCollisions(sourceNodes, positions);

  return positions;
}

export function resolveColumnCollisions(sourceNodes, positions) {
  const columns = new Map();

  sourceNodes.forEach((node) => {
    const nodeId = getNodeId(node);
    const position = positions.get(nodeId);

    if (!position) return;

    const columnKey = Math.round(position.x / 20) * 20;
    const list = columns.get(columnKey) || [];

    list.push({ node, nodeId, position });
    columns.set(columnKey, list);
  });

  columns.forEach((columnNodes) => {
    columnNodes.sort((a, b) => {
      if (a.position.y !== b.position.y) return a.position.y - b.position.y;
      return getLayoutSortValue(a.node) - getLayoutSortValue(b.node);
    });

    let previousBottom = null;

    columnNodes.forEach((item) => {
      const minimumY = previousBottom == null
        ? item.position.y
        : previousBottom + ROADMAP_LAYOUT.nodeVerticalGapY;
      const nextY = Math.max(item.position.y, minimumY);

      positions.set(item.nodeId, {
        ...item.position,
        y: nextY,
      });

      previousBottom = nextY + NODE_HEIGHT;
    });
  });
}

export function calculatePhaseBlockHeight(sideColumns, centerSpineChildren, choiceChildrenByParent, nestedChildrenByParent = new Map()) {
  const leftHeight = calculateSideColumnHeight(sideColumns.left, choiceChildrenByParent, nestedChildrenByParent);
  const rightHeight = calculateSideColumnHeight(sideColumns.right, choiceChildrenByParent, nestedChildrenByParent);

  const centerSpineHeight = centerSpineChildren.length > 0
    ? ROADMAP_LAYOUT.checkpointBelowPhaseOffsetY +
      calculateStackHeight(centerSpineChildren.map(() => NODE_HEIGHT), CHECKPOINT_GAP_Y - NODE_HEIGHT)
    : 0;

  return Math.max(MIN_PHASE_BLOCK_HEIGHT, leftHeight, rightHeight, centerSpineHeight);
}

export function calculateSideColumnHeight(children, choiceChildrenByParent, nestedChildrenByParent = new Map()) {
  return calculateStackHeight(
    children.map((child) => getSideRowHeight(child, choiceChildrenByParent, nestedChildrenByParent)),
    GROUP_ROW_GAP_Y
  );
}

export function splitSideChildrenIntoColumns(sideChildren, choiceChildrenByParent, nestedChildrenByParent = new Map()) {
  const columns = { left: [], right: [] };
  const heights = { left: 0, right: 0 };

  getSortedChildren(sideChildren).forEach((child) => {
    const preferredSide = getPreferredSide(child);
    const rowHeight = getSideRowHeight(child, choiceChildrenByParent, nestedChildrenByParent);
    const side = preferredSide || (heights.left <= heights.right ? "left" : "right");

    columns[side].push(child);
    heights[side] += rowHeight + GROUP_ROW_GAP_Y;
  });

  return columns;
}

export function getPreferredSide(node) {
  const value = node.layoutSide || node.layout_side || node.side || null;

  if (value === "left" || node.layoutRole === "side_left") return "left";
  if (value === "right" || node.layoutRole === "side_right") return "right";

  return null;
}

export function getSideRowHeight(child, choiceChildrenByParent, nestedChildrenByParent = new Map()) {
  const optionCount = getNestedChildIds(child, choiceChildrenByParent, nestedChildrenByParent).length;

  if (optionCount === 0) {
    return NODE_HEIGHT;
  }

  return Math.max(NODE_HEIGHT, NODE_HEIGHT + (optionCount - 1) * OPTION_GAP_Y);
}

export function calculateStackHeight(rowHeights, gapY) {
  if (rowHeights.length === 0) return 0;

  return rowHeights.reduce((sum, height) => sum + height, 0) +
    Math.max(0, rowHeights.length - 1) * gapY;
}

export function placeCenterSpineChildren(centerSpineChildren, phaseStartY, phaseHeight, positions) {
  if (centerSpineChildren.length === 0) return;

  const phaseCenterY = phaseStartY + phaseHeight / 2;
  let cursorY = phaseCenterY + ROADMAP_LAYOUT.checkpointBelowPhaseOffsetY;

  centerSpineChildren.forEach((node) => {
    positions.set(getNodeId(node), {
      x: TRUNK_X,
      y: cursorY,
    });

    cursorY += CHECKPOINT_GAP_Y;
  });
}

export function placeSideChildren(sideColumns, phaseStartY, phaseHeight, positions, choiceChildrenByParent, nestedChildrenByParent, nodesById) {
  placeSideColumn(
    sideColumns.left,
    "left",
    phaseStartY,
    phaseHeight,
    positions,
    choiceChildrenByParent,
    nestedChildrenByParent,
    nodesById
  );

  placeSideColumn(
    sideColumns.right,
    "right",
    phaseStartY,
    phaseHeight,
    positions,
    choiceChildrenByParent,
    nestedChildrenByParent,
    nodesById
  );
}

export function getSideColumnOffsetY(children, side, phaseHeight, totalHeight) {
  if (children.length !== 1) return 0;

  const maxOffset = Math.max(0, (phaseHeight - totalHeight) / 2 - NODE_HEIGHT);
  const offset = Math.min(ROADMAP_LAYOUT.sideColumnOffsetY, maxOffset);

  return side === "left" ? -offset : offset;
}

export function placeSideColumn(children, side, phaseStartY, phaseHeight, positions, choiceChildrenByParent, nestedChildrenByParent, nodesById) {
  if (children.length === 0) return;

  const rowHeights = children.map((child) => getSideRowHeight(child, choiceChildrenByParent, nestedChildrenByParent));
  const totalHeight = calculateStackHeight(rowHeights, GROUP_ROW_GAP_Y);
  const offsetY = getSideColumnOffsetY(children, side, phaseHeight, totalHeight);
  let cursorY = phaseStartY + phaseHeight / 2 - totalHeight / 2 + offsetY;
  const groupX = side === "left" ? LEFT_GROUP_X : RIGHT_GROUP_X;

  children.forEach((child, index) => {
    const childId = getNodeId(child);
    const rowHeight = rowHeights[index];
    const rowCenterY = cursorY + rowHeight / 2;
    const childY = rowCenterY - NODE_HEIGHT / 2;

    positions.set(childId, {
      x: child.layoutRole === "hidden" ? HIDDEN_X : groupX,
      y: childY,
    });

    const nestedChildren = getSortedChildren(
      getNestedChildIds(child, choiceChildrenByParent, nestedChildrenByParent)
        .map((id) => nodesById.get(id))
        .filter(Boolean)
    );

    placeChoiceChildren(nestedChildren, rowCenterY, positions, side);

    cursorY += rowHeight + GROUP_ROW_GAP_Y;
  });
}

export function placeChoiceChildren(choiceChildren, rowCenterY, positions, side) {
  if (choiceChildren.length === 0) return;

  const optionX = side === "left" ? LEFT_OPTION_X : RIGHT_OPTION_X;
  const startY = rowCenterY - ((choiceChildren.length - 1) * OPTION_GAP_Y) / 2 - NODE_HEIGHT / 2;

  choiceChildren.forEach((option, optionIndex) => {
    positions.set(getNodeId(option), {
      x: optionX,
      y: startY + optionIndex * OPTION_GAP_Y,
    });
  });
}

export function buildFlowEdges(sourceEdges, positionMap, nodesById) {
  return sourceEdges
    .map((edge) => {
      const source = getEdgeFromNodeId(edge);
      const target = getEdgeToNodeId(edge);

      if (!source || !target || source === target) return null;

      const edgeType = getEdgeType(edge);
      const sourceNode = nodesById?.get(source) || null;
      const targetNode = nodesById?.get(target) || null;

      if (!shouldRenderEdge(edge, sourceNode, targetNode)) {
        return null;
      }

      const handles = getEdgeHandlesForType(edgeType, source, target, positionMap);
      const style = getEdgeStyle(edgeType, edge.dependencyType);

      return {
        id: edge.roadmapEdgeId || edge.id || `edge-${edgeType}-${source}-${target}`,
        source,
        target,
        sourceHandle: handles.sourceHandle,
        targetHandle: handles.targetHandle,
        type: "balanced",
        markerEnd: edgeType === "sequence" ? { type: MarkerType.ArrowClosed } : undefined,
        style,
        data: {
          edgeType,
          dependencyType: edge.dependencyType,
        },
      };
    })
    .filter(Boolean);
}

export function shouldRenderEdge(edge, sourceNode, targetNode) {
  const edgeType = getEdgeType(edge);

  if (!VISIBLE_EDGE_TYPES.has(edgeType)) {
    return false;
  }

  const metadata = parseEdgeMetadata(edge);

  if (metadata.isVisual === false || metadata.visible === false || metadata.render === false) {
    return false;
  }

  if (edgeType === "sequence") {
    return isMainTrunkNode(sourceNode) && isMainTrunkNode(targetNode);
  }

  if (edgeType === "contains") {
    const sourceId = sourceNode ? getNodeId(sourceNode) : null;
    const targetParentId = targetNode ? getParentNodeId(targetNode) : null;

    return Boolean(sourceId && targetParentId === sourceId);
  }

  if (edgeType === "choice") {
    return getNodeType(sourceNode) === "choice_group";
  }

  return false;
}

export function parseEdgeMetadata(edge) {
  return parseMetadata(edge?.condition || edge?.metadata || edge?.data || {});
}

export function getNodeType(node) {
  return node?.nodeType || node?.node_type || null;
}

export function getLayoutRole(node) {
  return node?.layoutRole || node?.layout_role || null;
}

export function isMainTrunkNode(node) {
  return getNodeType(node) === "phase" || getLayoutRole(node) === "trunk";
}

export function getOrderedTrunkNodes(sourceNodes, sourceEdges, nodesById) {
  const explicitTrunkNodes = sourceNodes.filter(
    (node) => node.layoutRole === "trunk" || node.nodeType === "phase"
  );

  const sequenceEdges = sourceEdges.filter((edge) => {
    if (getEdgeType(edge) !== "sequence") return false;

    const sourceNode = nodesById.get(getEdgeFromNodeId(edge));
    const targetNode = nodesById.get(getEdgeToNodeId(edge));

    return isMainTrunkNode(sourceNode) && isMainTrunkNode(targetNode);
  });
  const sequenceNodeIds = new Set();
  const outgoing = new Map();
  const incoming = new Map();

  sequenceEdges.forEach((edge) => {
    const from = getEdgeFromNodeId(edge);
    const to = getEdgeToNodeId(edge);
    if (!from || !to) return;

    sequenceNodeIds.add(from);
    sequenceNodeIds.add(to);
    outgoing.set(from, to);
    incoming.set(to, from);
  });

  const sequenceStarts = [...sequenceNodeIds].filter((id) => !incoming.has(id));
  const ordered = [];
  const visited = new Set();

  sequenceStarts.forEach((startId) => {
    let currentId = startId;

    while (currentId && !visited.has(currentId)) {
      const node = nodesById.get(currentId);
      if (node && (node.layoutRole === "trunk" || node.nodeType === "phase")) {
        ordered.push(node);
      }

      visited.add(currentId);
      currentId = outgoing.get(currentId);
    }
  });

  const remainingTrunkNodes = explicitTrunkNodes
    .filter((node) => !visited.has(getNodeId(node)))
    .sort((a, b) => getLayoutSortValue(a) - getLayoutSortValue(b));

  return [...ordered, ...remainingTrunkNodes];
}

export function buildChildrenByParent(sourceNodes, sourceEdges) {
  const nodesById = toNodeMap(sourceNodes);
  const childrenByParent = new Map();

  sourceNodes.forEach((node) => {
    const parentId = getParentNodeId(node);
    if (!parentId) return;

    const list = childrenByParent.get(parentId) || [];
    list.push(node);
    childrenByParent.set(parentId, list);
  });

  sourceEdges
    .filter((edge) => getEdgeType(edge) === "contains")
    .forEach((edge) => {
      const parentId = getEdgeFromNodeId(edge);
      const childId = getEdgeToNodeId(edge);
      const child = nodesById.get(childId);

      if (!parentId || !child) return;

      const list = childrenByParent.get(parentId) || [];
      if (!list.some((existing) => getNodeId(existing) === childId)) {
        list.push(child);
      }
      childrenByParent.set(parentId, list);
    });

  return childrenByParent;
}

export function buildChildrenByEdgeType(sourceEdges, edgeType) {
  const result = new Map();

  sourceEdges
    .filter((edge) => getEdgeType(edge) === edgeType)
    .forEach((edge) => {
      const parentId = getEdgeFromNodeId(edge);
      const childId = getEdgeToNodeId(edge);

      if (!parentId || !childId) return;

      const list = result.get(parentId) || [];
      if (!list.includes(childId)) list.push(childId);
      result.set(parentId, list);
    });

  return result;
}

export function buildNestedChildrenByParent(sourceNodes, sourceEdges) {
  const childrenByParent = buildChildrenByParent(sourceNodes, sourceEdges);
  const trunkNodeIds = new Set(sourceNodes
    .filter((node) => getNodeType(node) === "phase" || getLayoutRole(node) === "trunk")
    .map(getNodeId));
  const result = new Map();

  childrenByParent.forEach((children, parentId) => {
    if (!trunkNodeIds.has(parentId)) {
      result.set(parentId, children.map(getNodeId));
    }
  });

  return result;
}

export function getNestedChildIds(child, choiceChildrenByParent, nestedChildrenByParent = new Map()) {
  const childId = getNodeId(child);
  const choiceChildren = choiceChildrenByParent.get(childId) || [];
  if (choiceChildren.length > 0) return choiceChildren;

  return nestedChildrenByParent.get(childId) || [];
}

export function buildFallbackParentEdges(sourceNodes) {
  return sourceNodes
    .filter((node) => getParentNodeId(node))
    .map((node) => ({
      fromNodeId: getParentNodeId(node),
      toNodeId: getNodeId(node),
      edgeType: node.nodeType === "choice_option" ? "choice" : "contains",
      dependencyType: "required",
    }));
}

export function toFlowNode(node, x, y, selectedNodeId) {
  const nodeId = getNodeId(node);
  const status = node.progress?.status || "pending";

  return {
    id: nodeId,
    type: "roadmapNode",
    position: { x, y },
    width: NODE_WIDTH,
    height: NODE_HEIGHT,
    data: {
      slug: node.slug,
      title: node.title,
      nodeType: node.nodeType,
      checkpointType: node.checkpointType,
      layoutRole: node.layoutRole,
      selectionType: node.selectionType,
      requiredCount: node.requiredCount,
      status,
      isSelected: selectedNodeId === nodeId,
    },
  };
}

export function findInitialSelectedNode(nodes) {
  return (
    nodes.find((node) => node.progress?.status === "in_progress") ||
    nodes.find((node) => node.progress?.status === "pending" && isManuallyTrackableNode(node)) ||
    nodes.find((node) => isManuallyTrackableNode(node)) ||
    nodes[0] ||
    null
  );
}

export function patchNodeProgress(roadmap, nodeId, status) {
  if (!roadmap) return roadmap;

  return {
    ...roadmap,
    nodes: roadmap.nodes.map((node) =>
      getNodeId(node) === nodeId
        ? {
            ...node,
            progress: {
              ...(node.progress || {}),
              status,
            },
          }
        : node
    ),
  };
}

export function getNodeId(node) {
  return String(node.roadmapNodeId || node.roadmapNodeID || node.nodeId || node.id);
}

export function getParentNodeId(node) {
  const value = node.parentNodeId || node.parentNodeID || node.parentId || null;
  return value ? String(value) : null;
}

export function getEdgeFromNodeId(edge) {
  const value = edge.fromNodeId || edge.from_node_id || edge.source || edge.from || null;
  return value ? String(value) : null;
}

export function getEdgeToNodeId(edge) {
  const value = edge.toNodeId || edge.to_node_id || edge.target || edge.to || null;
  return value ? String(value) : null;
}

export function getEdgeType(edge) {
  return edge.edgeType || edge.edge_type || edge.type || "dependency";
}

export function getLayoutSortValue(node) {
  return node.orderIndex ?? node.order ?? 0;
}

export function getSortedChildren(children) {
  return [...children].sort((a, b) => getLayoutSortValue(a) - getLayoutSortValue(b));
}

export function toNodeMap(nodes) {
  const map = new Map();
  nodes.forEach((node) => map.set(getNodeId(node), node));
  return map;
}

export function getEdgeHandlesForType(edgeType, sourceId, targetId, positionMap) {
  if (edgeType === "sequence") {
    return { sourceHandle: "bottom-source", targetHandle: "top-target" };
  }

  const sourcePosition = positionMap?.get(sourceId);
  const targetPosition = positionMap?.get(targetId);

  if (sourcePosition && targetPosition) {
    const deltaX = targetPosition.x - sourcePosition.x;
    const deltaY = targetPosition.y - sourcePosition.y;

    if (Math.abs(deltaX) < NODE_WIDTH / 2 && deltaY >= 0) {
      return { sourceHandle: "bottom-source", targetHandle: "top-target" };
    }

    if (targetPosition.x < sourcePosition.x) {
      return { sourceHandle: "left-source", targetHandle: "right-target" };
    }
  }

  return { sourceHandle: "right-source", targetHandle: "left-target" };
}

export function getEdgeStyle(edgeType, dependencyType) {
  if (edgeType === "sequence") {
    return { stroke: "#2563EB", strokeWidth: 4 };
  }

  if (edgeType === "contains") {
    return { stroke: "#2FA084", strokeWidth: 2.5, strokeDasharray: "7 7" };
  }

  if (edgeType === "choice") {
    return { stroke: "#7C3AED", strokeWidth: 2.5, strokeDasharray: "5 5" };
  }

  if (edgeType === "unlock") {
    return { stroke: "#16A34A", strokeWidth: 2.5, strokeDasharray: "8 6" };
  }

  if (edgeType === "dependency" || dependencyType === "required") {
    return { stroke: "#475569", strokeWidth: 2.35, strokeDasharray: "6 6" };
  }

  if (edgeType === "recommendation" || dependencyType === "recommended") {
    return { stroke: "#64748B", strokeWidth: 2, strokeDasharray: "6 6" };
  }

  return { stroke: "#2563EB", strokeWidth: 2.5 };
}

export function formatSelectionRule(node) {
  if (!node.selectionType) return "—";

  if (node.selectionType === "choose_one") return "Choose 1 option";
  if (node.selectionType === "choose_many") {
    return `Choose ${node.requiredCount || 1} option(s)`;
  }
  if (node.selectionType === "complete_all") return "Complete all required items";

  return node.selectionType.replaceAll("_", " ");
}

export function formatSelectionText(selectionType, requiredCount) {
  if (selectionType === "choose_one") return "choose 1";
  if (selectionType === "choose_many") return `choose ${requiredCount || 1}`;
  if (selectionType === "complete_all") return "complete all";
  return "group";
}

export function isManuallyTrackableNode(node) {
  return MANUAL_NODE_TYPES.has(node.nodeType) && node.isTrackable !== false;
}

export function isComputedNodeType(nodeType) {
  return COMPUTED_NODE_TYPES.has(nodeType);
}

export function getDisplayText(value) {
  if (value == null) return "";
  if (typeof value === "string") return value;
  if (typeof value === "number") return String(value);

  return value.name || value.title || value.text || value.description || value.label || "Untitled";
}

export function getDisplayKey(value, index) {
  if (value == null) return index;

  if (typeof value === "string" || typeof value === "number") {
    return String(value);
  }

  return value.skillId || value.resourceId || value.id || value.slug || value.name || value.title || index;
}

function getCompletedTypeColor(nodeType) {
  if (nodeType === "phase") return "#028C7C";
  if (nodeType === "choice_group") return "#82CFAE";
  if (nodeType === "choice_option" || nodeType === "topic" || nodeType === "option" || nodeType === "skill") return "#D8C98F";
  if (nodeType === "checkpoint" || nodeType === "project") return "#D7A16E";
  if (nodeType === "resource_group") return "#C9D99A";
  return "#D1D5DB";
}

export function getStatusColor(status, nodeType) {
  switch (status) {
    case "completed":
      return getCompletedTypeColor(nodeType);
    case "in_progress":
      if (nodeType === "choice_group") return "#24B1B1";
      return "#03A791";
    case "locked":
      return "#64748B";
    case "skipped":
      return "#4F6F73";
    default:
      if (nodeType === "phase") return "#03A791";
      if (nodeType === "choice_group") return "#A8EFCB";
      return "#FBBF24";
  }
}

export function getMiniMapColor(status, nodeType) {
  if (status === "completed") {
    return getCompletedTypeColor(nodeType);
  }
  if (status === "skipped") return "#4F6F73";
  if (status === "locked") {
    if (nodeType === "phase" || nodeType === "choice_group") return "#E5E7EB";
    if (nodeType === "choice_option" || nodeType === "topic") return "#FFE08A";
    if (nodeType === "project") return "#F1BA88";
    if (nodeType === "resource_group") return "#E9F5BE";
    return "#FFE08A";
  }
  if (status === "in_progress") return nodeType === "choice_group" ? "#24B1B1" : getMiniMapColor(null, nodeType);

  if (nodeType === "phase") return "#03A791";
  if (nodeType === "choice_group") return "#A8EFCB";
  if (nodeType === "choice_option" || nodeType === "topic") return "#FFE08A";
  if (nodeType === "project") return "#F1BA88";
  if (nodeType === "resource_group") return "#E9F5BE";

  return "#FFE08A";
}

export function getNodeTypeClass(nodeType, checkpointType) {
  if (nodeType === "phase") return "border-[#028C7C] bg-[#03A791] text-white";
  if (nodeType === "choice_group") return "border-[#7FDDB6] bg-[#A8EFCB] text-[#18332D] hover:bg-[#A8EFCB]";
  if (nodeType === "choice_option") return "border-[#E9C85F] bg-[#FFE08A] text-[#18332D] hover:bg-[#FFD975]";
  if (nodeType === "topic") return "border-[#E9C85F] bg-[#FFE08A] text-[#18332D] hover:bg-[#FFD975]";
  if (nodeType === "project") return "border-[#E6A66D] bg-[#F1BA88] text-[#18332D] hover:bg-[#EEAE78]";
  if (nodeType === "resource_group") return "border-[#CFE79B] bg-[#E9F5BE] text-[#18332D] hover:bg-[#E0F0AA]";

  if (nodeType === "checkpoint") {
    if (checkpointType === "gate") return "border-[#55C98E] bg-[#81E7AF] text-[#18332D] hover:bg-[#74DEA4]";
    if (checkpointType === "assessment") return "border-[#E9C85F] bg-[#FFE08A] text-[#18332D] hover:bg-[#FFD975]";
    if (checkpointType === "review") return "border-[#E6A66D] bg-[#F1BA88] text-[#18332D] hover:bg-[#EEAE78]";
    if (checkpointType === "project") return "border-[#E6A66D] bg-[#F1BA88] text-[#18332D] hover:bg-[#EEAE78]";
    return "border-[#B9D8CC] bg-[#F8FAFC] text-[#18332D] hover:bg-[#F1F5F9]";
  }

  return "border-[#E9C85F] bg-[#FFE08A] text-[#18332D] hover:bg-[#FFD975]";
}

export function parseMetadata(metadata) {
  if (!metadata) return {};

  if (typeof metadata === "string") {
    try {
      return JSON.parse(metadata);
    } catch {
      return {};
    }
  }

  if (typeof metadata === "object") return metadata;

  return {};
}

export function formatMetadataValue(value) {
  if (value == null) return "—";
  if (Array.isArray(value)) return value.join(", ");
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

export function splitCamelCase(value) {
  return value.replace(/([A-Z])/g, " $1").replace(/_/g, " ");
}
