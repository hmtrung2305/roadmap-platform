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
  onConnectGitHub,
}) {
  return (
    <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-[0_18px_45px_rgba(31,111,95,0.08)]">
      <div className="flex flex-col gap-4 border-b border-[#DCEBE5] pb-5 xl:flex-row xl:items-center xl:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-[#2FA084]">Repository manager</p>
          <p className="mt-2 text-sm font-semibold leading-6 text-[#667A73]">
            Toggle which repositories appear on your portfolio preview and public page.
          </p>
        </div>

        {isGitHubLinked ? (
          <button type="button" onClick={onSave} disabled={saving || syncing} className="inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-lg bg-[#2FA084] px-4 py-2.5 text-sm font-bold text-white shadow-sm shadow-emerald-900/20 transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60">
            {saving ? <Loader2 className="animate-spin" size={16} /> : <CheckCircle2 size={16} />}
            {saving ? "Saving..." : "Save selection"}
          </button>
        ) : (
          <button type="button" onClick={onConnectGitHub} className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#18332D] px-4 py-2.5 text-sm font-bold text-white transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]">
            <FaGithub size={16} />
            Connect GitHub
          </button>
        )}
      </div>

      {error && (
        <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
          {error}
        </div>
      )}

      {success && (
        <div className="mt-4 rounded-lg border border-[#B9D8CC] bg-[#6FCF97]/15 px-4 py-3 text-sm font-semibold text-[#1F6F5F]">
          {success}
        </div>
      )}

      <EditPortfolioBioPreview portfolio={portfolio} />

      {!isGitHubLinked ? (
        <div className="mt-5 rounded-lg border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-6 text-center">
          <FaGithub className="mx-auto text-[#1F6F5F]" size={30} />
          <p className="mt-3 text-lg font-bold text-[#18332D]">Connect GitHub to manage repositories</p>
          <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-[#667A73]">
            After connecting, your repositories will appear here for selection and saving.
          </p>
        </div>
      ) : repositoryLoading ? (
        <div className="mt-5 flex items-center gap-3 rounded-lg border border-[#DCEBE5] bg-[#F7F1E8]/55 p-5 text-sm font-semibold text-[#667A73]">
          <Loader2 className="animate-spin text-[#1F6F5F]" size={18} />
          Loading repositories...
        </div>
      ) : repositories.length === 0 ? (
        <div className="mt-5">
          <PortfolioEmptyState />
        </div>
      ) : (
        <div className="mt-5 grid gap-4 md:grid-cols-2">
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
      )}
    </section>
  );
}
