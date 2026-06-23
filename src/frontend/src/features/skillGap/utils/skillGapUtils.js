import {
  ASSESSMENT_LEVEL_STYLES,
  SKILL_GAP_PRIORITY_STYLES,
} from "../constants/skillGapConstants";

export function toArray(value) {
  return Array.isArray(value) ? value : [];
}

export function getValue(source, camelKey, pascalKey, fallback = null) {
  return source?.[camelKey] ?? source?.[pascalKey] ?? fallback;
}

export function normalizeId(value) {
  return String(value || "").trim();
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
  return (
    SKILL_GAP_PRIORITY_STYLES[getPriorityNumber(value)] ||
    SKILL_GAP_PRIORITY_STYLES[4]
  );
}

export function getAssessmentLevelStyle(levelSlug) {
  const normalized = String(levelSlug || "").toLowerCase();
  return ASSESSMENT_LEVEL_STYLES[normalized] || ASSESSMENT_LEVEL_STYLES.default;
}

export function getSelectionRuleLabel(selectionType) {
  const normalized = String(selectionType || "").toLowerCase();
  if (normalized === "complete_all" || normalized === "all") return "ALL";
  if (normalized === "choose_many" || normalized === "count") return "COUNT";
  if (normalized === "choose_one" || normalized === "any") return "ANY";
  return "ANY";
}

export function getRuleDescription(group) {
  const selectionType = group.selectionType || group.SelectionType;
  const rule = getSelectionRuleLabel(
    selectionType || group.completionRule || group.CompletionRule,
  );
  const requiredCount = group.requiredCount ?? group.requiredSkillCount ?? 1;
  const total =
    group.totalSkillCount ?? group.totalSkills ?? group.skills?.length ?? 0;

  if (rule === "ALL") return `All ${total} required`;
  if (rule === "COUNT") return `${requiredCount} of ${total} required`;
  return "Any 1 required";
}

export function isGroupCompleted(group, selectedNodeIds) {
  const selected = new Set(toArray(selectedNodeIds).map(normalizeId));
  const skills = toArray(group.skills);
  const matchedCount = skills.filter((skill) =>
    selected.has(
      normalizeId(skill.nodeId || skill.skillId || skill.id || skill.slug),
    ),
  ).length;
  const total = skills.length;
  const rule = getSelectionRuleLabel(
    group.selectionType || group.completionRule,
  );

  if (rule === "ALL") return matchedCount === total && total > 0;
  if (rule === "COUNT")
    return (
      matchedCount >= (group.requiredCount ?? group.requiredSkillCount ?? 1)
    );
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

export function normalizeAssessmentLevel(level) {
  if (!level) return null;

  const levelId = getValue(level, "levelId", "LevelId");
  const levelName = getValue(
    level,
    "levelName",
    "LevelName",
    getValue(level, "name", "Name", "Assessment level"),
  );
  const slug = getValue(
    level,
    "slug",
    "Slug",
    getValue(level, "levelSlug", "LevelSlug", ""),
  );
  const groupCount = getValue(level, "groupCount", "GroupCount", null);

  return {
    ...level,
    levelId,
    id: levelId,
    levelName,
    name: levelName,
    slug: String(slug || "").trim(),
    groupCount,
  };
}

export function normalizeAssessmentLevels(response) {
  return toArray(response)
    .map(normalizeAssessmentLevel)
    .filter((level) => level?.slug);
}

export function normalizeAssessmentSkill(skill) {
  const nodeId = normalizeId(
    getValue(skill, "nodeId", "NodeId") ??
      getValue(skill, "skillId", "SkillId") ??
      getValue(skill, "id", "Id") ??
      getValue(skill, "slug", "Slug"),
  );

  return {
    ...skill,
    nodeId,
    skillId: nodeId,
    id: nodeId,
    name: getValue(skill, "name", "Name", "Unnamed skill"),
    slug: getValue(skill, "slug", "Slug", nodeId),
  };
}

export function normalizeAssessmentGroups(response) {
  const groups = toArray(
    getValue(
      response,
      "groups",
      "Groups",
      getValue(response, "skillGroups", "SkillGroups", response),
    ),
  );

  return groups.map((group, index) => {
    const groupId = normalizeId(
      getValue(group, "groupId", "GroupId") ??
        getValue(group, "skillGroupId", "SkillGroupId") ??
        getValue(group, "id", "Id") ??
        getValue(group, "groupSlug", "GroupSlug") ??
        index,
    );
    const selectionType = getValue(
      group,
      "selectionType",
      "SelectionType",
      "choose_one",
    );
    const requiredCount = getValue(
      group,
      "requiredCount",
      "RequiredCount",
      getValue(group, "requiredSkillCount", "RequiredSkillCount", null),
    );
    const skills = toArray(getValue(group, "skills", "Skills")).map(
      normalizeAssessmentSkill,
    );

    return {
      ...group,
      groupId,
      skillGroupId: groupId,
      groupName: getValue(group, "groupName", "GroupName", "Unnamed group"),
      groupSlug: getValue(group, "groupSlug", "GroupSlug", ""),
      phaseName: getValue(group, "phaseName", "PhaseName", ""),
      sortOrder: Number(getValue(group, "sortOrder", "SortOrder", index)),
      priority: getPriorityNumber(index < 2 ? 1 : index < 5 ? 2 : 3),
      selectionType,
      completionRule: getSelectionRuleLabel(selectionType),
      requiredCount,
      requiredSkillCount: requiredCount,
      requirementDescription: getRuleDescription({
        selectionType,
        requiredCount,
        totalSkillCount: skills.length,
        skills,
      }),
      skills,
    };
  });
}

function buildSelectedSkillNameMap(groups, selectedNodeIds) {
  const selected = new Set(toArray(selectedNodeIds).map(normalizeId));
  const nameMap = new Map();

  toArray(groups).forEach((group) => {
    toArray(group.skills).forEach((skill) => {
      const nodeId = normalizeId(
        skill.nodeId || skill.skillId || skill.id || skill.slug,
      );
      if (selected.has(nodeId)) {
        nameMap.set(nodeId, skill.name || skill.slug || nodeId);
      }
    });
  });

  return nameMap;
}

export function normalizeSkillItems(value) {
  return toArray(value)
    .map((item) => {
      if (typeof item === "string") return item;

      const nodeId = getValue(
        item,
        "nodeId",
        "NodeId",
        getValue(item, "skillId", "SkillId", getValue(item, "id", "Id", "")),
      );
      const name = getValue(
        item,
        "name",
        "Name",
        getValue(
          item,
          "title",
          "Title",
          getValue(item, "slug", "Slug", "Unnamed skill"),
        ),
      );
      const slug = getValue(item, "slug", "Slug", "");

      return {
        ...item,
        nodeId,
        skillId: nodeId,
        id: nodeId,
        name,
        slug,
      };
    })
    .filter((item) => (typeof item === "string" ? item.trim() : item?.name));
}

export function normalizeMissingSkills(value) {
  return normalizeSkillItems(value);
}

export function normalizeSkillGapResult(rawResult, context = {}) {
  if (!rawResult) return null;

  const selected = new Set(toArray(context.selectedNodeIds).map(normalizeId));
  const selectedNameMap = buildSelectedSkillNameMap(
    context.groups,
    context.selectedNodeIds,
  );

  const groups = toArray(getValue(rawResult, "groups", "Groups")).map(
    (group, index) => {
      const groupId = normalizeId(
        getValue(group, "groupId", "GroupId") ??
          getValue(group, "skillGroupId", "SkillGroupId") ??
          `${getValue(group, "groupName", "GroupName", "group")}-${index}`,
      );
      const selectionType = getValue(
        group,
        "selectionType",
        "SelectionType",
        "choose_one",
      );
      const totalSkillCount = Number(
        getValue(
          group,
          "totalSkills",
          "TotalSkills",
          getValue(group, "totalSkillCount", "TotalSkillCount", 0),
        ),
      );
      const matchedSkillCount = Number(
        getValue(
          group,
          "matchedSkills",
          "MatchedSkills",
          getValue(group, "matchedSkillCount", "MatchedSkillCount", 0),
        ),
      );
      const matchingAssessmentGroup = toArray(context.groups).find(
        (item) =>
          normalizeId(item.groupId || item.skillGroupId) === groupId ||
          item.groupName === getValue(group, "groupName", "GroupName"),
      );
      const matchedSkillItems = normalizeSkillItems(
        getValue(
          group,
          "matchedSkillItems",
          "MatchedSkillItems",
          getValue(group, "alreadyHaveSkills", "AlreadyHaveSkills", []),
        ),
      );
      const matchedSkills =
        matchedSkillItems.length > 0
          ? matchedSkillItems
          : toArray(matchingAssessmentGroup?.skills)
              .filter((skill) =>
                selected.has(
                  normalizeId(
                    skill.nodeId || skill.skillId || skill.id || skill.slug,
                  ),
                ),
              )
              .map((skill) => ({
                nodeId: skill.nodeId || skill.skillId || skill.id || skill.slug,
                name:
                  skill.name || selectedNameMap.get(skill.nodeId) || skill.slug,
                slug: skill.slug || skill.nodeId || skill.skillId || skill.id,
              }))
              .filter((skill) => skill.name);

      return {
        ...group,
        groupId,
        skillGroupId: groupId,
        groupName: getValue(group, "groupName", "GroupName", "Unnamed group"),
        phaseName: getValue(group, "phaseName", "PhaseName", ""),
        sortOrder: Number(getValue(group, "sortOrder", "SortOrder", index)),
        priority: getPriorityNumber(index < 2 ? 1 : index < 5 ? 2 : 3),
        learningPriority: getPriorityNumber(index < 2 ? 1 : index < 5 ? 2 : 3),
        matchedSkillCount,
        totalSkillCount,
        completionRule: getSelectionRuleLabel(selectionType),
        selectionType,
        requiredCount: getValue(group, "requiredCount", "RequiredCount", null),
        requiredSkillCount: getValue(
          group,
          "requiredCount",
          "RequiredCount",
          null,
        ),
        isCompleted: Boolean(
          getValue(group, "isCompleted", "IsCompleted", false),
        ),
        matchedSkillItems,
        alreadyHaveSkills: matchedSkills,
        matchedSkills,
        suggestedSkills: normalizeMissingSkills(
          getValue(
            group,
            "missingSkills",
            "MissingSkills",
            getValue(group, "suggestedSkills", "SuggestedSkills", []),
          ),
        ),
      };
    },
  );

  const totalSkills = Number(
    getValue(
      rawResult,
      "totalSkills",
      "TotalSkills",
      groups.reduce((sum, group) => sum + group.totalSkillCount, 0),
    ),
  );
  const matchedSkills = Number(
    getValue(
      rawResult,
      "matchedSkills",
      "MatchedSkills",
      groups.reduce((sum, group) => sum + group.matchedSkillCount, 0),
    ),
  );
  const totalGroups = Number(
    getValue(rawResult, "totalGroups", "TotalGroups", groups.length),
  );
  const completedGroups = Number(
    getValue(rawResult, "completedGroups", "CompletedGroups", 0),
  );
  const missingSkills = Number(
    getValue(
      rawResult,
      "missingSkills",
      "MissingSkills",
      Math.max(totalSkills - matchedSkills, 0),
    ),
  );

  return {
    ...rawResult,
    careerRoleName: getValue(
      rawResult,
      "careerRoleName",
      "CareerRoleName",
      "Selected role",
    ),
    levelName: getValue(
      rawResult,
      "levelName",
      "LevelName",
      "Assessment level",
    ),
    levelSlug: getValue(rawResult, "levelSlug", "LevelSlug", ""),
    totalSkills,
    matchedSkills,
    missingSkills,
    totalGroups,
    completedGroups,
    missingGroups: Number(
      getValue(
        rawResult,
        "missingGroups",
        "MissingGroups",
        Math.max(totalGroups - completedGroups, 0),
      ),
    ),
    groups,
    missingGroupList: groups
      .filter((group) => !group.isCompleted)
      .map((group) => group.groupName),
  };
}
