import { ExternalLink, GitFork, Star } from "lucide-react";

export default function PortfolioRepositoryCard({ repository }) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <a
            href={repository.htmlUrl}
            target="_blank"
            rel="noreferrer"
            className="font-bold text-blue-700 hover:underline"
          >
            {repository.name}
          </a>

          <p className="mt-1 truncate text-sm text-slate-500">
            {repository.fullName}
          </p>
        </div>

        <a
          href={repository.htmlUrl}
          target="_blank"
          rel="noreferrer"
          className="text-slate-400 hover:text-blue-700"
        >
          <ExternalLink size={18} />
        </a>
      </div>

      <p className="mt-4 min-h-16 text-sm leading-6 text-slate-600">
        {repository.description || "No repository description."}
      </p>

      <div className="mt-5 flex flex-wrap items-center gap-2 border-t border-slate-200 pt-4">
        {repository.primaryLanguage && (
          <span className="rounded-md bg-blue-50 px-3 py-1 text-sm font-medium text-blue-700">
            {repository.primaryLanguage}
          </span>
        )}

        <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
          <Star size={14} />
          {repository.stars}
        </span>

        <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
          <GitFork size={14} />
          {repository.forks}
        </span>
      </div>
    </article>
  );
}