import { Loader2, RefreshCcw, ShieldCheck } from "lucide-react";
import { FaGithub } from "react-icons/fa";

export default function EditPortfolioGitHubSource({
  isGitHubLinked,
  syncing,
  reloadingSelection,
  saving,
  onSync,
  onReloadSelection,
  connectionAction = "connect",
  connectingGitHub = false,
  connectDisabled = false,
  onConnectGitHub,
}) {
  const isReconnect = connectionAction === "reconnect";
  const connectLabel = connectingGitHub
    ? isReconnect
      ? "Reconnecting..."
      : "Connecting..."
    : isReconnect
        ? "Reconnect GitHub"
        : "Connect GitHub";
  return (
    <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-[0_18px_45px_rgba(31,111,95,0.08)]">
      <div className="flex items-start gap-3">
        <span className="grid size-11 place-items-center rounded-lg bg-[#18332D] text-white">
          <FaGithub size={20} />
        </span>
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-[#2FA084]">GitHub source</p>
          <h2 className="mt-1 text-xl font-bold text-[#18332D]">
            {isGitHubLinked ? "Connected" : isReconnect ? "Reconnect needed" : "Not connected"}
          </h2>
        </div>
      </div>

      <p className="mt-4 text-sm leading-6 text-[#667A73]">
        {isGitHubLinked
          ? "Sync repositories, or reload your saved selection."
          : isReconnect
            ? "Refresh your GitHub connection to continue using repository sync and AI summaries."
            : "Connect GitHub to sync repositories and choose portfolio projects."}
      </p>

      {isGitHubLinked ? (
        <div className="mt-5 grid gap-2">
          <button
            type="button"
            onClick={onSync}
            disabled={syncing || reloadingSelection || saving}
            className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-3 py-2 !text-[14px] font-bold text-white shadow-sm transition-colors hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {syncing ? <Loader2 className="animate-spin" size={14} /> : <RefreshCcw size={14} />}
            {syncing ? "Syncing..." : "Sync repositories"}
          </button>

          <button
            type="button"
            onClick={onReloadSelection}
            disabled={syncing || reloadingSelection || saving}
            className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 !text-[14px] font-bold text-[#1F6F5F] shadow-sm transition-colors hover:bg-[#6FCF97]/20 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {reloadingSelection ? <Loader2 className="animate-spin" size={14} /> : <ShieldCheck size={14} />}
            {reloadingSelection ? "Reloading..." : "Reload saved selection"}
          </button>
        </div>
      ) : (
        <button
          type="button"
          onClick={onConnectGitHub}
          disabled={connectDisabled}
          className="mt-5 inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-3 py-2 !text-[14px] font-bold text-white shadow-sm transition-colors hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
        >
          {connectingGitHub ? <Loader2 className="animate-spin" size={14} /> : <FaGithub size={14} />}
          {connectLabel}
        </button>
      )}
    </section>
  );
}
