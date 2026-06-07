import { BaseEdge, Position } from "@xyflow/react";

export default function BalancedRoadmapEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  style,
  markerEnd,
}) {
  const edgePath = getBalancedEdgePath(
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition
  );

  return (
    <BaseEdge
      id={id}
      path={edgePath}
      markerEnd={markerEnd}
      style={{
        ...style,
        strokeLinecap: "round",
        strokeLinejoin: "round",
      }}
    />
  );
}

function getBalancedEdgePath(sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition) {
  const sameRow = Math.abs(sourceY - targetY) < 8;
  const sameColumn = Math.abs(sourceX - targetX) < 8;

  if (sameRow || sourcePosition === Position.Right || sourcePosition === Position.Left) {
    const midX = sourceX + (targetX - sourceX) / 2;

    return [
      `M ${sourceX},${sourceY}`,
      `L ${midX},${sourceY}`,
      `L ${midX},${targetY}`,
      `L ${targetX},${targetY}`,
    ].join(" ");
  }

  if (sameColumn || sourcePosition === Position.Bottom || sourcePosition === Position.Top) {
    const midY = sourceY + (targetY - sourceY) / 2;

    return [
      `M ${sourceX},${sourceY}`,
      `L ${sourceX},${midY}`,
      `L ${targetX},${midY}`,
      `L ${targetX},${targetY}`,
    ].join(" ");
  }

  const midX = sourceX + (targetX - sourceX) / 2;

  return [
    `M ${sourceX},${sourceY}`,
    `L ${midX},${sourceY}`,
    `L ${midX},${targetY}`,
    `L ${targetX},${targetY}`,
  ].join(" ");
}
