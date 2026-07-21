const RESOURCE_LABELS = {
  account: "Account",
  auth_provider: "Auth Providers",
  profile: "Profile",
  portfolio: "Portfolio",
  repository: "Repositories",
  repo_insight: "Repository Insights",
  ai_credit: "AI Credit",
  streak: "Streak",
  roadmap: "Roadmaps",
  roadmap_node: "Roadmap Nodes",
  roadmap_enrollment: "Roadmap Enrollment",
  roadmap_progress: "Roadmap Progress",
  learning_module: "Learning Modules",
  learning_module_enrollment: "Module Enrollment",
  learning_module_lesson: "Module Lessons",
  learning_module_progress: "Module Progress",
  learning_module_quiz: "Module Quizzes",
  learning_module_quiz_attempt: "Quiz Attempts",
  learning_module_quiz_question: "Quiz Questions",
  learning_module_chat: "Module AI Chat",
  career_role: "Career Roles",
  skill_gap_analysis: "Skill Gap Analysis",
  skill_gap_analysis_history: "Skill Gap History",
  skill_gap_config: "Skill Gap Config",
  market_pulse: "Market Pulse",
  skill: "Skills",
  user: "Users",
  role: "Roles",
  permission: "Permissions",
  role_permission: "Role Permissions",
  user_role: "User Roles",
  system_health: "System Health",
};

const ACTION_LABELS = {
  archive: "Archive",
  assign: "Assign",
  create: "Create",
  delete: "Delete",
  generate: "Generate",
  link: "Link",
  manage: "Manage",
  preview: "Preview",
  publish: "Publish",
  reorder: "Reorder",
  reindex: "Reindex",
  restore: "Restore",
  revoke: "Revoke",
  submit: "Submit",
  suspend: "Suspend",
  sync: "Sync",
  track: "Track",
  unlink: "Unlink",
  update: "Update",
  upsert: "Create or update",
  use: "Use",
  view: "View",
};

const SCOPE_LABELS = {
  any: "platform-wide",
  catalog: "catalog",
  enrolled: "enrolled content",
  own: "owned content",
  published: "published content",
  self: "own account",
};

function humanizeToken(value) {
  return String(value || "")
    .replaceAll("_", " ")
    .replace(/\b\w/g, (character) => character.toUpperCase());
}

export function getResourceLabel(resource) {
  return RESOURCE_LABELS[resource] || humanizeToken(resource);
}

export function describePermission(permissionName) {
  const [resource, action, scope] = String(permissionName || "").split(".");
  const actionLabel = ACTION_LABELS[action] || humanizeToken(action);
  const resourceLabel = getResourceLabel(resource).toLowerCase();
  const scopeLabel = SCOPE_LABELS[scope] || scope;
  const scopeDescription = scope === "any"
    ? " platform-wide"
    : scopeLabel
      ? ` for ${scopeLabel}`
      : "";

  return `${actionLabel} ${resourceLabel}${scopeDescription}`;
}
