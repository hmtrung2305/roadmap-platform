import axiosClient from "./axiosClient";

const encode = (value) => encodeURIComponent(value);

const adminModuleStatuses = ["draft", "published", "archived"];
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function getLearningModuleRouteSegment(module) {
  return encode(module?.slug || module?.Slug || module?.skillModuleId || module?.SkillModuleId || "");
}

function normalizeRouteSegment(value) {
  return decodeURIComponent(String(value || "")).trim().toLowerCase();
}

export const learningModuleApi = {
  getPublishedModules: async () => {
    const response = await axiosClient.get("/learning-modules");
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

export const counselorLearningModuleApi = {
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

  getModules: async (status) => {
    const response = await axiosClient.get("/counselor/learning-modules", {
      params: status && status !== "all" ? { status } : undefined,
    });
    return Array.isArray(response.data) ? response.data : [];
  },

  resolveModuleIdFromRoute: async (moduleSlugOrId) => {
    const routeValue = String(moduleSlugOrId || "").trim();

    if (guidPattern.test(routeValue)) {
      return routeValue;
    }

    const normalizedSlug = normalizeRouteSegment(routeValue);

    if (!normalizedSlug) {
      throw new Error("Learning module route is missing.");
    }

    const moduleLists = await Promise.all(
      adminModuleStatuses.map((status) => counselorLearningModuleApi.getModules(status)),
    );

    const module = moduleLists
      .flat()
      .find((item) => normalizeRouteSegment(item.slug || item.Slug) === normalizedSlug);

    if (!module?.skillModuleId && !module?.SkillModuleId) {
      throw new Error("Learning module was not found.");
    }

    return module.skillModuleId || module.SkillModuleId;
  },

  createModule: async (payload) => {
    const response = await axiosClient.post("/counselor/learning-modules", payload);
    return response.data;
  },

  getModule: async (moduleId) => {
    const response = await axiosClient.get(`/counselor/learning-modules/${moduleId}`);
    return response.data;
  },

  updateModule: async (moduleId, payload) => {
    const response = await axiosClient.patch(`/counselor/learning-modules/${moduleId}`, payload);
    return response.data;
  },

  deleteDraftModule: async (moduleId) => {
    const response = await axiosClient.delete(`/counselor/learning-modules/${moduleId}`);
    return response.data;
  },

  publishModule: async (moduleId) => {
    const response = await axiosClient.post(`/counselor/learning-modules/${moduleId}/publish`);
    return response.data;
  },

  archiveModule: async (moduleId) => {
    const response = await axiosClient.post(`/counselor/learning-modules/${moduleId}/archive`);
    return response.data;
  },

  restoreModule: async (moduleId) => {
    const response = await axiosClient.post(`/counselor/learning-modules/${moduleId}/restore`);
    return response.data;
  },

  getPreview: async (moduleId) => {
    const response = await axiosClient.get(`/counselor/learning-modules/${moduleId}/preview`);
    return response.data;
  },

  bulkUploadLessons: async (moduleId, lessons, files) => {
    const formData = new FormData();
    formData.append("lessonsJson", JSON.stringify({ lessons }));

    files.forEach((file) => {
      formData.append("files", file);
    });

    const response = await axiosClient.post(
      `/counselor/learning-modules/${moduleId}/lessons/bulk`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );

    return response.data;
  },

  reorderLessons: async (moduleId, lessons) => {
    const response = await axiosClient.patch(
      `/counselor/learning-modules/${moduleId}/lessons/reorder`,
      { lessons },
    );
    return response.data;
  },

  updateLesson: async (moduleId, lessonId, payload) => {
    const response = await axiosClient.patch(
      `/counselor/learning-modules/${moduleId}/lessons/${lessonId}`,
      payload,
    );
    return response.data;
  },

  replaceLessonContent: async (moduleId, lessonId, file) => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await axiosClient.put(
      `/counselor/learning-modules/${moduleId}/lessons/${lessonId}/content`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );

    return response.data;
  },

  getLessonPreview: async (moduleId, lessonId) => {
    const response = await axiosClient.get(
      `/counselor/learning-modules/${moduleId}/lessons/${lessonId}/preview`,
    );
    return response.data;
  },

  deleteLesson: async (moduleId, lessonId) => {
    const response = await axiosClient.delete(
      `/counselor/learning-modules/${moduleId}/lessons/${lessonId}`,
    );
    return response.data;
  },

  upsertQuiz: async (moduleId, payload) => {
    const response = await axiosClient.put(`/counselor/learning-modules/${moduleId}/quiz`, payload);
    return response.data;
  },

  addQuestion: async (moduleId, payload) => {
    const response = await axiosClient.post(
      `/counselor/learning-modules/${moduleId}/quiz/questions`,
      payload,
    );
    return response.data;
  },

  updateQuestion: async (moduleId, questionId, payload) => {
    const response = await axiosClient.patch(
      `/counselor/learning-modules/${moduleId}/quiz/questions/${questionId}`,
      payload,
    );
    return response.data;
  },

  reorderQuestions: async (moduleId, questions) => {
    const response = await axiosClient.patch(
      `/counselor/learning-modules/${moduleId}/quiz/questions/reorder`,
      { questions },
    );
    return response.data;
  },

  deleteQuestion: async (moduleId, questionId) => {
    const response = await axiosClient.delete(
      `/counselor/learning-modules/${moduleId}/quiz/questions/${questionId}`,
    );
    return response.data;
  },
};
