import PortfolioRepositoryCard from "./PortfolioRepositoryCard";
import PortfolioEmptyState from "./PortfolioEmptyState";

export default function PortfolioRepositoryList({ repositories = [] }) {
  return (
    <section>
      <div className="mb-4">
        <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
          Featured Projects
        </p>

        <h2 className="mt-1 text-sm font-extrabold text-[#18332D]">
          {repositories.length} selected projects · public repositories
        </h2>
      </div>

      {repositories.length === 0 ? (
        <PortfolioEmptyState />
      ) : (
        <div className="grid items-stretch gap-5 md:grid-cols-2 xl:grid-cols-3">
          {repositories.map((repo) => (
            <PortfolioRepositoryCard
              key={repo.repositoryId || repo.repoName || repo.name}
              repository={repo}
            />
          ))}
        </div>
      )}
    </section>
  );
}
