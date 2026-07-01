import axiosClient from "./axiosClient";

const encode = (value) => encodeURIComponent(value);


const MODULE_DETAIL_CACHE_MS = 20000;
const MODULE_TAB_CACHE_MS = 20000;

const moduleDetailCache = new Map();
const moduleDetailInFlight = new Map();
const moduleOverviewCache = new Map();
const moduleOverviewInFlight = new Map();
const moduleLessonsCache = new Map();
const moduleLessonsInFlight = new Map();
const moduleQuizCache = new Map();
const moduleQuizInFlight = new Map();

function now() {
  return Date.now();
}

function getCachedEntry(cache, key, maxAgeMs) {
  const entry = cache.get(key);

  if (!entry) return null;
  if (now() - entry.cachedAt > maxAgeMs) {
    cache.delete(key);
    return null;
  }

  return entry.value;
}

function invalidateModuleDetailCache(moduleId) {
  if (!moduleId) return;

  const key = String(moduleId);
  moduleDetailCache.delete(key);
  moduleDetailInFlight.delete(key);
  moduleOverviewCache.delete(key);
  moduleOverviewInFlight.delete(key);
  moduleLessonsCache.delete(key);
  moduleLessonsInFlight.delete(key);
  moduleQuizCache.delete(key);
  moduleQuizInFlight.delete(key);
}

function invalidateAuthoringCaches(moduleId) {
  invalidateModuleDetailCache(moduleId);
}

function requireModuleId(moduleId) {
  const key = String(moduleId || "");

  if (!key) {
    throw new Error("Learning module id is required.");
  }

  return key;
}

async function getCachedModuleResource({
  moduleId,
  force = false,
  cache,
  inFlight,
  path,
  normalize = (data) => data,
}) {
  const key = requireModuleId(moduleId);

  if (!force) {
    const entry = cache.get(key);

    if (entry && now() - entry.cachedAt <= MODULE_TAB_CACHE_MS) {
      return entry.value;
    }

    if (entry) {
      cache.delete(key);
    }

    if (inFlight.has(key)) {
      return inFlight.get(key);
    }
  }

  const request = axiosClient
    .get(path(key))
    .then((response) => {
      const value = normalize(response.data);
      cache.set(key, {
        value,
        cachedAt: now(),
      });

      return value;
    })
    .finally(() => {
      inFlight.delete(key);
    });

  inFlight.set(key, request);
  return request;
}

export function getLearningModuleRouteSegment(module) {
  if (typeof module === "string") return encode(module);

  return encode(
    module?.skillModuleId
      || module?.SkillModuleId
      || module?.id
      || module?.Id
      || "",
  );
}

export const learningModuleApi = {
  getPublishedModules: async () => {
    const response = await axiosClient.get("/learning-modules");
    return Array.isArray(response.data) ? response.data : [];
  },

  getEnrolledModules: async () => {
    const response = await axiosClient.get("/learning-modules/enrolled");
    return Array.isArray(response.data) ? response.data : [];
  },

  getPublishedModuleBySlug: async (slug) => {
    const response = await axiosClient.get(`/learning-modules/${encode(slug)}`);
    return response.data;
  },

  enroll: async (moduleId) => {
    const response = await axiosClient.post(`/learning-modules/${moduleId}/enroll`);
    return response.data;
  },

  getLessonContent: async (moduleId, lessonId) => {
    const response = await axiosClient.get(`/learning-modules/${moduleId}/lessons/${lessonId}`);
    return response.data;
  },

  updateLessonProgress: async (moduleId, lessonId, status) => {
    const response = await axiosClient.patch(
      `/learning-modules/${moduleId}/lessons/${lessonId}/progress`,
      { status },
    );
    return response.data;
  },

  getQuizAttempts: async (moduleId) => {
    const response = await axiosClient.get(`/learning-modules/${moduleId}/quiz/attempts`);
    return Array.isArray(response.data) ? response.data : [];
  },

  startQuizAttempt: async (moduleId) => {
    const response = await axiosClient.post(`/learning-modules/${moduleId}/quiz/attempts`);
    return response.data;
  },

  submitQuizAttempt: async (moduleId, attemptId, answers) => {
    const response = await axiosClient.post(
      `/learning-modules/${moduleId}/quiz/attempts/${attemptId}/submit`,
      { answers },
    );
    return response.data;
  },

  getQuizAttemptReview: async (moduleId, attemptId) => {
    const response = await axiosClient.get(`/learning-modules/${moduleId}/quiz/attempts/${attemptId}`);
    return response.data;
  },

  sendModuleChatMessage: async (moduleId, payload) => {
    const response = await axiosClient.post(`/learning-modules/${moduleId}/assistant/chat`, payload);
    return response.data;
  },
};

export const contentManagerLearningModuleApi = {
  searchSkills: async (search) => {
    const response = await axiosClient.get("/skills", {
      params: {
        search,
        limit: 20,
      },
    });

    if (Array.isArray(response.data)) return response.data;
    if (Array.isArray(response.data?.items)) return response.data.items;
    if (Array.isArray(response.data?.Items)) return response.data.Items;

    return [];
  },

  getModules: async ({
    status,
    search,
    difficulty,
    sort = "updated_desc",
    page = 1,
    pageSize = 20,
    signal,
  } = {}) => {
    const response = await axiosClient.get("/content/learning-modules", {
      params: {
        status: status && status !== "all" ? status : undefined,
        search: search?.trim() || undefined,
        difficulty: difficulty && difficulty !== "all" ? difficulty : undefined,
        sort,
        page,
        pageSize,
      },
      signal,
    });

    if (Array.isArray(response.data)) {
      return {
        items: response.data,
        totalCount: response.data.length,
        page: 1,
        pageSize: response.data.length,
        totalPages: response.data.length > 0 ? 1 : 0,
        statusCounts: {
          draft: response.data.filter((module) => module.status === "draft").length,
          published: response.data.filter((module) => module.status === "published").length,
          archived: response.data.filter((module) => module.status === "archived").length,
        },
      };
    }

    return {
      items: Array.isArray(response.data?.items) ? response.data.items : [],
      totalCount: response.data?.totalCount ?? 0,
      page: response.data?.page ?? 1,
      pageSize: response.data?.pageSize ?? pageSize,
      totalPages: response.data?.totalPages ?? 0,
      statusCounts: {
        draft: response.data?.statusCounts?.draft ?? 0,
        published: response.data?.statusCounts?.published ?? 0,
        archived: response.data?.statusCounts?.archived ?? 0,
      },
    };
  },

  createModule: async (payload) => {
    const response = await axiosClient.post("/content/learning-modules", payload);
    return response.data;
  },

  getModule: async (moduleId, { force = false } = {}) => {
    const key = requireModuleId(moduleId);

    if (!force) {
      const cached = getCachedEntry(moduleDetailCache, key, MODULE_DETAIL_CACHE_MS);
      if (cached) return cached;

      if (moduleDetailInFlight.has(key)) {
        return moduleDetailInFlight.get(key);
      }
    }

    const request = axiosClient
      .get(`/content/learning-modules/${encode(key)}`)
      .then((response) => {
        moduleDetailCache.set(key, {
          value: response.data,
          cachedAt: now(),
        });

        return response.data;
      })
      .finally(() => {
        moduleDetailInFlight.delete(key);
      });

    moduleDetailInFlight.set(key, request);
    return request;
  },

  getModuleOverview: async (moduleId, { force = false } = {}) => getCachedModuleResource({
    moduleId,
    force,
    cache: moduleOverviewCache,
    inFlight: moduleOverviewInFlight,
    path: (key) => `/content/learning-modules/${encode(key)}/overview`,
  }),

  getModuleLessons: async (moduleId, { force = false } = {}) => getCachedModuleResource({
    moduleId,
    force,
    cache: moduleLessonsCache,
    inFlight: moduleLessonsInFlight,
    path: (key) => `/content/learning-modules/${encode(key)}/lessons`,
    normalize: (data) => Array.isArray(data) ? data : [],
  }),

  getModuleQuiz: async (moduleId, { force = false } = {}) => getCachedModuleResource({
    moduleId,
    force,
    cache: moduleQuizCache,
    inFlight: moduleQuizInFlight,
    path: (key) => `/content/learning-modules/${encode(key)}/quiz`,
    normalize: (data) => data || null,
  }),

  getPublishReadiness: async (moduleId) => {
    const response = await axiosClient.get(
      `/content/learning-modules/${moduleId}/publish-readiness`,
    );
    return response.data;
  },

  updateModule: async (moduleId, payload) => {
    const response = await axiosClient.patch(`/content/learning-modules/${moduleId}`, payload);
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  deleteDraftModule: async (moduleId) => {
    const response = await axiosClient.delete(`/content/learning-modules/${moduleId}`);
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  publishModule: async (moduleId) => {
    const response = await axiosClient.post(`/content/learning-modules/${moduleId}/publish`);
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  archiveModule: async (moduleId) => {
    const response = await axiosClient.post(`/content/learning-modules/${moduleId}/archive`);
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  getPreview: async (moduleId) => {
    const response = await axiosClient.get(`/content/learning-modules/${moduleId}/preview`);
    return response.data;
  },

  bulkUploadLessons: async (moduleId, lessons, files) => {
    const formData = new FormData();
    formData.append("lessonsJson", JSON.stringify({ lessons }));

    files.forEach((file) => {
      formData.append("files", file);
    });

    const response = await axiosClient.post(
      `/content/learning-modules/${moduleId}/lessons/bulk`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );

    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  reorderLessons: async (moduleId, lessons) => {
    const response = await axiosClient.patch(
      `/content/learning-modules/${moduleId}/lessons/reorder`,
      { lessons },
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  updateLesson: async (moduleId, lessonId, payload) => {
    const response = await axiosClient.patch(
      `/content/learning-modules/${moduleId}/lessons/${lessonId}`,
      payload,
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  replaceLessonContent: async (moduleId, lessonId, file) => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await axiosClient.put(
      `/content/learning-modules/${moduleId}/lessons/${lessonId}/content`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );

    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  getLessonPreview: async (moduleId, lessonId) => {
    const response = await axiosClient.get(
      `/content/learning-modules/${moduleId}/lessons/${lessonId}/preview`,
    );
    return response.data;
  },

  reindexLesson: async (moduleId, lessonId) => {
    const response = await axiosClient.post(
      `/content/learning-modules/${moduleId}/lessons/${lessonId}/reindex`,
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  deleteLesson: async (moduleId, lessonId) => {
    const response = await axiosClient.delete(
      `/content/learning-modules/${moduleId}/lessons/${lessonId}`,
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  upsertQuiz: async (moduleId, payload) => {
    const response = await axiosClient.put(`/content/learning-modules/${moduleId}/quiz`, payload);
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  addQuestion: async (moduleId, payload) => {
    const response = await axiosClient.post(
      `/content/learning-modules/${moduleId}/quiz/questions`,
      payload,
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  updateQuestion: async (moduleId, questionId, payload) => {
    const response = await axiosClient.patch(
      `/content/learning-modules/${moduleId}/quiz/questions/${questionId}`,
      payload,
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  reorderQuestions: async (moduleId, questions) => {
    const response = await axiosClient.patch(
      `/content/learning-modules/${moduleId}/quiz/questions/reorder`,
      { questions },
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },

  deleteQuestion: async (moduleId, questionId) => {
    const response = await axiosClient.delete(
      `/content/learning-modules/${moduleId}/quiz/questions/${questionId}`,
    );
    invalidateAuthoringCaches(moduleId);
    return response.data;
  },
};
