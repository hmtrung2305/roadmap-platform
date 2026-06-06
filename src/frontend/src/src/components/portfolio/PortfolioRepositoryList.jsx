import { Link } from "react-router-dom";
import { ArrowRight, Sparkles } from "lucide-react";
import PortfolioRepositoryCard from "./PortfolioRepositoryCard";
import PortfolioEmptyState from "./PortfolioEmptyState";

export default function PortfolioRepositoryList({ repositories = [], editable = false }) {
  return (
    <section className="rounded-[1.7rem] border border-slate-200/80 bg-white p-5 shadow-sm sm:p-6">
      <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="flex items-center gap-2 text-xl font-black tracking-tight text-slate-950">
            <span className="text-emerald-700">
              <Sparkles size={22} />
            </span>
            Selected Repositories
          </h2>
          <p className="mt-1 text-sm text-slate-500">Projects selected to appear on the public portfolio.</p>
        </div>

        {editable ? (
          <Link
            to="/portfolio/repositories"
            className="inline-flex w-fit items-center gap-1.5 text-sm font-bold text-emerald-700 transition hover:text-slate-950"
          >
            Manage repositories
            <ArrowRight size={15} />
          </Link>
        ) : (
          <span className="w-fit rounded-full bg-emerald-50 px-3 py-1.5 text-sm font-bold text-emerald-700 ring-1 ring-emerald-100">
            {repositories.length} selected
          </span>
        )}
      </div>

      {repositories.length === 0 ? (
        <PortfolioEmptyState />
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {repositories.map((repo) => (
            <PortfolioRepositoryCard key={repo.repositoryId} repository={repo} />
          ))}
        </div>
      )}
    </section>
  );
}
