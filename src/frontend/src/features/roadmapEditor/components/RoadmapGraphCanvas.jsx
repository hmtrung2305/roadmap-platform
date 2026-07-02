import { memo, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import {
  Background,
  Controls,
  MiniMap,
  ReactFlow,
  useEdgesState,
  useNodesState,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";

import BalancedRoadmapEdge from "../../roadmaps/components/BalancedRoadmapEdge";
import RoadmapFlowNode from "../../roadmaps/components/RoadmapFlowNode";
import {
  NODE_HEIGHT,
  NODE_WIDTH,
  buildRoadmapFlow,
  getMiniMapColor,
  getNodeId,
  getStartFlowNode,
  normalizeRoadmap,
} from "../../roadmaps/utils/roadmapUtils";
import { ModuleEmptyState } from "../../learningModules/components/learningModuleUi";
import { normalizeNodes } from "../roadmapEditorUtils";

const nodeTypes = {
  roadmapNode: RoadmapFlowNode,
};

const edgeTypes = {
  balanced: BalancedRoadmapEdge,
};

const EMPTY_NODE_IDS = [];

function buildEditorRoadmapGraph(detail, sourceNodes) {
  return normalizeRoadmap({
    ...detail,
    nodes: normalizeNodes(sourceNodes),
    edges: detail?.edges || [],
  });
}

function focusEditorRoadmapStart(flowInstance, flowNodes, containerElement) {
  const startNode = getStartFlowNode(flowNodes);

  if (!startNode) return;

  const zoom = 0.38;
  const bounds = containerElement?.getBoundingClientRect?.();
  const viewportWidth = bounds?.width || window.innerWidth || 1200;
  const startCenterX = startNode.position.x + NODE_WIDTH / 2;
  const topPadding = 286;

  flowInstance.setViewport(
    {
      x: viewportWidth / 2 - startCenterX * zoom,
      y: topPadding - startNode.position.y * zoom,
      zoom,
    },
    { duration: 0 },
  );
}

const RoadmapGraphCanvas = memo(function RoadmapGraphCanvas({
  detail,
  nodes: sourceNodes,
  selectedNodeId,
  focusNodeRequest,
  newNodeIds = EMPTY_NODE_IDS,
  onSelect,
  className = "h-[640px] min-h-[460px]",
}) {
  const flowWrapperRef = useRef(null);
  const [flowInstance, setFlowInstance] = useState(null);
  const [isViewportReady, setIsViewportReady] = useState(false);
  const focusedRoadmapKeyRef = useRef(null);
  const selectedNodeIdRef = useRef(selectedNodeId);

  useEffect(() => {
    selectedNodeIdRef.current = selectedNodeId;
  }, [selectedNodeId]);

  const displayRoadmap = useMemo(
    () => buildEditorRoadmapGraph(detail, sourceNodes),
    [detail, sourceNodes],
  );

  const { flowNodes: baseFlowNodes, flowEdges, nodeLookup } = useMemo(
    () => buildRoadmapFlow(displayRoadmap, null),
    [displayRoadmap],
  );

  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);

  const newNodeIdSet = useMemo(() => new Set(newNodeIds.map(String)), [newNodeIds]);

  useEffect(() => {
    setNodes(baseFlowNodes.map((node) => ({
      ...node,
      data: {
        ...node.data,
        isSelected: node.id === selectedNodeIdRef.current,
        isNew: newNodeIdSet.has(String(node.id)),
      },
    })));
    setEdges(flowEdges);
  }, [baseFlowNodes, flowEdges, newNodeIdSet, setNodes, setEdges]);

  useEffect(() => {
    setNodes((currentNodes) => currentNodes.map((node) => {
      const isSelected = node.id === selectedNodeId;
      const isNew = newNodeIdSet.has(String(node.id));

      if (node.data?.isSelected === isSelected && node.data?.isNew === isNew) return node;

      return {
        ...node,
        data: {
          ...node.data,
          isSelected,
          isNew,
        },
      };
    }));
  }, [selectedNodeId, newNodeIdSet, setNodes]);

  const viewportKey = useMemo(
    () => `${detail?.roadmapVersionId || detail?.slug || "roadmap"}`,
    [detail?.roadmapVersionId, detail?.slug],
  );

  useEffect(() => {
    if (focusedRoadmapKeyRef.current !== viewportKey) {
      setIsViewportReady(false);
    }
  }, [viewportKey]);

  useLayoutEffect(() => {
    if (!flowInstance || baseFlowNodes.length === 0) return;

    if (focusedRoadmapKeyRef.current === viewportKey) {
      setIsViewportReady(true);
      return;
    }

    focusedRoadmapKeyRef.current = viewportKey;
    focusEditorRoadmapStart(flowInstance, baseFlowNodes, flowWrapperRef.current);
    setIsViewportReady(true);
  }, [flowInstance, baseFlowNodes, viewportKey]);

  useEffect(() => {
    if (!flowInstance || !focusNodeRequest?.id || !isViewportReady || baseFlowNodes.length === 0) return;

    const targetNode = baseFlowNodes.find((node) => node.id === focusNodeRequest.id);
    if (!targetNode?.position) return;

    flowInstance.setCenter(
      targetNode.position.x + NODE_WIDTH / 2,
      targetNode.position.y + NODE_HEIGHT / 2,
      { zoom: 0.44, duration: 320 },
    );
  }, [flowInstance, baseFlowNodes, focusNodeRequest?.id, focusNodeRequest?.requestId, isViewportReady]);

  if (sourceNodes.length === 0) {
    return (
      <ModuleEmptyState title="No nodes found">
        No roadmap nodes are available.
      </ModuleEmptyState>
    );
  }

  return (
    <div className={`${className} overflow-hidden rounded-xl border border-[#B9D8CC] bg-[#F7F1E8]`}>
      <div
        ref={flowWrapperRef}
        className={[
          "h-full w-full transition-opacity",
          isViewportReady ? "opacity-100" : "opacity-0",
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
          onNodeClick={(_, node) => {
            const sourceNode = nodeLookup.get(node.id);
            onSelect(sourceNode ? getNodeId(sourceNode) : node.id);
          }}
          onInit={setFlowInstance}
          nodesDraggable={false}
          nodesConnectable={false}
          elementsSelectable
          onlyRenderVisibleElements
          minZoom={0.18}
          maxZoom={1.1}
          defaultViewport={{ x: 0, y: 0, zoom: 0.44 }}
          proOptions={{ hideAttribution: true }}
        >
          <Background color="#B9D8CC" gap={28} size={1} />
          {nodes.length <= 150 && (
            <MiniMap
              pannable
              zoomable
              nodeStrokeWidth={2}
              nodeColor={(node) => getMiniMapColor(node.data?.status, node.data?.nodeType)}
              className="!rounded-lg !border-2 !border-[#B9D8CC] !bg-white !shadow-sm"
            />
          )}
          <Controls
            showInteractive={false}
            className="!rounded-lg !border !border-[#B9D8CC] !bg-white !shadow-sm"
          />
        </ReactFlow>
      </div>
    </div>
  );
});

export default RoadmapGraphCanvas;
