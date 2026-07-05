import axiosClient from "./axiosClient";

const SKILL_DETAIL_CACHE_MS = 5 * 60 * 1000;
const skillDetailCache = new Map();
const skillDetailInFlight = new Map();

function now() {
  return Date.now();
}

function normalizeSkillId(skillId) {
  return String(skillId || "").trim();
}

function getCachedSkill(skillId) {
  const key = normalizeSkillId(skillId);
  const entry = skillDetailCache.get(key);

  if (!entry) return null;

  if (now() - entry.cachedAt > SKILL_DETAIL_CACHE_MS) {
    skillDetailCache.delete(key);
    return null;
  }

  return entry.value;
}

function rememberSkill(skill) {
  const skillId = normalizeSkillId(skill?.skillId);
  if (!skillId) return skill;

  skillDetailCache.set(skillId, {
    value: skill,
    cachedAt: now(),
  });

  return skill;
}

function rememberSkills(skills) {
  if (!Array.isArray(skills)) return skills;
  skills.forEach(rememberSkill);
  return skills;
}

export function clearSkillApiCache(skillId = null) {
  if (!skillId) {
    skillDetailCache.clear();
    skillDetailInFlight.clear();
    return;
  }

  const key = normalizeSkillId(skillId);
  skillDetailCache.delete(key);
  skillDetailInFlight.delete(key);
}

export const skillApi = {
  searchSkills: async ({
    search = "",
    category = "",
    sort = "",
    limit = 20,
    offset = 0,
    signal,
  } = {}) => {
    const response = await axiosClient.get("/skills", {
      params: {
        search: search || undefined,
        category: category || undefined,
        sort: sort || undefined,
        limit,
        offset,
      },
      signal,
    });

    const items = rememberSkills(
      Array.isArray(response.data?.items) ? response.data.items : [],
    );

    return {
      items,
      totalCount: Number(response.data?.totalCount ?? 0),
      limit: Number(response.data?.limit ?? limit),
      offset: Number(response.data?.offset ?? offset),
      hasMore: Boolean(response.data?.hasMore),
    };
  },

  getSuggestions: async ({ limit = 6 } = {}) => {
    const response = await axiosClient.get("/skills/suggestions", {
      params: { limit },
    });

    const items = Array.isArray(response.data) ? response.data : [];
    return rememberSkills(items);
  },

  getSkillById: async (skillId) => {
    const key = normalizeSkillId(skillId);
    if (!key) return null;

    const cached = getCachedSkill(key);
    if (cached) return cached;

    const existingRequest = skillDetailInFlight.get(key);
    if (existingRequest) return existingRequest;

    const request = axiosClient
      .get(`/skills/${encodeURIComponent(key)}`)
      .then((response) => rememberSkill(response.data))
      .finally(() => {
        skillDetailInFlight.delete(key);
      });

    skillDetailInFlight.set(key, request);
    return request;
  },

  getCategories: async () => {
    const response = await axiosClient.get("/skills/categories");
    return Array.isArray(response.data) ? response.data : [];
  },
};
