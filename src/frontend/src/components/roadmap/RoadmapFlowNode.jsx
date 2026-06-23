import { Handle, Position } from "@xyflow/react";
import {
  NODE_WIDTH,
  NODE_HEIGHT,
  formatSelectionText,
  getNodeTypeClass,
  getStatusColor,
} from "./roadmapUtils";

const handleClass = "!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white";

const LOCKED_GRAY_NODE_TYPES = new Set(["phase", "choice_group"]);
const LOCKED_TEXT_COLOR = "oklch(55.4% .046 257.417)";
const lockedGrayNodeStyle = {
  backgroundColor: "#E5E7EB",
  borderColor: "#CBD5E1",
  color: LOCKED_TEXT_COLOR,
};

const lockedTypeColorNodeStyle = {
  color: LOCKED_TEXT_COLOR,
};

function getLockedNodeStyle(nodeType) {
  return LOCKED_GRAY_NODE_TYPES.has(nodeType)
    ? lockedGrayNodeStyle
    : lockedTypeColorNodeStyle;
}

const statusNodeClassMap = {};

const COMPLETED_NODE_STYLE_BY_TYPE = {
  phase: {
    backgroundColor: "#028C7C",
    borderColor: "#111827",
    color: "#111827",
  },
  choice_group: {
    backgroundColor: "#82CFAE",
    borderColor: "#111827",
    color: "#111827",
  },
  topic: {
    backgroundColor: "#D8C98F",
    borderColor: "#111827",
    color: "#111827",
  },
  option: {
    backgroundColor: "#D8C98F",
    borderColor: "#111827",
    color: "#111827",
  },
  choice_option: {
    backgroundColor: "#D8C98F",
    borderColor: "#111827",
    color: "#111827",
  },
  skill: {
    backgroundColor: "#D8C98F",
    borderColor: "#111827",
    color: "#111827",
  },
  checkpoint: {
    backgroundColor: "#D7A16E",
    borderColor: "#111827",
    color: "#111827",
  },
  project: {
    backgroundColor: "#D7A16E",
    borderColor: "#111827",
    color: "#111827",
  },
  resource_group: {
    backgroundColor: "#C9D99A",
    borderColor: "#111827",
    color: "#111827",
  },
};

const fallbackCompletedNodeStyle = {
  backgroundColor: "#D1D5DB",
  borderColor: "#111827",
  color: "#111827",
};

function getCompletedNodeStyle(nodeType) {
  return COMPLETED_NODE_STYLE_BY_TYPE[nodeType] || fallbackCompletedNodeStyle;
}

const skippedNodeStyle = {
  backgroundColor: "#4F6F73",
  borderColor: "#111827",
  color: "#111827",
};

const inProgressChoiceGroupStyle = {
  backgroundColor: "#24B1B1",
  borderColor: "#159797",
  color: "#FFFFFF",
};

function getStatusNodeClass(status, nodeType) {
  if (status === "completed") {
    return "border-[#111827]";
  }

  if (status === "locked") {
    return LOCKED_GRAY_NODE_TYPES.has(nodeType)
      ? "border-[#CBD5E1] bg-[#E5E7EB] hover:bg-[#E5E7EB]"
      : "";
  }

  if (status === "in_progress") {
    if (nodeType === "choice_group") {
      return "border-[#159797] text-white hover:!bg-[#24B1B1]";
    }

    return "";
  }

  if (status === "skipped") return "border-[#111827]";

  return statusNodeClassMap[status] || "border-[#B9D8CC]";
}

export default function RoadmapFlowNode({ data }) {
  const status = data.status || "pending";
  const isCompleted = status === "completed";
  const isSkipped = status === "skipped";
  const isLocked = status === "locked";

  return (
    <button
      type="button"
      className={[
        "group relative flex cursor-pointer flex-col items-center justify-center gap-1 overflow-hidden rounded-lg border px-4 py-3 text-center shadow-sm outline-none transition-all duration-150 focus-visible:ring-4 focus-visible:ring-[#81E7AF]/35",
        getNodeTypeClass(data.nodeType, data.checkpointType),
        getStatusNodeClass(status, data.nodeType),
        data.isSelected ? "z-10 ring-4 ring-[#1F6F5F]/35 shadow-[0_0_0_2px_#1F6F5F,0_18px_32px_rgba(31,111,95,0.22)] scale-[1.03]" : "",
      ].join(" ")}
      style={{
        width: NODE_WIDTH,
        height: NODE_HEIGHT,
        ...(isCompleted ? getCompletedNodeStyle(data.nodeType) : null),
        ...(status === "in_progress" && data.nodeType === "choice_group" ? inProgressChoiceGroupStyle : null),
        ...(isLocked ? getLockedNodeStyle(data.nodeType) : null),
        ...(isSkipped ? skippedNodeStyle : null),
      }}
    >
      <Handle id="top-target" type="target" position={Position.Top} className={handleClass} />
      <Handle id="top-source" type="source" position={Position.Top} className={handleClass} />
      <Handle id="bottom-target" type="target" position={Position.Bottom} className={handleClass} />
      <Handle id="bottom-source" type="source" position={Position.Bottom} className={handleClass} />
      <Handle id="left-target" type="target" position={Position.Left} className={handleClass} />
      <Handle id="left-source" type="source" position={Position.Left} className={handleClass} />
      <Handle id="right-target" type="target" position={Position.Right} className={handleClass} />
      <Handle id="right-source" type="source" position={Position.Right} className={handleClass} />

      <span
        className="absolute left-2 top-1.5 h-2.5 w-2.5 rounded-lg border border-white/80"
        style={{ background: getStatusColor(status, data.nodeType) }}
      />

      {data.isSelected && (
        <span className="absolute right-1.5 top-1.5 h-3 w-3 rounded-full border-2 border-white bg-[#1F6F5F] shadow-sm" />
      )}

      {isLocked && (
        <span
          className="absolute right-2 top-1 text-xs font-black text-[#111827]"
          title="Locked"
        >
          🔒
        </span>
      )}

      <span
        className={[
          "line-clamp-3 max-w-full !text-[18px] font-black leading-4 tracking-tight sm:text-sm sm:leading-5",
          isCompleted || isSkipped ? "line-through decoration-current decoration-2" : "",
        ].join(" ")}
      >
        {data.title}
      </span>

      {data.nodeType === "choice_group" && (
        <span className="rounded-lg border border-[#B9D8CC] bg-white/95 px-2 py-0.5 text-[9px] font-extrabold tracking-[0.08em] text-[#18332D]">
          {formatSelectionText(data.selectionType, data.requiredCount)}
        </span>
      )}

      {data.nodeType === "checkpoint" && (
        <span className="rounded-lg border border-[#B9D8CC] bg-white/95 px-2 py-0.5 text-[9px] font-extrabold tracking-[0.08em] text-[#18332D]">
          {data.checkpointType || "checkpoint"}
        </span>
      )}
    </button>
  );
}
