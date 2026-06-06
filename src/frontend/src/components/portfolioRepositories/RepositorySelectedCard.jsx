import { ExternalLink, GitFork, Star } from "lucide-react";

export default function RepositorySelectCard({ repo, checked, onToggle }) {
  return (
    <article
      className={`rounded-2xl border bg-white p-6 shadow-sm transition ${
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
            {repo.description || "No repository description."}
          </p>

          <div className="mt-5 flex flex-wrap items-center gap-2">
            {repo.primaryLanguage && (
              <span className="rounded-md bg-[#6FCF97]/15 px-3 py-1 text-sm font-medium text-[#1F6F5F]">
                {repo.primaryLanguage}
              </span>
            )}

            <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
              <Star size={14} />
              {repo.stars}
            </span>

            <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
              <GitFork size={14} />
              {repo.forks}
            </span>
          </div>
        </div>
      </div>
    </article>
  );
}