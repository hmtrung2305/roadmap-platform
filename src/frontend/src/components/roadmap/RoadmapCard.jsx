export default function RoadmapCard({ roadmap, onOpen, index = 0 }) {
  const progressPercent = getRoadmapProgressPercent(roadmap);
  const title = cleanRoadmapTitle(roadmap.careerRole?.name || roadmap.title || "Roadmap");
  const enrollment = roadmap.enrollment || roadmap.currentEnrollment || roadmap.userProgress || null;

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
      aria-label={`Open ${title}`}
      onClick={onOpen}
      onKeyDown={handleKeyDown}
      className="group relative flex min-h-[68px] cursor-pointer items-center overflow-hidden rounded-lg border border-[#A8D3C4] bg-white px-4 py-2.5 shadow-sm outline-none transition-all duration-200 ease-out hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-[0_10px_24px_rgba(31,111,95,0.12)] focus-visible:border-[#2FA084] focus-visible:ring-4 focus-visible:ring-[#2FA084]/15 active:translate-y-0 active:shadow-sm"
      style={{
        animation: "roadmapCardIn 420ms ease-out both",
        animationDelay: `${Math.min(index, 9) * 55}ms`,
      }}
    >
      <h2 className="min-w-0 flex-1 whitespace-normal pr-14 text-sm font-black leading-5 tracking-tight text-[#18332D] sm:text-[15px]">
        {title}
      </h2>

      {enrollment && (
        <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 rounded-md border border-[#A8D3C4] bg-[#EAF8F1] px-2.5 py-1 text-[10px] font-extrabold text-[#1F6F5F] opacity-0 shadow-sm transition-all duration-150 group-hover:opacity-100 group-focus-visible:opacity-100">
          {progressPercent}%
        </span>
      )}

      <div className="absolute inset-x-0 bottom-0 h-1 bg-[#E3EEE8]">
        <div
          className="h-full bg-[#22C55E] transition-[width] duration-300 ease-out"
          style={{ width: `${progressPercent}%` }}
        />
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

function cleanRoadmapTitle(title) {
  return String(title)
    .replace(/\s*Roadmap\s*v\d+(\.\d+)?\s*$/i, "")
    .replace(/\s*v\d+(\.\d+)?\s*$/i, "")
    .trim();
}
