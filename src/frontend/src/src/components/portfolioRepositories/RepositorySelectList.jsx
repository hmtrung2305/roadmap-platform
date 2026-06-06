import RepositorySelectCard from "./RepositorySelectedCard";

export default function RepositorySelectList({ repositories, selectedIds, onToggleRepository }) {
  return (
    <section className="grid grid-cols-1 gap-5 lg:grid-cols-2">
      {repositories.map((repo) => (
        <RepositorySelectCard
          key={repo.repositoryId}
          repo={repo}
          checked={selectedIds.includes(repo.repositoryId)}
          onToggle={() => onToggleRepository(repo.repositoryId)}
        />
      ))}
    </section>
  );
}
