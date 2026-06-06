import { Check, ExternalLink, GitFork, Star } from "lucide-react";

export default function RepositorySelectCard({ repo, checked, onToggle }) {
  return (
    <article
      onClick={onToggle}
      className={`group cursor-pointer rounded-[1.75rem] border bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-lg ${
        checked
          ? "border-[#2FA084] ring-4 ring-[#6FCF97]/35"
          : "border-slate-200 hover:border-[#6FCF97]"
      }`}
    >
      <div className="flex items-start gap-4">
        <div
          className={`mt-1 flex h-6 w-6 shrink-0 items-center justify-center rounded-lg border transition ${
            checked
              ? "border-emerald-600 bg-[#2FA084] text-white"
              : "border-slate-300 bg-white text-transparent group-hover:border-[#2FA084]"
          }`}
        >
          <Check size={15} strokeWidth={3} />
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="line-clamp-1 font-black text-slate-950">{repo.name}</p>
              <p className="mt-1 truncate text-sm font-medium text-slate-500">{repo.fullName}</p>
            </div>

            {repo.htmlUrl && (
              <a
                href={repo.htmlUrl}
                target="_blank"
                rel="noreferrer"
                onClick={(event) => event.stopPropagation()}
                className="flex h-9 w-9 shrink-0 items-center justify-center rounded-2xl bg-slate-100 text-slate-400 transition hover:bg-[#2FA084] hover:text-white"
              >
                <ExternalLink size={17} />
              </a>
            )}
          </div>

          <p className="mt-4 line-clamp-3 min-h-[4.5rem] text-sm leading-6 text-slate-600">
            {repo.description || "No repository description."}
          </p>

          <div className="mt-5 flex flex-wrap items-center gap-2 border-t border-slate-100 pt-4">
            {repo.primaryLanguage && (
              <span className="rounded-full bg-[#6FCF97]/25 px-3 py-1.5 text-xs font-bold text-[#1F6F5F] ring-1 ring-[#6FCF97]/35">
                {repo.primaryLanguage}
              </span>
            )}

            <span className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-3 py-1.5 text-xs font-bold text-slate-600">
              <Star size={14} />
              {repo.stars ?? 0}
            </span>

            <span className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-3 py-1.5 text-xs font-bold text-slate-600">
              <GitFork size={14} />
              {repo.forks ?? 0}
            </span>
          </div>
        </div>
      </div>
    </article>
  );
}
