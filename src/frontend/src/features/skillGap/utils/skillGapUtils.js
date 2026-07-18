export function toArray(value) {
  if (Array.isArray(value)) return value;
  if (Array.isArray(value?.items)) return value.items;
  if (Array.isArray(value?.data)) return value.data;
  return [];
}

export function normalizeId(value) {
  if (value == null) return "";
  return String(value).trim();
}

function pick(obj, keys, fallback = "") {
  for (const key of keys) {
    const value = obj?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
  }
  return fallback;
}

function toNumber(value, fallback = 0) {
  const numeric = Number(value);
  return Number.isFinite(numeric) ? numeric : fallback;
}

export function normalizeCareerRole(role) {
  if (!role) return null;

  const careerRoleId = normalizeId(
    pick(role, ["careerRoleId", "CareerRoleId", "id", "Id"]),
  );
  const name = String(pick(role, ["name", "Name", "title", "Title"], "Untitled role"));
  const slug = String(
    pick(role, ["slug", "Slug", "careerRoleSlug", "CareerRoleSlug", "roleSlug", "RoleSlug"], ""),
  );

  return {
    ...role,
    careerRoleId,
    id: careerRoleId,
    name,
    slug,
  };
}

export function normalizeCareerRoles(roles) {
  return toArray(roles).map(normalizeCareerRole).filter(Boolean);
}

export function normalizeRoadmapOption(roadmap) {
  if (!roadmap) return null;

  const roadmapId = normalizeId(pick(roadmap, ["roadmapId", "RoadmapId", "id", "Id"]));
  const publishedRoadmapVersionId = normalizeId(
    pick(roadmap, ["publishedRoadmapVersionId", "PublishedRoadmapVersionId"]),
  );
  const slug = String(
    pick(roadmap, ["slug", "Slug", "roadmapSlug", "RoadmapSlug"], ""),
  );
  const title = String(
    pick(roadmap, ["title", "Title", "roadmapName", "RoadmapName", "name", "Name"], "Untitled roadmap"),
  );

  return {
    ...roadmap,
    roadmapId,
    id: roadmapId,
    publishedRoadmapVersionId,
    slug,
    roadmapSlug: slug,
    title,
    roadmapName: String(pick(roadmap, ["roadmapName", "RoadmapName"], title)),
    careerRoleName: String(pick(roadmap, ["careerRoleName", "CareerRoleName"], "")),
    authorName: String(pick(roadmap, ["authorName", "AuthorName"], "")),
    roadmapVersionTitle: String(pick(roadmap, ["roadmapVersionTitle", "RoadmapVersionTitle"], "")),
    versionNumber: toNumber(pick(roadmap, ["versionNumber", "VersionNumber", "roadmapVersionNumber", "RoadmapVersionNumber"]), 0),
    roadmapVersionNumber: toNumber(pick(roadmap, ["roadmapVersionNumber", "RoadmapVersionNumber", "versionNumber", "VersionNumber"]), 0),
    publishedAt: pick(roadmap, ["publishedAt", "PublishedAt"], null),
    totalSkills: toNumber(pick(roadmap, ["totalSkills", "TotalSkills"], 0), 0),
  };
}

export function normalizeRoadmapOptions(roadmaps) {
  return toArray(roadmaps).map(normalizeRoadmapOption).filter((item) => item?.roadmapId);
}

export function normalizeAssessmentSkill(skill) {
  if (!skill) return null;

  const skillId = normalizeId(pick(skill, ["skillId", "SkillId", "id", "Id"]));
  const skillName = String(pick(skill, ["skillName", "SkillName", "name", "Name"], "Untitled skill"));
  const skillSlug = String(
    pick(skill, ["skillSlug", "SkillSlug", "slug", "Slug"], ""),
  );

  return {
    ...skill,
    skillId,
    id: skillId,
    skillName,
    skillSlug,
    slug: skillSlug,
    name: skillName,
    isMatched: Boolean(pick(skill, ["isMatched", "IsMatched"], false)),
  };
}

export function normalizeAssessmentCategory(category) {
  if (!category) return null;

  const categoryName = String(
    pick(category, ["categoryName", "CategoryName", "name", "Name"], "Uncategorized"),
  );
  const skills = toArray(pick(category, ["skills", "Skills"], []))
    .map(normalizeAssessmentSkill)
    .filter((skill) => skill?.skillId);

  return {
    ...category,
    categoryName,
    name: categoryName,
    displayOrder: toNumber(pick(category, ["displayOrder", "DisplayOrder"], 0), 0),
    totalSkills: toNumber(pick(category, ["totalSkills", "TotalSkills"], skills.length), skills.length),
    matchedSkills: toNumber(pick(category, ["matchedSkills", "MatchedSkills"], skills.filter((skill) => skill.isMatched).length), 0),
    missingSkills: toNumber(pick(category, ["missingSkills", "MissingSkills"], Math.max(0, skills.length - skills.filter((skill) => skill.isMatched).length)), 0),
    skills,
  };
}

export function normalizeAssessmentCategories(input) {
  const rawCategories = Array.isArray(input)
    ? input
    : toArray(pick(input, ["categories", "Categories"], []));

  return rawCategories
    .map(normalizeAssessmentCategory)
    .filter(Boolean)
    .sort((left, right) => left.displayOrder - right.displayOrder || left.categoryName.localeCompare(right.categoryName));
}

export function normalizeAssessmentResponse(response) {
  if (!response) return null;

  const roadmapId = normalizeId(pick(response, ["roadmapId", "RoadmapId", "id", "Id"]));
  const roadmapName = String(
    pick(response, ["roadmapName", "RoadmapName", "title", "Title"], "Selected roadmap"),
  );

  return {
    ...response,
    roadmapId,
    roadmapName,
    careerRoleName: String(pick(response, ["careerRoleName", "CareerRoleName"], "")),
    roadmapVersionTitle: String(pick(response, ["roadmapVersionTitle", "RoadmapVersionTitle"], "")),
    roadmapVersionNumber: toNumber(pick(response, ["roadmapVersionNumber", "RoadmapVersionNumber"], 0), 0),
    authorName: String(pick(response, ["authorName", "AuthorName"], "")),
    categories: normalizeAssessmentCategories(response),
  };
}

export function normalizeSkillGapResult(response) {
  if (!response) return null;

  const categories = normalizeAssessmentCategories(response);
  const roadmapId = normalizeId(pick(response, ["roadmapId", "RoadmapId", "id", "Id"]));
  const totalSkills = toNumber(
    pick(response, ["totalSkills", "TotalSkills"], categories.reduce((sum, category) => sum + category.skills.length, 0)),
    0,
  );
  const matchedSkills = toNumber(
    pick(response, ["matchedSkills", "MatchedSkills"], categories.reduce((sum, category) => sum + category.skills.filter((skill) => skill.isMatched).length, 0)),
    0,
  );

  return {
    ...response,
    skillGapAnalysisHistoryId: normalizeId(
      pick(response, ["skillGapAnalysisHistoryId", "SkillGapAnalysisHistoryId", "historyId", "HistoryId"]),
    ),
    roadmapId,
    roadmapVersionId: normalizeId(
      pick(response, ["roadmapVersionId", "RoadmapVersionId", "publishedRoadmapVersionId", "PublishedRoadmapVersionId"]),
    ),
    roadmapSlug: String(
      pick(response, ["roadmapSlug", "RoadmapSlug", "slug", "Slug"], ""),
    ),
    roadmapName: String(
      pick(response, ["roadmapName", "RoadmapName", "roadmapTitle", "RoadmapTitle", "title", "Title"], "Selected roadmap"),
    ),
    careerRoleName: String(pick(response, ["careerRoleName", "CareerRoleName"], "")),
    roadmapVersionTitle: String(pick(response, ["roadmapVersionTitle", "RoadmapVersionTitle"], "")),
    roadmapVersionNumber: toNumber(pick(response, ["roadmapVersionNumber", "RoadmapVersionNumber"], 0), 0),
    authorName: String(pick(response, ["authorName", "AuthorName"], "")),
    matchedSkills,
    totalSkills,
    missingSkills: toNumber(pick(response, ["missingSkills", "MissingSkills"], Math.max(0, totalSkills - matchedSkills)), 0),
    categories,
  };
}

export function normalizeSkillGapHistoryItem(item) {
  if (!item) return null;

  return {
    ...item,
    skillGapAnalysisHistoryId: normalizeId(
      pick(item, ["skillGapAnalysisHistoryId", "SkillGapAnalysisHistoryId", "historyId", "HistoryId", "id", "Id"]),
    ),
    roadmapId: normalizeId(pick(item, ["roadmapId", "RoadmapId"])),
    roadmapTitle: String(pick(item, ["roadmapTitle", "RoadmapTitle", "roadmapName", "RoadmapName"], "Untitled roadmap")),
    careerRoleName: String(pick(item, ["careerRoleName", "CareerRoleName"], "")),
    authorName: String(pick(item, ["authorName", "AuthorName"], "")),
    matchedSkills: toNumber(pick(item, ["matchedSkills", "MatchedSkills"], 0), 0),
    totalSkills: toNumber(pick(item, ["totalSkills", "TotalSkills"], 0), 0),
    missingSkills: toNumber(pick(item, ["missingSkills", "MissingSkills"], 0), 0),
    createdAt: pick(item, ["createdAt", "CreatedAt"], null),
  };
}

export function normalizeSkillGapHistory(items) {
  return toArray(items).map(normalizeSkillGapHistoryItem).filter(Boolean);
}

export function getRoadmapId(roadmap) {
  return normalizeId(roadmap?.roadmapId || roadmap?.RoadmapId || roadmap?.id || roadmap?.Id);
}

export function getRoleSlug(role) {
  return String(role?.slug || role?.Slug || role?.careerRoleSlug || role?.CareerRoleSlug || "").trim();
}

export function formatDateTime(value) {
  if (!value) return "—";

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";

  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

export function getSelectionSummary(categories, selectedSkillIds) {
  const selectedSet = new Set(toArray(selectedSkillIds).map(normalizeId).filter(Boolean));
  const allSkills = normalizeAssessmentCategories(categories).flatMap((category) => category.skills);

  return {
    selectedCount: allSkills.filter((skill) => selectedSet.has(skill.skillId)).length,
    totalCount: allSkills.length,
  };
}
