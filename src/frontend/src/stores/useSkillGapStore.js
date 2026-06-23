import { create } from "zustand";
import { skillGapApi } from "../api/skillGapApi";
import {
  normalizeAssessmentGroups,
  normalizeAssessmentLevels,
  normalizeCareerRole,
  normalizeCareerRoles,
  normalizeId,
  normalizeSkillGapResult,
} from "../components/skillGap/skillGapUtils";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCacheByPrefix,
} from "../utils/requestCacheUtils";

const ROLES_CACHE_KEY = "skill-gap:career-roles";
const LEVELS_CACHE_PREFIX = "skill-gap:assessment-levels";
const GROUPS_CACHE_PREFIX = "skill-gap:assessment-groups";
const ROLES_CACHE_MS = 5 * 60 * 1000;
const LEVELS_CACHE_MS = 5 * 60 * 1000;
const GROUPS_CACHE_MS = 5 * 60 * 1000;

let skillGapRequestVersion = 0;
let skillGapLevelsRequestVersion = 0;
let skillGapGroupsRequestVersion = 0;
let skillGapAnalysisRequestVersion = 0;

function normalizeSlug(value) {
  return String(value || "").trim();
}

function getRoleSlug(role) {
  return normalizeSlug(
    role?.slug ||
      role?.Slug ||
      role?.careerRoleSlug ||
      role?.CareerRoleSlug ||
      role?.roleSlug ||
      role?.RoleSlug,
  );
}

function getLevelSlug(level) {
  return normalizeSlug(level?.slug || level?.Slug || level?.levelSlug || level?.LevelSlug);
}

function getLevelsCacheKey(roleSlug) {
  return [LEVELS_CACHE_PREFIX, normalizeSlug(roleSlug)].join(":");
}

function getGroupsCacheKey(roleSlug, levelSlug) {
  return [GROUPS_CACHE_PREFIX, normalizeSlug(roleSlug), normalizeSlug(levelSlug)].join(":");
}

function createAnalysisKey(roleSlug, levelSlug, selectedNodeIds = []) {
  const normalizedNodes = [...new Set(selectedNodeIds.map(normalizeId).filter(Boolean))].sort();

  return ["skill-gap", "analysis", normalizeSlug(roleSlug), normalizeSlug(levelSlug), normalizedNodes.join(",")].join(":");
}

function hasSameSelection(left = [], right = []) {
  const normalizedLeft = [...new Set(left.map(normalizeId).filter(Boolean))].sort();
  const normalizedRight = [...new Set(right.map(normalizeId).filter(Boolean))].sort();

  if (normalizedLeft.length !== normalizedRight.length) return false;
  return normalizedLeft.every((item, index) => item === normalizedRight[index]);
}

export const useSkillGapStore = create((set, get) => ({
  roles: [],
  levels: [],
  selectedRole: null,
  selectedLevel: null,
  groups: [],
  levelsByRoleSlug: {},
  groupsByRoleLevelKey: {},
  selectedNodeIds: [],
  result: null,
  analysisByKey: {},
  step: 1,
  isRolesLoading: true,
  isLevelsLoading: false,
  isGroupsLoading: false,
  isAnalyzing: false,
  error: "",

  loadRoles: async ({ force = false } = {}) => {
    const requestVersion = ++skillGapRequestVersion;
    const hasRoles = get().roles.length > 0;

    set({
      isRolesLoading: !hasRoles,
      error: "",
    });

    try {
      const roles = await cachedRequest(
        ROLES_CACHE_KEY,
        async () => normalizeCareerRoles(await skillGapApi.getCareerRoles()),
        { ttlMs: ROLES_CACHE_MS, force },
      );

      if (requestVersion !== skillGapRequestVersion) return roles;

      set({
        roles,
        isRolesLoading: false,
      });

      return roles;
    } catch (error) {
      if (requestVersion !== skillGapRequestVersion) return [];

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to load career roles."),
        isRolesLoading: false,
      });

      return [];
    }
  },

  selectRole: (role) => {
    const normalizedRole = normalizeCareerRole(role);
    const roleSlug = getRoleSlug(normalizedRole);
    const currentRoleSlug = getRoleSlug(get().selectedRole);

    if (roleSlug && roleSlug === currentRoleSlug) {
      set({ error: "" });
      return;
    }

    skillGapLevelsRequestVersion += 1;
    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;

    set((state) => ({
      selectedRole: normalizedRole,
      levels: roleSlug ? state.levelsByRoleSlug[roleSlug] || [] : [],
      selectedLevel: null,
      groups: [],
      selectedNodeIds: [],
      result: null,
      error: "",
      isLevelsLoading: false,
      isGroupsLoading: false,
      isAnalyzing: false,
    }));
  },

  loadLevels: async ({ role = null, force = false } = {}) => {
    const selectedRole = role ? normalizeCareerRole(role) : get().selectedRole;
    const roleSlug = getRoleSlug(selectedRole);

    if (!roleSlug) return [];

    const requestVersion = ++skillGapLevelsRequestVersion;
    const cachedLevels = get().levelsByRoleSlug[roleSlug];

    set({
      isLevelsLoading: !cachedLevels?.length,
      error: "",
    });

    try {
      const levels = await cachedRequest(
        getLevelsCacheKey(roleSlug),
        async () => normalizeAssessmentLevels(await skillGapApi.getAssessmentLevels(roleSlug)),
        { ttlMs: LEVELS_CACHE_MS, force },
      );

      if (
        requestVersion !== skillGapLevelsRequestVersion ||
        getRoleSlug(get().selectedRole) !== roleSlug
      ) {
        return levels;
      }

      set((state) => ({
        levelsByRoleSlug: {
          ...state.levelsByRoleSlug,
          [roleSlug]: levels,
        },
        levels,
        selectedLevel: force ? null : state.selectedLevel,
        groups: [],
        selectedNodeIds: [],
        result: null,
        error: "",
      }));

      return levels;
    } catch (error) {
      if (
        requestVersion !== skillGapLevelsRequestVersion ||
        getRoleSlug(get().selectedRole) !== roleSlug
      ) {
        return [];
      }

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to load assessment levels for this role."),
      });

      return [];
    } finally {
      if (
        requestVersion === skillGapLevelsRequestVersion &&
        getRoleSlug(get().selectedRole) === roleSlug
      ) {
        set({ isLevelsLoading: false });
      }
    }
  },

  selectLevel: (level) => {
    const normalizedLevel = normalizeAssessmentLevels([level])[0] || null;
    const levelSlug = getLevelSlug(normalizedLevel);
    const currentLevelSlug = getLevelSlug(get().selectedLevel);

    if (levelSlug && levelSlug === currentLevelSlug) {
      set({ error: "" });
      return;
    }

    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;

    set({
      selectedLevel: normalizedLevel,
      groups: [],
      selectedNodeIds: [],
      result: null,
      error: "",
      isGroupsLoading: false,
      isAnalyzing: false,
    });
  },

  loadGroups: async ({ force = false } = {}) => {
    const selectedRole = get().selectedRole;
    const selectedLevel = get().selectedLevel;
    const roleSlug = getRoleSlug(selectedRole);
    const levelSlug = getLevelSlug(selectedLevel);

    if (!roleSlug || !levelSlug) return [];

    const requestVersion = ++skillGapGroupsRequestVersion;
    const cacheKey = getGroupsCacheKey(roleSlug, levelSlug);
    const cachedGroups = get().groupsByRoleLevelKey[cacheKey];

    set({
      isGroupsLoading: !cachedGroups?.length,
      error: "",
    });

    try {
      const groups = await cachedRequest(
        cacheKey,
        async () => normalizeAssessmentGroups(await skillGapApi.getAssessmentByLevel(roleSlug, levelSlug)),
        { ttlMs: GROUPS_CACHE_MS, force },
      );

      if (
        requestVersion !== skillGapGroupsRequestVersion ||
        getRoleSlug(get().selectedRole) !== roleSlug ||
        getLevelSlug(get().selectedLevel) !== levelSlug
      ) {
        return groups;
      }

      set((state) => ({
        groupsByRoleLevelKey: {
          ...state.groupsByRoleLevelKey,
          [cacheKey]: groups,
        },
        groups,
        selectedNodeIds: force ? [] : state.selectedNodeIds,
        result: null,
        step: 2,
        error: "",
      }));

      return groups;
    } catch (error) {
      if (
        requestVersion !== skillGapGroupsRequestVersion ||
        getRoleSlug(get().selectedRole) !== roleSlug ||
        getLevelSlug(get().selectedLevel) !== levelSlug
      ) {
        return [];
      }

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to load assessment skills for this level."),
      });

      return [];
    } finally {
      if (
        requestVersion === skillGapGroupsRequestVersion &&
        getRoleSlug(get().selectedRole) === roleSlug &&
        getLevelSlug(get().selectedLevel) === levelSlug
      ) {
        set({ isGroupsLoading: false });
      }
    }
  },

  toggleSkill: (nodeId) => {
    const normalizedNodeId = normalizeId(nodeId);

    if (!normalizedNodeId || get().isAnalyzing || get().isGroupsLoading) return;

    set((state) => ({
      selectedNodeIds: state.selectedNodeIds.includes(normalizedNodeId)
        ? state.selectedNodeIds.filter((item) => item !== normalizedNodeId)
        : [...state.selectedNodeIds, normalizedNodeId],
      result: null,
      error: "",
    }));
  },

  analyze: async ({ force = false } = {}) => {
    const state = get();
    const roleSlug = getRoleSlug(state.selectedRole);
    const levelSlug = getLevelSlug(state.selectedLevel);

    if (!roleSlug || !levelSlug || state.isGroupsLoading) return null;

    const selectedNodeIds = [...state.selectedNodeIds];
    const analysisKey = createAnalysisKey(roleSlug, levelSlug, selectedNodeIds);
    const cachedResult = state.analysisByKey[analysisKey];

    if (!force && cachedResult) {
      set({
        result: cachedResult,
        step: 3,
        error: "",
      });

      return cachedResult;
    }

    const requestVersion = ++skillGapAnalysisRequestVersion;

    set({
      isAnalyzing: true,
      error: "",
    });

    try {
      const result = await guardedMutation(analysisKey, async () => {
        const response = await skillGapApi.analyzeSkillGap({
          careerRoleSlug: roleSlug,
          levelSlug,
          selectedNodeIds,
        });

        return normalizeSkillGapResult(response, {
          groups: state.groups,
          selectedNodeIds,
        });
      });

      const currentState = get();
      const currentRoleSlug = getRoleSlug(currentState.selectedRole);
      const currentLevelSlug = getLevelSlug(currentState.selectedLevel);

      if (
        requestVersion !== skillGapAnalysisRequestVersion ||
        currentRoleSlug !== roleSlug ||
        currentLevelSlug !== levelSlug ||
        !hasSameSelection(currentState.selectedNodeIds, selectedNodeIds)
      ) {
        return result;
      }

      set((latest) => ({
        analysisByKey: {
          ...latest.analysisByKey,
          [analysisKey]: result,
        },
        result,
        step: 3,
        error: "",
      }));

      return result;
    } catch (error) {
      const currentState = get();

      if (
        requestVersion !== skillGapAnalysisRequestVersion ||
        getRoleSlug(currentState.selectedRole) !== roleSlug ||
        getLevelSlug(currentState.selectedLevel) !== levelSlug ||
        !hasSameSelection(currentState.selectedNodeIds, selectedNodeIds)
      ) {
        return null;
      }

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to analyze your skill gap."),
      });

      return null;
    } finally {
      if (requestVersion === skillGapAnalysisRequestVersion) {
        set({ isAnalyzing: false });
      }
    }
  },

  goToRoleStep: () => {
    set({ step: 1, error: "" });
  },

  goToSkillStep: () => {
    const hasRoleAndLevel = Boolean(getRoleSlug(get().selectedRole) && getLevelSlug(get().selectedLevel));

    set({
      step: hasRoleAndLevel ? 2 : 1,
      error: "",
    });
  },

  showHistoryResult: (result) => {
    const normalizedResult = normalizeSkillGapResult(result);

    if (!normalizedResult) return;

    skillGapLevelsRequestVersion += 1;
    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;

    set({
      selectedRole: null,
      selectedLevel: null,
      groups: [],
      selectedNodeIds: [],
      result: normalizedResult,
      step: 3,
      error: "",
      isLevelsLoading: false,
      isGroupsLoading: false,
      isAnalyzing: false,
    });
  },

  reset: () => {
    skillGapLevelsRequestVersion += 1;
    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;

    set({
      selectedRole: null,
      levels: [],
      selectedLevel: null,
      groups: [],
      selectedNodeIds: [],
      result: null,
      error: "",
      step: 1,
    });
  },

  resetSkillGap: () => {
    skillGapRequestVersion += 1;
    skillGapLevelsRequestVersion += 1;
    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;
    invalidateRequestCacheByPrefix("skill-gap");

    set({
      roles: [],
      levels: [],
      selectedRole: null,
      selectedLevel: null,
      groups: [],
      levelsByRoleSlug: {},
      groupsByRoleLevelKey: {},
      selectedNodeIds: [],
      result: null,
      analysisByKey: {},
      step: 1,
      isRolesLoading: false,
      isLevelsLoading: false,
      isGroupsLoading: false,
      isAnalyzing: false,
      error: "",
    });
  },
}));
