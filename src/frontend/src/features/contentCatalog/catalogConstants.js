export const resourceTypeOptions = [
  { value: "documentation", label: "Documentation" },
  { value: "video", label: "Video" },
  { value: "course", label: "Course" },
  { value: "article", label: "Article" },
  { value: "book", label: "Book" },
  { value: "practice", label: "Practice" },
  { value: "project", label: "Project" },
  { value: "other", label: "Other" },
];

export const resourceDifficultyOptions = [
  { value: "", label: "Not set" },
  { value: "beginner", label: "Beginner" },
  { value: "intermediate", label: "Intermediate" },
  { value: "advanced", label: "Advanced" },
];

export function normalizeCatalogText(value) {
  return String(value ?? "").trim();
}

export function normalizeNullableCatalogText(value) {
  const normalized = normalizeCatalogText(value);
  return normalized || null;
}

export function normalizeSkillForm(skill = null, fallbackName = "") {
  return {
    name: skill?.name || fallbackName || "",
    category: skill?.category || "",
    description: skill?.description || "",
  };
}

export function normalizeResourceForm(resource = null, fallbackTitle = "", fallbackUrl = "") {
  return {
    title: resource?.title || fallbackTitle || "",
    url: resource?.url || fallbackUrl || "",
    resourceType: resource?.resourceType || "documentation",
    provider: resource?.provider || "",
    difficultyLevel: resource?.difficultyLevel || "",
    description: resource?.description || "",
  };
}

export function buildSkillPayload(form) {
  return {
    name: normalizeCatalogText(form.name),
    category: normalizeCatalogText(form.category),
    description: normalizeNullableCatalogText(form.description),
  };
}

export function buildResourcePayload(form) {
  return {
    title: normalizeCatalogText(form.title),
    url: normalizeCatalogText(form.url),
    resourceType: normalizeCatalogText(form.resourceType),
    provider: normalizeNullableCatalogText(form.provider),
    difficultyLevel: normalizeNullableCatalogText(form.difficultyLevel),
    description: normalizeNullableCatalogText(form.description),
  };
}

export function areSkillFormsEqual(left, right) {
  return (
    normalizeCatalogText(left?.name) === normalizeCatalogText(right?.name)
    && normalizeCatalogText(left?.category) === normalizeCatalogText(right?.category)
    && normalizeCatalogText(left?.description) === normalizeCatalogText(right?.description)
  );
}

export function areResourceFormsEqual(left, right) {
  return (
    normalizeCatalogText(left?.title) === normalizeCatalogText(right?.title)
    && normalizeCatalogText(left?.url) === normalizeCatalogText(right?.url)
    && normalizeCatalogText(left?.resourceType) === normalizeCatalogText(right?.resourceType)
    && normalizeCatalogText(left?.provider) === normalizeCatalogText(right?.provider)
    && normalizeCatalogText(left?.difficultyLevel) === normalizeCatalogText(right?.difficultyLevel)
    && normalizeCatalogText(left?.description) === normalizeCatalogText(right?.description)
  );
}
