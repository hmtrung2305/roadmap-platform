export default function RoadmapViewerLearningControls({
  progressPercent = 0,
  isEnrolled = false,
  hasAvailableUpdate = false,
  isEnrolling = false,
  onOpenGuide,
  onEnroll,
}) {
  const normalizedProgress = Math.min(
    100,
    Math.max(0, Number(progressPercent) || 0),
  );
  const shouldShowStartButton = !isEnrolled && !hasAvailableUpdate;

  return (
    <div className="pointer-events-none absolute right-4 top-4 z-20 flex flex-col items-end gap-2">
      <div
        aria-label="Roadmap progress"
        className="pointer-events-auto flex h-12 w-36 flex-col justify-center rounded-lg border border-[#B9D8CC] bg-white/95 px-3 shadow-sm backdrop-blur sm:w-44"
      >
        <div className="flex items-center justify-between gap-2 text-[11px] font-extrabold tracking-tight text-slate-500">
          <span>Progress</span>
          <span>{Math.round(normalizedProgress)}%</span>
        </div>

        <div className="mt-1.5 h-1.5 overflow-hidden rounded-md border border-[#B9D8CC] bg-white">
          <div
            className="h-full rounded-md bg-[#22C55E]"
            style={{ width: `${normalizedProgress}%` }}
          />
        </div>
      </div>

      <div
        aria-label="Roadmap learning actions"
        className="pointer-events-auto flex w-36 flex-col gap-2 sm:w-44"
      >
        {shouldShowStartButton && (
          <button
            type="button"
            data-roadmap-guide-target="start-roadmap"
            onClick={onEnroll}
            disabled={isEnrolling}
            className="h-8 w-full rounded-lg border border-[#B9D8CC] bg-[#2FA084] px-3 text-xs font-[900] tracking-[0.04em] text-white shadow-sm transition hover:bg-[#1F6F5F] disabled:opacity-60"
          >
            {isEnrolling ? "Starting..." : "Start roadmap"}
          </button>
        )}

        <button
          type="button"
          data-roadmap-guide-target="how-to-learn"
          onClick={onOpenGuide}
          className="h-8 w-full rounded-lg border border-[#B9D8CC] bg-white/95 px-3 text-xs font-[900] tracking-[0.04em] text-[#1F6F5F] shadow-sm backdrop-blur transition hover:bg-[#EAF8F1]"
        >
          How to learn
        </button>
      </div>
    </div>
  );
}
