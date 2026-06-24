import { CheckCircle2, Loader2 } from "lucide-react";
import { FaGithub } from "react-icons/fa";

import PortfolioEmptyState from "../shared/PortfolioEmptyState";
import AiCreditStatus from "./AiCreditStatus";
import EditPortfolioRepositoryCard from "./EditPortfolioRepositoryCard";
import { getRepositoryId } from "../../utils/portfolioEditUtils";

export default function EditPortfolioRepositoryManager({
  repositories,
  selectedIds,
  selectedCount = selectedIds?.length ?? 0,
  isGitHubLinked,
  repositoryLoading,
  syncing,
  reloadingSelection,
  saving,
  analyzingRepositoryIds = {},
  username,
  onSave,
  onToggleRepository,
  onGenerateInsight,
  creditStatus,
  isLoadingCreditStatus = false,
}) {
  const repositoryActionLocked = Boolean(
    repositoryLoading || syncing || reloadingSelection || saving,
  );

  return (
    <section className="flex h-full min-h-[620px] flex-col overflow-hidden rounded-lg border border-[#B9D8CC] bg-white p-4 shadow-[0_18px_45px_rgba(31,111,95,0.08)] lg:h-[640px] lg:min-h-[640px]">
      <div className="flex shrink-0 flex-col gap-2 border-b border-[#DCEBE5] pb-3 xl:flex-row xl:items-center xl:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-[#2FA084]">
            Repository manager
          </p>
          <p className="mt-1.5 !text-[12px] font-semibold leading-4 text-[#667A73]">
            Toggle which repositories appear on your portfolio preview and
            public page. {selectedCount} projects selected.
          </p>
        </div>

        {isGitHubLinked && (
          <button
            type="button"
            onClick={onSave}
            disabled={repositoryActionLocked}
            className="inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-lg bg-[#2FA084] px-3 py-1.5 !text-[14px] font-bold text-white shadow-sm shadow-emerald-900/20 transition-colors hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {saving ? (
              <Loader2 className="animate-spin" size={13} />
            ) : (
              <CheckCircle2 size={13} />
            )}
            {saving ? "Saving..." : "Save selection"}
          </button>
        )}
      </div>

      {isGitHubLinked && (
        <AiCreditStatus status={creditStatus} isLoading={isLoadingCreditStatus} />
      )}

      {!isGitHubLinked ? (
        <div className="mt-4 flex min-h-0 flex-1 flex-col items-center justify-center rounded-lg border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-6 text-center">
          <FaGithub className="mx-auto text-[#1F6F5F]" size={30} />
          <p className="mt-3 text-lg font-bold text-[#18332D]">
            Connect GitHub to manage repositories
          </p>
          <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-[#667A73]">
            Use the GitHub source card above to connect or reconnect your account.
            Your repositories will appear here after syncing.
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
                  isAnalyzing={Boolean(analyzingRepositoryIds?.[repositoryId])}
                  actionDisabled={repositoryActionLocked}
                  aiCreditDisabled={creditStatus?.remainingCreditsToday <= 0}
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
