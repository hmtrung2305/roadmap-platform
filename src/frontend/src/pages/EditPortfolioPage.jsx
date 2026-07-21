import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "react-toastify";

import { redirectToGitHubLink } from "../api/authProviderApi";
import { useAuthStore } from "../stores/useAuthStore";
import { useAuthProviderStore } from "../stores/useAuthProviderStore";
import { usePortfolioEditorStore } from "../stores/usePortfolioEditorStore";
import { useProfileStore } from "../stores/useProfileStore";
import EditPortfolioErrorState from "../features/portfolio/components/editor/EditPortfolioErrorState";
import EditPortfolioBioPreview from "../features/portfolio/components/editor/EditPortfolioBioPreview";
import EditPortfolioHero from "../features/portfolio/components/editor/EditPortfolioHero";
import EditPortfolioLoadingState from "../features/portfolio/components/editor/EditPortfolioLoadingState";
import EditPortfolioProfileDetails from "../features/portfolio/components/editor/EditPortfolioProfileDetails";
import EditPortfolioRepositoryManager from "../features/portfolio/components/editor/EditPortfolioRepositoryManager";
import RepoInsightConfirmationModal from "../features/portfolio/components/editor/RepoInsightConfirmationModal";
import EditPortfolioStatsGrid from "../features/portfolio/components/editor/EditPortfolioStatsGrid";
import { useAiCreditStore } from "../stores/useAiCreditStore";
import { getCurrentReturnUrl } from "../utils/navigationUtils";

export default function EditPortfolioPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const providerActionLoading = useAuthProviderStore(
    (state) => state.actionLoading,
  );
  const connectingProvider = useAuthProviderStore(
    (state) => state.connectingProvider,
  );
  const providerError = useAuthProviderStore((state) => state.error);
  const startConnectingProvider = useAuthProviderStore(
    (state) => state.startConnectingProvider,
  );
  const profile = useProfileStore((state) => state.profile);
  const loadProfile = useProfileStore((state) => state.loadProfile);

  const portfolio = usePortfolioEditorStore((state) => state.portfolio);
  const portfolioLoading = usePortfolioEditorStore(
    (state) => state.portfolioLoading,
  );
  const portfolioError = usePortfolioEditorStore(
    (state) => state.portfolioError,
  );
  const repositories = usePortfolioEditorStore((state) => state.repositories);
  const selectedIds = usePortfolioEditorStore((state) => state.selectedIds);
  const isGitHubLinked = usePortfolioEditorStore(
    (state) => state.isGitHubLinked,
  );
  const githubConnectionAction = usePortfolioEditorStore(
    (state) => state.githubConnectionAction,
  );
  const repositoryLoading = usePortfolioEditorStore(
    (state) => state.repositoryLoading,
  );
  const syncing = usePortfolioEditorStore((state) => state.syncing);
  const reloadingSelection = usePortfolioEditorStore(
    (state) => state.reloadingSelection,
  );
  const saving = usePortfolioEditorStore((state) => state.saving);
  const analyzingRepositoryIds = usePortfolioEditorStore(
    (state) => state.analyzingRepositoryIds,
  );
  const repoError = usePortfolioEditorStore((state) => state.repoError);
  const repoSuccess = usePortfolioEditorStore((state) => state.repoSuccess);
  const initEditor = usePortfolioEditorStore((state) => state.initEditor);
  const syncRepositories = usePortfolioEditorStore(
    (state) => state.syncRepositories,
  );
  const reloadSavedSelection = usePortfolioEditorStore(
    (state) => state.reloadSavedSelection,
  );
  const toggleRepository = usePortfolioEditorStore(
    (state) => state.toggleRepository,
  );
  const generateInsight = usePortfolioEditorStore(
    (state) => state.generateInsight,
  );
  const saveSelection = usePortfolioEditorStore((state) => state.saveSelection);
  const clearRepositoryMessages = usePortfolioEditorStore(
    (state) => state.clearRepositoryMessages,
  );
  const creditStatus = useAiCreditStore((state) => state.creditStatus);
  const isLoadingCreditStatus = useAiCreditStore(
    (state) => state.isLoadingCreditStatus,
  );
  const loadCreditStatus = useAiCreditStore((state) => state.loadCreditStatus);

  const [copied, setCopied] = useState(false);
  const [pendingInsightRequest, setPendingInsightRequest] = useState(null);
  const lastRepoErrorToastRef = useRef("");
  const lastRepoSuccessToastRef = useRef("");

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

  const displayName =
    portfolio?.displayName ||
    portfolio?.fullName ||
    username ||
    "Student Portfolio";
  const headline =
    portfolio?.headline ||
    [portfolio?.currentRole, portfolio?.careerGoal]
      .filter(Boolean)
      .join(" | ") ||
    "Developer Portfolio";

  const selectedCount = selectedIds.length;
  const availableCount = repositories.length;
  const isPortfolioPublic = Boolean(
    profile?.isPublic ??
    profile?.isPublicPortfolio ??
    profile?.publicProfile ??
    portfolio?.isPublic ??
    portfolio?.isPublicPortfolio ??
    portfolio?.publicProfile,
  );
  const isConnectingGitHub = connectingProvider === "github";
  const isSocialProviderActionLocked = Boolean(connectingProvider);
  const disableGitHubConnect =
    isGitHubLinked || providerActionLoading || isSocialProviderActionLocked;
  const publicLink = username
    ? `${window.location.origin}/portfolio/${encodeURIComponent(username)}`
    : "";

  useEffect(() => {
    initEditor().catch(() => {});
  }, [initEditor]);

  useEffect(() => {
    loadProfile().catch(() => {});
  }, [loadProfile]);

  useEffect(() => {
    loadCreditStatus().catch(() => {});
  }, [loadCreditStatus]);

  useEffect(() => {
    if (!repoError) {
      lastRepoErrorToastRef.current = "";
      return;
    }

    if (lastRepoErrorToastRef.current === repoError) return;

    toast.error(repoError, { toastId: `portfolio-repo-error-${repoError}` });
    lastRepoErrorToastRef.current = repoError;
  }, [repoError]);

  useEffect(() => {
    if (!repoSuccess) {
      lastRepoSuccessToastRef.current = "";
      return;
    }

    if (lastRepoSuccessToastRef.current === repoSuccess) return;

    toast.success(repoSuccess, {
      toastId: `portfolio-repo-success-${repoSuccess}`,
    });
    lastRepoSuccessToastRef.current = repoSuccess;
  }, [repoSuccess]);

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

  const handleGenerateInsight = (
    repositoryId,
    force = false,
    repositoryName = "",
  ) => {
    if (repositoryLoading || syncing || reloadingSelection || saving) return;
    if (creditStatus?.remainingCreditsToday <= 0) {
      toast.error(
        "You have no AI credits left today. Try again after the daily reset.",
      );
      return;
    }

    setPendingInsightRequest({
      repositoryId,
      force,
      repositoryName,
    });
  };

  const handleConfirmGenerateInsight = () => {
    const request = pendingInsightRequest;
    if (!request) return;

    setPendingInsightRequest(null);

    if (creditStatus?.remainingCreditsToday <= 0) {
      toast.error(
        "You have no AI credits left today. Try again after the daily reset.",
      );
      return;
    }

    generateInsight(request.repositoryId, { force: request.force })
      .then(() => {
        loadCreditStatus({ force: true }).catch(() => {});
      })
      .catch(() => {});
  };

  const handleGitHubRedirect = () => {
    if (disableGitHubConnect) return;

    clearRepositoryMessages();

    const startedProvider = startConnectingProvider("github");

    if (!startedProvider) {
      toast.error(
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
      toast.success("Public portfolio link copied.");
      setTimeout(() => setCopied(false), 1800);
    } catch (error) {
      console.error("Copy public portfolio link failed:", error);
      toast.error("Could not copy the public portfolio link.");
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
          scrollbar-width: none;
          -ms-overflow-style: none;
        }

        .portfolio-repo-list-scroll::-webkit-scrollbar {
          display: none;
          width: 0;
          height: 0;
        }
      `}</style>

      <div className="tm-soft-enter mx-auto max-w-7xl space-y-4">
        <EditPortfolioHero
          portfolio={portfolio}
          displayName={displayName}
          headline={headline}
          copied={copied}
          onCopyPublicLink={handleCopyPublicLink}
        />

        <EditPortfolioBioPreview portfolio={portfolio} />

        <EditPortfolioStatsGrid
          isGitHubLinked={isGitHubLinked}
          isPortfolioPublic={isPortfolioPublic}
          selectedCount={selectedCount}
          availableCount={availableCount}
          onManageVisibility={() => navigate("/settings/privacy")}
          syncing={syncing}
          reloadingSelection={reloadingSelection}
          saving={saving}
          repositoryLoading={repositoryLoading}
          onSync={handleSync}
          onReloadSelection={handleReloadSavedSelection}
          connectionAction={
            githubConnectionAction === "reconnect" ? "reconnect" : "connect"
          }
          connectingGitHub={isConnectingGitHub}
          connectDisabled={disableGitHubConnect}
          onConnectGitHub={handleGitHubRedirect}
        />

        <section className="tm-animate-item grid gap-4 lg:grid-cols-[320px_minmax(0,1fr)] lg:items-stretch">
          <aside className="flex flex-col gap-4 pr-1">
            <EditPortfolioProfileDetails
              portfolio={portfolio}
              displayName={displayName}
              headline={headline}
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
            username={username}
            onSave={handleSave}
            onToggleRepository={handleToggleRepository}
            onGenerateInsight={handleGenerateInsight}
            creditStatus={creditStatus}
            isLoadingCreditStatus={isLoadingCreditStatus}
          />
        </section>
      </div>

      <RepoInsightConfirmationModal
        open={Boolean(pendingInsightRequest)}
        repositoryName={pendingInsightRequest?.repositoryName}
        force={Boolean(pendingInsightRequest?.force)}
        onCancel={() => setPendingInsightRequest(null)}
        onConfirm={handleConfirmGenerateInsight}
      />
    </main>
  );
}
