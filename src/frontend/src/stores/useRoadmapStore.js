import { create } from "zustand";
import { roadmapApi } from "../api/roadmapApi";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import {
  cachedRequest,
  guardedMutation,
  invalidateRequestCache,
  invalidateRequestCacheByPrefix,
  setCachedRequestData,
} from "../utils/requestCacheUtils";

const ROADMAP_CACHE_PREFIX = "roadmap";
const ROADMAPS_CACHE_KEY = [ROADMAP_CACHE_PREFIX, "list"];
const ROADMAPS_CACHE_TTL_MS = 5 * 60 * 1000;
const ENROLLMENT_CACHE_TTL_MS = 2 * 60 * 1000;
const ROADMAP_GRAPH_CACHE_TTL_MS = 5 * 60 * 1000;
const NODE_DETAIL_CACHE_TTL_MS = 10 * 60 * 1000;

let roadmapRequestVersion = 0;

function normalizeStringKey(value) {
  return String(value || "").trim();
}

function normalizeSlug(slug) {
  return normalizeStringKey(slug).toLowerCase();
}

function getEnrollmentCacheKey(roadmapVersionId) {
  return [ROADMAP_CACHE_PREFIX, "enrollment", roadmapVersionId];
}

function getGraphCacheKey(slug) {
  return [ROADMAP_CACHE_PREFIX, "graph", normalizeSlug(slug)];
}

export function getRoadmapNodeDetailKey(roadmapVersionId, nodeId) {
  return [ROADMAP_CACHE_PREFIX, "node-detail", roadmapVersionId, nodeId]
    .filter(Boolean)
    .map(String)
    .join(":");
}

function getNodeDetailCacheKey(roadmapVersionId, nodeId) {
  return [ROADMAP_CACHE_PREFIX, "node-detail", roadmapVersionId, nodeId];
}

function getNodeId(node) {
  return String(node?.roadmapNodeId || node?.roadmapNodeID || node?.nodeId || node?.id || "");
}

function normalizeRoadmapGraph(data) {
  if (!data || typeof data !== "object") return data;

  return {
    ...data,
    nodes: data.nodes || data.roadmapNodes || data.roadmapVersionNodes || [],
    edges: data.edges || data.roadmapEdges || data.dependencies || [],
    layoutDirection: data.layoutDirection || "TB",
    layoutAlgorithm: data.layoutAlgorithm || "custom",
  };
}

function mergeRoadmapWithEnrollment(roadmap, enrollment) {
  if (!roadmap) return roadmap;

  if (!enrollment) {
    return {
      ...roadmap,
      enrollment: enrollment ?? roadmap.enrollment ?? null,
      progressPercent: roadmap.progressPercent ?? 0,
    };
  }

  return {
    ...roadmap,
    enrollment,
    progressPercent: enrollment.progressPercent ?? roadmap.progressPercent ?? 0,
  };
}

export function getRoadmapVersionId(roadmap) {
  return (
    roadmap?.roadmapVersionId ||
    roadmap?.roadmapVersionID ||
    roadmap?.versionId ||
    roadmap?.versionID ||
    roadmap?.currentVersion?.roadmapVersionId ||
    roadmap?.currentVersion?.versionId ||
    roadmap?.activeVersion?.roadmapVersionId ||
    roadmap?.activeVersion?.versionId ||
    roadmap?.version?.roadmapVersionId ||
    roadmap?.version?.versionId ||
    null
  );
}

function toRecordValue(record, key, value) {
  return {
    ...record,
    [String(key)]: value,
  };
}

function removeRecordKey(record, key) {
  const nextRecord = { ...record };
  delete nextRecord[String(key)];
  return nextRecord;
}

function applyProgressUpdateResultToRoadmap(roadmap, result) {
  if (!roadmap || !result) return roadmap;

  const changedNodes = result.changedNodes || result.changedProgress || [];
  const changedByNodeId = new Map(
    changedNodes
      .map((progress) => [
        String(progress.roadmapNodeId || progress.nodeId || progress.id || ""),
        progress,
      ])
      .filter(([nodeId]) => nodeId && nodeId !== "undefined"),
  );

  return {
    ...roadmap,
    enrollment: result.enrollment || roadmap.enrollment,
    trackableNodeCount: result.trackableNodeCount ?? roadmap.trackableNodeCount,
    completedNodeCount: result.completedNodeCount ?? roadmap.completedNodeCount,
    progressPercent: result.progressPercent ?? roadmap.progressPercent,
    nodes: (roadmap.nodes || []).map((node) => {
      const changedProgress = changedByNodeId.get(getNodeId(node));

      if (!changedProgress) return node;

      return {
        ...node,
        progress: {
          ...(node.progress || {}),
          ...changedProgress,
        },
      };
    }),
  };
}

function patchNodeDetailProgress(detail, result) {
  if (!detail || !result) return detail;

  const nodeId = getNodeId(detail);
  const changedNodes = result.changedNodes || result.changedProgress || [];
  const changedProgress = changedNodes.find((progress) => {
    const changedNodeId = String(progress.roadmapNodeId || progress.nodeId || progress.id || "");
    return changedNodeId === nodeId;
  });

  if (!changedProgress) return detail;

  return {
    ...detail,
    progress: {
      ...(detail.progress || {}),
      ...changedProgress,
    },
  };
}

function updateGraphCacheForSlug(slug, roadmap) {
  if (!slug || !roadmap) return;
  setCachedRequestData(getGraphCacheKey(slug), roadmap);
}

export const useRoadmapStore = create((set, get) => ({
  roadmaps: [],
  roadmapsStatus: "idle",
  roadmapsError: "",
  roadmapsFetchedAt: 0,

  enrollmentByVersionId: {},
  enrollmentLoadingByVersionId: {},
  enrollmentErrorByVersionId: {},

  graphBySlug: {},
  graphStatusBySlug: {},
  graphErrorBySlug: {},
  graphFetchedAtBySlug: {},

  nodeDetailByKey: {},
  nodeDetailLoadingByKey: {},

  enrollingByVersionId: {},
  progressUpdatingByNodeId: {},

  getRoadmapsWithEnrollments: () => {
    const state = get();

    return state.roadmaps.map((roadmap) => {
      const roadmapVersionId = getRoadmapVersionId(roadmap);
      const enrollmentKey = String(roadmapVersionId);
      const hasEnrollment = Object.prototype.hasOwnProperty.call(
        state.enrollmentByVersionId,
        enrollmentKey,
      );
      const enrollment = hasEnrollment
        ? state.enrollmentByVersionId[enrollmentKey]
        : roadmap.enrollment ?? null;

      return mergeRoadmapWithEnrollment(roadmap, enrollment);
    });
  },

  loadRoadmaps: async ({ force = false, includeEnrollments = true } = {}) => {
    const requestVersion = roadmapRequestVersion;

    try {
      set((current) => ({
        roadmapsStatus: current.roadmaps.length > 0 ? "success" : "loading",
        roadmapsError: "",
      }));

      const data = await cachedRequest(
        ROADMAPS_CACHE_KEY,
        roadmapApi.getRoadmaps,
        {
          ttlMs: ROADMAPS_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion !== roadmapRequestVersion) {
        return data;
      }

      const roadmaps = Array.isArray(data) ? data : [];

      set({
        roadmaps,
        roadmapsStatus: "success",
        roadmapsError: "",
        roadmapsFetchedAt: Date.now(),
      });

      if (includeEnrollments) {
        await get().loadEnrollmentsForRoadmaps(roadmaps, { force });
      }

      return get().getRoadmapsWithEnrollments();
    } catch (error) {
      if (requestVersion === roadmapRequestVersion) {
        set({
          roadmapsStatus: "error",
          roadmapsError: getFriendlyApiErrorMessage(
            error,
            "Failed to load roadmaps. Check that the API is running.",
          ),
        });
      }

      throw error;
    }
  },

  loadEnrollment: async (roadmapVersionId, { force = false } = {}) => {
    const versionId = normalizeStringKey(roadmapVersionId);

    if (!versionId) return null;

    const requestVersion = roadmapRequestVersion;

    try {
      set((state) => ({
        enrollmentLoadingByVersionId: toRecordValue(
          state.enrollmentLoadingByVersionId,
          versionId,
          true,
        ),
        enrollmentErrorByVersionId: removeRecordKey(
          state.enrollmentErrorByVersionId,
          versionId,
        ),
      }));

      const enrollment = await cachedRequest(
        getEnrollmentCacheKey(versionId),
        () => roadmapApi.getCurrentEnrollment(versionId),
        {
          ttlMs: ENROLLMENT_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion !== roadmapRequestVersion) {
        return enrollment;
      }

      set((state) => ({
        enrollmentByVersionId: toRecordValue(
          state.enrollmentByVersionId,
          versionId,
          enrollment,
        ),
        enrollmentLoadingByVersionId: removeRecordKey(
          state.enrollmentLoadingByVersionId,
          versionId,
        ),
      }));

      return enrollment;
    } catch (error) {
      if (requestVersion === roadmapRequestVersion) {
        set((state) => ({
          enrollmentLoadingByVersionId: removeRecordKey(
            state.enrollmentLoadingByVersionId,
            versionId,
          ),
          enrollmentErrorByVersionId: toRecordValue(
            state.enrollmentErrorByVersionId,
            versionId,
            getFriendlyApiErrorMessage(error, "Unable to load roadmap progress."),
          ),
        }));
      }

      throw error;
    }
  },

  loadEnrollmentsForRoadmaps: async (roadmaps, { force = false } = {}) => {
    const roadmapsWithVersion = (roadmaps || []).filter((roadmap) => getRoadmapVersionId(roadmap));

    if (roadmapsWithVersion.length === 0) return [];

    const results = await Promise.allSettled(
      roadmapsWithVersion.map((roadmap) =>
        get().loadEnrollment(getRoadmapVersionId(roadmap), { force }),
      ),
    );

    return results;
  },

  loadRoadmapGraph: async (slug, { force = false } = {}) => {
    const requestSlug = normalizeStringKey(slug);
    const normalizedSlug = normalizeSlug(slug);

    if (!requestSlug || !normalizedSlug) return null;

    const requestVersion = roadmapRequestVersion;

    try {
      set((state) => ({
        graphStatusBySlug: toRecordValue(
          state.graphStatusBySlug,
          normalizedSlug,
          state.graphBySlug[normalizedSlug] ? "success" : "loading",
        ),
        graphErrorBySlug: removeRecordKey(state.graphErrorBySlug, normalizedSlug),
      }));

      const graph = await cachedRequest(
        getGraphCacheKey(normalizedSlug),
        async () => normalizeRoadmapGraph(await roadmapApi.getRoadmapGraph(requestSlug)),
        {
          ttlMs: ROADMAP_GRAPH_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion !== roadmapRequestVersion) {
        return graph;
      }

      set((state) => ({
        graphBySlug: toRecordValue(state.graphBySlug, normalizedSlug, graph),
        graphStatusBySlug: toRecordValue(state.graphStatusBySlug, normalizedSlug, "success"),
        graphFetchedAtBySlug: toRecordValue(state.graphFetchedAtBySlug, normalizedSlug, Date.now()),
      }));

      return graph;
    } catch (error) {
      if (requestVersion === roadmapRequestVersion) {
        set((state) => ({
          graphStatusBySlug: toRecordValue(state.graphStatusBySlug, normalizedSlug, "error"),
          graphErrorBySlug: toRecordValue(
            state.graphErrorBySlug,
            normalizedSlug,
            getFriendlyApiErrorMessage(
              error,
              "Failed to load roadmap. Check that the API is running.",
            ),
          ),
        }));
      }

      throw error;
    }
  },

  loadNodeDetail: async ({ roadmapVersionId, nodeId }, { force = false } = {}) => {
    const versionId = normalizeStringKey(roadmapVersionId);
    const normalizedNodeId = normalizeStringKey(nodeId);

    if (!versionId || !normalizedNodeId) return null;

    const detailKey = getRoadmapNodeDetailKey(versionId, normalizedNodeId);
    const requestVersion = roadmapRequestVersion;

    try {
      set((state) => ({
        nodeDetailLoadingByKey: toRecordValue(state.nodeDetailLoadingByKey, detailKey, true),
      }));

      const detail = await cachedRequest(
        getNodeDetailCacheKey(versionId, normalizedNodeId),
        () => roadmapApi.getNodeDetail(versionId, normalizedNodeId),
        {
          ttlMs: NODE_DETAIL_CACHE_TTL_MS,
          force,
        },
      );

      if (requestVersion !== roadmapRequestVersion) {
        return detail;
      }

      set((state) => ({
        nodeDetailByKey: toRecordValue(state.nodeDetailByKey, detailKey, detail),
        nodeDetailLoadingByKey: removeRecordKey(state.nodeDetailLoadingByKey, detailKey),
      }));

      return detail;
    } catch (error) {
      if (requestVersion === roadmapRequestVersion) {
        set((state) => ({
          nodeDetailLoadingByKey: removeRecordKey(state.nodeDetailLoadingByKey, detailKey),
        }));
      }

      throw error;
    }
  },

  enroll: async (roadmapVersionId, { slug } = {}) => {
    const versionId = normalizeStringKey(roadmapVersionId);

    if (!versionId) return null;

    const requestVersion = roadmapRequestVersion;

    try {
      set((state) => ({
        enrollingByVersionId: toRecordValue(state.enrollingByVersionId, versionId, true),
      }));

      const data = await guardedMutation(
        [ROADMAP_CACHE_PREFIX, "enroll", versionId],
        () => roadmapApi.enroll(versionId),
      );

      invalidateRequestCache(getEnrollmentCacheKey(versionId));

      if (slug) {
        invalidateRequestCache(getGraphCacheKey(slug));
      }

      if (requestVersion !== roadmapRequestVersion) {
        return data;
      }

      const enrollment = await get().loadEnrollment(versionId, { force: true });
      const graph = slug ? await get().loadRoadmapGraph(slug, { force: true }) : null;

      return {
        response: data,
        enrollment,
        graph,
      };
    } finally {
      if (requestVersion === roadmapRequestVersion) {
        set((state) => ({
          enrollingByVersionId: removeRecordKey(state.enrollingByVersionId, versionId),
        }));
      }
    }
  },

  updateNodeProgress: async ({ enrollmentId, nodeId, status, slug, roadmapVersionId }) => {
    const normalizedNodeId = normalizeStringKey(nodeId);
    const versionId = normalizeStringKey(roadmapVersionId);

    if (!enrollmentId || !normalizedNodeId) return null;

    const requestVersion = roadmapRequestVersion;

    try {
      set((state) => ({
        progressUpdatingByNodeId: toRecordValue(
          state.progressUpdatingByNodeId,
          normalizedNodeId,
          true,
        ),
      }));

      const result = await guardedMutation(
        [ROADMAP_CACHE_PREFIX, "progress", enrollmentId, normalizedNodeId],
        () => roadmapApi.updateNodeProgress({
          enrollmentId,
          nodeId: normalizedNodeId,
          status,
        }),
      );

      if (requestVersion !== roadmapRequestVersion) {
        return result;
      }

      const normalizedSlug = normalizeSlug(slug);

      if (normalizedSlug) {
        set((state) => {
          const currentGraph = state.graphBySlug[normalizedSlug];
          const nextGraph = applyProgressUpdateResultToRoadmap(currentGraph, result);

          if (nextGraph) {
            updateGraphCacheForSlug(normalizedSlug, nextGraph);
          }

          return {
            graphBySlug: nextGraph
              ? toRecordValue(state.graphBySlug, normalizedSlug, nextGraph)
              : state.graphBySlug,
          };
        });
      }

      if (versionId) {
        const detailKey = getRoadmapNodeDetailKey(versionId, normalizedNodeId);

        set((state) => {
          const currentDetail = state.nodeDetailByKey[detailKey];
          const nextDetail = patchNodeDetailProgress(currentDetail, result);

          if (nextDetail) {
            setCachedRequestData(getNodeDetailCacheKey(versionId, normalizedNodeId), nextDetail);
          }

          return {
            nodeDetailByKey: nextDetail
              ? toRecordValue(state.nodeDetailByKey, detailKey, nextDetail)
              : state.nodeDetailByKey,
          };
        });
      }

      if (result?.enrollment) {
        const resultVersionId =
          result.enrollment.roadmapVersionId || result.enrollment.versionId || versionId;

        if (resultVersionId) {
          set((state) => ({
            enrollmentByVersionId: toRecordValue(
              state.enrollmentByVersionId,
              resultVersionId,
              result.enrollment,
            ),
          }));
          setCachedRequestData(getEnrollmentCacheKey(resultVersionId), result.enrollment);
        }
      }

      return result;
    } finally {
      if (requestVersion === roadmapRequestVersion) {
        set((state) => ({
          progressUpdatingByNodeId: removeRecordKey(
            state.progressUpdatingByNodeId,
            normalizedNodeId,
          ),
        }));
      }
    }
  },

  invalidateRoadmaps: () => {
    invalidateRequestCache(ROADMAPS_CACHE_KEY);
    set({ roadmapsFetchedAt: 0 });
  },

  invalidateRoadmapGraph: (slug) => {
    const normalizedSlug = normalizeSlug(slug);

    if (!normalizedSlug) return;

    invalidateRequestCache(getGraphCacheKey(normalizedSlug));
    set((state) => ({
      graphFetchedAtBySlug: removeRecordKey(state.graphFetchedAtBySlug, normalizedSlug),
    }));
  },

  resetRoadmaps: () => {
    roadmapRequestVersion += 1;
    invalidateRequestCacheByPrefix(ROADMAP_CACHE_PREFIX);

    set({
      roadmaps: [],
      roadmapsStatus: "idle",
      roadmapsError: "",
      roadmapsFetchedAt: 0,
      enrollmentByVersionId: {},
      enrollmentLoadingByVersionId: {},
      enrollmentErrorByVersionId: {},
      graphBySlug: {},
      graphStatusBySlug: {},
      graphErrorBySlug: {},
      graphFetchedAtBySlug: {},
      nodeDetailByKey: {},
      nodeDetailLoadingByKey: {},
      enrollingByVersionId: {},
      progressUpdatingByNodeId: {},
    });
  },

  clearRoadmapError: () => {
    set({ roadmapsError: "" });
  },
}));
