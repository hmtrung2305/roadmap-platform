import { CheckCircle2, Loader2 } from "lucide-react";
import { FaGithub } from "react-icons/fa";

import PortfolioEmptyState from "../portfolio/PortfolioEmptyState";
import EditPortfolioBioPreview from "./EditPortfolioBioPreview";
import EditPortfolioRepositoryCard from "./EditPortfolioRepositoryCard";
import { getRepositoryId } from "./portfolioEditUtils";

export default function EditPortfolioRepositoryManager({
  repositories,
  selectedIds,
  selectedCount,
  isGitHubLinked,
  repositoryLoading,
  syncing,
  saving,
  analyzingRepositoryId,
  error,
  success,
  username,
  portfolio,
  onSave,
  onToggleRepository,
  onGenerateInsight,
  connectionAction = "connect",
  connectingGitHub = false,
  connectDisabled = false,
  onConnectGitHub,
  managerHeight,
}) {
  const lockedHeight = managerHeight || null;
  const isReconnect = connectionAction === "reconnect";
  const connectLabel = connectingGitHub
    ? isReconnect
      ? "Reconnecting..."
      : "Connecting..."
    : isReconnect
        ? "Reconnect GitHub"
        : "Connect GitHub";

  return (
    <section
      className="flex min-h-0 flex-col overflow-hidden rounded-lg border border-[#B9D8CC] bg-white p-4 shadow-[0_18px_45px_rgba(31,111,95,0.08)] lg:self-start"
      style={lockedHeight ? { height: `${lockedHeight}px`, maxHeight: `${lockedHeight}px`, minHeight: 0 } : undefined}
    >
      <div className="flex shrink-0 flex-col gap-2 border-b border-[#DCEBE5] pb-3 xl:flex-row xl:items-center xl:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-[#2FA084]">Repository manager</p>
          <p className="mt-1.5 !text-[12px] font-semibold leading-4 text-[#667A73]">
            Toggle which repositories appear on your portfolio preview and public page.
          </p>
        </div>

        {isGitHubLinked ? (
          <button
            type="button"
            onClick={onSave}
            disabled={saving || syncing}
            className="inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-lg bg-[#2FA084] px-3 py-1.5 !text-[14px] font-bold text-white shadow-sm shadow-emerald-900/20 transition-colors hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {saving ? <Loader2 className="animate-spin" size={13} /> : <CheckCircle2 size={13} />}
            {saving ? "Saving..." : "Save selection"}
          </button>
        ) : (
          <button
            type="button"
            onClick={onConnectGitHub}
            disabled={connectDisabled}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#18332D] px-3 py-1.5 !text-[14px] font-bold text-white transition-colors hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {connectingGitHub ? <Loader2 className="animate-spin" size={16} /> : <FaGithub size={16} />}
            {connectLabel}
          </button>
        )}
      </div>

      {error && (
        <div className="mt-2 shrink-0 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-semibold text-red-700">
          {error}
        </div>
      )}

      {success && (
        <div className="mt-2 shrink-0 rounded-lg border border-[#B9D8CC] bg-[#6FCF97]/15 px-3 py-2 text-xs font-semibold text-[#1F6F5F]">
          {success}
        </div>
      )}

      <div className="shrink-0">
        <EditPortfolioBioPreview portfolio={portfolio} />
      </div>

      {!isGitHubLinked ? (
        <div className="mt-4 flex min-h-0 flex-1 flex-col items-center justify-center rounded-lg border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-6 text-center">
          <FaGithub className="mx-auto text-[#1F6F5F]" size={30} />
          <p className="mt-3 text-lg font-bold text-[#18332D]">
            {connectingGitHub
              ? isReconnect
                ? "Reconnecting GitHub..."
                : "Connecting GitHub..."
              : isReconnect
                  ? "Reconnect GitHub to manage repositories"
                  : "Connect GitHub to manage repositories"}
          </p>
          <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-[#667A73]">
            {isReconnect
              ? "Your GitHub connection needs to be refreshed before repository sync and summaries can continue."
              : "After connecting, your repositories will appear here for selection and saving."}
          </p>
        </div>
      ) : repositoryLoading ? (
        <div className="mt-4 flex min-h-0 flex-1 items-center gap-3 rounded-lg border border-[#DCEBE5] bg-[#F7F1E8]/55 p-5 text-sm font-semibold text-[#667A73]">
          <Loader2 className="animate-spin text-[#1F6F5F]" size={18} />
          Loading repositories...
        </div>
      ) : repositories.length === 0 ? (
        <div className="mt-5 min-h-0 flex-1">
          <PortfolioEmptyState />
        </div>
      ) : (
        <div className="portfolio-repo-list-scroll mt-3 min-h-0 flex-1 overflow-y-auto pr-2">
          <div className="grid gap-3 md:grid-cols-2">
            {repositories.map((repo) => {
              const repositoryId = getRepositoryId(repo);
              return (
                <EditPortfolioRepositoryCard
                  key={repositoryId}
                  repository={repo}
                  username={username}
                  isSelected={selectedIds.includes(repositoryId)}
                  isAnalyzing={analyzingRepositoryId === repositoryId}
                  onToggle={() => onToggleRepository(repositoryId)}
                  onGenerateInsight={onGenerateInsight}
                />
              );
            })}
          </div>
        </div>
      )}
    </section>
  );
}
