import { RefreshCw, Save, Sparkles } from "lucide-react";

export default function RepositoryPageHeader({
  selectedCount,
  totalCount,
  syncing,
  saving,
  onSync,
  onSave,
}) {
  return (
    <section className="relative overflow-hidden rounded-[2rem] border border-emerald-100 bg-gradient-to-br from-white via-[#ecfdf5] to-[#f8fafc] p-6 shadow-sm ring-1 ring-white/80 sm:p-7">
      <div className="absolute -right-16 -top-20 h-56 w-56 rounded-full bg-emerald-200/50 blur-3xl" />
      <div className="absolute -bottom-24 left-16 h-60 w-60 rounded-full bg-cyan-200/40 blur-3xl" />

      <div className="relative flex flex-col gap-5 md:flex-row md:items-center md:justify-between">
        <div>
          <p className="mb-3 inline-flex items-center gap-2 rounded-full bg-white/80 px-3 py-1 text-xs font-black uppercase tracking-[0.18em] text-emerald-700 ring-1 ring-emerald-100">
            <Sparkles size={14} />
            Portfolio Builder
          </p>

          <h1 className="text-3xl font-black tracking-tight text-slate-950 sm:text-4xl">Manage repositories</h1>

          <p className="mt-2 max-w-2xl text-sm font-medium leading-6 text-slate-600">
            Sync GitHub, choose your strongest projects, then save the public repository showcase.
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <button
            type="button"
            onClick={onSync}
            disabled={syncing}
            className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <RefreshCw size={16} className={syncing ? "animate-spin" : ""} />
            {syncing ? "Syncing..." : "Sync GitHub"}
          </button>

          <button
            type="button"
            onClick={onSave}
            disabled={saving}
            className="inline-flex items-center gap-2 rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <Save size={16} />
            {saving ? "Saving..." : "Save Selection"}
          </button>
        </div>
      </div>

      <div className="relative mt-6 grid grid-cols-1 gap-3 sm:grid-cols-3">
        <Metric label="Selected" value={selectedCount} />
        <Metric label="Available" value={totalCount ?? 0} />
        <Metric label="Status" value={selectedCount > 0 ? "Ready" : "Empty"} />
      </div>
    </section>
  );
}

function Metric({ label, value }) {
  return (
    <div className="rounded-3xl border border-white/80 bg-white/75 p-4 shadow-sm backdrop-blur">
      <p className="text-xs font-black uppercase tracking-[0.16em] text-slate-400">{label}</p>
      <p className="mt-2 text-2xl font-black text-slate-950">{value}</p>
    </div>
  );
}
