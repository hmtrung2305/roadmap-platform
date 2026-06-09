import { useEffect, useMemo, useState } from "react";

import {
  getMyPortfolioApi,
  updatePortfolioRepositoriesApi,
} from "../api/portfolioApi";
import {
  generateRepositoryInsightApi,
  getSavedGitHubRepositoriesApi,
  syncGitHubRepositoriesApi,
} from "../api/githubRepositoryApi";
import {
  getAuthProvidersApi,
  redirectToGitHubLink,
} from "../api/authProviderApi";
import { useAuthStore } from "../stores/useAuthStore";
import EditPortfolioErrorState from "../components/portfolioEdit/EditPortfolioErrorState";
import EditPortfolioGitHubSource from "../components/portfolioEdit/EditPortfolioGitHubSource";
import EditPortfolioHero from "../components/portfolioEdit/EditPortfolioHero";
import EditPortfolioLoadingState from "../components/portfolioEdit/EditPortfolioLoadingState";
import EditPortfolioProfileDetails from "../components/portfolioEdit/EditPortfolioProfileDetails";
import EditPortfolioRepositoryManager from "../components/portfolioEdit/EditPortfolioRepositoryManager";
import EditPortfolioStatsGrid from "../components/portfolioEdit/EditPortfolioStatsGrid";
import EditPortfolioStatusBar from "../components/portfolioEdit/EditPortfolioStatusBar";
import { getInitiallySelectedIds } from "../components/portfolioEdit/portfolioEditUtils";

export default function EditPortfolioPage() {
  const user = useAuthStore((state) => state.user);

  const [portfolio, setPortfolio] = useState(null);
  const [portfolioLoading, setPortfolioLoading] = useState(true);
  const [portfolioError, setPortfolioError] = useState("");

  const [repositories, setRepositories] = useState([]);
  const [selectedIds, setSelectedIds] = useState([]);
  const [isGitHubLinked, setIsGitHubLinked] = useState(false);
  const [repositoryLoading, setRepositoryLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [reloadingSelection, setReloadingSelection] = useState(false);
  const [saving, setSaving] = useState(false);
  const [analyzingRepositoryId, setAnalyzingRepositoryId] = useState(null);
  const [repoError, setRepoError] = useState("");
  const [repoSuccess, setRepoSuccess] = useState("");

  const username = useMemo(() => {
    return (
      portfolio?.username ||
      user?.username ||
      user?.userName ||
      user?.Username ||
      user?.email?.split("@")[0] ||
      "student"
    );
  }, [portfolio, user]);

  const displayName = portfolio?.displayName || portfolio?.fullName || username || "Student Portfolio";
  const headline =
    portfolio?.headline ||
    [portfolio?.currentRole, portfolio?.careerGoal].filter(Boolean).join(" | ") ||
    "Developer Portfolio";

  const selectedCount = selectedIds.length;
  const availableCount = repositories.length;
  const totalStars = repositories.reduce(
    (sum, repo) => sum + Number(repo?.stars ?? repo?.starCount ?? 0),
    0,
  );

  useEffect(() => {
    initEditor();
  }, []);

  async function initEditor() {
    try {
      setPortfolioLoading(true);
      setRepositoryLoading(true);
      setPortfolioError("");
      setRepoError("");

      const [portfolioData, providers] = await Promise.all([
        getMyPortfolioApi(),
        getAuthProvidersApi(),
      ]);

      setPortfolio(portfolioData);

      const githubProvider = providers.find((provider) => provider.provider === "github");
      const linked = githubProvider?.isLinked ?? false;
      setIsGitHubLinked(linked);

      if (linked) {
        await fetchRepositories();
      }
    } catch (error) {
      console.error("Load portfolio editor failed:", error);
      setPortfolioError(error?.message || "Could not load your portfolio editor.");
    } finally {
      setPortfolioLoading(false);
      setRepositoryLoading(false);
    }
  }

  async function fetchRepositories() {
    const data = await getSavedGitHubRepositoriesApi();
    setRepositories(data || []);
    setSelectedIds(getInitiallySelectedIds(data || []));
  }

  const handleSync = async () => {
    try {
      setSyncing(true);
      setRepoError("");
      setRepoSuccess("");

      const data = await syncGitHubRepositoriesApi();
      setRepositories(data || []);
      setSelectedIds(getInitiallySelectedIds(data || []));
      setRepoSuccess("Repositories synced. Choose the projects you want to show, then save.");
    } catch (error) {
      console.error("Failed to sync repositories:", error);
      setRepoError(error?.message || "Cannot sync repositories. Please connect GitHub first.");
    } finally {
      setSyncing(false);
    }
  };

  const handleReloadSavedSelection = async () => {
    try {
      setReloadingSelection(true);
      setRepoError("");
      setRepoSuccess("");

      await fetchRepositories();
      setRepoSuccess("Saved repository selection reloaded.");
    } catch (error) {
      console.error("Failed to reload saved selection:", error);
      setRepoError(error?.message || "Cannot reload saved repository selection.");
    } finally {
      setReloadingSelection(false);
    }
  };

  const handleToggleRepository = (repositoryId) => {
    setRepoSuccess("");
    setSelectedIds((current) => {
      if (current.includes(repositoryId)) {
        return current.filter((id) => id !== repositoryId);
      }
      return [...current, repositoryId];
    });
  };


  const handleGenerateInsight = async (repositoryId, force = false) => {
    try {
      setAnalyzingRepositoryId(repositoryId);
      setRepoError("");
      setRepoSuccess("");

      const insight = await generateRepositoryInsightApi(repositoryId, { force });

      setRepositories((current) =>
        current.map((repo) =>
          repo.repositoryId === repositoryId
            ? { ...repo, insight }
            : repo
        )
      );

      setRepoSuccess(force ? "Repository AI summary regenerated." : "Repository AI summary generated.");
    } catch (error) {
      console.error("Failed to generate repository insight:", error);
      setRepoError(error?.message || "Cannot generate repository AI summary.");
    } finally {
      setAnalyzingRepositoryId(null);
    }
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setRepoError("");
      setRepoSuccess("");

      await updatePortfolioRepositoriesApi(selectedIds);
      setRepoSuccess("Portfolio projects saved successfully.");
      await fetchRepositories();
    } catch (error) {
      console.error("Failed to save selected repositories:", error);
      setRepoError(error?.message || "Cannot save selected repositories.");
    } finally {
      setSaving(false);
    }
  };

  if (portfolioLoading) {
    return <EditPortfolioLoadingState />;
  }

  if (portfolioError) {
    return <EditPortfolioErrorState message={portfolioError} />;
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#F7F1E8]/70 px-4 py-7 text-[#18332D] sm:px-6">
      <style>{`
        .portfolio-editor-nowrap {
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }

        .portfolio-editor-clamp-2 {
          display: -webkit-box;
          -webkit-line-clamp: 2;
          -webkit-box-orient: vertical;
          overflow: hidden;
        }
      `}</style>

      <div className="mx-auto max-w-6xl space-y-5">
        <EditPortfolioHero portfolio={portfolio} displayName={displayName} headline={headline} />

        <EditPortfolioStatusBar selectedCount={selectedCount} availableCount={availableCount} />

        <EditPortfolioStatsGrid
          username={username}
          isGitHubLinked={isGitHubLinked}
          selectedCount={selectedCount}
          totalStars={totalStars}
        />

        <section className="grid gap-5 lg:grid-cols-[320px_1fr]">
          <aside className="space-y-5">
            <EditPortfolioProfileDetails portfolio={portfolio} displayName={displayName} headline={headline} />
            <EditPortfolioGitHubSource
              isGitHubLinked={isGitHubLinked}
              syncing={syncing}
              reloadingSelection={reloadingSelection}
              saving={saving}
              onSync={handleSync}
              onReloadSelection={handleReloadSavedSelection}
              onConnectGitHub={redirectToGitHubLink}
            />
          </aside>

          <EditPortfolioRepositoryManager
            repositories={repositories}
            selectedIds={selectedIds}
            selectedCount={selectedCount}
            isGitHubLinked={isGitHubLinked}
            repositoryLoading={repositoryLoading}
            syncing={syncing}
            saving={saving}
            analyzingRepositoryId={analyzingRepositoryId}
            error={repoError}
            success={repoSuccess}
            username={username}
            portfolio={portfolio}
            onSave={handleSave}
            onToggleRepository={handleToggleRepository}
            onGenerateInsight={handleGenerateInsight}
            onConnectGitHub={redirectToGitHubLink}
          />
        </section>
      </div>
    </main>
  );
}
