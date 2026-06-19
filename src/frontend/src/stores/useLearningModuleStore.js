import { create } from "zustand";
import { learningModuleApi } from "../api/learningModuleApi";
import {
  cachedRequest,
  guardedMutation,
  setCachedRequestData,
  invalidateRequestCache,
  invalidateRequestCacheByPrefix,
} from "../utils/requestCacheUtils";

const MODULE_LIST_TTL_MS = 5 * 60 * 1000;
const MODULE_DETAIL_TTL_MS = 5 * 60 * 1000;
const LESSON_CONTENT_TTL_MS = 10 * 60 * 1000;
const QUIZ_ATTEMPTS_TTL_MS = 60 * 1000;
const QUIZ_REVIEW_TTL_MS = 5 * 60 * 1000;
const QUIZ_SESSION_TTL_MS = 30 * 1000;

const PUBLISHED_MODULES_KEY = "learning-modules:published";
const ENROLLED_MODULES_KEY = "learning-modules:enrolled";
const EMPTY_ARRAY = Object.freeze([]);

let publishedModulesRequestVersion = 0;
let enrolledModulesRequestVersion = 0;
let moduleDetailRequestVersion = 0;
let lessonContentRequestVersion = 0;
let quizAttemptsRequestVersion = 0;
let quizReviewRequestVersion = 0;

function normalizeSlug(slug) {
  return String(slug || "").trim();
}

function getModuleId(module) {
  return module?.skillModuleId || module?.SkillModuleId || module?.id || module?.Id || null;
}

function getLessonId(lesson) {
  return lesson?.skillModuleLessonId || lesson?.SkillModuleLessonId || lesson?.lessonId || lesson?.LessonId || null;
}

function detailKey(slug) {
  return `learning-module:detail:${normalizeSlug(slug)}`;
}

function lessonContentKey(moduleId, lessonId) {
  return `learning-module:lesson-content:${moduleId}:${lessonId}`;
}

function quizAttemptsKey(moduleId) {
  return `learning-module:quiz-attempts:${moduleId}`;
}

function quizReviewKey(moduleId, attemptId) {
  return `learning-module:quiz-review:${moduleId}:${attemptId}`;
}

function quizSessionKey(moduleId, attemptId) {
  return `learning-module:quiz-session:${moduleId}:${attemptId}`;
}

function moduleMatchesId(module, moduleId) {
  return getModuleId(module) === moduleId;
}

function patchModuleEnrollment(module, lessonId, status, result = {}) {
  if (!module?.enrollment) {
    return module;
  }

  return {
    ...module,
    enrollment: {
      ...module.enrollment,
      status: result?.enrollmentStatus || result?.EnrollmentStatus || module.enrollment.status,
      progressPercent:
        result?.progressPercent
        ?? result?.ProgressPercent
        ?? module.enrollment.progressPercent,
      lessonProgress: {
        ...(module.enrollment.lessonProgress || {}),
        [lessonId]: status,
      },
      lastAccessedLessonId: lessonId,
    },
  };
}

function patchModuleInList(list, moduleId, patcher) {
  if (!Array.isArray(list)) return list;

  return list.map((module) => (moduleMatchesId(module, moduleId) ? patcher(module) : module));
}

function patchModuleMapsById(map, moduleId, patcher) {
  return Object.fromEntries(
    Object.entries(map || {}).map(([slug, module]) => [
      slug,
      moduleMatchesId(module, moduleId) ? patcher(module) : module,
    ]),
  );
}

function getFirstLessonId(module) {
  return module?.lessons?.map(getLessonId).find(Boolean) || null;
}

export const useLearningModuleStore = create((set, get) => ({
  publishedModules: [],
  enrolledModules: [],
  moduleBySlug: {},
  moduleLoadedBySlug: {},
  moduleLoadingBySlug: {},
  moduleErrorBySlug: {},
  lessonContentByKey: {},
  lessonLoadingByKey: {},
  lessonErrorByKey: {},
  quizAttemptsByModuleId: {},
  quizAttemptsLoadingByModuleId: {},
  quizReviewByAttemptId: {},
  quizReviewLoadingByAttemptId: {},
  progressUpdatingByKey: {},
  isPublishedModulesLoading: false,
  isEnrolledModulesLoading: false,
  publishedModulesLoaded: false,
  enrolledModulesLoaded: false,
  publishedModulesError: null,
  enrolledModulesError: null,

  loadPublishedModules: async ({ force = false } = {}) => {
    const requestVersion = ++publishedModulesRequestVersion;

    try {
      set((state) => ({
        isPublishedModulesLoading: force || state.publishedModules.length === 0,
        publishedModulesError: null,
      }));

      const modules = await cachedRequest(
        PUBLISHED_MODULES_KEY,
        learningModuleApi.getPublishedModules,
        { ttlMs: MODULE_LIST_TTL_MS, force },
      );

      if (requestVersion === publishedModulesRequestVersion) {
        set({
          publishedModules: modules,
          isPublishedModulesLoading: false,
          publishedModulesLoaded: true,
          publishedModulesError: null,
        });
      }

      return modules;
    } catch (error) {
      if (requestVersion === publishedModulesRequestVersion) {
        set({
          isPublishedModulesLoading: false,
          publishedModulesLoaded: true,
          publishedModulesError: error?.message || "Unable to load learning modules.",
        });
      }

      throw error;
    }
  },

  loadEnrolledModules: async ({ force = false } = {}) => {
    const requestVersion = ++enrolledModulesRequestVersion;

    try {
      set((state) => ({
        isEnrolledModulesLoading: force || state.enrolledModules.length === 0,
        enrolledModulesError: null,
      }));

      const modules = await cachedRequest(
        ENROLLED_MODULES_KEY,
        learningModuleApi.getEnrolledModules,
        { ttlMs: MODULE_LIST_TTL_MS, force },
      );

      if (requestVersion === enrolledModulesRequestVersion) {
        set({
          enrolledModules: modules,
          isEnrolledModulesLoading: false,
          enrolledModulesLoaded: true,
          enrolledModulesError: null,
        });
      }

      return modules;
    } catch (error) {
      if (requestVersion === enrolledModulesRequestVersion) {
        set({
          isEnrolledModulesLoading: false,
          enrolledModulesLoaded: true,
          enrolledModulesError: error?.message || "Unable to load your learning modules.",
        });
      }

      throw error;
    }
  },

  loadModuleBySlug: async (slug, { force = false } = {}) => {
    const normalizedSlug = normalizeSlug(slug);
    if (!normalizedSlug) return null;

    const requestVersion = ++moduleDetailRequestVersion;

    try {
      set((state) => ({
        moduleLoadingBySlug: {
          ...state.moduleLoadingBySlug,
          [normalizedSlug]: force || !state.moduleBySlug[normalizedSlug],
        },
        moduleErrorBySlug: {
          ...state.moduleErrorBySlug,
          [normalizedSlug]: null,
        },
      }));

      const module = await cachedRequest(
        detailKey(normalizedSlug),
        () => learningModuleApi.getPublishedModuleBySlug(normalizedSlug),
        { ttlMs: MODULE_DETAIL_TTL_MS, force },
      );

      if (requestVersion === moduleDetailRequestVersion) {
        set((state) => ({
          moduleBySlug: {
            ...state.moduleBySlug,
            [normalizedSlug]: module,
          },
          moduleLoadedBySlug: {
            ...state.moduleLoadedBySlug,
            [normalizedSlug]: true,
          },
          moduleLoadingBySlug: {
            ...state.moduleLoadingBySlug,
            [normalizedSlug]: false,
          },
          moduleErrorBySlug: {
            ...state.moduleErrorBySlug,
            [normalizedSlug]: null,
          },
        }));
      }

      return module;
    } catch (error) {
      if (requestVersion === moduleDetailRequestVersion) {
        set((state) => ({
          moduleLoadingBySlug: {
            ...state.moduleLoadingBySlug,
            [normalizedSlug]: false,
          },
          moduleErrorBySlug: {
            ...state.moduleErrorBySlug,
            [normalizedSlug]: error?.message || "Unable to load this module.",
          },
        }));
      }

      throw error;
    }
  },

  enrollModule: async (moduleId, { slug } = {}) => {
    if (!moduleId) return null;

    const result = await guardedMutation(
      ["learning-module", "enroll", moduleId],
      () => learningModuleApi.enroll(moduleId),
    );

    const normalizedSlug = normalizeSlug(slug || result?.slug || result?.Slug);

    invalidateRequestCache(ENROLLED_MODULES_KEY);
    invalidateRequestCache(PUBLISHED_MODULES_KEY);

    if (normalizedSlug) {
      invalidateRequestCache(detailKey(normalizedSlug));
    }

    const refreshedModule = normalizedSlug
      ? await get().loadModuleBySlug(normalizedSlug, { force: true }).catch(() => null)
      : null;

    const nextModule = refreshedModule || result;

    set((state) => {
      const shouldPatchModule = nextModule && moduleMatchesId(nextModule, moduleId);
      const patchedPublishedModules = patchModuleInList(
        state.publishedModules,
        moduleId,
        (module) => shouldPatchModule ? { ...module, ...nextModule } : {
          ...module,
          enrollment: nextModule?.enrollment || module.enrollment || { status: "in_progress" },
        },
      );
      const existingEnrolledModules = Array.isArray(state.enrolledModules)
        ? state.enrolledModules
        : [];
      const hasEnrolledModule = existingEnrolledModules.some((module) => moduleMatchesId(module, moduleId));
      const enrolledModuleToInsert = shouldPatchModule
        ? nextModule
        : patchedPublishedModules.find((module) => moduleMatchesId(module, moduleId));
      const patchedEnrolledModules = hasEnrolledModule
        ? patchModuleInList(
            existingEnrolledModules,
            moduleId,
            (module) => shouldPatchModule ? { ...module, ...nextModule } : module,
          )
        : enrolledModuleToInsert
          ? [enrolledModuleToInsert, ...existingEnrolledModules]
          : existingEnrolledModules;

      setCachedRequestData(PUBLISHED_MODULES_KEY, patchedPublishedModules);
      setCachedRequestData(ENROLLED_MODULES_KEY, patchedEnrolledModules);

      return {
        publishedModules: patchedPublishedModules,
        enrolledModules: patchedEnrolledModules,
      };
    });

    if (!refreshedModule) {
      await Promise.allSettled([
        get().loadPublishedModules({ force: true }),
        get().loadEnrolledModules({ force: true }),
      ]);
    }

    return result;
  },

  loadLessonContent: async (moduleId, lessonId, { force = false } = {}) => {
    if (!moduleId || !lessonId) return null;

    const key = lessonContentKey(moduleId, lessonId);
    const requestVersion = ++lessonContentRequestVersion;

    try {
      set((state) => ({
        lessonLoadingByKey: {
          ...state.lessonLoadingByKey,
          [key]: force || !state.lessonContentByKey[key],
        },
        lessonErrorByKey: {
          ...state.lessonErrorByKey,
          [key]: null,
        },
      }));

      const content = await cachedRequest(
        key,
        () => learningModuleApi.getLessonContent(moduleId, lessonId),
        { ttlMs: LESSON_CONTENT_TTL_MS, force },
      );

      if (requestVersion === lessonContentRequestVersion) {
        set((state) => ({
          lessonContentByKey: {
            ...state.lessonContentByKey,
            [key]: content,
          },
          lessonLoadingByKey: {
            ...state.lessonLoadingByKey,
            [key]: false,
          },
          lessonErrorByKey: {
            ...state.lessonErrorByKey,
            [key]: null,
          },
        }));
      }

      return content;
    } catch (error) {
      if (requestVersion === lessonContentRequestVersion) {
        set((state) => ({
          lessonLoadingByKey: {
            ...state.lessonLoadingByKey,
            [key]: false,
          },
          lessonErrorByKey: {
            ...state.lessonErrorByKey,
            [key]: error?.message || "Unable to load this lesson.",
          },
        }));
      }

      throw error;
    }
  },

  updateLessonProgress: async (moduleId, lessonId, status) => {
    if (!moduleId || !lessonId || !status) return null;

    const key = `learning-module:progress:${moduleId}:${lessonId}`;

    if (get().progressUpdatingByKey[key]) {
      return null;
    }

    set((state) => ({
      progressUpdatingByKey: {
        ...state.progressUpdatingByKey,
        [key]: true,
      },
    }));

    try {
      const result = await guardedMutation(
        key,
        () => learningModuleApi.updateLessonProgress(moduleId, lessonId, status),
      );

      set((state) => ({
        moduleBySlug: patchModuleMapsById(
          state.moduleBySlug,
          moduleId,
          (module) => patchModuleEnrollment(module, lessonId, status, result),
        ),
        enrolledModules: patchModuleInList(
          state.enrolledModules,
          moduleId,
          (module) => patchModuleEnrollment(module, lessonId, status, result),
        ),
        publishedModules: patchModuleInList(
          state.publishedModules,
          moduleId,
          (module) => patchModuleEnrollment(module, lessonId, status, result),
        ),
      }));

      const nextState = get();
      setCachedRequestData(ENROLLED_MODULES_KEY, nextState.enrolledModules);
      setCachedRequestData(PUBLISHED_MODULES_KEY, nextState.publishedModules);
      Object.entries(nextState.moduleBySlug || {}).forEach(([slug, module]) => {
        setCachedRequestData(detailKey(slug), module);
      });

      return result;
    } finally {
      set((state) => ({
        progressUpdatingByKey: {
          ...state.progressUpdatingByKey,
          [key]: false,
        },
      }));
    }
  },

  loadQuizAttempts: async (moduleId, { force = false } = {}) => {
    if (!moduleId) return [];

    const requestVersion = ++quizAttemptsRequestVersion;

    try {
      set((state) => ({
        quizAttemptsLoadingByModuleId: {
          ...state.quizAttemptsLoadingByModuleId,
          [moduleId]: force || !state.quizAttemptsByModuleId[moduleId],
        },
      }));

      const attempts = await cachedRequest(
        quizAttemptsKey(moduleId),
        () => learningModuleApi.getQuizAttempts(moduleId),
        { ttlMs: QUIZ_ATTEMPTS_TTL_MS, force },
      );

      if (requestVersion === quizAttemptsRequestVersion) {
        set((state) => ({
          quizAttemptsByModuleId: {
            ...state.quizAttemptsByModuleId,
            [moduleId]: attempts,
          },
          quizAttemptsLoadingByModuleId: {
            ...state.quizAttemptsLoadingByModuleId,
            [moduleId]: false,
          },
        }));
      }

      return attempts;
    } catch (error) {
      if (requestVersion === quizAttemptsRequestVersion) {
        set((state) => ({
          quizAttemptsLoadingByModuleId: {
            ...state.quizAttemptsLoadingByModuleId,
            [moduleId]: false,
          },
          quizAttemptsByModuleId: {
            ...state.quizAttemptsByModuleId,
            [moduleId]: [],
          },
        }));
      }

      throw error;
    }
  },

  startQuizAttempt: async (moduleId) => {
    if (!moduleId) return null;

    const attempt = await guardedMutation(
      ["learning-module", "quiz-attempt", "start", moduleId],
      () => learningModuleApi.startQuizAttempt(moduleId),
    );

    invalidateRequestCache(quizAttemptsKey(moduleId));
    await get().loadQuizAttempts(moduleId, { force: true }).catch(() => []);

    return attempt;
  },

  loadQuizAttemptSession: async (moduleId, attemptId, { force = false } = {}) => {
    if (!moduleId || !attemptId) return null;

    return cachedRequest(
      quizSessionKey(moduleId, attemptId),
      () => learningModuleApi.getQuizAttemptSession(moduleId, attemptId),
      { ttlMs: QUIZ_SESSION_TTL_MS, force },
    );
  },

  loadQuizAttemptReview: async (moduleId, attemptId, { force = false } = {}) => {
    if (!moduleId || !attemptId) return null;

    const requestVersion = ++quizReviewRequestVersion;
    const reviewMapKey = `${moduleId}:${attemptId}`;

    try {
      set((state) => ({
        quizReviewLoadingByAttemptId: {
          ...state.quizReviewLoadingByAttemptId,
          [reviewMapKey]: true,
        },
      }));

      const review = await cachedRequest(
        quizReviewKey(moduleId, attemptId),
        () => learningModuleApi.getQuizAttemptReview(moduleId, attemptId),
        { ttlMs: QUIZ_REVIEW_TTL_MS, force },
      );

      if (requestVersion === quizReviewRequestVersion) {
        set((state) => ({
          quizReviewByAttemptId: {
            ...state.quizReviewByAttemptId,
            [reviewMapKey]: review,
          },
          quizReviewLoadingByAttemptId: {
            ...state.quizReviewLoadingByAttemptId,
            [reviewMapKey]: false,
          },
        }));
      }

      return review;
    } catch (error) {
      if (requestVersion === quizReviewRequestVersion) {
        set((state) => ({
          quizReviewLoadingByAttemptId: {
            ...state.quizReviewLoadingByAttemptId,
            [reviewMapKey]: false,
          },
        }));
      }

      throw error;
    }
  },

  submitQuizAttempt: async (moduleId, attemptId, answers) => {
    if (!moduleId || !attemptId) return null;

    const review = await guardedMutation(
      ["learning-module", "quiz-attempt", "submit", moduleId, attemptId],
      () => learningModuleApi.submitQuizAttempt(moduleId, attemptId, answers),
    );

    invalidateRequestCache(quizAttemptsKey(moduleId));
    invalidateRequestCache(quizSessionKey(moduleId, attemptId));
    invalidateRequestCache(quizReviewKey(moduleId, attemptId));
    invalidateRequestCacheByPrefix("learning-module:detail:");

    set((state) => ({
      quizReviewByAttemptId: {
        ...state.quizReviewByAttemptId,
        [`${moduleId}:${attemptId}`]: review,
      },
    }));

    await get().loadQuizAttempts(moduleId, { force: true }).catch(() => []);

    return review;
  },

  sendModuleChatMessage: async (moduleId, payload) => {
    if (!moduleId) return null;

    return guardedMutation(
      ["learning-module", "assistant-chat", moduleId],
      () => learningModuleApi.sendModuleChatMessage(moduleId, payload),
    );
  },

  invalidateModuleLists: () => {
    invalidateRequestCache(PUBLISHED_MODULES_KEY);
    invalidateRequestCache(ENROLLED_MODULES_KEY);
  },

  resetLearningModules: () => {
    publishedModulesRequestVersion += 1;
    enrolledModulesRequestVersion += 1;
    moduleDetailRequestVersion += 1;
    lessonContentRequestVersion += 1;
    quizAttemptsRequestVersion += 1;
    quizReviewRequestVersion += 1;

    invalidateRequestCache(PUBLISHED_MODULES_KEY);
    invalidateRequestCache(ENROLLED_MODULES_KEY);
    invalidateRequestCacheByPrefix("learning-module:");

    set({
      publishedModules: [],
      enrolledModules: [],
      moduleBySlug: {},
      moduleLoadedBySlug: {},
      moduleLoadingBySlug: {},
      moduleErrorBySlug: {},
      lessonContentByKey: {},
      lessonLoadingByKey: {},
      lessonErrorByKey: {},
      quizAttemptsByModuleId: {},
      quizAttemptsLoadingByModuleId: {},
      quizReviewByAttemptId: {},
      quizReviewLoadingByAttemptId: {},
      progressUpdatingByKey: {},
      isPublishedModulesLoading: false,
      isEnrolledModulesLoading: false,
      publishedModulesLoaded: false,
      enrolledModulesLoaded: false,
      publishedModulesError: null,
      enrolledModulesError: null,
    });
  },

  getPublishedModules: () => get().publishedModules,
  getEnrolledModules: () => get().enrolledModules,
  getModuleSnapshot: (slug) => get().moduleBySlug[normalizeSlug(slug)] || null,
  getModuleLoaded: (slug) => Boolean(get().moduleLoadedBySlug[normalizeSlug(slug)]),
  getModuleLoading: (slug) => Boolean(get().moduleLoadingBySlug[normalizeSlug(slug)]),
  getModuleError: (slug) => get().moduleErrorBySlug[normalizeSlug(slug)] || null,
  getLessonContent: (moduleId, lessonId) => get().lessonContentByKey[lessonContentKey(moduleId, lessonId)] || null,
  getLessonLoading: (moduleId, lessonId) => Boolean(get().lessonLoadingByKey[lessonContentKey(moduleId, lessonId)]),
  getLessonError: (moduleId, lessonId) => get().lessonErrorByKey[lessonContentKey(moduleId, lessonId)] || null,
  getProgressUpdating: (moduleId, lessonId) => Boolean(get().progressUpdatingByKey[`learning-module:progress:${moduleId}:${lessonId}`]),
  getQuizAttempts: (moduleId) => get().quizAttemptsByModuleId[moduleId] || EMPTY_ARRAY,
  getQuizAttemptsLoading: (moduleId) => Boolean(get().quizAttemptsLoadingByModuleId[moduleId]),
  getQuizReview: (moduleId, attemptId) => get().quizReviewByAttemptId[`${moduleId}:${attemptId}`] || null,
  getQuizReviewLoading: (moduleId, attemptId) => Boolean(get().quizReviewLoadingByAttemptId[`${moduleId}:${attemptId}`]),
  getFirstLessonId,
}));
