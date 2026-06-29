const nodeTypeOrder = {
  phase: 0,
  group: 1,
  choice_group: 1,
  resource_group: 1,
  topic: 2,
  choice_option: 2,
  checkpoint: 3,
  project: 4,
};


export function formatVersionLabel(version) {
  if (!version) return "v-";
  if (version.versionLabel) return String(version.versionLabel);

  const major = Number(version.majorVersion);
  const minor = Number(version.minorVersion);
  const patch = Number(version.patchVersion);

  if (Number.isFinite(major) && Number.isFinite(minor) && Number.isFinite(patch)) {
    return `v${major}.${minor}.${patch}`;
  }

  const legacyVersion = Number(version.versionNumber);
  return Number.isFinite(legacyVersion) && legacyVersion > 0 ? `v${legacyVersion}.0.0` : "v-";
}

export function getNextMajorVersionLabel(versions = []) {
  const majorVersions = normalizeNodes(versions)
    .map((version) => Number(version?.majorVersion))
    .filter((value) => Number.isFinite(value) && value > 0);

  if (majorVersions.length > 0) {
    return `v${Math.max(...majorVersions) + 1}.0.0`;
  }

  const legacyVersions = normalizeNodes(versions)
    .map((version) => Number(version?.versionNumber))
    .filter((value) => Number.isFinite(value) && value > 0);

  return legacyVersions.length > 0 ? `v${Math.max(...legacyVersions) + 1}.0.0` : "v1.0.0";
}

export function getNextPatchVersionLabel(currentVersion, versions = []) {
  const major = Number(currentVersion?.majorVersion);
  const minor = Number(currentVersion?.minorVersion);

  if (!Number.isFinite(major) || !Number.isFinite(minor)) {
    return "v-";
  }

  const patchVersions = normalizeNodes(versions)
    .filter((version) => Number(version?.majorVersion) === major && Number(version?.minorVersion) === minor)
    .map((version) => Number(version?.patchVersion))
    .filter((value) => Number.isFinite(value) && value >= 0);

  const currentPatch = Number(currentVersion?.patchVersion);
  const nextPatch = Math.max(Number.isFinite(currentPatch) ? currentPatch : 0, ...patchVersions) + 1;

  return `v${major}.${minor}.${nextPatch}`;
}

export function prettyReleaseType(releaseType) {
  const normalized = String(releaseType || "").toLowerCase();
  if (!normalized) return "Release";
  if (normalized === "initial") return "Initial";
  return normalized.charAt(0).toUpperCase() + normalized.slice(1);
}

export function parsePage(value) {
  const parsed = Number.parseInt(value || "1", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1;
}

export function isCanceledRequest(error) {
  return error?.code === "ERR_CANCELED" || error?.name === "CanceledError";
}

export function normalizeNodes(nodes) {
  return Array.isArray(nodes) ? nodes : [];
}

export function normalizeEdges(edges) {
  return Array.isArray(edges) ? edges : [];
}

export function prettyStatus(status) {
  if (status === "all") return "All";
  if (!status) return "Unknown";
  return status.charAt(0).toUpperCase() + status.slice(1);
}

export function getStatusTone(status) {
  if (status === "published") return "green";
  if (status === "draft") return "blue";
  if (status === "archived") return "slate";
  return "slate";
}

export function getNodeTone(nodeType) {
  if (nodeType === "phase") return "purple";
  if (nodeType === "group" || nodeType === "choice_group" || nodeType === "resource_group") return "blue";
  if (nodeType === "project") return "amber";
  if (nodeType === "topic" || nodeType === "choice_option") return "green";
  if (nodeType === "checkpoint") return "purple";
  return "slate";
}

export function formatDate(value) {
  if (!value) return "-";

  try {
    return new Intl.DateTimeFormat(undefined, {
      month: "short",
      day: "numeric",
      year: "numeric",
    }).format(new Date(value));
  } catch {
    return "-";
  }
}

export function toFormNumber(value) {
  if (value === null || value === undefined) return "";
  return String(value);
}

export function fromFormNumber(value) {
  if (value === "" || value === null || value === undefined) return null;
  const numeric = Number(value);
  return Number.isFinite(numeric) ? numeric : null;
}

export function normalizeComparableText(value) {
  return String(value || "").trim();
}

export function normalizeComparableNumber(value) {
  return value === "" || value === null || value === undefined ? "" : String(value);
}

export function isSameNullableNumber(left, right) {
  return normalizeComparableNumber(left) === normalizeComparableNumber(right);
}

export function getNodeSortValue(node) {
  return [
    Number.isFinite(Number(node?.orderIndex)) ? Number(node.orderIndex) : Number.MAX_SAFE_INTEGER,
    nodeTypeOrder[node?.nodeType] ?? 99,
    String(node?.title || ""),
  ];
}

export function compareNodes(a, b) {
  const left = getNodeSortValue(a);
  const right = getNodeSortValue(b);

  for (let index = 0; index < left.length; index += 1) {
    if (left[index] < right[index]) return -1;
    if (left[index] > right[index]) return 1;
  }

  return 0;
}

export function getVisibleRoadmapNodes(nodes, searchTerm) {
  const allNodes = normalizeNodes(nodes).slice().sort(compareNodes);
  const normalizedSearch = searchTerm.trim().toLowerCase();

  if (!normalizedSearch) return allNodes;

  const byId = new Map(allNodes.map((node) => [String(node.roadmapNodeId), node]));
  const childrenByParent = new Map();

  allNodes.forEach((node) => {
    if (!node.parentNodeId) return;
    const parentId = String(node.parentNodeId);
    const children = childrenByParent.get(parentId) || [];
    children.push(node);
    childrenByParent.set(parentId, children);
  });

  const visibleIds = new Set();

  function includeAncestors(node) {
    let current = node;
    while (current?.parentNodeId && byId.has(String(current.parentNodeId))) {
      const parent = byId.get(String(current.parentNodeId));
      visibleIds.add(String(parent.roadmapNodeId));
      current = parent;
    }
  }

  function includeDescendants(node) {
    const children = childrenByParent.get(String(node.roadmapNodeId)) || [];
    children.forEach((child) => {
      const childId = String(child.roadmapNodeId);
      if (visibleIds.has(childId)) return;
      visibleIds.add(childId);
      includeDescendants(child);
    });
  }

  allNodes.forEach((node) => {
    const text = `${node.title || ""} ${node.nodeType || ""} ${node.description || ""}`.toLowerCase();
    if (!text.includes(normalizedSearch)) return;

    visibleIds.add(String(node.roadmapNodeId));
    includeAncestors(node);
    includeDescendants(node);
  });

  return allNodes.filter((node) => visibleIds.has(String(node.roadmapNodeId)));
}

export function countMissingMappings(nodes) {
  return normalizeNodes(nodes).filter((node) => (
    node.isTrackable
    && ((node.resources?.length || 0) === 0 || (node.skills?.length || 0) === 0)
  )).length;
}

export function countTrackableNodes(nodes) {
  return normalizeNodes(nodes).filter((node) => node.isTrackable).length;
}

export function normalizeTextList(value) {
  if (Array.isArray(value)) {
    return value.map((item) => String(item || "").trim()).filter(Boolean);
  }

  if (typeof value === "string") {
    return value.split(/\r?\n/).map((item) => item.trim()).filter(Boolean);
  }

  return [];
}

export function listToText(value) {
  return normalizeTextList(value).join("\n");
}

export function textToList(value) {
  return normalizeTextList(value);
}

export function getMetadataValue(metadata, key) {
  if (!metadata || !key) return undefined;
  if (Object.prototype.hasOwnProperty.call(metadata, key)) return metadata[key];

  const lowerKey = key.toLowerCase();
  const match = Object.keys(metadata).find((candidate) => candidate.toLowerCase() === lowerKey);
  return match ? metadata[match] : undefined;
}

export function getFirstMetadataText(metadata, keys) {
  for (const key of keys) {
    const value = getMetadataValue(metadata, key);
    if (typeof value === "string" && value.trim()) return value.trim();
  }

  return "";
}

export function getFirstMetadataList(metadata, keys) {
  for (const key of keys) {
    const list = normalizeTextList(getMetadataValue(metadata, key));
    if (list.length > 0) return list;
  }

  return [];
}

export function areTextListsEqual(left, right) {
  const leftList = normalizeTextList(left);
  const rightList = normalizeTextList(right);

  if (leftList.length !== rightList.length) return false;
  return leftList.every((item, index) => item === rightList[index]);
}
