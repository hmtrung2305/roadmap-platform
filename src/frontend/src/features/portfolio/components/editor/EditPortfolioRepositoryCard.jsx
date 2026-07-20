import {
  Check,
  ExternalLink,
  GitFork,
  Loader2,
  Sparkles,
  Star,
} from "lucide-react";
import {
  getRepoDescription,
  getRepoName,
  getRepositoryId,
} from "../../utils/portfolioEditUtils";

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
      className:
        "bg-white text-[#1F6F5F] ring-1 ring-[#B9D8CC] hover:bg-[#6FCF97]/15",
    };
  }

  const status = String(
    insight.analysisStatus || insight.status || "",
  ).toLowerCase();

  if (status === "failed") {
    return {
      label: "Retry",
      className: "bg-red-50 text-red-700 ring-1 ring-red-200 hover:bg-red-100",
    };
  }

  if (status === "pending") {
    return {
      label: "Pending",
      className:
        "bg-amber-50 text-amber-700 ring-1 ring-amber-200 hover:bg-amber-100",
    };
  }

  return {
    label: "Generate",
    className:
      "bg-[#6FCF97]/18 text-[#1F6F5F] ring-1 ring-[#B9D8CC] hover:bg-[#6FCF97]/25",
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

  const fallbackTags = [
    repository?.primaryLanguage || repository?.language,
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

function ExtraTagCount({ count }) {
  if (!count) return null;

  return (
    <span className="rounded-full bg-white px-2 py-0.5 !text-[12px] font-bold text-[#667A73] ring-1 ring-[#DCEBE5]">
      +{count}
    </span>
  );
}

export default function EditPortfolioRepositoryCard({
  repository,
  isSelected,
  isAnalyzing,
  actionDisabled = false,
  selectionDisabled = false,
  selectionDisabledReason = "",
  aiCreditDisabled = false,
  onToggle,
  onGenerateInsight,
}) {
  const repositoryId = getRepositoryId(repository);
  const repoName = getRepoName(repository);
  const insight = repository?.insight;
  const insightAnalysisStatus = String(
    insight?.analysisStatus || insight?.status || "",
  ).toLowerCase();
  const hasCompletedInsight =
    insightAnalysisStatus === "completed" && insight?.summary;
  const description = hasCompletedInsight
    ? insight.summary
    : getRepoDescription(repository);
  const stars = Number(repository?.stars ?? repository?.starCount ?? 0);
  const forks = Number(repository?.forks ?? repository?.forkCount ?? 0);
  const repositoryUrl = repository?.htmlUrl || repository?.repoUrl;
  const repositoryFullName =
    repository?.fullName || repository?.full_name || repository?.repoFullName;
  const insightStatus = getInsightStatus(insight);
  const insightButtonLabel = hasCompletedInsight
    ? "Regenerate"
    : insightStatus.label;
  const tags = getUniqueTags(repository, insight);
  const { visible: visibleTags, extraCount } = getVisibleTags(tags, 4);

  return (
    <article
      className={`flex min-h-[132px] flex-col rounded-lg border p-3.5 shadow-[0_10px_24px_rgba(31,111,95,0.06)] transition-colors hover:border-[#2FA084] ${isSelected ? "border-[#2FA084] bg-[#6FCF97]/12" : "border-[#E2D2B8] bg-[#FFF8EF] hover:bg-white"}`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate !text-[17px] font-bold leading-5 text-[#18332D]">
            {repoName}
          </p>

          {repositoryFullName && repositoryUrl ? (
            <a
              href={repositoryUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-1 inline-flex max-w-full items-center gap-1 truncate !text-[13px] font-semibold text-[#667A73] hover:text-[#1F6F5F] hover:underline"
              title={repositoryFullName}
            >
              <span className="truncate">{repositoryFullName}</span>
              <ExternalLink size={12} className="shrink-0" />
            </a>
          ) : repositoryFullName ? (
            <p
              className="mt-1 truncate !text-[13px] font-semibold text-[#667A73]"
              title={repositoryFullName}
            >
              {repositoryFullName}
            </p>
          ) : null}
        </div>

        <button
          type="button"
          onClick={onToggle}
          disabled={selectionDisabled}
          aria-label={
            isSelected ? "Remove from portfolio" : "Select for portfolio"
          }
          title={
            selectionDisabled
              ? selectionDisabledReason ||
                "Repository selection is temporarily disabled."
              : isSelected
                ? "Selected"
                : "Choose"
          }
          className={`grid size-5 shrink-0 place-items-center rounded-[3px] cursor-pointer transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${isSelected ? "bg-[#2FA084] text-white" : "bg-white text-[#8BA39B] ring-1 ring-[#B9D8CC] hover:bg-[#EAF8F1] hover:text-[#1F6F5F]"}`}
        >
          {isSelected ? <Check size={12} strokeWidth={3} /> : null}
        </button>
      </div>

      <p
        className="portfolio-editor-clamp-2 mt-2.5 !text-[15px] leading-5 text-[#34544C]"
        title={description}
      >
        {description}
      </p>

      {tags.length > 0 && (
        <div className="mt-2.5 flex flex-wrap items-start gap-1.5">
          {visibleTags.map((tag) => (
            <span
              key={tag}
              className="max-w-[155px] truncate rounded-full bg-white px-2 py-0.5 !text-[12px] font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC]"
              title={tag}
            >
              {tag}
            </span>
          ))}
          {extraCount > 0 && <ExtraTagCount count={extraCount} />}
        </div>
      )}

      <div
        className={[
          "border-t border-[#E2D2B8] pt-2.5",
          tags.length > 0 ? "mt-2.5" : "mt-3",
        ].join(" ")}
      >
        <div className="flex flex-wrap items-center gap-2 !text-[13px] font-semibold text-[#667A73]">
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-2.5 py-0.5">
            <Star size={13} /> {stars}
          </span>
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-2.5 py-0.5">
            <GitFork size={13} /> {forks}
          </span>

          <button
            type="button"
            onClick={() =>
              onGenerateInsight?.(repositoryId, hasCompletedInsight, repoName)
            }
            disabled={
              actionDisabled || aiCreditDisabled || isAnalyzing || !repositoryId
            }
            className={`ml-auto inline-flex shrink-0 cursor-pointer items-center justify-center gap-1 rounded-full px-2.5 py-0.5 !text-[14px] font-bold transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${insightStatus.className}`}
            title={
              hasCompletedInsight
                ? "Regenerate the AI project summary from repository data."
                : aiCreditDisabled
                  ? "You have no AI credits left today."
                  : "Generate a short AI summary for this repository."
            }
          >
            {isAnalyzing ? (
              <Loader2 className="animate-spin" size={12} />
            ) : (
              <Sparkles size={12} />
            )}
            {isAnalyzing ? "Generating" : insightButtonLabel}
          </button>
        </div>
      </div>
    </article>
  );
}
