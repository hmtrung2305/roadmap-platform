import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  Background,
  Controls,
  MiniMap,
  ReactFlow,
  useEdgesState,
  useNodesState,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";

import {
  getRoadmapNodeDetailKey,
  getRoadmapVersionId,
  useRoadmapStore,
} from "../stores/useRoadmapStore";
import BalancedRoadmapEdge from "../components/roadmap/BalancedRoadmapEdge";
import RoadmapDetailDrawer from "../components/roadmap/RoadmapDetailDrawer";
import RoadmapFlowNode from "../components/roadmap/RoadmapFlowNode";
import RoadmapFullScreen from "../components/roadmap/RoadmapFullScreen";
import RoadmapLegend from "../components/roadmap/RoadmapLegend";
import {
  buildRoadmapFlow,
  focusRoadmapStart,
  getMiniMapColor,
  getNodeId,
  mergeGraphNodeWithDetail,
  patchNodeProgress,
  applyProgressUpdateResult,
  findChangedProgress,
} from "../components/roadmap/roadmapUtils";

const nodeTypes = {
  roadmapNode: RoadmapFlowNode,
};

const edgeTypes = {
  balanced: BalancedRoadmapEdge,
};

export default function RoadmapViewerPage() {
  const navigate = useNavigate();
  const { slug } = useParams();

  const [roadmap, setRoadmap] = useState(null);
  const [selectedNode, setSelectedNode] = useState(null);
  const [isLoadingNodeDetail, setIsLoadingNodeDetail] = useState(false);
  const [status, setStatus] = useState("loading");
  const [message, setMessage] = useState("");
  const [isEnrolling, setIsEnrolling] = useState(false);
  const [isUpdatingProgress, setIsUpdatingProgress] = useState(false);
  const [isLegendOpen, setIsLegendOpen] = useState(false);
  const [flowInstance, setFlowInstance] = useState(null);
  const [isInitialViewportReady, setIsInitialViewportReady] = useState(false);
  const flowWrapperRef = useRef(null);
  const focusedRoadmapKeyRef = useRef(null);
  const loadRoadmapGraph = useRoadmapStore((state) => state.loadRoadmapGraph);
  const loadNodeDetailFromStore = useRoadmapStore((state) => state.loadNodeDetail);
  const enrollInRoadmap = useRoadmapStore((state) => state.enroll);
  const updateNodeProgressInStore = useRoadmapStore((state) => state.updateNodeProgress);
  const nodeDetailByKey = useRoadmapStore((state) => state.nodeDetailByKey);
  const loadRequestRef = useRef(0);
  const nodeDetailRequestRef = useRef(0);
  const roadmapVersionId = getRoadmapVersionId(roadmap);

  const { flowNodes, flowEdges, nodeLookup } = useMemo(() => {
    if (!roadmap?.nodes?.length) {
      return { flowNodes: [], flowEdges: [], nodeLookup: new Map() };
    }

    return buildRoadmapFlow(
      roadmap,
      selectedNode ? getNodeId(selectedNode) : null,
    );
  }, [roadmap, selectedNode]);

  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);

  useEffect(() => {
    setNodes(flowNodes);
    setEdges(flowEdges);
  }, [flowNodes, flowEdges, setNodes, setEdges]);

  useLayoutEffect(() => {
    if (!flowInstance || flowNodes.length === 0) return;

    const roadmapFocusKey =
      roadmapVersionId || slug || "current-roadmap";

    if (focusedRoadmapKeyRef.current === roadmapFocusKey) {
      setIsInitialViewportReady(true);
      return;
    }

    focusedRoadmapKeyRef.current = roadmapFocusKey;
    focusRoadmapStart(flowInstance, flowNodes, flowWrapperRef.current);
    setIsInitialViewportReady(true);
  }, [flowInstance, flowNodes, roadmapVersionId, slug]);

  useEffect(() => {
    loadPage();
  }, [slug]);

  useEffect(() => {
    function handleEscape(event) {
      if (event.key === "Escape") {
        setSelectedNode(null);
      }
    }

    window.addEventListener("keydown", handleEscape);
    return () => window.removeEventListener("keydown", handleEscape);
  }, []);

  const handleNodeClick = useCallback(
    (_, node) => {
      const sourceNode = nodeLookup.get(node.id);

      if (sourceNode) {
        loadNodeDetail(sourceNode);
      }
    },
    [nodeLookup, roadmapVersionId, nodeDetailByKey],
  );

  async function loadNodeDetail(graphNode) {
    if (!graphNode || !roadmapVersionId) return;

    const requestId = nodeDetailRequestRef.current + 1;
    nodeDetailRequestRef.current = requestId;

    const nodeId = getNodeId(graphNode);
    const detailKey = getRoadmapNodeDetailKey(roadmapVersionId, nodeId);
    const cachedNode = nodeDetailByKey[detailKey];

    if (cachedNode) {
      setSelectedNode(mergeGraphNodeWithDetail(graphNode, cachedNode));
      setIsLoadingNodeDetail(false);
      return;
    }

    setSelectedNode(graphNode);
    setIsLoadingNodeDetail(true);
    setMessage("");

    try {
      const detail = await loadNodeDetailFromStore({
        roadmapVersionId,
        nodeId,
      });

      if (nodeDetailRequestRef.current !== requestId) return;

      setSelectedNode(mergeGraphNodeWithDetail(graphNode, detail));
    } catch (error) {
      if (nodeDetailRequestRef.current !== requestId) return;

      console.error(error);
      setMessage(error?.message || "Failed to load node details.");
    } finally {
      if (nodeDetailRequestRef.current === requestId) {
        setIsLoadingNodeDetail(false);
      }
    }
  }

  async function loadPage({ force = false } = {}) {
    if (!slug) return;

    const requestId = loadRequestRef.current + 1;
    loadRequestRef.current = requestId;

    nodeDetailRequestRef.current += 1;
    setIsLoadingNodeDetail(false);
    setStatus("loading");
    setMessage("");

    try {
      const normalizedRoadmap = await loadRoadmapGraph(slug, { force });

      if (loadRequestRef.current !== requestId) return;

      focusedRoadmapKeyRef.current = null;
      setIsInitialViewportReady(false);
      setRoadmap(normalizedRoadmap);
      setSelectedNode(null);
      setStatus("success");
    } catch (error) {
      if (loadRequestRef.current !== requestId) return;

      console.error(error);
      setMessage(
        error?.message ||
          "Failed to load roadmap. Check that the API is running.",
      );
      setStatus("error");
    }
  }

  async function handleEnroll() {
    if (!roadmapVersionId || isEnrolling) return;

    setIsEnrolling(true);
    setMessage("");

    try {
      const enrollResult = await enrollInRoadmap(roadmapVersionId, { slug });

      if (enrollResult?.graph) {
        focusedRoadmapKeyRef.current = null;
        setIsInitialViewportReady(false);
        setRoadmap(enrollResult.graph);
        setSelectedNode(null);
        setStatus("success");
      } else {
        await loadPage({ force: true });
      }
    } catch (error) {
      console.error(error);
      setMessage(error?.message || "Failed to enroll in roadmap.");
    } finally {
      setIsEnrolling(false);
    }
  }

  async function handleProgressChange(nextStatus) {
    const enrollmentId = roadmap?.enrollment?.roadmapEnrollmentId;
    const selectedNodeId = selectedNode ? getNodeId(selectedNode) : null;

    if (!enrollmentId || !selectedNodeId || isUpdatingProgress) return;

    setIsUpdatingProgress(true);
    setMessage("");

    const previousRoadmap = roadmap;
    const previousSelectedNode = selectedNode;

    setRoadmap((current) =>
      patchNodeProgress(current, selectedNodeId, nextStatus),
    );

    setSelectedNode((current) =>
      current
        ? {
            ...current,
            progress: {
              ...(current.progress || {}),
              status: nextStatus,
            },
          }
        : current,
    );

    try {
      const result = await updateNodeProgressInStore({
        enrollmentId,
        nodeId: selectedNodeId,
        status: nextStatus,
        slug,
        roadmapVersionId,
      });

      setRoadmap((current) => applyProgressUpdateResult(current, result));

      setSelectedNode((current) => {
        if (!current) return current;

        const changedProgress = findChangedProgress(result, selectedNodeId);
        const nextStatusFromResult = changedProgress?.status || nextStatus;

        return {
          ...current,
          progress: {
            ...(current.progress || {}),
            ...(changedProgress || {}),
            status: nextStatusFromResult,
          },
        };
      });
    } catch (error) {
      console.error(error);
      setRoadmap(previousRoadmap);
      setSelectedNode(previousSelectedNode);
      setMessage(error?.message || "Failed to update progress.");
    } finally {
      setIsUpdatingProgress(false);
    }
  }

  if (status === "loading") {
    return <RoadmapFullScreen>Loading roadmap…</RoadmapFullScreen>;
  }

  if (status === "error") {
    return (
      <div className="min-h-[calc(100vh-64px)] bg-[#F7F1E8] text-[#18332D]">
        <main className="mx-auto flex min-h-[calc(100vh-64px)] max-w-7xl items-center justify-center px-6 py-8">
          <div className="max-w-md rounded-lg border border-[#B9D8CC] bg-white p-8 text-center shadow-lg">
            <h1 className="text-2xl font-black text-[#18332D]">
              Couldn&apos;t load roadmap
            </h1>

            <p className="mt-3 text-sm font-semibold leading-6 text-slate-600">
              {message}
            </p>

            <div className="mt-6 flex justify-center gap-3">
              <button
                type="button"
                onClick={() => navigate("/roadmaps")}
                className="rounded-lg border border-[#B9D8CC] bg-white px-5 py-2 text-sm font-extrabold shadow-sm"
              >
                Back
              </button>

              <button
                type="button"
                onClick={() => loadPage({ force: true })}
                className="rounded-lg border border-[#B9D8CC] bg-[#2FA084] px-5 py-2 text-sm font-extrabold text-white shadow-sm"
              >
                Retry
              </button>
            </div>
          </div>
        </main>
      </div>
    );
  }

  const progressPercent =
    roadmap?.progressPercent ?? roadmap?.enrollment?.progressPercent ?? 0;
  const isEnrolled = Boolean(roadmap?.enrollment);
  const shouldShowMiniMap = nodes.length <= 150;

  return (
    <div className="flex min-h-[calc(100vh-64px)] flex-col bg-[#F7F1E8] text-[#18332D]">
      <main
        className="relative min-h-0 w-full overflow-hidden"
        style={{ height: "calc(100vh - 64px)" }}
      >
        <section className="grid h-full min-h-0 w-full grid-rows-[auto_minmax(0,1fr)]">
          <div className="z-10 border-b border-[#B9D8CC] bg-white/95 px-2 py-1 shadow-sm backdrop-blur">
            <div className="flex min-h-10 items-center gap-1">
              <button
                type="button"
                onClick={() => navigate("/roadmaps")}
                className="shrink-0 border border-transparent !text-[14px] transition transition-color px-2 !font-semibold cursor-pointer tracking-[0.12em] text-[#1F6F5F] hover:text-[#03221c]"
              >
                ← All Roadmaps
              </button>

              <div className="min-w-0 border-l border-[#B9D8CC] pl-3">
                <h1 className="truncate text-lg font-black tracking-tight text-[#18332D] sm:text-xl">
                  {getViewerHeading(roadmap)}
                </h1>
              </div>
            </div>

            {message && (
              <div className="mt-2 rounded-lg border border-[#B9D8CC] bg-[#FEE2E2] px-3 py-2 text-xs font-black text-[#18332D]">
                {message}
              </div>
            )}
          </div>

          <div className="relative min-h-0 w-full overflow-hidden bg-[#F7F1E8]">
            <div className="pointer-events-none absolute right-4 top-4 z-20 flex flex-row items-stretch gap-2">
              <div className="pointer-events-auto flex h-12 w-36 flex-col justify-center rounded-lg border border-[#B9D8CC] bg-white/95 px-3 shadow-sm backdrop-blur sm:w-44">
                <div className="flex items-center justify-between gap-2 text-[11px] font-extrabold tracking-tight text-slate-500">
                  <span>Progress</span>
                  <span>{Math.round(progressPercent)}%</span>
                </div>

                <div className="mt-1.5 h-1.5 overflow-hidden rounded-md border border-[#B9D8CC] bg-white">
                  <div
                    className="h-full rounded-md bg-[#22C55E]"
                    style={{
                      width: `${Math.min(100, Math.max(0, progressPercent))}%`,
                    }}
                  />
                </div>
              </div>

              {!isEnrolled && (
                <button
                  type="button"
                  onClick={handleEnroll}
                  disabled={isEnrolling}
                  className="pointer-events-auto h-12 rounded-lg border border-[#B9D8CC] bg-[#2FA084] px-4 text-xs font-extrabold tracking-[0.08em] text-white shadow-sm disabled:opacity-60"
                >
                  {isEnrolling ? "Starting..." : "Start roadmap"}
                </button>
              )}
            </div>
            {nodes.length === 0 ? (
              <div className="flex h-full items-center justify-center p-8">
                <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 text-center shadow-lg">
                  <h2 className="text-xl font-black text-[#18332D]">
                    No roadmap nodes found
                  </h2>
                  <p className="mt-2 text-sm font-semibold text-slate-600">
                    The roadmap loaded, but the API did not return any nodes.
                  </p>
                </div>
              </div>
            ) : (
              <div
                ref={flowWrapperRef}
                className={[
                  "absolute inset-0 h-full w-full",
                  isInitialViewportReady ? "opacity-100" : "opacity-0",
                ].join(" ")}
              >
                <ReactFlow
                  className="h-full w-full"
                  style={{ height: "100%", width: "100%" }}
                  nodes={nodes}
                  edges={edges}
                  nodeTypes={nodeTypes}
                  edgeTypes={edgeTypes}
                  onNodesChange={onNodesChange}
                  onEdgesChange={onEdgesChange}
                  onNodeClick={handleNodeClick}
                  onPaneClick={() => setSelectedNode(null)}
                  onInit={setFlowInstance}
                  nodesDraggable={false}
                  nodesConnectable={false}
                  elementsSelectable
                  onlyRenderVisibleElements
                  minZoom={0.28}
                  maxZoom={1.25}
                  defaultViewport={{ x: 0, y: 0, zoom: 0.74 }}
                  proOptions={{ hideAttribution: true }}
                >
                  <Background color="#B9D8CC" gap={28} size={1} />

                  {shouldShowMiniMap && (
                    <MiniMap
                      pannable
                      zoomable
                      nodeStrokeWidth={2}
                      nodeColor={(node) =>
                        getMiniMapColor(node.data?.status, node.data?.nodeType)
                      }
                      className="!rounded-lg !border-2 !border-[#B9D8CC] !bg-white !shadow-sm"
                    />
                  )}

                  <Controls className="!rounded-lg !border !border-[#B9D8CC] !bg-white !shadow-sm" />

                  <RoadmapLegend
                    isOpen={isLegendOpen}
                    onToggle={() => setIsLegendOpen((current) => !current)}
                  />
                </ReactFlow>
              </div>
            )}
          </div>
        </section>

        {selectedNode && (
          <RoadmapDetailDrawer
            node={selectedNode}
            isEnrolled={isEnrolled}
            isUpdating={isUpdatingProgress}
            isLoadingDetail={isLoadingNodeDetail}
            onClose={() => setSelectedNode(null)}
            onProgressChange={handleProgressChange}
          />
        )}
      </main>
    </div>
  );
}

function getViewerHeading(roadmap) {
  const baseTitle = cleanViewerTitle(
    roadmap?.careerRole?.name || roadmap?.title || "Roadmap",
  );

  if (!baseTitle) return "Roadmap";
  if (/roadmap$/i.test(baseTitle)) return baseTitle;

  return `${baseTitle} Roadmap`;
}

function cleanViewerTitle(title) {
  return String(title)
    .replace(/\s*Roadmap\s*v\d+(\.\d+)?\s*$/i, "")
    .replace(/\s*Roadmap\s*$/i, "")
    .replace(/\s*v\d+(\.\d+)?\s*$/i, "")
    .trim();
}
