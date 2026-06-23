export const LEARNING_NODE_TYPES = new Set(["topic", "project"]);
export const STRUCTURAL_NODE_TYPES = new Set(["phase", "group", "choice_group", "resource_group"]);

export function normalizeNodeType(node) {
  return String(node?.nodeType || "").trim().toLowerCase();
}

export function canEditLearningFields(node) {
  return LEARNING_NODE_TYPES.has(normalizeNodeType(node));
}

export function canEditMappings(node) {
  return canEditLearningFields(node);
}

export function getNodeKindLabel(node) {
  if (canEditLearningFields(node)) return "Learning node";
  if (STRUCTURAL_NODE_TYPES.has(normalizeNodeType(node))) return "Container";
  if (node?.isTrackable) return "Trackable node";
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
