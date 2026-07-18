import { create } from "zustand";
import { skillGapApi } from "../api/skillGapApi";
import {
  getRoadmapId,
  getRoleSlug,
  normalizeAssessmentResponse,
  normalizeCareerRole,
  normalizeCareerRoles,
  normalizeId,
  normalizeRoadmapOptions,
  normalizeSkillGapResult,
} from "../features/skillGap/utils/skillGapUtils";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCacheByPrefix,
} from "../utils/requestCacheUtils";

const ROLES_CACHE_KEY = "skill-gap:career-roles";
const ROADMAPS_CACHE_PREFIX = "skill-gap:published-roadmaps";
const ASSESSMENT_CACHE_PREFIX = "skill-gap:assessment";
const ROLES_CACHE_MS = 5 * 60 * 1000;
const ROADMAPS_CACHE_MS = 5 * 60 * 1000;
const ASSESSMENT_CACHE_MS = 2 * 60 * 1000;

let rolesRequestVersion = 0;
let roadmapsRequestVersion = 0;
let assessmentRequestVersion = 0;
let analysisRequestVersion = 0;

function getRoadmapsCacheKey(roleSlug) {
  return [ROADMAPS_CACHE_PREFIX, roleSlug].join(":");
}

function getAssessmentCacheKey(roadmapId) {
  return [ASSESSMENT_CACHE_PREFIX, roadmapId].join(":");
}

function createAnalysisKey(roadmapId, selectedSkillIds = []) {
  const normalizedSkills = [
    ...new Set(selectedSkillIds.map(normalizeId).filter(Boolean)),
  ].sort();

  return ["skill-gap", "analysis", normalizeId(roadmapId), normalizedSkills.join(",")].join(":");
}

function hasSameSelection(left = [], right = []) {
  const normalizedLeft = [...new Set(left.map(normalizeId).filter(Boolean))].sort();
  const normalizedRight = [...new Set(right.map(normalizeId).filter(Boolean))].sort();

  if (normalizedLeft.length !== normalizedRight.length) return false;
  return normalizedLeft.every((item, index) => item === normalizedRight[index]);
}

export const useSkillGapStore = create((set, get) => ({
  roles: [],
  roadmaps: [],
  selectedRole: null,
  selectedRoadmap: null,
  assessment: null,
  categories: [],
  roadmapsByRoleSlug: {},
  assessmentsByRoadmapId: {},
  selectedSkillIds: [],
  result: null,
  step: 1,
  isRolesLoading: true,
  isRoadmapsLoading: false,
  isAssessmentLoading: false,
  isAnalyzing: false,
  error: "",

  loadRoles: async ({ force = false } = {}) => {
    const requestVersion = ++rolesRequestVersion;
    const hasRoles = get().roles.length > 0;

    set({ isRolesLoading: !hasRoles, error: "" });

    try {
      const roles = await cachedRequest(
        ROLES_CACHE_KEY,
        async () => normalizeCareerRoles(await skillGapApi.getCareerRoles()),
        { ttlMs: ROLES_CACHE_MS, force },
      );

      if (requestVersion !== rolesRequestVersion) return roles;

      set({ roles, isRolesLoading: false });
      return roles;
    } catch (error) {
      if (requestVersion !== rolesRequestVersion) return [];

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

    roadmapsRequestVersion += 1;
    assessmentRequestVersion += 1;
    analysisRequestVersion += 1;

    set((state) => ({
      selectedRole: normalizedRole,
      roadmaps: roleSlug ? state.roadmapsByRoleSlug[roleSlug] || [] : [],
      selectedRoadmap: null,
      assessment: null,
      categories: [],
      selectedSkillIds: [],
      result: null,
      error: "",
      isRoadmapsLoading: false,
      isAssessmentLoading: false,
      isAnalyzing: false,
    }));
  },

  loadRoadmaps: async ({ role = null, force = false } = {}) => {
    const selectedRole = role ? normalizeCareerRole(role) : get().selectedRole;
    const roleSlug = getRoleSlug(selectedRole);

    if (!roleSlug) return [];

    const requestVersion = ++roadmapsRequestVersion;
    const cachedRoadmaps = get().roadmapsByRoleSlug[roleSlug];

    set({ isRoadmapsLoading: !cachedRoadmaps?.length, error: "" });

    try {
      const roadmaps = await cachedRequest(
        getRoadmapsCacheKey(roleSlug),
        async () => normalizeRoadmapOptions(await skillGapApi.getPublishedRoadmapsByRole(roleSlug)),
        { ttlMs: ROADMAPS_CACHE_MS, force },
      );

      if (requestVersion !== roadmapsRequestVersion || getRoleSlug(get().selectedRole) !== roleSlug) {
        return roadmaps;
      }

      set((state) => ({
        roadmapsByRoleSlug: {
          ...state.roadmapsByRoleSlug,
          [roleSlug]: roadmaps,
        },
        roadmaps,
        selectedRoadmap: force ? null : state.selectedRoadmap,
        assessment: null,
        categories: [],
        selectedSkillIds: [],
        result: null,
        error: "",
      }));

      return roadmaps;
    } catch (error) {
      if (requestVersion !== roadmapsRequestVersion || getRoleSlug(get().selectedRole) !== roleSlug) {
        return [];
      }

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to load published roadmaps for this role."),
      });
      return [];
    } finally {
      if (requestVersion === roadmapsRequestVersion && getRoleSlug(get().selectedRole) === roleSlug) {
        set({ isRoadmapsLoading: false });
      }
    }
  },

  selectRoadmap: (roadmap) => {
    const normalizedRoadmap = normalizeRoadmapOptions([roadmap])[0] || null;
    const roadmapId = getRoadmapId(normalizedRoadmap);
    const currentRoadmapId = getRoadmapId(get().selectedRoadmap);

    if (roadmapId && roadmapId === currentRoadmapId) {
      set({ error: "" });
      return;
    }

    assessmentRequestVersion += 1;
    analysisRequestVersion += 1;

    set({
      selectedRoadmap: normalizedRoadmap,
      assessment: null,
      categories: [],
      selectedSkillIds: [],
      result: null,
      error: "",
      isAssessmentLoading: false,
      isAnalyzing: false,
    });
  },

  loadAssessment: async ({ force = false } = {}) => {
    const selectedRoadmap = get().selectedRoadmap;
    const roadmapId = getRoadmapId(selectedRoadmap);

    if (!roadmapId) return null;

    const requestVersion = ++assessmentRequestVersion;
    const cachedAssessment = get().assessmentsByRoadmapId[roadmapId];

    set({ isAssessmentLoading: !cachedAssessment?.categories?.length, error: "" });

    try {
      const assessment = await cachedRequest(
        getAssessmentCacheKey(roadmapId),
        async () => normalizeAssessmentResponse(await skillGapApi.getAssessmentByRoadmap(roadmapId)),
        { ttlMs: ASSESSMENT_CACHE_MS, force },
      );

      if (requestVersion !== assessmentRequestVersion || getRoadmapId(get().selectedRoadmap) !== roadmapId) {
        return assessment;
      }

      set((state) => ({
        assessmentsByRoadmapId: {
          ...state.assessmentsByRoadmapId,
          [roadmapId]: assessment,
        },
        assessment,
        categories: assessment?.categories || [],
        selectedSkillIds: force ? [] : state.selectedSkillIds,
        result: null,
        step: 2,
        error: "",
      }));

      return assessment;
    } catch (error) {
      if (requestVersion !== assessmentRequestVersion || getRoadmapId(get().selectedRoadmap) !== roadmapId) {
        return null;
      }

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to load skill checklist for this roadmap."),
      });
      return null;
    } finally {
      if (requestVersion === assessmentRequestVersion && getRoadmapId(get().selectedRoadmap) === roadmapId) {
        set({ isAssessmentLoading: false });
      }
    }
  },

  toggleSkill: (skillId) => {
    const normalizedSkillId = normalizeId(skillId);

    if (!normalizedSkillId || get().isAnalyzing || get().isAssessmentLoading) return;

    set((state) => ({
      selectedSkillIds: state.selectedSkillIds.includes(normalizedSkillId)
        ? state.selectedSkillIds.filter((item) => item !== normalizedSkillId)
        : [...state.selectedSkillIds, normalizedSkillId],
      result: null,
      error: "",
    }));
  },

  analyze: async () => {
    const state = get();
    const roadmapId = getRoadmapId(state.selectedRoadmap);

    if (!roadmapId || state.isAssessmentLoading || state.isAnalyzing) {
      return null;
    }

    const selectedSkillIds = [...state.selectedSkillIds];
    const analysisKey = createAnalysisKey(roadmapId, selectedSkillIds);

    const requestVersion = ++analysisRequestVersion;

    set({ isAnalyzing: true, error: "" });

    try {
      const result = await guardedMutation(analysisKey, async () => {
        const response = await skillGapApi.analyzeSkillGap({ roadmapId, selectedSkillIds });
        return normalizeSkillGapResult(response);
      });

      const currentState = get();
      const currentRoadmapId = getRoadmapId(currentState.selectedRoadmap);

      if (
        requestVersion !== analysisRequestVersion ||
        currentRoadmapId !== roadmapId ||
        !hasSameSelection(currentState.selectedSkillIds, selectedSkillIds)
      ) {
        return result;
      }

      set({
        result,
        step: 3,
        error: "",
      });

      return result;
    } catch (error) {
      const currentState = get();

      if (
        requestVersion !== analysisRequestVersion ||
        getRoadmapId(currentState.selectedRoadmap) !== roadmapId ||
        !hasSameSelection(currentState.selectedSkillIds, selectedSkillIds)
      ) {
        return null;
      }

      set({
        error: getFriendlyApiErrorMessage(error, "Unable to analyze your skill gap."),
      });
      return null;
    } finally {
      if (requestVersion === analysisRequestVersion) {
        set({ isAnalyzing: false });
      }
    }
  },

  goToRoleStep: () => set({ step: 1, error: "" }),

  goToSkillStep: () => {
    set({ step: getRoadmapId(get().selectedRoadmap) ? 2 : 1, error: "" });
  },

  showHistoryResult: (result) => {
    const normalizedResult = normalizeSkillGapResult(result);
    if (!normalizedResult) return;

    roadmapsRequestVersion += 1;
    assessmentRequestVersion += 1;
    analysisRequestVersion += 1;

    set({
      selectedRole: null,
      roadmaps: [],
      selectedRoadmap: null,
      assessment: null,
      categories: [],
      selectedSkillIds: [],
      result: normalizedResult,
      step: 3,
      error: "",
      isRoadmapsLoading: false,
      isAssessmentLoading: false,
      isAnalyzing: false,
    });
  },

  reset: () => {
    roadmapsRequestVersion += 1;
    assessmentRequestVersion += 1;
    analysisRequestVersion += 1;

    set({
      selectedRole: null,
      roadmaps: [],
      selectedRoadmap: null,
      assessment: null,
      categories: [],
      selectedSkillIds: [],
      result: null,
      error: "",
      step: 1,
    });
  },

  resetSkillGap: () => {
    rolesRequestVersion += 1;
    roadmapsRequestVersion += 1;
    assessmentRequestVersion += 1;
    analysisRequestVersion += 1;
    invalidateRequestCacheByPrefix("skill-gap");

    set({
      roles: [],
      roadmaps: [],
      selectedRole: null,
      selectedRoadmap: null,
      assessment: null,
      categories: [],
      roadmapsByRoleSlug: {},
      assessmentsByRoadmapId: {},
      selectedSkillIds: [],
      result: null,
      step: 1,
      isRolesLoading: false,
      isRoadmapsLoading: false,
      isAssessmentLoading: false,
      isAnalyzing: false,
      error: "",
    });
  },
}));
