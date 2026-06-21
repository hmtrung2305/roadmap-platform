import { ExternalLink, GitFork, Star } from "lucide-react";
import { FaGithub } from "react-icons/fa";

function toTagArray(value) {
  if (!value) return [];
  if (Array.isArray(value)) return value;
  if (typeof value === "string") {
    return value
      .split(/[;,]/)
      .map((item) => item.trim())
      .filter(Boolean);
  }
  return [];
}

function getProjectTags(repository, insight) {
  const aiTags = [
    insight?.projectType,
    ...toTagArray(insight?.techStack),
    ...toTagArray(insight?.detectedSkills),
    ...toTagArray(insight?.skills),
  ].filter(Boolean);

  const readmeTags = [
    ...toTagArray(repository.detectedSkills),
    ...toTagArray(repository.techStack),
    ...toTagArray(repository.readmeSkills),
    ...toTagArray(repository.readmeTechnologies),
  ].filter(Boolean);

  const fallbackTags = [
    repository.primaryLanguage || repository.language,
  ].filter(Boolean);
  const source =
    aiTags.length > 0
      ? aiTags
      : readmeTags.length > 0
        ? readmeTags
        : fallbackTags;

  return Array.from(
    new Set(source.map((tag) => String(tag).trim()).filter(Boolean)),
  );
}

function getVisibleTags(tags, limit = 4) {
  const visible = tags.slice(0, limit);
  const hidden = tags.slice(limit);

  return {
    visible,
    hidden,
    extraCount: hidden.length,
  };
}

function ExtraTagPopover({ hiddenTags }) {
  if (!hiddenTags?.length) return null;

  return (
    <span className="group/extra relative inline-flex overflow-visible">
      <span className="cursor-help rounded-lg bg-[#EAF8F1] px-2 py-1 text-xs font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC] transition-colors hover:bg-[#DDF3EA]">
        +{hiddenTags.length}
      </span>

      <span className="pointer-events-none absolute top-full left-0 !z-[999] mb-2 hidden w-[400px] max-w-[calc(100vw-48px)] rounded-md border border-[#B9D8CC] bg-white px-4 py-3 text-xs font-bold leading-5 text-[#18332D] shadow-[0_18px_44px_rgba(31,111,95,0.18)] group-hover/extra:block">
        <span className="mb-2 block h-0.5 w-14 rounded-md bg-[#2FA084]" />

        <span className="flex flex-wrap gap-1.5">
          {hiddenTags.map((tag) => (
            <span
              key={tag}
              className="whitespace-normal break-words rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-2.5 py-1 text-xs font-semibold leading-5 text-slate-700"
            >
              {tag}
            </span>
          ))}
        </span>
      </span>
    </span>
  );
}

export default function PortfolioRepositoryCard({ repository }) {
  const insight = repository.insight;
  const insightStatus = String(insight?.analysisStatus || insight?.status || "").toLowerCase();
  const hasCompletedInsight =
    insightStatus === "completed" && insight?.summary;
  const publicInsight = hasCompletedInsight ? insight : null;
  const name = repository.name || repository.repoName || "Untitled repository";
  const href = repository.htmlUrl || repository.repoUrl;

  const description =
    (hasCompletedInsight ? insight.summary : null) ||
    repository.summary ||
    repository.description ||
    "No description provided.";

  const tags = getProjectTags(repository, publicInsight);
  const {
    visible: visibleTags,
    hidden: hiddenTags,
    extraCount,
  } = getVisibleTags(tags, 4);

  return (
    <article className="group relative z-1 hover:z-2 flex min-h-[250px] flex-col rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-[0_14px_34px_rgba(31,111,95,0.08)] transition hover:-translate-y-1 hover:shadow-[0_18px_44px_rgba(31,111,95,0.13)]">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="line-clamp-1 text-lg font-extrabold text-[#18332D]">
            {name}
          </h3>

          <p className="mt-1 text-xs font-extrabold uppercase tracking-[0.14em] text-[#2FA084]">
            {hasCompletedInsight ? "AI summarized project" : "Repository"}
          </p>
        </div>

        {href && (
          <a
            href={href}
            target="_blank"
            rel="noreferrer"
            onClick={(event) => event.stopPropagation()}
            className="shrink-0 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] p-2 text-[#1F6F5F] transition hover:bg-[#6FCF97]/30"
            aria-label="Open repository"
          >
            <ExternalLink size={15} />
          </a>
        )}
      </div>

      <div className="group/summary relative mt-4 min-h-[3rem]">
        <p className="line-clamp-2 cursor-help text-sm font-medium leading-6 text-slate-600">
          {description}
        </p>

        <div className="pointer-events-none absolute left-0 top-full z-100 mt-2 hidden w-full rounded-lg border border-[#B9D8CC] bg-white px-4 py-3 text-xs font-semibold leading-5 text-[#18332D] shadow-[0_18px_44px_rgba(31,111,95,0.18)] group-hover/summary:block">
          <div className="mb-2 h-0.5 w-16 rounded-lg bg-[#2FA084]" />
          {description}
        </div>
      </div>

      <div className="mt-4 min-h-[3.1rem] border-b border-[#DCEBE5] pb-4">
        {tags.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {visibleTags.map((tag) => (
              <span
                key={tag}
                className="rounded-lg bg-[#F7F1E8] px-2 py-1 text-xs font-bold text-slate-700 ring-1 ring-[#B9D8CC]"
              >
                {tag}
              </span>
            ))}
            {extraCount > 0 && <ExtraTagPopover hiddenTags={hiddenTags} />}
          </div>
        ) : (
          <span className="text-xs font-medium text-slate-400">
            No tech tags
          </span>
        )}
      </div>

      <div className="mt-auto flex items-center gap-2 pt-4 text-xs font-extrabold text-slate-600">
        <span className="inline-flex items-center gap-1 rounded-lg bg-[#EEEEEE] px-2 py-1">
          <Star size={13} />
          {repository.stars ?? repository.starCount ?? 0}
        </span>

        <span className="inline-flex items-center gap-1 rounded-lg bg-[#EEEEEE] px-2 py-1">
          <GitFork size={13} />
          {repository.forks ?? repository.forkCount ?? 0}
        </span>

        <span className="ml-auto text-[#1F6F5F]">
          <FaGithub size={15} />
        </span>
      </div>
    </article>
  );
}
