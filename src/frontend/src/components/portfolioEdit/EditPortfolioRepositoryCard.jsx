import { ExternalLink, GitFork, Loader2, Sparkles, Star } from "lucide-react";
import { getRepoDescription, getRepoName, getRepoOwner } from "./portfolioEditUtils";

function getInsightStatus(insight) {
  if (!insight) {
    return {
      label: "No AI summary",
      className: "bg-white text-[#667A73] ring-1 ring-[#DCEBE5]",
    };
  }

  if (insight.analysisStatus === "failed") {
    return {
      label: "Summary failed",
      className: "bg-red-50 text-red-700 ring-1 ring-red-200",
    };
  }

  if (insight.analysisStatus === "pending") {
    return {
      label: "Summary pending",
      className: "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
    };
  }

  return {
    label: "AI summary ready",
    className: "bg-[#6FCF97]/18 text-[#1F6F5F] ring-1 ring-[#B9D8CC]",
  };
}

export default function EditPortfolioRepositoryCard({
  repository,
  username,
  isSelected,
  isAnalyzing,
  onToggle,
  onGenerateInsight,
}) {
  const repoName = getRepoName(repository);
  const owner = getRepoOwner(repository, username);
  const insight = repository?.insight;
  const hasCompletedInsight = insight?.analysisStatus === "completed" && insight?.summary;
  const description = hasCompletedInsight ? insight.summary : getRepoDescription(repository);
  const language = repository?.primaryLanguage || repository?.language || insight?.techStack?.[0] || repository?.techStack?.[0] || "Project";
  const stars = Number(repository?.stars ?? repository?.starCount ?? 0);
  const forks = Number(repository?.forks ?? repository?.forkCount ?? 0);
  const repositoryUrl = repository?.htmlUrl || repository?.repoUrl;
  const insightStatus = getInsightStatus(insight);
  const buttonLabel = hasCompletedInsight ? "Regenerate" : "Generate";

  return (
    <article className={`rounded-lg border p-4 shadow-[0_10px_24px_rgba(31,111,95,0.06)] transition hover:-translate-y-1 hover:shadow-[0_18px_40px_rgba(31,111,95,0.11)] ${isSelected ? "border-[#2FA084] bg-[#6FCF97]/12" : "border-[#E2D2B8] bg-[#FFF8EF]"}`}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-base font-bold text-[#18332D]">{repoName}</p>
          <p className="mt-1 truncate text-xs font-semibold text-[#667A73]">{owner}/{repoName}</p>
        </div>

        <button type="button" onClick={onToggle} className={`shrink-0 rounded-full px-3 py-1 text-xs font-bold transition ${isSelected ? "bg-[#2FA084] text-white" : "bg-white text-[#667A73] ring-1 ring-[#DCEBE5] hover:text-[#1F6F5F]"}`}>
          {isSelected ? "Selected" : "Choose"}
        </button>
      </div>

      <p className="portfolio-editor-clamp-2 mt-4 min-h-[48px] text-sm leading-6 text-[#34544C]">{description}</p>

      {hasCompletedInsight && insight.techStack?.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-2">
          {insight.techStack.slice(0, 3).map((tech) => (
            <span key={tech} className="rounded-full bg-white px-3 py-1 text-xs font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC]">
              {tech}
            </span>
          ))}
        </div>
      )}

      <div className="mt-4 border-t border-[#E2D2B8] pt-3">
        <div className="flex flex-wrap items-center gap-2 text-xs font-semibold text-[#667A73]">
          <span className="rounded-full bg-white px-3 py-1 text-[#1F6F5F]">{language}</span>
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-3 py-1"><Star size={13} /> {stars}</span>
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-3 py-1"><GitFork size={13} /> {forks}</span>
          {repositoryUrl && (
            <a href={repositoryUrl} target="_blank" rel="noreferrer" className="ml-auto inline-flex items-center gap-1 text-[#1F6F5F] hover:underline">
              View <ExternalLink size={13} />
            </a>
          )}
        </div>
      </div>

      <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <span className={`inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold ${insightStatus.className}`}>
          {insightStatus.label}
        </span>

        <button
          type="button"
          onClick={() => onGenerateInsight?.(repository.repositoryId, hasCompletedInsight)}
          disabled={isAnalyzing}
          className="inline-flex w-fit items-center justify-center gap-1.5 rounded-full bg-white px-3 py-1 text-xs font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC] transition hover:bg-[#6FCF97]/15 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isAnalyzing ? <Loader2 className="animate-spin" size={13} /> : <Sparkles size={13} />}
          {isAnalyzing ? "Generating..." : buttonLabel}
        </button>
      </div>
    </article>
  );
}
