import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

import {
  getSavedGitHubRepositoriesApi,
  syncGitHubRepositoriesApi,
} from "../api/githubRepositoryApi";

import { updatePortfolioRepositoriesApi } from "../api/portfolioApi";
import {
  getAuthProvidersApi,
  redirectToGitHubLink,
} from "../api/authProviderApi";

import RepositoryPageHeader from "../components/portfolioRepositories/RepositoryPageHeader";
import GitHubNotLinkedState from "../components/portfolioRepositories/GitHubNotLinkedState";
import RepositoryStatusMessage from "../components/portfolioRepositories/RepositoryStatusMessage";
import RepositoryEmptyState from "../components/portfolioRepositories/RepositoryEmptyState";
import RepositorySelectList from "../components/portfolioRepositories/RepositorySelectList";

export default function ManagePortfolioRepositoriesPage() {
  const navigate = useNavigate();

  const [repositories, setRepositories] = useState([]);
  const [selectedIds, setSelectedIds] = useState([]);
  const [isGitHubLinked, setIsGitHubLinked] = useState(false);

  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [saving, setSaving] = useState(false);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const selectedCount = useMemo(() => selectedIds.length, [selectedIds]);

  useEffect(() => {
    initPage();
  }, []);

  async function initPage() {
    try {
      setLoading(true);
      setError("");

      const providers = await getAuthProvidersApi();
      const githubProvider = providers.find(
        (provider) => provider.provider === "github"
      );

      const linked = githubProvider?.isLinked ?? false;
      setIsGitHubLinked(linked);

      if (linked) {
        await fetchRepositories();
      }
    } catch (err) {
      console.error("Failed to initialize repository page:", err);
      setError(err.message || "Cannot load GitHub connection status.");
    } finally {
      setLoading(false);
    }
  }

  async function fetchRepositories() {
    const data = await getSavedGitHubRepositoriesApi();

    setRepositories(data);

    const selected = data
      .filter((repo) => repo.isSelectedForPortfolio)
      .map((repo) => repo.repositoryId);

    setSelectedIds(selected);
  }

  const handleSync = async () => {
    try {
      setSyncing(true);
      setError("");
      setSuccess("");

      const data = await syncGitHubRepositoriesApi();

      setRepositories(data);

      const selected = data
        .filter((repo) => repo.isSelectedForPortfolio)
        .map((repo) => repo.repositoryId);

      setSelectedIds(selected);
      setSuccess("GitHub repositories synced successfully.");
    } catch (err) {
      console.error("Failed to sync repositories:", err);
      setError(
        err.message ||
          "Cannot sync repositories. Please connect GitHub first."
      );
    } finally {
      setSyncing(false);
    }
  };

  const handleToggleRepository = (repositoryId) => {
    setSelectedIds((prev) => {
      if (prev.includes(repositoryId)) {
        return prev.filter((id) => id !== repositoryId);
      }

      return [...prev, repositoryId];
    });
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError("");
      setSuccess("");

      await updatePortfolioRepositoriesApi(selectedIds);

      setSuccess("Portfolio repositories updated successfully.");
      await fetchRepositories();
    } catch (err) {
      console.error("Failed to save repositories:", err);
      setError(err.message || "Cannot save selected repositories.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-slate-100 px-6 py-8">
        <div className="mx-auto max-w-6xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
          <p className="text-slate-500">Loading repositories...</p>
        </div>
      </main>
    );
  }

  if (!isGitHubLinked) {
    return (
      <GitHubNotLinkedState
        error={error}
        onConnectGitHub={redirectToGitHubLink}
        onBack={() => navigate("/portfolio")}
      />
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] bg-slate-100 px-6 py-8">
      <div className="mx-auto max-w-6xl space-y-6">
        <RepositoryPageHeader
          selectedCount={selectedCount}
          syncing={syncing}
          saving={saving}
          onSync={handleSync}
          onSave={handleSave}
        />

        <RepositoryStatusMessage error={error} success={success} />

        {repositories.length === 0 ? (
          <RepositoryEmptyState />
        ) : (
          <RepositorySelectList
            repositories={repositories}
            selectedIds={selectedIds}
            onToggleRepository={handleToggleRepository}
          />
        )}
      </div>
    </main>
  );
}