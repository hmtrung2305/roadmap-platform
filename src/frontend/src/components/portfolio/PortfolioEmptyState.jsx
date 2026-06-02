export default function PortfolioEmptyState() {
  return (
    <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center shadow-sm">
      <p className="font-semibold text-slate-700">
        No repositories selected yet.
      </p>

      <p className="mt-2 text-sm text-slate-500">
        Connect GitHub, sync repositories, then choose which projects to show.
      </p>
    </div>
  );
}