import { ExternalLink, GitFork, Star } from "lucide-react";
import { getRepoDescription, getRepoName, getRepoOwner } from "./portfolioEditUtils";

export default function EditPortfolioRepositoryCard({ repository, username, isSelected, onToggle }) {
  const repoName = getRepoName(repository);
  const owner = getRepoOwner(repository, username);
  const description = getRepoDescription(repository);
  const language = repository?.primaryLanguage || repository?.language || repository?.techStack?.[0] || "Project";
  const stars = Number(repository?.stars ?? repository?.starCount ?? 0);
  const forks = Number(repository?.forks ?? repository?.forkCount ?? 0);

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

      <div className="mt-4 border-t border-[#E2D2B8] pt-3">
        <div className="flex flex-wrap items-center gap-2 text-xs font-semibold text-[#667A73]">
          <span className="rounded-full bg-white px-3 py-1 text-[#1F6F5F]">{language}</span>
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-3 py-1"><Star size={13} /> {stars}</span>
          <span className="inline-flex items-center gap-1 rounded-full bg-white px-3 py-1"><GitFork size={13} /> {forks}</span>
          {repository?.repoUrl && (
            <a href={repository.repoUrl} target="_blank" rel="noreferrer" className="ml-auto inline-flex items-center gap-1 text-[#1F6F5F] hover:underline">
              View <ExternalLink size={13} />
            </a>
          )}
        </div>
      </div>
    </article>
  );
}
