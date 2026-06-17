import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";

import { redirectToGitHubLink } from "../api/authProviderApi";
import { useAuthStore } from "../stores/useAuthStore";
import { useAuthProviderStore } from "../stores/useAuthProviderStore";
import { usePortfolioEditorStore } from "../stores/usePortfolioEditorStore";
import EditPortfolioErrorState from "../components/portfolioEdit/EditPortfolioErrorState";
import EditPortfolioGitHubSource from "../components/portfolioEdit/EditPortfolioGitHubSource";
import EditPortfolioHero from "../components/portfolioEdit/EditPortfolioHero";
import EditPortfolioLoadingState from "../components/portfolioEdit/EditPortfolioLoadingState";
import EditPortfolioProfileDetails from "../components/portfolioEdit/EditPortfolioProfileDetails";
import EditPortfolioRepositoryManager from "../components/portfolioEdit/EditPortfolioRepositoryManager";
import EditPortfolioStatsGrid from "../components/portfolioEdit/EditPortfolioStatsGrid";
import EditPortfolioStatusBar from "../components/portfolioEdit/EditPortfolioStatusBar";
import { getCurrentReturnUrl } from "../utils/navigationUtils";

export default function EditPortfolioPage() {
  const user = useAuthStore((state) => state.user);
  const providerActionLoading = useAuthProviderStore((state) => state.actionLoading);
  const connectingProvider = useAuthProviderStore((state) => state.connectingProvider);
  const providerError = useAuthProviderStore((state) => state.error);
  const startConnectingProvider = useAuthProviderStore((state) => state.startConnectingProvider);

  const portfolio = usePortfolioEditorStore((state) => state.portfolio);
  const portfolioLoading = usePortfolioEditorStore((state) => state.portfolioLoading);
  const portfolioError = usePortfolioEditorStore((state) => state.portfolioError);
  const repositories = usePortfolioEditorStore((state) => state.repositories);
  const selectedIds = usePortfolioEditorStore((state) => state.selectedIds);
  const isGitHubLinked = usePortfolioEditorStore((state) => state.isGitHubLinked);
  const githubConnectionAction = usePortfolioEditorStore((state) => state.githubConnectionAction);
  const repositoryLoading = usePortfolioEditorStore((state) => state.repositoryLoading);
  const syncing = usePortfolioEditorStore((state) => state.syncing);
  const reloadingSelection = usePortfolioEditorStore((state) => state.reloadingSelection);
  const saving = usePortfolioEditorStore((state) => state.saving);
  const analyzingRepositoryIds = usePortfolioEditorStore((state) => state.analyzingRepositoryIds);
  const repoError = usePortfolioEditorStore((state) => state.repoError);
  const repoSuccess = usePortfolioEditorStore((state) => state.repoSuccess);
  const initEditor = usePortfolioEditorStore((state) => state.initEditor);
  const syncRepositories = usePortfolioEditorStore((state) => state.syncRepositories);
  const reloadSavedSelection = usePortfolioEditorStore((state) => state.reloadSavedSelection);
  const toggleRepository = usePortfolioEditorStore((state) => state.toggleRepository);
  const generateInsight = usePortfolioEditorStore((state) => state.generateInsight);
  const saveSelection = usePortfolioEditorStore((state) => state.saveSelection);
  const setRepositoryError = usePortfolioEditorStore((state) => state.setRepositoryError);
  const clearRepositoryMessages = usePortfolioEditorStore((state) => state.clearRepositoryMessages);

  const [copied, setCopied] = useState(false);
  const leftColumnRef = useRef(null);
  const [managerHeight, setManagerHeight] = useState(null);

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
  const isConnectingGitHub = connectingProvider === "github";
  const isSocialProviderActionLocked = Boolean(connectingProvider);
  const disableGitHubConnect =
    isGitHubLinked ||
    providerActionLoading ||
    isSocialProviderActionLocked;
  const totalStars = repositories.reduce(
    (sum, repo) => sum + Number(repo?.stars ?? repo?.starCount ?? 0),
    0,
  );

  const publicLink = username
    ? `${window.location.origin}/portfolio/${encodeURIComponent(username)}`
    : "";

  useEffect(() => {
    initEditor().catch(() => {});
  }, [initEditor]);

  useLayoutEffect(() => {
    if (!leftColumnRef.current) return;

    function updateManagerHeight() {
      const nextHeight = leftColumnRef.current?.getBoundingClientRect().height;
      if (!nextHeight) return;
      setManagerHeight(Math.round(nextHeight));
    }

    updateManagerHeight();

    const observer = new ResizeObserver(updateManagerHeight);
    observer.observe(leftColumnRef.current);

    window.addEventListener("resize", updateManagerHeight);

    return () => {
      observer.disconnect();
      window.removeEventListener("resize", updateManagerHeight);
    };
  }, [portfolio, isGitHubLinked, repositories.length, syncing, reloadingSelection, saving]);

  const handleSync = () => {
    if (repositoryLoading || syncing || reloadingSelection || saving) return;

    syncRepositories().catch(() => {});
  };

  const handleReloadSavedSelection = () => {
    if (repositoryLoading || syncing || reloadingSelection || saving) return;

    reloadSavedSelection().catch(() => {});
  };

  const handleToggleRepository = (repositoryId) => {
    if (repositoryLoading || syncing || reloadingSelection || saving) return;

    toggleRepository(repositoryId);
  };

  const handleGenerateInsight = (repositoryId, force = false) => {
    if (repositoryLoading || syncing || reloadingSelection || saving) return;

    generateInsight(repositoryId, { force }).catch(() => {});
  };

  const handleGitHubRedirect = () => {
    if (disableGitHubConnect) return;

    clearRepositoryMessages();

    const startedProvider = startConnectingProvider("github");

    if (!startedProvider) {
      setRepositoryError(
        providerError ||
          "Another account connection is already in progress. Please try again shortly.",
      );
      return;
    }

    redirectToGitHubLink({ returnUrl: getCurrentReturnUrl() });
  };

  const handleCopyPublicLink = async () => {
    if (!publicLink) return;

    try {
      await navigator.clipboard.writeText(publicLink);
      setCopied(true);
      setTimeout(() => setCopied(false), 1800);
    } catch (error) {
      console.error("Copy public portfolio link failed:", error);
      setRepositoryError("Could not copy the public portfolio link.");
    }
  };

  const handleSave = () => {
    saveSelection().catch(() => {});
  };

  if (portfolioLoading) {
    return <EditPortfolioLoadingState />;
  }

  if (portfolioError) {
    return <EditPortfolioErrorState message={portfolioError} />;
  }

  return (
    <main className="tm-page min-h-[calc(100vh-4rem)] px-6 py-12 text-[#18332D]">
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

        .portfolio-repo-list-scroll {
          scrollbar-width: thin;
          scrollbar-color: #8BA39B transparent;
        }

        .portfolio-repo-list-scroll::-webkit-scrollbar {
          width: 6px;
        }

        .portfolio-repo-list-scroll::-webkit-scrollbar-track {
          background: transparent;
        }

        .portfolio-repo-list-scroll::-webkit-scrollbar-thumb {
          background: #8BA39B;
          border-radius: 999px;
        }
      `}</style>

      <div className="tm-soft-enter mx-auto max-w-7xl space-y-5">
        <EditPortfolioHero
          portfolio={portfolio}
          displayName={displayName}
          headline={headline}
          copied={copied}
          onCopyPublicLink={handleCopyPublicLink}
        />

        <EditPortfolioStatusBar selectedCount={selectedCount} availableCount={availableCount} />

        <EditPortfolioStatsGrid
          username={username}
          isGitHubLinked={isGitHubLinked}
          selectedCount={selectedCount}
          totalStars={totalStars}
        />

        <section className="tm-animate-item grid gap-5 lg:grid-cols-[320px_minmax(0,1fr)] lg:items-start">
          <aside ref={leftColumnRef} className="flex flex-col gap-5 pr-1">
            <EditPortfolioProfileDetails portfolio={portfolio} displayName={displayName} headline={headline} />
            <EditPortfolioGitHubSource
              isGitHubLinked={isGitHubLinked}
              syncing={syncing}
              reloadingSelection={reloadingSelection}
              saving={saving}
              repositoryLoading={repositoryLoading}
              onSync={handleSync}
              onReloadSelection={handleReloadSavedSelection}
              connectionAction={githubConnectionAction === "reconnect" ? "reconnect" : "connect"}
              connectingGitHub={isConnectingGitHub}
              connectDisabled={disableGitHubConnect}
              onConnectGitHub={handleGitHubRedirect}
            />
          </aside>

          <EditPortfolioRepositoryManager
            repositories={repositories}
            selectedIds={selectedIds}
            selectedCount={selectedCount}
            isGitHubLinked={isGitHubLinked}
            repositoryLoading={repositoryLoading}
            syncing={syncing}
            reloadingSelection={reloadingSelection}
            saving={saving}
            analyzingRepositoryIds={analyzingRepositoryIds}
            error={repoError}
            success={repoSuccess}
            username={username}
            portfolio={portfolio}
            onSave={handleSave}
            onToggleRepository={handleToggleRepository}
            onGenerateInsight={handleGenerateInsight}
            connectionAction={githubConnectionAction === "reconnect" ? "reconnect" : "connect"}
            connectingGitHub={isConnectingGitHub}
            connectDisabled={disableGitHubConnect}
            onConnectGitHub={handleGitHubRedirect}
            managerHeight={managerHeight}
          />
        </section>
      </div>
    </main>
  );
}
