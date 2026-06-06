import { ExternalLink, GitFork, Star } from "lucide-react";
import { FaGithub } from "react-icons/fa";

export default function PortfolioRepositoryCard({ repository }) {
  const name = repository.name || repository.repoName || "Untitled repository";
  const href = repository.htmlUrl || repository.repoUrl;

  const description =
    repository.summary || repository.description || "No description provided.";

  const tags = [
    repository.primaryLanguage || repository.language,
    ...(repository.techStack || repository.detectedSkills || []),
  ].filter(Boolean);

  return (
    <article className="flex min-h-[240px] flex-col rounded-2xl border border-[#B9D8CC] bg-white p-5 shadow-[0_14px_34px_rgba(31,111,95,0.08)] transition hover:-translate-y-1 hover:shadow-[0_18px_44px_rgba(31,111,95,0.13)]">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="line-clamp-1 text-lg font-extrabold text-[#18332D]">
            {name}
          </h3>

          <p className="mt-1 text-xs font-extrabold uppercase tracking-[0.14em] text-[#2FA084]">
            Repository
          </p>
        </div>

        {href && (
          <a
            href={href}
            target="_blank"
            rel="noreferrer"
            onClick={(event) => event.stopPropagation()}
            className="shrink-0 rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] p-2 text-[#1F6F5F] transition hover:bg-[#6FCF97]/30"
            aria-label="Open repository"
          >
            <ExternalLink size={15} />
          </a>
        )}
      </div>

      <p className="mt-4 line-clamp-2 min-h-[3rem] text-sm font-medium leading-6 text-slate-600">
        {description}
      </p>

      <div className="mt-4 min-h-[2rem]">
        {tags.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {tags.slice(0, 4).map((tag) => (
              <span
                key={tag}
                className="rounded-lg bg-[#F7F1E8] px-2 py-1 text-xs font-bold text-slate-700 ring-1 ring-[#B9D8CC]"
              >
                {tag}
              </span>
            ))}
          </div>
        ) : (
          <span className="text-xs font-medium text-slate-400">
            No tech tags
          </span>
        )}
      </div>

      <div className="mt-auto flex items-center gap-2 border-t border-[#DCEBE5] pt-4 text-xs font-extrabold text-slate-600">
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
