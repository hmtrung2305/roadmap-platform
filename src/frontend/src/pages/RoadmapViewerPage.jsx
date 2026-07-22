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
import { History, X } from "lucide-react";
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
import RoadmapMigrationNotice from "../features/roadmaps/components/RoadmapMigrationNotice";
import RoadmapViewerLearningControls from "../features/roadmaps/components/RoadmapViewerLearningControls";
import { CreatorByline } from "../features/creatorProfile/components/CreatorProfileDisplay";
const nodeTypes = {
  roadmapNode: RoadmapFlowNode,
};

const edgeTypes = {
  balanced: BalancedRoadmapEdge,
};

const ROADMAP_GUIDE_RETURN_TOKEN_KEY = "techmap:roadmapGuideReturnToken";

export default function RoadmapViewerPage() {
  const navigate = useNavigate();
  const { slug } = useParams();
  const [searchParams] = useSearchParams();
  const returnNodeId = searchParams.get("nodeId");
  const returnGuideToken = searchParams.get("guideToken");
  const shouldResumeGuideFromReturn =
    searchParams.get("guide") === "1" && Boolean(returnGuideToken);

  const [roadmap, setRoadmap] = useState(null);
  const [selectedNode, setSelectedNode] = useState(null);
  const [isLoadingNodeDetail, setIsLoadingNodeDetail] = useState(false);
  const [status, setStatus] = useState("loading");
  const [message, setMessage] = useState("");
  const [isEnrolling, setIsEnrolling] = useState(false);
  const [isMigratingUpdate, setIsMigratingUpdate] = useState(false);
  const [isUpdatingProgress, setIsUpdatingProgress] = useState(false);
  const [isLegendOpen, setIsLegendOpen] = useState(false);
  const [isGuideOpen, setIsGuideOpen] = useState(false);
  const [guideStep, setGuideStep] = useState(0);
  const [guideTargetRect, setGuideTargetRect] = useState(null);
  const [isChangelogOpen, setIsChangelogOpen] = useState(false);
  const [flowInstance, setFlowInstance] = useState(null);
  const [isInitialViewportReady, setIsInitialViewportReady] = useState(false);
  const flowWrapperRef = useRef(null);
  const focusedRoadmapKeyRef = useRef(null);
  const openedReturnNodeRef = useRef(null);
  const guideReturnDecisionRef = useRef(new Map());
  const lastGuideTargetRectRef = useRef(null);
  const loadRoadmapGraph = useRoadmapStore((state) => state.loadRoadmapGraph);
  const loadNodeDetailFromStore = useRoadmapStore((state) => state.loadNodeDetail);
  const enrollInRoadmap = useRoadmapStore((state) => state.enroll);
  const migrateRoadmapEnrollment = useRoadmapStore((state) => state.migrateEnrollment);
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
      const clearFrameId = window.requestAnimationFrame(() => {
        setGuideTargetRect(null);
      });

      return () => window.cancelAnimationFrame(clearFrameId);
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

    const returnNode = nodeLookup.get(String(returnNodeId));
    if (!returnNode) return;

    const guideReturnKey = `${roadmapVersionId}:${returnNodeId}:${
      returnGuideToken || "no-token"
    }`;
    const shouldResumeGuide = shouldResumeGuideFromReturn
      ? getGuideReturnDecision({
          cache: guideReturnDecisionRef.current,
          key: guideReturnKey,
          token: returnGuideToken,
        })
      : false;

    const returnKey = `${roadmapVersionId}:${returnNodeId}:${
      shouldResumeGuide ? "guide" : "normal"
    }:${returnGuideToken || ""}`;
    if (openedReturnNodeRef.current === returnKey) return;

    openedReturnNodeRef.current = returnKey;

    const openFrameId = window.requestAnimationFrame(() => {
      if (shouldResumeGuide) {
        setGuideStep(5);
        setIsGuideOpen(true);
      } else {
        setIsGuideOpen(false);
        if (searchParams.has("guide") || searchParams.has("guideToken")) {
          clearGuideReturnFlag();
        }
      }

      focusFlowNode(returnNodeId);
      loadNodeDetail(returnNode);
    });

    return () => window.cancelAnimationFrame(openFrameId);
  }, [
    returnNodeId,
    returnGuideToken,
    roadmapVersionId,
    nodeLookup,
    flowInstance,
    shouldResumeGuideFromReturn,
  ]);

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

  function clearGuideReturnFlag() {
    if (!searchParams.has("guide")) return;

    const nextParams = new URLSearchParams(searchParams);
    nextParams.delete("guide");
    nextParams.delete("guideToken");

    navigate(
      {
        pathname: `/roadmaps/${slug}`,
        search: nextParams.toString() ? `?${nextParams.toString()}` : "",
      },
      { replace: true },
    );
  }

  function handleCloseGuide() {
    setIsGuideOpen(false);
    setGuideTargetRect(null);
    clearGuideReturnFlag();
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
    const selectedNodeId = selectedNode ? getNodeId(selectedNode) : null;

    if (slug) params.set("fromRoadmap", slug);
    if (selectedNodeId) params.set("roadmapNodeId", selectedNodeId);

    if (isGuideOpen && guideStep === 3 && selectedNodeId) {
      const guideToken = createGuideReturnToken({
        roadmapSlug: slug,
        nodeId: selectedNodeId,
      });

      if (guideToken) {
        params.set("guide", "1");
        params.set("guideToken", guideToken);
      }
    }

    navigate(
      `/learning-modules/${selectedNodeModuleSlug}/overview${
        params.toString() ? `?${params.toString()}` : ""
      }`,
    );

  }

  async function handleMigrateUpdate() {
    const availableUpdate = roadmap?.availableUpdate;

    if (!availableUpdate?.roadmapEnrollmentId || !availableUpdate?.targetRoadmapVersionId || isMigratingUpdate) {
      return false;
    }

    setIsMigratingUpdate(true);
    setMessage("");

    try {
      const result = await migrateRoadmapEnrollment({
        enrollmentId: availableUpdate.roadmapEnrollmentId,
        targetRoadmapVersionId: availableUpdate.targetRoadmapVersionId,
        slug,
      });

      if (result?.graph) {
        focusedRoadmapKeyRef.current = null;
        setIsInitialViewportReady(false);
        setRoadmap(result.graph);
        setSelectedNode(null);
        setStatus("success");
      } else {
        await loadPage({ force: true });
      }

      return true;
    } catch (error) {
      console.error(error);
      setMessage(error?.message || "Failed to migrate roadmap update.");
      return false;
    } finally {
      setIsMigratingUpdate(false);
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

      if (isGuideOpen && guideStep === 5 && nextStatus === "completed") {
        handleCloseGuide();
      }
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

  const availableUpdate = roadmap?.availableUpdate || null;
  const versionHistory = normalizeVersionHistory(roadmap);
  const latestVersion = getLatestRoadmapVersion(roadmap, versionHistory);
  const learningVersionLabel = getLearningVersionLabel(roadmap, availableUpdate);
  const latestVersionLabel = getVersionLabel(latestVersion) || roadmap?.versionLabel || "-";
  const progressPercent = isEnrolled
    ? roadmap?.progressPercent ?? roadmap?.enrollment?.progressPercent ?? 0
    : availableUpdate?.progressPercent ?? roadmap?.progressPercent ?? 0;
  const shouldShowMiniMap = nodes.length <= 150;

  return (
    <div className="flex min-h-[calc(100vh-64px)] flex-col bg-[#F7F1E8] text-[#18332D]">
      <main
        className="relative min-h-0 w-full overflow-hidden"
        style={{ height: "calc(100vh - 64px)" }}
      >
        <section className="grid h-full min-h-0 w-full grid-rows-[auto_minmax(0,1fr)]">
          <div className="z-10 border-b border-[#B9D8CC] bg-white/95 px-2 py-1 shadow-sm backdrop-blur">
            <div className="flex min-h-10 flex-wrap items-center gap-2">
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
                <CreatorByline
                  creatorProfile={roadmap.creatorProfile}
                  label="Owned by"
                  showHeadline
                  className="mt-0.5 max-w-[420px]"
                />
              </div>

              <div className="ml-auto flex min-w-0 flex-wrap items-center justify-end gap-2">
                <VersionBadge label="Learning" value={learningVersionLabel} />
                <VersionBadge label="Latest" value={latestVersionLabel} />
                <button
                  type="button"
                  onClick={() => setIsChangelogOpen((current) => !current)}
                  className="inline-flex h-8 items-center gap-1.5 rounded-md border border-[#B9D8CC] bg-white px-2.5 text-xs font-extrabold text-[#1F6F5F] shadow-sm transition hover:bg-[#F4FBF8]"
                >
                  <History size={14} />
                  Changelog
                </button>
              </div>
            </div>

            {message && (
              <div className="mt-2 rounded-lg border border-[#B9D8CC] bg-[#FEE2E2] px-3 py-2 text-xs font-black text-[#18332D]">
                {message}
              </div>
            )}

            <RoadmapMigrationNotice
              availableUpdate={availableUpdate}
              isMigrating={isMigratingUpdate}
              onMigrate={handleMigrateUpdate}
            />
          </div>

          <div className="relative min-h-0 w-full overflow-hidden bg-[#F7F1E8]">
            <RoadmapViewerLearningControls
              progressPercent={progressPercent}
              isEnrolled={isEnrolled}
              hasAvailableUpdate={Boolean(availableUpdate)}
              isEnrolling={isEnrolling}
              onOpenGuide={handleOpenGuide}
              onEnroll={handleEnroll}
            />

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
            shouldContinueGuide={isGuideOpen && guideStep === 3}
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
          onClose={handleCloseGuide}
          onStepChange={handleGuideStepChange}
          onStartRoadmap={handleEnroll}
          onFocusPhase={handleGuideFocusPhase}
          onMoveToTopic={handleGuideMoveToTopic}
          onFocusTopic={handleGuideFocusTopic}
          onOpenSelectedModule={handleOpenSelectedModule}
        />

        {isChangelogOpen && (
          <RoadmapChangelogPanel
            roadmap={roadmap}
            versionHistory={versionHistory}
            learningVersionLabel={learningVersionLabel}
            latestVersionLabel={latestVersionLabel}
            onClose={() => setIsChangelogOpen(false)}
          />
        )}
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

function createGuideReturnToken({ roadmapSlug, nodeId }) {
  if (typeof window === "undefined") return null;

  const token = [
    roadmapSlug || "roadmap",
    nodeId || "node",
    Date.now(),
    Math.random().toString(36).slice(2),
  ].join(":");

  try {
    window.sessionStorage.setItem(ROADMAP_GUIDE_RETURN_TOKEN_KEY, token);
    return token;
  } catch {
    return null;
  }
}

function getGuideReturnDecision({ cache, key, token }) {
  if (!token) return false;

  if (cache.has(key)) {
    return cache.get(key);
  }

  const shouldResume = consumeGuideReturnToken(token);
  cache.set(key, shouldResume);
  return shouldResume;
}

function consumeGuideReturnToken(token) {
  if (typeof window === "undefined" || !token) return false;

  try {
    const storedToken = window.sessionStorage.getItem(
      ROADMAP_GUIDE_RETURN_TOKEN_KEY,
    );
    const shouldResume = storedToken === token;

    if (shouldResume) {
      window.sessionStorage.removeItem(ROADMAP_GUIDE_RETURN_TOKEN_KEY);
    }

    return shouldResume;
  } catch {
    return false;
  }
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

function VersionBadge({ label, value }) {
  return (
    <div className="inline-flex h-8 items-center gap-1.5 rounded-md border border-[#B9D8CC] bg-[#F4FBF8] px-2.5 text-xs font-extrabold text-[#18332D]">
      <span className="text-slate-500">{label}</span>
      <span>{value || "-"}</span>
    </div>
  );
}

function RoadmapChangelogPanel({
  roadmap,
  versionHistory,
  learningVersionLabel,
  latestVersionLabel,
  onClose,
}) {
  const availableUpdate = roadmap?.availableUpdate || null;

  return (
    <div className="absolute left-3 top-16 z-40 w-[min(440px,calc(100vw-24px))] overflow-hidden rounded-lg border border-[#B9D8CC] bg-white shadow-2xl">
      <div className="flex items-start justify-between gap-3 border-b border-[#B9D8CC] bg-[#F4FBF8] px-4 py-3">
        <div>
          <div className="flex items-center gap-2 text-sm font-black text-[#18332D]">
            <History size={16} className="text-[#1F6F5F]" />
            Roadmap changelog
          </div>
          <p className="mt-1 text-xs font-semibold text-slate-600">
            Learning {learningVersionLabel || "-"} - Latest {latestVersionLabel || "-"}
          </p>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="grid h-8 w-8 place-items-center rounded-md border border-[#B9D8CC] bg-white text-slate-600 transition hover:bg-[#F7F1E8]"
          aria-label="Close changelog"
        >
          <X size={15} />
        </button>
      </div>

      {availableUpdate && (
        <div className="border-b border-[#B9D8CC] px-4 py-3">
          <p className="text-xs font-black uppercase tracking-[0.12em] text-[#1F6F5F]">
            Update available
          </p>
          <p className="mt-1 text-sm font-bold text-[#18332D]">
            {availableUpdate.currentVersionLabel} -&gt; {availableUpdate.targetVersionLabel}
          </p>
        </div>
      )}

      <div className="max-h-[min(560px,calc(100vh-180px))] overflow-y-auto px-4 py-3">
        {versionHistory.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 px-3 py-6 text-center text-sm font-bold text-slate-500">
            No version changelog is available yet.
          </div>
        ) : (
          <div className="space-y-3">
            {versionHistory.map((version) => (
              <article
                key={version.roadmapVersionId || version.versionLabel}
                className="rounded-md border border-[#B9D8CC]/80 bg-white p-3"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="rounded-md border border-[#B9D8CC] bg-[#F7F1E8] px-2 py-0.5 text-xs font-black text-[#18332D]">
                      {getVersionLabel(version)}
                    </span>
                    <span className="text-xs font-extrabold uppercase text-[#1F6F5F]">
                      {version.releaseType || "release"}
                    </span>
                  </div>
                  <span className="text-[11px] font-bold text-slate-500">
                    {formatViewerDate(version.publishedAt || version.createdAt)}
                  </span>
                </div>
                <h3 className="mt-2 text-sm font-black text-[#18332D]">
                  {version.title || roadmap?.title || "Roadmap version"}
                </h3>
                <p className="mt-1 whitespace-pre-wrap text-sm font-semibold leading-6 text-slate-700">
                  {version.changeLog || version.description || "Initial published roadmap."}
                </p>
              </article>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function normalizeVersionHistory(roadmap) {
  const source = roadmap?.versionHistory || roadmap?.VersionHistory || [];
  return Array.isArray(source)
    ? source
        .filter((version) => version?.roadmapVersionId || version?.versionLabel)
        .sort(compareVersionsDesc)
    : [];
}

function compareVersionsDesc(first, second) {
  const versionParts = ["majorVersion", "minorVersion", "patchVersion", "versionNumber"];

  for (const key of versionParts) {
    const delta = Number(second?.[key] || 0) - Number(first?.[key] || 0);
    if (delta !== 0) return delta;
  }

  return new Date(second?.publishedAt || second?.createdAt || 0) -
    new Date(first?.publishedAt || first?.createdAt || 0);
}

function getLatestRoadmapVersion(roadmap, versionHistory) {
  return versionHistory.find((version) =>
    String(version.status || "").toLowerCase() === "published"
  ) || versionHistory[0] || roadmap;
}

function getLearningVersionLabel(roadmap, availableUpdate) {
  if (availableUpdate?.currentVersionLabel) {
    return availableUpdate.currentVersionLabel;
  }

  return roadmap?.versionLabel || buildSemanticVersionLabel(roadmap);
}

function getVersionLabel(version) {
  return version?.versionLabel || buildSemanticVersionLabel(version);
}

function buildSemanticVersionLabel(version) {
  if (!version) return "";

  const major = version.majorVersion;
  const minor = version.minorVersion;
  const patch = version.patchVersion;

  if (major == null || minor == null || patch == null) return "";
  return `v${major}.${minor}.${patch}`;
}

function formatViewerDate(value) {
  if (!value) return "-";

  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(value));

}

function getViewerHeading(roadmap) {
  const baseTitle = cleanViewerTitle(
    roadmap?.title || roadmap?.careerRole?.name || "Roadmap",
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
