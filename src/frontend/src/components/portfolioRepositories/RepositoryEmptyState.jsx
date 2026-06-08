export default function RepositoryEmptyState() {
  return (
    <section className="rounded-lg border border-dashed border-slate-300 bg-white p-10 text-center shadow-sm">
      <h2 className="text-lg font-bold text-slate-900">
        No repositories found
      </h2>

      <p className="mt-2 text-slate-500">
        Click Sync GitHub to import your public repositories.
      </p>
    </section>
  );
}