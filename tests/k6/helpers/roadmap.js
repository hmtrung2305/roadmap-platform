import http from "k6/http";
import { check, fail } from "k6";
import { config, jsonParams } from "./config.js";
import { optionalAuthCookieParams } from "./auth-cookie.js";

function unwrap(body) {
  return body?.data || body?.result || body;
}

function getNodes(graph) {
  return (
    graph?.nodes ||
    graph?.roadmapNodes ||
    graph?.graph?.nodes ||
    graph?.data?.nodes ||
    []
  );
}

function getEdges(graph) {
  return (
    graph?.edges ||
    graph?.roadmapEdges ||
    graph?.graph?.edges ||
    graph?.data?.edges ||
    []
  );
}

function getVersionId(graph) {
  return (
    config.roadmapVersionId ||
    graph?.roadmapVersionId ||
    graph?.versionId ||
    graph?.id ||
    graph?.roadmapVersion?.roadmapVersionId ||
    graph?.roadmapVersion?.id ||
    graph?.version?.roadmapVersionId ||
    graph?.version?.id
  );
}

function getNodeId(node) {
  return node?.roadmapNodeId || node?.nodeId || node?.id;
}

function isTrackableNode(node) {
  if (node?.isTrackable === true) return true;

  const type = String(node?.nodeType || node?.node_type || node?.type || "").toLowerCase();
  return type === "topic" || type === "project";
}

function isNotLocked(node) {
  const status = String(node?.status || node?.progressStatus || "").toLowerCase();
  return status !== "locked";
}

export function fetchRoadmapGraph(cookieHeader = "") {
  const res = http.get(
    `${config.baseUrl}/api/roadmaps/${config.roadmapSlug}/graph`,
    optionalAuthCookieParams(cookieHeader)
  );

  check(res, {
    "setup graph status is 200": (r) => r.status === 200,
  });

  if (res.status !== 200) {
    fail(`Could not load roadmap graph during setup. Status: ${res.status}. Body: ${res.body}`);
  }

  return unwrap(res.json());
}

export function resolveRoadmapIds(cookieHeader = "") {
  if (config.roadmapVersionId && config.roadmapNodeId) {
    return {
      roadmapVersionId: config.roadmapVersionId,
      roadmapNodeId: config.roadmapNodeId,
    };
  }

  const graph = fetchRoadmapGraph(cookieHeader);
  const nodes = getNodes(graph);
  const versionId = getVersionId(graph);

  if (!versionId) {
    fail("Could not resolve roadmapVersionId from graph response. Set ROADMAP_VERSION_ID manually.");
  }

  const preferredNode =
    nodes.find((node) => isTrackableNode(node) && isNotLocked(node)) ||
    nodes.find((node) => isTrackableNode(node)) ||
    nodes.find((node) => getNodeId(node));

  const nodeId = config.roadmapNodeId || getNodeId(preferredNode);

  if (!nodeId) {
    fail("Could not resolve roadmapNodeId from graph response. Set ROADMAP_NODE_ID manually.");
  }

  return {
    roadmapVersionId: versionId,
    roadmapNodeId: nodeId,
  };
}

export function graphSizeChecks(res) {
  return check(res, {
    "graph status is 200": (r) => r.status === 200,
    "graph has nodes": (r) => {
      if (r.status !== 200) return false;
      const graph = unwrap(r.json());
      return getNodes(graph).length > 0;
    },
    "graph has edges field": (r) => {
      if (r.status !== 200) return false;
      const graph = unwrap(r.json());
      return Array.isArray(getEdges(graph));
    },
  });
}
