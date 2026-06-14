import { ExternalLink, GitFork, Loader2, Sparkles, Star } from "lucide-react";

function getInsightStatus(insight) {
  if (!insight) {
    return {
      label: "No AI summary yet",
      className: "border-slate-200 bg-slate-50 text-slate-500",
    };
  }

  if (insight.analysisStatus === "failed") {
    return {
      label: "Summary failed",
      className: "border-red-200 bg-red-50 text-red-700",
    };
  }

  if (insight.analysisStatus === "pending") {
    return {
      label: "Summary pending",
      className: "border-amber-200 bg-amber-50 text-amber-700",
    };
  }

  return {
    label: "AI summary ready",
    className: "border-[#B9D8CC] bg-[#6FCF97]/15 text-[#1F6F5F]",
  };
}

export default function RepositorySelectCard({
  repo,
  checked,
  isAnalyzing,
  onToggle,
  onGenerateInsight,
}) {
  const insight = repo.insight;
  const insightStatus = getInsightStatus(insight);
  const hasCompletedInsight = insight?.analysisStatus === "completed" && insight?.summary;
  const buttonLabel = hasCompletedInsight ? "Regenerate" : "Generate";

  return (
    <article
      className={`rounded-lg border bg-white p-6 shadow-sm transition ${
        checked
          ? "border-blue-300 ring-2 ring-blue-100"
          : "border-slate-200 hover:border-[#B9D8CC]"
      }`}
    >
      <div className="flex items-start gap-4">
        <input
          type="checkbox"
          checked={checked}
          onChange={onToggle}
          className="mt-1 h-5 w-5 accent-blue-700"
        />

        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <a
                href={repo.htmlUrl}
                target="_blank"
                rel="noreferrer"
                className="font-bold text-[#1F6F5F] hover:underline"
              >
                {repo.name}
              </a>

              <p className="mt-1 truncate text-sm text-slate-500">
                {repo.fullName}
              </p>
            </div>

            <a
              href={repo.htmlUrl}
              target="_blank"
              rel="noreferrer"
              className="text-slate-400 hover:text-[#1F6F5F]"
            >
              <ExternalLink size={18} />
            </a>
          </div>

          <p className="mt-4 text-sm leading-6 text-slate-600">
            {hasCompletedInsight ? insight.summary : repo.description || "No repository description."}
          </p>

          {hasCompletedInsight && insight.techStack?.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-2">
              {insight.techStack.slice(0, 4).map((tech) => (
                <span
                  key={tech}
                  className="rounded-md bg-[#F7F1E8] px-2 py-1 text-xs font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC]"
                >
                  {tech}
                </span>
              ))}
            </div>
          )}

          <div className="mt-5 flex flex-wrap items-center gap-2">
            {repo.primaryLanguage && (
              <span className="rounded-md bg-[#6FCF97]/15 px-3 py-1 text-sm font-medium text-[#1F6F5F]">
                {repo.primaryLanguage}
              </span>
            )}

            <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
              <Star size={13} />
              {repo.stars}
            </span>

            <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
              <GitFork size={13} />
              {repo.forks}
            </span>
          </div>

          <div className="mt-5 flex flex-col gap-3 border-t border-slate-100 pt-4 sm:flex-row sm:items-center sm:justify-between">
            <span className={`inline-flex w-fit rounded-md border px-3 py-1 text-xs font-bold ${insightStatus.className}`}>
              {insightStatus.label}
            </span>

            <button
              type="button"
              onClick={() => onGenerateInsight?.(repo.repositoryId, hasCompletedInsight)}
              disabled={isAnalyzing}
              className="inline-flex w-fit items-center justify-center gap-1.5 rounded-md bg-white px-3 py-1 text-xs font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC] transition hover:bg-[#6FCF97]/15 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isAnalyzing ? <Loader2 className="animate-spin" size={13} /> : <Sparkles size={13} />}
              {isAnalyzing ? "Generating..." : buttonLabel}
            </button>
          </div>
        </div>
      </div>
    </article>
  );
}
