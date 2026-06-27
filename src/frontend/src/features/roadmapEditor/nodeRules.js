export const ACTIONABLE_NODE_TYPES = new Set(["topic", "choice_option", "checkpoint", "project"]);
export const MAPPING_NODE_TYPES = new Set(["topic", "choice_option", "project"]);
export const GUIDE_NODE_TYPES = new Set(["checkpoint", "project"]);
export const STRUCTURAL_NODE_TYPES = new Set(["phase", "group", "choice_group", "resource_group"]);
export const CHILD_CONTAINER_NODE_TYPES = new Set(["phase", "group", "choice_group", "resource_group"]);
export const LEAF_NODE_TYPES = new Set(["topic", "choice_option", "checkpoint", "project"]);

export function normalizeNodeType(node) {
  const value = String(node?.nodeType || "").trim().toLowerCase();
  return value === "group" ? "resource_group" : value;
}

export function canEditLearningFields(node) {
  return ACTIONABLE_NODE_TYPES.has(normalizeNodeType(node));
}

export function canEditMappings(node) {
  return MAPPING_NODE_TYPES.has(normalizeNodeType(node));
}

export function canEditGuideMetadata(node) {
  return GUIDE_NODE_TYPES.has(normalizeNodeType(node));
}

export function canCreateChildNodes(node) {
  return CHILD_CONTAINER_NODE_TYPES.has(normalizeNodeType(node));
}

export function getAllowedChildNodeTypes(parentNode) {
  const parentType = normalizeNodeType(parentNode);

  if (parentType === "phase") {
    return ["resource_group", "project", "checkpoint"];
  }

  if (parentType === "resource_group" || parentType === "group") {
    return ["topic", "project", "checkpoint"];
  }

  if (parentType === "choice_group") {
    return ["topic", "project", "checkpoint"];
  }

  return [];
}

export function getNodeKindLabel(node) {
  const nodeType = normalizeNodeType(node);
  if (nodeType === "checkpoint") return "Checkpoint";
  if (nodeType === "project") return "Project";
  if (MAPPING_NODE_TYPES.has(nodeType)) return "Learning node";
  if (STRUCTURAL_NODE_TYPES.has(nodeType)) return "Container";
  if (node?.isTrackable) return "Actionable node";
  return "Node";
}

export function getNodeLabel(node) {
  if (!node) return "Select a node";
  return node.title || "Untitled node";
}

export function getSkillId(skill) {
  return skill?.skillId || skill?.id;
}

export function getSkillName(skill) {
  return skill?.name || skill?.title || "Untitled skill";
}

export function getResourceId(resource) {
  return resource?.resourceId || resource?.learningResourceId || resource?.id;
}

export function getResourceTitle(resource) {
  return resource?.title || resource?.name || "Untitled resource";
}
