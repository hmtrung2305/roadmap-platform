import { Loader2, RefreshCcw, ShieldCheck } from "lucide-react";
import { FaGithub } from "react-icons/fa";

export default function EditPortfolioGitHubSource({
  isGitHubLinked,
  syncing,
  reloadingSelection,
  saving,
  onSync,
  onReloadSelection,
  onConnectGitHub,
}) {
  return (
    <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-[0_18px_45px_rgba(31,111,95,0.08)]">
      <div className="flex items-start gap-3">
        <span className="grid size-11 place-items-center rounded-lg bg-[#18332D] text-white">
          <FaGithub size={20} />
        </span>
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-[#2FA084]">GitHub source</p>
          <h2 className="mt-1 text-xl font-bold text-[#18332D]">
            {isGitHubLinked ? "Connected" : "Not connected"}
          </h2>
        </div>
      </div>

      <p className="mt-4 text-sm leading-6 text-[#667A73]">
        Sync repositories, or reload your saved selection.
      </p>

      {isGitHubLinked ? (
        <div className="mt-5 grid gap-2">
          <button
            type="button"
            onClick={onSync}
            disabled={syncing || reloadingSelection || saving}
            className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {syncing ? <Loader2 className="animate-spin" size={16} /> : <RefreshCcw size={16} />}
            {syncing ? "Syncing..." : "Sync repositories"}
          </button>

          <button
            type="button"
            onClick={onReloadSelection}
            disabled={syncing || reloadingSelection || saving}
            className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-bold text-[#1F6F5F] shadow-sm transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/20 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {reloadingSelection ? <Loader2 className="animate-spin" size={16} /> : <ShieldCheck size={16} />}
            {reloadingSelection ? "Reloading..." : "Reload saved selection"}
          </button>
        </div>
      ) : (
        <button type="button" onClick={onConnectGitHub} className="mt-5 inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]">
          <FaGithub size={16} />
          Connect GitHub
        </button>
      )}
    </section>
  );
}
