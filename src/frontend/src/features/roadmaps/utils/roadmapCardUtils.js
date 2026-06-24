export function getRoadmapProgressPercent(roadmap) {
  const rawValue =
    roadmap?.progressPercent ??
    roadmap?.enrollment?.progressPercent ??
    roadmap?.currentEnrollment?.progressPercent ??
    roadmap?.userProgress?.progressPercent ??
    0;

  const numericValue = Number(rawValue);

  if (!Number.isFinite(numericValue)) return 0;

  return Math.min(100, Math.max(0, Math.round(numericValue)));
}

export function cleanRoadmapTitle(title) {
  return String(title || "Roadmap")
    .replace(/\s*Roadmap\s*v\d+(\.\d+)?\s*$/i, "")
    .replace(/\s*v\d+(\.\d+)?\s*$/i, "")
    .trim();
}

export function getRoadmapEnrollment(roadmap) {
  return (
    roadmap?.enrollment ||
    roadmap?.currentEnrollment ||
    roadmap?.userProgress ||
    null
  );
}

export function getRoadmapActionLabel({ hasEnrollment, progressPercent }) {
  if (!hasEnrollment) return "Start";
  if (progressPercent >= 100) return "Review";
  return "Continue";
}