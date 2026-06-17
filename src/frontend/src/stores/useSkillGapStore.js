import { create } from "zustand";
import { skillGapApi } from "../api/skillGapApi";
import {
  normalizeAssessmentGroups,
  normalizeCareerRole,
  normalizeCareerRoles,
  normalizeSkillGapResult,
} from "../components/skillGap/skillGapUtils";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCacheByPrefix,
} from "../utils/requestCacheUtils";

const ROLES_CACHE_KEY = "skill-gap:career-roles";
const GROUPS_CACHE_PREFIX = "skill-gap:assessment-groups";
const ROLES_CACHE_MS = 5 * 60 * 1000;
const GROUPS_CACHE_MS = 5 * 60 * 1000;

let skillGapRequestVersion = 0;
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
      role?.RoleSlug
  );
}

function getGroupsCacheKey(roleSlug) {
  return [GROUPS_CACHE_PREFIX, roleSlug];
}

function createAnalysisKey(roleSlug, selectedSkillSlugs = []) {
  const normalizedSkills = [...new Set(selectedSkillSlugs.map(normalizeSlug).filter(Boolean))].sort();

  return ["skill-gap", "analysis", normalizeSlug(roleSlug), normalizedSkills.join(",")].join(":");
}

function hasSameSelection(left = [], right = []) {
  const normalizedLeft = [...new Set(left.map(normalizeSlug).filter(Boolean))].sort();
  const normalizedRight = [...new Set(right.map(normalizeSlug).filter(Boolean))].sort();

  if (normalizedLeft.length !== normalizedRight.length) return false;
  return normalizedLeft.every((item, index) => item === normalizedRight[index]);
}

export const useSkillGapStore = create((set, get) => ({
  roles: [],
  selectedRole: null,
  groups: [],
  groupsByRoleSlug: {},
  selectedSkillSlugs: [],
  result: null,
  analysisByKey: {},
  step: 1,
  isRolesLoading: true,
  isGroupsLoading: false,
  isAnalyzing: false,
  error: "",

  loadRoles: async ({ force = false } = {}) => {
    const requestVersion = skillGapRequestVersion;
    const hasRoles = get().roles.length > 0;

    set({
      isRolesLoading: !hasRoles,
      error: "",
    });

    try {
      const roles = await cachedRequest(
        ROLES_CACHE_KEY,
        async () => normalizeCareerRoles(await skillGapApi.getCareerRoles()),
        { ttlMs: ROLES_CACHE_MS, force }
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

    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;

    set((state) => ({
      selectedRole: normalizedRole,
      groups: roleSlug ? state.groupsByRoleSlug[roleSlug] || [] : [],
      selectedSkillSlugs: [],
      result: null,
      error: "",
      isGroupsLoading: false,
      isAnalyzing: false,
    }));
  },

  loadGroups: async ({ force = false } = {}) => {
    const selectedRole = get().selectedRole;
    const roleSlug = getRoleSlug(selectedRole);

    if (!roleSlug) return [];

    const requestVersion = ++skillGapGroupsRequestVersion;
    const cachedGroups = get().groupsByRoleSlug[roleSlug];

    set({
      isGroupsLoading: !cachedGroups?.length,
      error: "",
    });

    try {
      const groups = await cachedRequest(
        getGroupsCacheKey(roleSlug),
        async () => normalizeAssessmentGroups(await skillGapApi.getAssessmentSkills(roleSlug)),
        { ttlMs: GROUPS_CACHE_MS, force }
      );

      if (
        requestVersion !== skillGapGroupsRequestVersion ||
        getRoleSlug(get().selectedRole) !== roleSlug
      ) {
        return groups;
      }

      set((state) => ({
        groupsByRoleSlug: {
          ...state.groupsByRoleSlug,
          [roleSlug]: groups,
        },
        groups,
        selectedSkillSlugs: force ? [] : state.selectedSkillSlugs,
        result: null,
        step: 2,
        error: "",
      }));

      return groups;
    } catch (error) {
      if (
        requestVersion !== skillGapGroupsRequestVersion ||
        getRoleSlug(get().selectedRole) !== roleSlug
      ) {
        return [];
      }

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to load assessment skills for this role."),
      });

      return [];
    } finally {
      if (
        requestVersion === skillGapGroupsRequestVersion &&
        getRoleSlug(get().selectedRole) === roleSlug
      ) {
        set({ isGroupsLoading: false });
      }
    }
  },

  toggleSkill: (slug) => {
    const normalizedSlug = normalizeSlug(slug);

    if (!normalizedSlug || get().isAnalyzing || get().isGroupsLoading) return;

    set((state) => ({
      selectedSkillSlugs: state.selectedSkillSlugs.includes(normalizedSlug)
        ? state.selectedSkillSlugs.filter((item) => item !== normalizedSlug)
        : [...state.selectedSkillSlugs, normalizedSlug],
      result: null,
      error: "",
    }));
  },

  analyze: async ({ force = false } = {}) => {
    const state = get();
    const roleSlug = getRoleSlug(state.selectedRole);

    if (!roleSlug || state.isGroupsLoading) return null;

    const selectedSkillSlugs = [...state.selectedSkillSlugs];
    const analysisKey = createAnalysisKey(roleSlug, selectedSkillSlugs);
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
          selectedSkillSlugs,
        });

        return normalizeSkillGapResult(response);
      });

      const currentState = get();
      const currentRoleSlug = getRoleSlug(currentState.selectedRole);

      if (
        requestVersion !== skillGapAnalysisRequestVersion ||
        currentRoleSlug !== roleSlug ||
        !hasSameSelection(currentState.selectedSkillSlugs, selectedSkillSlugs)
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
        !hasSameSelection(currentState.selectedSkillSlugs, selectedSkillSlugs)
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
    const hasRole = Boolean(getRoleSlug(get().selectedRole));

    set({
      step: hasRole ? 2 : 1,
      error: "",
    });
  },

  reset: () => {
    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;

    set({
      selectedRole: null,
      groups: [],
      selectedSkillSlugs: [],
      result: null,
      error: "",
      step: 1,
    });
  },

  resetSkillGap: () => {
    skillGapRequestVersion += 1;
    skillGapGroupsRequestVersion += 1;
    skillGapAnalysisRequestVersion += 1;
    invalidateRequestCacheByPrefix("skill-gap");

    set({
      roles: [],
      selectedRole: null,
      groups: [],
      groupsByRoleSlug: {},
      selectedSkillSlugs: [],
      result: null,
      analysisByKey: {},
      step: 1,
      isRolesLoading: false,
      isGroupsLoading: false,
      isAnalyzing: false,
      error: "",
    });
  },
}));
