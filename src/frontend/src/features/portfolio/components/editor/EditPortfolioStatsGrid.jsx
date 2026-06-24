import {
  Eye,
  EyeOff,
  FolderKanban,
  Loader2,
  RefreshCcw,
  ShieldCheck,
} from "lucide-react";
import { FaGithub } from "react-icons/fa";

import EditPortfolioInfoTile from "./EditPortfolioInfoTile";

export default function EditPortfolioStatsGrid({
  isGitHubLinked,
  isPortfolioPublic,
  selectedCount,
  availableCount = 0,
  onManageVisibility,
  syncing,
  reloadingSelection,
  saving,
  repositoryLoading,
  onSync,
  onReloadSelection,
  connectionAction = "connect",
  connectingGitHub = false,
  connectDisabled = false,
  onConnectGitHub,
}) {
  const actionLocked = Boolean(syncing || reloadingSelection || saving || repositoryLoading);
  const isReconnect = connectionAction === "reconnect";
  const connectLabel = connectingGitHub
    ? isReconnect
      ? "Reconnecting..."
      : "Connecting..."
    : isReconnect
      ? "Reconnect GitHub"
      : "Connect GitHub";
  const githubValue = isGitHubLinked
    ? "Connected"
    : isReconnect
      ? "Reconnect needed"
      : "Not connected";

  return (
    <section className="grid grid-cols-1 gap-3 md:grid-cols-3">
      <EditPortfolioInfoTile
        icon={isPortfolioPublic ? <Eye size={16} /> : <EyeOff size={16} />}
        label="Visibility"
        value={isPortfolioPublic ? "Public" : "Private"}
        helper={
          isPortfolioPublic
            ? "Public visitors can view your portfolio."
            : "Public visitors cannot view your portfolio yet."
        }
        actionLabel="Manage visibility"
        onClick={onManageVisibility}
      />

      <article className="group rounded-2xl border border-[#B9D8CC]/75 bg-white p-3.5 text-left shadow-sm transition duration-200 hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="text-[11px] font-bold uppercase tracking-[0.14em] text-[#6F837C]">
              GitHub source
            </p>
            <p className="portfolio-editor-nowrap mt-1.5 text-lg font-bold text-[#18332D]">
              {githubValue}
            </p>
          </div>
          <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-[#18332D] text-white ring-1 ring-[#D8EAE2]">
            <FaGithub size={16} />
          </span>
        </div>

        {isGitHubLinked ? (
          <div className="mt-3 grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={onSync}
              disabled={actionLocked}
              className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-2.5 py-1.5 text-[11px] font-extrabold text-white transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {syncing ? <Loader2 className="animate-spin" size={12} /> : <RefreshCcw size={12} />}
              {syncing ? "Syncing..." : "Sync Repos"}
            </button>
            <button
              type="button"
              onClick={onReloadSelection}
              disabled={actionLocked}
              className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-[#B9D8CC]/75 bg-white px-2.5 py-1.5 text-[11px] font-extrabold text-[#1F6F5F] transition hover:bg-[#6FCF97]/15 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {reloadingSelection ? (
                <Loader2 className="animate-spin" size={12} />
              ) : (
                <ShieldCheck size={12} />
              )}
              {reloadingSelection ? "Resetting..." : "Restored Saved"}
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={onConnectGitHub}
            disabled={connectDisabled}
            className="mt-3 inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#18332D] px-2.5 py-1.5 text-[11px] font-extrabold text-white transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {connectingGitHub ? <Loader2 className="animate-spin" size={12} /> : <FaGithub size={12} />}
            {connectLabel}
          </button>
        )}
      </article>

      <EditPortfolioInfoTile
        icon={<FolderKanban size={16} />}
        label="Selected"
        value={`${selectedCount} projects`}
        helper={`${selectedCount} of ${availableCount} repositories selected for your learning portfolio.`}
      />
    </section>
  );
}
