import { SKILL_GAP_PRIORITY_STYLES } from "./skillGapConstants";

export function toArray(value) {
  return Array.isArray(value) ? value : [];
}

export function getValue(source, camelKey, pascalKey, fallback = null) {
  return source?.[camelKey] ?? source?.[pascalKey] ?? fallback;
}

export function getPriorityNumber(value) {
  if (typeof value === "number") return value;

  const normalized = String(value || "").toLowerCase();
  if (normalized === "critical") return 1;
  if (normalized === "high") return 2;
  if (normalized === "medium") return 3;
  return 4;
}

export function getPriorityStyle(value) {
  return SKILL_GAP_PRIORITY_STYLES[getPriorityNumber(value)] || SKILL_GAP_PRIORITY_STYLES[4];
}

export function getRuleDescription(group) {
  const rule = group.completionRule || "ANY";
  const requiredCount = group.requiredSkillCount ?? 1;
  const total = group.skills?.length ?? 0;

  if (rule === "ALL") return `All ${total} required`;
  if (rule === "COUNT") return `${requiredCount} of ${total} required`;
  return "Any 1 required";
}

export function isGroupCompleted(group, selectedSkillSlugs) {
  const selected = new Set(selectedSkillSlugs);
  const matchedCount = toArray(group.skills).filter((skill) => selected.has(skill.slug)).length;
  const total = toArray(group.skills).length;

  if (group.completionRule === "ALL") return matchedCount === total && total > 0;
  if (group.completionRule === "COUNT") return matchedCount >= (group.requiredSkillCount ?? 1);
  return matchedCount >= 1;
}


export function normalizeCareerRole(role) {
  if (!role) return null;

  const careerRoleId =
    getValue(role, "careerRoleId", "CareerRoleId") ??
    getValue(role, "id", "Id");
  const name =
    getValue(role, "name", "Name") ??
    getValue(role, "roleName", "RoleName") ??
    getValue(role, "title", "Title", "Unnamed role");
  const slug =
    getValue(role, "slug", "Slug") ??
    getValue(role, "careerRoleSlug", "CareerRoleSlug") ??
    getValue(role, "roleSlug", "RoleSlug", "");

  return {
    ...role,
    careerRoleId,
    id: getValue(role, "id", "Id", careerRoleId),
    name,
    slug: String(slug || "").trim(),
    description: getValue(role, "description", "Description", ""),
  };
}

export function normalizeCareerRoles(response) {
  return toArray(response).map(normalizeCareerRole).filter(Boolean);
}

export function normalizeAssessmentGroups(response) {
  return toArray(getValue(response, "skillGroups", "SkillGroups")).map((group) => ({
    skillGroupId: getValue(group, "skillGroupId", "SkillGroupId"),
    groupName: getValue(group, "groupName", "GroupName", "Unnamed group"),
    priority: getValue(group, "priority", "Priority", 4),
    completionRule: getValue(group, "completionRule", "CompletionRule", "ANY"),
    requiredSkillCount: getValue(group, "requiredSkillCount", "RequiredSkillCount", null),
    requirementDescription: getValue(group, "requirementDescription", "RequirementDescription", ""),
    skills: toArray(getValue(group, "skills", "Skills")).map((skill) => ({
      skillId: getValue(skill, "skillId", "SkillId"),
      name: getValue(skill, "name", "Name", "Unnamed skill"),
      slug: getValue(skill, "slug", "Slug", ""),
    })),
  }));
}

export function normalizeSkillGapResult(rawResult) {
  if (!rawResult) return null;

  const groups = toArray(getValue(rawResult, "groups", "Groups")).map((group) => ({
    skillGroupId: getValue(group, "skillGroupId", "SkillGroupId"),
    groupName: getValue(group, "groupName", "GroupName", "Unnamed group"),
    priority: getValue(group, "priority", "Priority", 4),
    learningPriority: getValue(
      group,
      "learningPriority",
      "LearningPriority",
      getValue(group, "priority", "Priority", 4)
    ),
    matchedSkillCount: Number(getValue(group, "matchedSkillCount", "MatchedSkillCount", 0)),
    totalSkillCount: Number(getValue(group, "totalSkillCount", "TotalSkillCount", 0)),
    completionRule: getValue(group, "completionRule", "CompletionRule", "ANY"),
    requiredSkillCount: getValue(group, "requiredSkillCount", "RequiredSkillCount", null),
    isCompleted: Boolean(getValue(group, "isCompleted", "IsCompleted", false)),
    matchedSkills: toArray(getValue(group, "matchedSkills", "MatchedSkills")),
    suggestedSkills: toArray(getValue(group, "suggestedSkills", "SuggestedSkills")),
  }));

  return {
    careerRoleName: getValue(rawResult, "careerRoleName", "CareerRoleName", "Selected role"),
    totalGroups: Number(getValue(rawResult, "totalGroups", "TotalGroups", groups.length)),
    completedGroups: Number(getValue(rawResult, "completedGroups", "CompletedGroups", 0)),
    missingGroups: Number(getValue(rawResult, "missingGroups", "MissingGroups", 0)),
    readinessPercent: Number(getValue(rawResult, "readinessPercent", "ReadinessPercent", 0)),
    groups,
    missingGroupList: toArray(getValue(rawResult, "missingGroupList", "MissingGroupList")),
  };
}
