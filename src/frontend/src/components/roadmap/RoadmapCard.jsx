export default function RoadmapCard({ roadmap, onOpen }) {
  const totalHours = roadmap.estimatedTotalHours ?? roadmap.estimatedHours ?? null;
  const hoursLabel = typeof totalHours === "number" ? `${totalHours}h` : "Flexible";
  const isFlexible = totalHours === null;
  const progressPercent = getRoadmapProgressPercent(roadmap);
  const progressLabel = getRoadmapProgressLabel(roadmap, progressPercent);

  function handleKeyDown(event) {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      onOpen();
    }
  }

  return (
    <article
      role="button"
      tabIndex={0}
      aria-label={`Open ${roadmap.title}`}
      onClick={onOpen}
      onKeyDown={handleKeyDown}
      className="group relative flex cursor-pointer flex-col overflow-hidden rounded-2xl border border-[#B9D8CC] bg-white shadow-sm outline-none transition-all duration-200 ease-out hover:-translate-y-1 hover:border-[#2FA084] hover:shadow-[0_12px_28px_rgba(31,111,95,0.12)] focus-visible:border-[#2FA084] focus-visible:ring-4 focus-visible:ring-[#2FA084]/15 active:translate-y-0 active:shadow-sm"
    >
      <span
        aria-hidden
        className="absolute inset-x-0 top-0 h-0.5 bg-gradient-to-r from-[#2FA084] to-[#4EC9A8] opacity-0 transition-opacity duration-200 group-hover:opacity-100"
      />

      <div className="flex flex-1 flex-col gap-1 px-4 pb-3 pt-4">
        <h2
          className="text-sm font-black leading-snug tracking-tight text-[#18332D]"
          style={{
            display: "-webkit-box",
            WebkitLineClamp: 3,
            WebkitBoxOrient: "vertical",
            overflow: "hidden",
          }}
        >
          {roadmap.title}
        </h2>
      </div>

      <div className="border-t border-[#EDF5F2] bg-[#F5FBF8] px-4 py-2.5">
        <div className="mb-2 flex items-center justify-between gap-3">
          <span className="text-[11px] font-extrabold tracking-wide text-[#1F6F5F]">
            {progressLabel}
          </span>

          <div className="flex items-center gap-1.5">
            <svg
              aria-hidden
              className="h-3 w-3 shrink-0 text-[#2FA084]"
              viewBox="0 0 16 16"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <circle cx="8" cy="8" r="6.5" />
              <path d="M8 4.5V8l2.5 1.5" />
            </svg>
            <span className={`text-[11px] font-extrabold tracking-wide ${isFlexible ? "text-slate-400" : "text-[#1F6F5F]"}`}>
              {hoursLabel}
            </span>
          </div>
        </div>

        <div className="h-1.5 overflow-hidden rounded-full border border-[#B9D8CC] bg-white">
          <div
            className="h-full rounded-full bg-[#2FA084] transition-all duration-300"
            style={{ width: `${progressPercent}%` }}
          />
        </div>
      </div>
    </article>
  );
}

function getRoadmapProgressPercent(roadmap) {
  const rawValue =
    roadmap.progressPercent ??
    roadmap.enrollment?.progressPercent ??
    roadmap.currentEnrollment?.progressPercent ??
    roadmap.userProgress?.progressPercent ??
    0;

  const numericValue = Number(rawValue);

  if (!Number.isFinite(numericValue)) return 0;

  return Math.min(100, Math.max(0, Math.round(numericValue)));
}

function getRoadmapProgressLabel(roadmap, progressPercent) {
  const enrollment = roadmap.enrollment || roadmap.currentEnrollment || roadmap.userProgress || null;

  if (progressPercent >= 100) return "Completed";
  if (!enrollment && progressPercent === 0) return "Not started";
  if (progressPercent === 0) return "0% done";

  return `${progressPercent}% done`;
}
