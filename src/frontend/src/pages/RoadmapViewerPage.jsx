/* eslint-disable react-hooks/exhaustive-deps */
import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  Background,
  Controls,
  MiniMap,
  ReactFlow,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";

import {
  getRoadmapNodeDetailKey,
  getRoadmapVersionId,
  useRoadmapStore,
} from "../stores/useRoadmapStore";
import RoadmapLegend from "../features/roadmaps/components/RoadmapLegend";
import {
  buildRoadmapFlow,
  focusRoadmapStart,
  getMiniMapColor,
  getNodeId,
  getNodeType,
  isManuallyTrackableNode,
  NODE_HEIGHT,
  NODE_WIDTH,
  mergeGraphNodeWithDetail,
  patchNodeProgress,
  applyProgressUpdateResult,
  findChangedProgress,
} from "../features/roadmaps/utils/roadmapUtils";
import BalancedRoadmapEdge from "../features/roadmaps/components/BalancedRoadmapEdge";
import RoadmapFlowNode from "../features/roadmaps/components/RoadmapFlowNode";
import RoadmapFullScreen from "../features/roadmaps/components/RoadmapFullScreen";
import RoadmapDetailDrawer from "../features/roadmaps/components/RoadmapDetailDrawer";
import RoadmapStudyGuide from "../features/roadmaps/components/RoadmapStudyGuide";
const nodeTypes = {
  roadmapNode: RoadmapFlowNode,
};

const edgeTypes = {
  balanced: BalancedRoadmapEdge,
};

export default function RoadmapViewerPage() {
  const navigate = useNavigate();
  const { slug } = useParams();
  const [searchParams] = useSearchParams();
  const returnNodeId = searchParams.get("nodeId");

  const [roadmap, setRoadmap] = useState(null);
  const [selectedNode, setSelectedNode] = useState(null);
  const [isLoadingNodeDetail, setIsLoadingNodeDetail] = useState(false);
  const [status, setStatus] = useState("loading");
  const [message, setMessage] = useState("");
  const [isEnrolling, setIsEnrolling] = useState(false);
  const [isUpdatingProgress, setIsUpdatingProgress] = useState(false);
  const [isLegendOpen, setIsLegendOpen] = useState(false);
  const [isGuideOpen, setIsGuideOpen] = useState(false);
  const [guideStep, setGuideStep] = useState(0);
  const [guideTargetRect, setGuideTargetRect] = useState(null);
  const [flowInstance, setFlowInstance] = useState(null);
  const [isInitialViewportReady, setIsInitialViewportReady] = useState(false);
  const flowWrapperRef = useRef(null);
  const focusedRoadmapKeyRef = useRef(null);
  const openedReturnNodeRef = useRef(null);
  const lastGuideTargetRectRef = useRef(null);
  const loadRoadmapGraph = useRoadmapStore((state) => state.loadRoadmapGraph);
  const loadNodeDetailFromStore = useRoadmapStore((state) => state.loadNodeDetail);
  const enrollInRoadmap = useRoadmapStore((state) => state.enroll);
  const updateNodeProgressInStore = useRoadmapStore((state) => state.updateNodeProgress);
  const nodeDetailByKey = useRoadmapStore((state) => state.nodeDetailByKey);
  const loadRequestRef = useRef(0);
  const nodeDetailRequestRef = useRef(0);
  const roadmapVersionId = getRoadmapVersionId(roadmap);
  const isEnrolled = Boolean(roadmap?.enrollment);

  const baseFlow = useMemo(() => {
    if (!roadmap?.nodes?.length) {
      return { flowNodes: [], flowEdges: [], nodeLookup: new Map() };
    }

    return buildRoadmapFlow(roadmap, null);
  }, [roadmap]);

  const guideTargets = useMemo(
    () => getRoadmapGuideTargets(roadmap, baseFlow.flowNodes),
    [roadmap, baseFlow.flowNodes],
  );

  const selectedNodeId = selectedNode ? getNodeId(selectedNode) : null;
  const guideNodeTarget = useMemo(
    () =>
      getRoadmapGuideNodeTarget({
        stepIndex: guideStep,
        isGuideOpen,
        isEnrolled,
        phaseNodeId: guideTargets.phase ? getNodeId(guideTargets.phase) : null,
        topicNodeId: guideTargets.topic ? getNodeId(guideTargets.topic) : null,
      }),
    [guideStep, isGuideOpen, isEnrolled, guideTargets.phase, guideTargets.topic],
  );

  const flowNodes = useMemo(
    () =>
      decorateRoadmapFlowNodes(baseFlow.flowNodes, {
        selectedNodeId,
        guideNodeTargetId: guideNodeTarget?.nodeId,
        guideTargetLabel: guideNodeTarget?.label,
      }),
    [baseFlow.flowNodes, selectedNodeId, guideNodeTarget?.nodeId, guideNodeTarget?.label],
  );
  const flowEdges = baseFlow.flowEdges;
  const nodeLookup = baseFlow.nodeLookup;

  const guideTargetSelector = useMemo(
    () =>
      getRoadmapGuideTargetSelector({
        stepIndex: guideStep,
        isEnrolled,
      }),
    [guideStep, isEnrolled],
  );

  const selectedNodeModuleSlug = getFirstLearningModuleSlug(selectedNode);
  const hasSelectedNodeModule = Boolean(selectedNodeModuleSlug);

  const nodes = flowNodes;
  const edges = flowEdges;
  const onNodesChange = useCallback(() => {}, []);
  const onEdgesChange = useCallback(() => {}, []);

  useLayoutEffect(() => {
    if (!flowInstance || baseFlow.flowNodes.length === 0) return;

    const roadmapFocusKey =
      roadmapVersionId || slug || "current-roadmap";

    if (focusedRoadmapKeyRef.current === roadmapFocusKey) {
      setIsInitialViewportReady(true);
      return;
    }

    focusedRoadmapKeyRef.current = roadmapFocusKey;
    focusRoadmapStart(flowInstance, baseFlow.flowNodes, flowWrapperRef.current);
    setIsInitialViewportReady(true);
  }, [flowInstance, baseFlow.flowNodes, roadmapVersionId, slug]);

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

  useEffect(() => {
    if (!isGuideOpen || !guideTargetSelector) {
      lastGuideTargetRectRef.current = null;
      setGuideTargetRect(null);
      return;
    }

    let animationFrameId = 0;
    let resizeObserver = null;
    const timeoutIds = [];

    function commitTargetRect(rect) {
      const nextRect = rect
        ? {
            left: rect.left,
            top: rect.top,
            right: rect.right,
            bottom: rect.bottom,
            width: rect.width,
            height: rect.height,
          }
        : null;

      if (areGuideRectsClose(lastGuideTargetRectRef.current, nextRect)) {
        return;
      }

      lastGuideTargetRectRef.current = nextRect;
      setGuideTargetRect(nextRect);
    }

    function readTargetRect() {
      const targetElement = document.querySelector(guideTargetSelector);

      if (!targetElement) {
        commitTargetRect(null);
        return null;
      }

      commitTargetRect(targetElement.getBoundingClientRect());
      return targetElement;
    }

    function scheduleUpdate(delay = 0) {
      if (delay > 0) {
        const timeoutId = window.setTimeout(() => scheduleUpdate(0), delay);
        timeoutIds.push(timeoutId);
        return;
      }

      window.cancelAnimationFrame(animationFrameId);
      animationFrameId = window.requestAnimationFrame(readTargetRect);
    }

    const targetElement = readTargetRect();

    if (targetElement && typeof ResizeObserver !== "undefined") {
      resizeObserver = new ResizeObserver(() => scheduleUpdate());
      resizeObserver.observe(targetElement);
    }

    [0, 80, 180, 320].forEach((delay) => scheduleUpdate(delay));
    window.addEventListener("resize", scheduleUpdate);
    window.addEventListener("scroll", scheduleUpdate, true);
    window.addEventListener("transitionend", scheduleUpdate, true);

    return () => {
      window.cancelAnimationFrame(animationFrameId);
      timeoutIds.forEach((timeoutId) => window.clearTimeout(timeoutId));
      resizeObserver?.disconnect();
      window.removeEventListener("resize", scheduleUpdate);
      window.removeEventListener("scroll", scheduleUpdate, true);
      window.removeEventListener("transitionend", scheduleUpdate, true);
    };
  }, [
    isGuideOpen,
    guideTargetSelector,
    guideStep,
    selectedNodeModuleSlug,
    selectedNodeId,
  ]);

  useEffect(() => {
    if (!returnNodeId || !roadmapVersionId || !nodeLookup.size || !flowInstance) {
      return;
    }

    const returnKey = `${roadmapVersionId}:${returnNodeId}`;
    if (openedReturnNodeRef.current === returnKey) return;

    const returnNode = nodeLookup.get(String(returnNodeId));
    if (!returnNode) return;

    openedReturnNodeRef.current = returnKey;
    setGuideStep(5);
    setIsGuideOpen(true);

    window.requestAnimationFrame(() => {
      focusFlowNode(returnNodeId);
      loadNodeDetail(returnNode);
    });
  }, [returnNodeId, roadmapVersionId, nodeLookup, flowInstance]);

  const handleNodeClick = useCallback(
    (_, node) => {
      const sourceNode = nodeLookup.get(node.id);

      if (sourceNode) {
        const clickedNodeId = getNodeId(sourceNode);
        const phaseNodeId = guideTargets.phase ? getNodeId(guideTargets.phase) : null;
        const topicNodeId = guideTargets.topic ? getNodeId(guideTargets.topic) : null;

        if (isGuideOpen && guideStep === 1 && clickedNodeId === phaseNodeId) {
          const nextLearningNode = guideTargets.topic || sourceNode;

          setSelectedNode(null);
          setGuideStep(2);

          window.requestAnimationFrame(() => {
            focusFlowNode(getNodeId(nextLearningNode));
          });

          return;
        }

        if (isGuideOpen && guideStep === 2 && clickedNodeId === topicNodeId) {
          setGuideStep(3);
        }

        loadNodeDetail(sourceNode);
      }
    },
    [
      nodeLookup,
      roadmapVersionId,
      nodeDetailByKey,
      isGuideOpen,
      guideStep,
      guideTargets.phase,
      guideTargets.topic,
    ],
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

      setGuideStep(1);
      setIsGuideOpen(true);
    } catch (error) {
      console.error(error);
      setMessage(error?.message || "Failed to enroll in roadmap.");
    } finally {
      setIsEnrolling(false);
    }
  }

  function handleOpenGuide() {
    setGuideStep(isEnrolled ? 1 : 0);
    setIsGuideOpen(true);
  }

  function focusFlowNode(nodeId) {
    if (!flowInstance || !nodeId) return;

    const flowNode = baseFlow.flowNodes.find((node) => node.id === String(nodeId));
    if (!flowNode?.position) return;

    const bounds = flowWrapperRef.current?.getBoundingClientRect?.();
    const viewportWidth = bounds?.width || window.innerWidth || 1200;
    const viewportHeight = bounds?.height || window.innerHeight || 720;
    const zoom = 0.72;
    const nodeCenterX = flowNode.position.x + NODE_WIDTH / 2;
    const nodeCenterY = flowNode.position.y + NODE_HEIGHT / 2;

    flowInstance.setViewport(
      {
        x: viewportWidth / 2 - nodeCenterX * zoom,
        y: viewportHeight * 0.42 - nodeCenterY * zoom,
        zoom,
      },
      { duration: 360 },
    );
  }

  async function focusGuideNode(graphNode) {
    if (!graphNode) return;

    const nodeId = getNodeId(graphNode);
    focusFlowNode(nodeId);
    await loadNodeDetail(graphNode);
  }

  function previewGuideTopicNode() {
    const nextLearningNode = guideTargets.topic || guideTargets.phase;

    setSelectedNode(null);

    if (!nextLearningNode) return;

    window.requestAnimationFrame(() => {
      focusFlowNode(getNodeId(nextLearningNode));
    });
  }

  function handleGuideStepChange(nextStep) {
    const normalizedStep = Math.min(Math.max(Number(nextStep) || 0, 0), 5);

    if (normalizedStep === 1 && guideTargets.phase) {
      setSelectedNode(null);
      window.requestAnimationFrame(() => {
        focusFlowNode(getNodeId(guideTargets.phase));
      });
    }

    if (normalizedStep === 2) {
      previewGuideTopicNode();
    }

    setGuideStep(normalizedStep);
  }

  function handleGuideFocusPhase() {
    handleGuideStepChange(1);
  }

  function handleGuideMoveToTopic() {
    previewGuideTopicNode();
    setGuideStep(2);
  }

  async function handleGuideFocusTopic() {
    await focusGuideNode(guideTargets.topic || guideTargets.phase);
    setGuideStep(3);
  }

  function handleOpenSelectedModule() {
    if (!selectedNodeModuleSlug) return;

    const params = new URLSearchParams();
    if (slug) params.set("fromRoadmap", slug);
    if (selectedNode) params.set("roadmapNodeId", getNodeId(selectedNode));

    navigate(
      `/learning-modules/${selectedNodeModuleSlug}/overview${
        params.toString() ? `?${params.toString()}` : ""
      }`,
    );
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
  const shouldShowMiniMap = flowNodes.length <= 150;

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

              <button
                type="button"
                data-roadmap-guide-target="how-to-learn"
                onClick={handleOpenGuide}
                className="pointer-events-auto h-12 rounded-lg border border-[#B9D8CC] bg-white/95 px-4 text-xs font-extrabold tracking-[0.08em] text-[#1F6F5F] shadow-sm backdrop-blur transition hover:bg-[#EAF8F1]"
              >
                How to learn
              </button>

              {!isEnrolled && (
                <button
                  type="button"
                  data-roadmap-guide-target="start-roadmap"
                  onClick={handleEnroll}
                  disabled={isEnrolling}
                  className="pointer-events-auto h-12 rounded-lg border border-[#B9D8CC] bg-[#2FA084] px-4 text-xs font-extrabold tracking-[0.08em] text-white shadow-sm transition hover:bg-[#1F6F5F] disabled:opacity-60"
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

                  <LearningOrderHint />
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
            roadmapSlug={slug}
            onClose={() => setSelectedNode(null)}
            onProgressChange={handleProgressChange}
          />
        )}

        <RoadmapStudyGuide
          isOpen={isGuideOpen}
          stepIndex={guideStep}
          targetRect={guideTargetRect}
          isEnrolled={isEnrolled}
          isEnrolling={isEnrolling}
          recommendedPhaseTitle={guideTargets.phase?.title}
          recommendedTopicTitle={guideTargets.topic?.title}
          hasSelectedNodeModule={hasSelectedNodeModule}
          onClose={() => setIsGuideOpen(false)}
          onStepChange={handleGuideStepChange}
          onStartRoadmap={handleEnroll}
          onFocusPhase={handleGuideFocusPhase}
          onMoveToTopic={handleGuideMoveToTopic}
          onFocusTopic={handleGuideFocusTopic}
          onOpenSelectedModule={handleOpenSelectedModule}
        />
      </main>
    </div>
  );
}

function LearningOrderHint() {
  const [isPinnedOpen, setIsPinnedOpen] = useState(false);

  return (
    <div className="pointer-events-auto absolute left-[154px] top-4 z-20">
      <div className="group relative">
        <button
          type="button"
          aria-label="Learning order"
          aria-expanded={isPinnedOpen}
          onClick={() => setIsPinnedOpen((current) => !current)}
          className="grid h-9 w-9 place-items-center rounded-full border border-[#B9D8CC] bg-white text-sm font-black text-[#1F6F5F] shadow-sm transition hover:bg-[#EAF8F1]"
        >
          ?
        </button>

        <div
          className={[
            "absolute left-0 top-11 w-[min(300px,calc(100vw-32px))] rounded-lg border border-[#B9D8CC] bg-white/95 px-3 py-2.5 text-xs font-bold leading-5 text-slate-600 shadow-lg backdrop-blur",
            isPinnedOpen ? "block" : "hidden group-hover:block group-focus-within:block",
          ].join(" ")}
        >
          <div className="flex items-start justify-between gap-3">
            <p>
              <span className="font-black text-[#1F6F5F]">Learning order:</span>{" "}
              follow phases from top to bottom. Inside the same phase, left or
              right nodes can be learned flexibly unless a node is locked.
            </p>
            {isPinnedOpen && (
              <button
                type="button"
                onClick={() => setIsPinnedOpen(false)}
                className="rounded-md border border-[#D6E4DE] px-1.5 text-[10px] font-black text-slate-500 hover:bg-[#F7F1E8]"
              >
                ×
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function getRoadmapGuideNodeTarget({
  stepIndex,
  isGuideOpen,
  isEnrolled,
  phaseNodeId,
  topicNodeId,
}) {
  if (!isGuideOpen) return null;

  if ((stepIndex === 0 && isEnrolled) || stepIndex === 1) {
    return phaseNodeId
      ? { nodeId: phaseNodeId, label: stepIndex === 1 ? "First phase" : "Go to phase" }
      : null;
  }

  if (stepIndex === 2) {
    return topicNodeId ? { nodeId: topicNodeId, label: "Click this node" } : null;
  }

  return null;
}

function decorateRoadmapFlowNodes(
  flowNodes,
  { selectedNodeId, guideNodeTargetId, guideTargetLabel },
) {
  if (!selectedNodeId && !guideNodeTargetId) return flowNodes;

  return flowNodes.map((node) => {
    const isSelected = selectedNodeId === node.id;
    const isGuideTarget = guideNodeTargetId === node.id;

    if (node.data?.isSelected === isSelected &&
      node.data?.isGuideTarget === isGuideTarget &&
      node.data?.guideTargetLabel === (isGuideTarget ? guideTargetLabel : undefined)) {
      return node;
    }

    return {
      ...node,
      zIndex: isGuideTarget ? 60 : isSelected ? 40 : node.zIndex,
      data: {
        ...node.data,
        isSelected,
        isGuideTarget,
        guideTargetLabel: isGuideTarget ? guideTargetLabel : undefined,
      },
    };
  });
}

function getRoadmapGuideTargetSelector({ stepIndex, isEnrolled }) {
  if (stepIndex === 0 && !isEnrolled) {
    return '[data-roadmap-guide-target="start-roadmap"]';
  }

  if (stepIndex === 3) {
    return '[data-roadmap-guide-target="open-module"]';
  }

  if (stepIndex === 5) {
    return '[data-roadmap-guide-target="node-status-select"]';
  }

  return null;
}

function areGuideRectsClose(previousRect, nextRect) {
  if (!previousRect || !nextRect) return previousRect === nextRect;

  return (
    Math.abs(previousRect.left - nextRect.left) < 0.75 &&
    Math.abs(previousRect.top - nextRect.top) < 0.75 &&
    Math.abs(previousRect.width - nextRect.width) < 0.75 &&
    Math.abs(previousRect.height - nextRect.height) < 0.75
  );
}

function getRoadmapGuideTargets(roadmap, flowNodes) {
  const roadmapNodes = roadmap?.nodes || [];
  const flowNodeById = new Map(flowNodes.map((node) => [node.id, node]));
  const sortedNodes = [...roadmapNodes].sort((a, b) => {
    const nodeA = flowNodeById.get(getNodeId(a));
    const nodeB = flowNodeById.get(getNodeId(b));
    const positionA = nodeA?.position || { x: 0, y: 0 };
    const positionB = nodeB?.position || { x: 0, y: 0 };

    if (Math.abs(positionA.y - positionB.y) > 24) {
      return positionA.y - positionB.y;
    }

    return positionA.x - positionB.x;
  });

  const phase =
    sortedNodes.find((node) => getNodeType(node) === "phase") ||
    sortedNodes[0] ||
    null;

  const topic =
    sortedNodes.find((node) => isRecommendedLearningNode(node)) ||
    sortedNodes.find((node) => isManuallyTrackableNode(node)) ||
    null;

  return { phase, topic };
}

function isRecommendedLearningNode(node) {
  if (!node || !isManuallyTrackableNode(node)) return false;

  const status = node.progress?.status || "pending";
  const nodeType = getNodeType(node);

  return (
    status !== "locked" &&
    status !== "completed" &&
    ["topic", "choice_option", "project", "checkpoint"].includes(nodeType)
  );
}

function getFirstLearningModuleSlug(node) {
  const modules = node?.learningModules || node?.LearningModules || [];
  const firstModule = modules.find(Boolean);

  return firstModule?.slug || firstModule?.Slug || null;
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
