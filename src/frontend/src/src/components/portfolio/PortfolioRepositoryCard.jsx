import { ExternalLink, GitFork, Star } from "lucide-react";

export default function PortfolioRepositoryCard({ repository }) {
  return (
    <article className="group relative overflow-hidden rounded-2xl border border-slate-200 bg-white p-4 shadow-sm transition duration-200 hover:-translate-y-1 hover:border-emerald-200 hover:shadow-lg hover:shadow-emerald-100/60">
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-start gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-700 ring-1 ring-emerald-100">
            <GitFork size={17} />
          </div>

          <div className="min-w-0">
            <a
              href={repository.htmlUrl}
              target="_blank"
              rel="noreferrer"
              className="line-clamp-1 text-base font-black text-slate-950 transition hover:text-emerald-700"
            >
              {repository.name || "Untitled repository"}
            </a>
            <p className="mt-1 line-clamp-1 text-xs font-medium text-slate-500">
              {repository.fullName || "No full name"}
            </p>
          </div>
        </div>

        {repository.htmlUrl && (
          <a
            href={repository.htmlUrl}
            target="_blank"
            rel="noreferrer"
            className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-slate-100 text-slate-500 transition group-hover:bg-emerald-600 group-hover:text-white"
            aria-label="Open repository"
          >
            <ExternalLink size={16} />
          </a>
        )}
      </div>

      <p className="mt-4 line-clamp-3 min-h-[4.5rem] text-sm leading-6 text-slate-600">
        {repository.description || "No repository description available."}
      </p>

      <div className="mt-4 flex flex-wrap items-center gap-2 border-t border-slate-100 pt-4">
        {repository.primaryLanguage && (
          <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-bold text-emerald-700 ring-1 ring-emerald-100">
            {repository.primaryLanguage}
          </span>
        )}

        <Metric icon={<Star size={13} />} value={repository.stars ?? 0} />
        <Metric icon={<GitFork size={13} />} value={repository.forks ?? 0} />
      </div>
    </article>
  );
}

function Metric({ icon, value }) {
  return (
    <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600">
      {icon}
      {value}
    </span>
  );
}
