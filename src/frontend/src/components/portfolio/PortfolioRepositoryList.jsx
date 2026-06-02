import PortfolioRepositoryCard from "./PortfolioRepositoryCard";
import PortfolioEmptyState from "./PortfolioEmptyState";
import { MdFeaturedPlayList } from "react-icons/md";

export default function PortfolioRepositoryList({ repositories }) {
  return (
    <section>
      <div className="mb-5 flex items-center justify-between">
        <h2 className="flex items-center gap-2 text-2xl font-bold text-slate-900">
          <span className="text-blue-700">
            <MdFeaturedPlayList size={24}/>
          </span>
          Featured Repositories
        </h2>

        <span className="rounded-full bg-slate-200 px-3 py-1 text-sm text-slate-600">
          {repositories.length} selected
        </span>
      </div>

      {repositories.length === 0 ? (
        <PortfolioEmptyState />
      ) : (
        <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
          {repositories.map((repo) => (
            <PortfolioRepositoryCard
              key={repo.repositoryId}
              repository={repo}
            />
          ))}
        </div>
      )}
    </section>
  );
}