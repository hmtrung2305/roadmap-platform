import {
  cleanRoadmapTitle,
  getRoadmapEnrollment,
  getRoadmapProgressPercent,
} from "../utils/roadmapCardUtils";

export default function RoadmapCard({ roadmap, onOpen, index = 0 }) {
  const progressPercent = getRoadmapProgressPercent(roadmap);
  const title = cleanRoadmapTitle(
    roadmap.careerRole?.name || roadmap.title || "Roadmap",
  );
  const enrollment = getRoadmapEnrollment(roadmap);

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
      className="group relative flex min-h-[86px] cursor-pointer items-center overflow-hidden rounded-2xl border border-[#B9D8CC]/75 bg-white px-5 py-4 text-left outline-none transition duration-200 hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md focus-visible:border-[#2FA084] focus-visible:ring-4 focus-visible:ring-[#2FA084]/15"
      style={{
        animation: "roadmapCardIn 420ms ease-out both",
        animationDelay: `${Math.min(index, 9) * 55}ms`,
      }}
    >
      <h2 className="min-w-0 flex-1 whitespace-nowrap pr-16 text-[13px] font-black leading-5 tracking-tight text-[#18332D] sm:text-[13.5px] lg:text-[14px]">
        {title}
      </h2>

      {enrollment && progressPercent > 0 ? (
        <span className="pointer-events-none absolute right-3 top-2.5 max-w-[52px] rounded-md border border-[#A8D3C4]/70 bg-[#EAF8F1] px-2 py-0.5 text-[10px] font-extrabold text-[#1F6F5F] opacity-0 shadow-sm transition-opacity duration-150 group-hover:opacity-100 group-focus-visible:opacity-100">
          {progressPercent}%
        </span>
      ) : null}

      <div className="absolute inset-x-0 bottom-0 h-1 bg-[#E3EEE8]">
        <div
          className="h-full bg-[#22C55E] transition-[width] duration-300 ease-out"
          style={{ width: `${progressPercent}%` }}
        />
      </div>
    </article>
  );
}
