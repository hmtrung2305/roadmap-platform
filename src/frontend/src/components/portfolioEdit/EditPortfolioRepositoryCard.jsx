import { ExternalLink, GitFork, Loader2, Sparkles, Star } from "lucide-react";
import { getRepoDescription, getRepoName } from "./portfolioEditUtils";

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

function getInsightStatus(insight) {
  if (!insight) {
    return {
      label: "Generate",
      className: "bg-white text-[#1F6F5F] ring-1 ring-[#B9D8CC] hover:bg-[#6FCF97]/15",
    };
  }

  if (insight.analysisStatus === "failed") {
    return {
      label: "Retry",
      className: "bg-red-50 text-red-700 ring-1 ring-red-200 hover:bg-red-100",
    };
  }

  if (insight.analysisStatus === "pending") {
    return {
      label: "Pending",
      className: "bg-amber-50 text-amber-700 ring-1 ring-amber-200 hover:bg-amber-100",
    };
  }

  return {
    label: "Generate",
    className: "bg-[#6FCF97]/18 text-[#1F6F5F] ring-1 ring-[#B9D8CC] hover:bg-[#6FCF97]/25",
  };
}

function getUniqueTags(repository, insight) {
  const aiTags = [
    insight?.projectType,
    ...toTagArray(insight?.techStack),
    ...toTagArray(insight?.detectedSkills),
    ...toTagArray(insight?.skills),
  ].filter(Boolean);

  const readmeTags = [
    ...toTagArray(repository?.detectedSkills),
    ...toTagArray(repository?.techStack),
    ...toTagArray(repository?.readmeSkills),
    ...toTagArray(repository?.readmeTechnologies),
  ].filter(Boolean);

  const fallbackTags = [repository?.primaryLanguage || repository?.language].filter(Boolean);
  const source = aiTags.length > 0 ? aiTags : readmeTags.length > 0 ? readmeTags : fallbackTags;

  return Array.from(new Set(source.map((tag) => String(tag).trim()).filter(Boolean)));
}

function getVisibleTags(tags) {
  const visible = tags.slice(0, 2);
  const extraCount = Math.max(tags.length - visible.length, 0);
  return { visible, extraCount };
}


export default function EditPortfolioRepositoryCard({
  repository,
  isSelected,
  isAnalyzing,
  onToggle,
  onGenerateInsight,
}) {
  const repoName = getRepoName(repository);
  const insight = repository?.insight;
  const hasCompletedInsight = insight?.analysisStatus === "completed" && insight?.summary;
  const description = hasCompletedInsight ? insight.summary : getRepoDescription(repository);
  const stars = Number(repository?.stars ?? repository?.starCount ?? 0);
  const forks = Number(repository?.forks ?? repository?.forkCount ?? 0);
  const repositoryUrl = repository?.htmlUrl || repository?.repoUrl;
  const insightStatus = getInsightStatus(insight);
  const insightButtonLabel = hasCompletedInsight ? "Regenerate" : insightStatus.label;
  const tags = getUniqueTags(repository, insight);
  const { visible: visibleTags, extraCount } = getVisibleTags(tags);

  return (
    <article className={`flex flex-col rounded-lg border p-4 shadow-[0_10px_24px_rgba(31,111,95,0.06)] transition hover:shadow-[0_16px_34px_rgba(31,111,95,0.09)] ${isSelected ? "border-[#2FA084] bg-[#6FCF97]/12" : "border-[#E2D2B8] bg-[#FFF8EF]"}`}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-base font-bold text-[#18332D]">{repoName}</p>
          {(repository?.fullName || repository?.full_name || repository?.repoFullName) && (
            <p className="mt-1 truncate text-xs font-semibold text-[#667A73]">
              {repository.fullName || repository.full_name || repository.repoFullName}
            </p>
          )}
        </div>

        <button
          type="button"
          onClick={onToggle}
          className={`shrink-0 rounded-[12px] px-3 py-1 text-xs cursor-pointer font-bold transition ${isSelected ? "bg-[#2FA084] text-white" : "bg-white text-[#667A73] ring-1 ring-[#DCEBE5] hover:text-[#1F6F5F]"}`}
        >
          {isSelected ? "Selected" : "Choose"}
        </button>
      </div>

      <p
        className="portfolio-editor-clamp-2 mt-4 text-sm leading-6 text-[#34544C]"
        title={description}
      >
        {description}
      </p>

      <div className="mt-3 space-y-3">
        {tags.length > 0 && (
        <div className="flex flex-wrap items-start gap-1.5">
          {visibleTags.map((tag) => (
            <span key={tag} className="max-w-[155px] truncate rounded-full bg-white px-2.5 py-1 text-[11px] font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC]" title={tag}>
              {tag}
            </span>
          ))}
          {extraCount > 0 && (
            <span className="rounded-full bg-white px-2.5 py-1 text-[11px] font-bold text-[#667A73] ring-1 ring-[#DCEBE5]" title={tags.slice(2).join(", ")}>
              +{extraCount}
            </span>
          )}
        </div>
        )}

        <div className="flex justify-end">
          <button
            type="button"
            onClick={() => onGenerateInsight?.(repository.repositoryId, hasCompletedInsight)}
            disabled={isAnalyzing}
            className={`inline-flex shrink-0 items-center justify-center gap-1 rounded-full px-2.5 py-1 !text-[12px] cursor-pointer font-bold transition disabled:cursor-not-allowed disabled:opacity-60 ${insightStatus.className}`}
            title={hasCompletedInsight ? "Regenerate the AI project summary from repository data." : "Generate a short AI summary for this repository."}
          >
            {isAnalyzing ? <Loader2 className="animate-spin" size={12} /> : <Sparkles size={12} />}
            {isAnalyzing ? "Generating" : insightButtonLabel}
          </button>
        </div>
      </div>

      <div className="mt-3 border-t border-[#E2D2B8] pt-3">
        <div className="flex flex-wrap items-center gap-2 text-xs font-semibold text-[#667A73]">
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-3 py-1"><Star size={13} /> {stars}</span>
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-3 py-1"><GitFork size={13} /> {forks}</span>
          {repositoryUrl && (
            <a href={repositoryUrl} target="_blank" rel="noreferrer" className="ml-auto inline-flex items-center gap-1 text-[#1F6F5F] hover:underline">
              View <ExternalLink size={13} />
            </a>
          )}
        </div>
      </div>
    </article>
  );
}
