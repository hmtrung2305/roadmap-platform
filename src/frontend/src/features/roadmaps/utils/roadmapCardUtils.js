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
  const cleanedTitle = String(title || "Roadmap")
    .replace(/\s*v\d+(\.\d+){0,2}\s*$/i, "")
    .trim();

  return cleanedTitle || "Roadmap";
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