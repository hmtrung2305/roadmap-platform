import { RefreshCw, Save } from "lucide-react";

export default function RepositoryPageHeader({
  selectedCount,
  syncing,
  saving,
  onSync,
  onSave,
}) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-7 shadow-sm">
      <div className="flex flex-col gap-5 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            Manage Portfolio Repositories
          </h1>

          <p className="mt-1 text-slate-500">
            Select which GitHub repositories should appear on your portfolio.
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <button
            type="button"
            onClick={onSync}
            disabled={syncing}
            className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <RefreshCw size={16} className={syncing ? "animate-spin" : ""} />
            {syncing ? "Syncing..." : "Sync GitHub"}
          </button>

          <button
            type="button"
            onClick={onSave}
            disabled={saving}
            className="inline-flex items-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2 text-sm font-semibold text-white hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            <Save size={16} />
            {saving ? "Saving..." : "Save Selection"}
          </button>
        </div>
      </div>

      <div className="mt-6 rounded-lg bg-[#6FCF97]/15 px-4 py-3 text-sm font-medium text-[#1F6F5F]">
        {selectedCount} repositories selected for your portfolio.
      </div>
    </section>
  );
}