import { Handle, Position } from "@xyflow/react";
import {
  NODE_WIDTH,
  NODE_HEIGHT,
  formatSelectionText,
  getNodeTypeClass,
  getStatusColor,
} from "./roadmapUtils";

export default function RoadmapFlowNode({ data }) {
  const status = data.status || "pending";

  return (
    <button
      type="button"
      className={[
        "group relative flex flex-col items-center justify-center gap-1.5 rounded-xl border border-[#B9D8CC] px-4 py-3 text-center shadow-sm transition-none",
        getNodeTypeClass(data.nodeType, data.checkpointType),
        data.isSelected ? "outline outline-4 outline-[#2563EB]" : "",
        status === "completed" ? "bg-[#BBF7D0] text-[#18332D]" : "",
        status === "in_progress" ? "bg-[#BFDBFE] text-[#18332D]" : "",
        status === "locked" ? "bg-[#E5E7EB] text-slate-500" : "",
        status === "skipped" ? "bg-[#FED7AA] text-[#18332D]" : "",
      ].join(" ")}
      style={{ width: NODE_WIDTH, height: NODE_HEIGHT }}
    >
      <Handle id="top-target" type="target" position={Position.Top} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />
      <Handle id="top-source" type="source" position={Position.Top} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />
      <Handle id="bottom-target" type="target" position={Position.Bottom} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />
      <Handle id="bottom-source" type="source" position={Position.Bottom} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />
      <Handle id="left-target" type="target" position={Position.Left} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />
      <Handle id="left-source" type="source" position={Position.Left} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />
      <Handle id="right-target" type="target" position={Position.Right} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />
      <Handle id="right-source" type="source" position={Position.Right} className="!h-2 !w-2 !border-2 !border-[#B9D8CC] !bg-white" />

      <span
        className="absolute left-2 top-1.5 h-2.5 w-2.5 rounded-xl border border-[#B9D8CC]"
        style={{ background: getStatusColor(status, data.nodeType) }}
      />

      {status === "locked" && (
        <span className="absolute right-2 top-1 text-xs font-black" title="Locked">
          🔒
        </span>
      )}

      <span className="line-clamp-3 text-base font-black leading-5 tracking-tight sm:text-lg sm:leading-6">
        {data.title}
      </span>

      {data.nodeType === "choice_group" && (
        <span className="rounded-xl border border-[#B9D8CC] bg-white px-2 py-0.5 text-[10px] font-extrabold tracking-[0.08em] text-[#18332D]">
          {formatSelectionText(data.selectionType, data.requiredCount)}
        </span>
      )}

      {data.nodeType === "checkpoint" && (
        <span className="rounded-xl border border-[#B9D8CC] bg-white px-2 py-0.5 text-[10px] font-extrabold tracking-[0.08em] text-[#18332D]">
          {data.checkpointType || "checkpoint"}
        </span>
      )}
    </button>
  );
}
