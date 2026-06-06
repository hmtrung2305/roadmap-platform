import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, Eye, Loader2, Search, X } from "lucide-react";

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
  const [searchTerm, setSearchTerm] = useState("");

  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [saving, setSaving] = useState(false);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const selectedCount = selectedIds.length;

  const filteredRepositories = useMemo(() => {
    const keyword = searchTerm.trim().toLowerCase();

    if (!keyword) return repositories;

    return repositories.filter((repo) => {
      const text = [
        repo.name,
        repo.fullName,
        repo.description,
        repo.primaryLanguage,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return text.includes(keyword);
    });
  }, [repositories, searchTerm]);

  const selectedRepositories = useMemo(() => {
    return repositories.filter((repo) => selectedIds.includes(repo.repositoryId));
  }, [repositories, selectedIds]);

  useEffect(() => {
    initPage();
  }, []);

  async function initPage() {
    try {
      setLoading(true);
      setError("");

      const providers = await getAuthProvidersApi();
      const githubProvider = providers.find(
        (provider) => provider.provider?.toLowerCase() === "github",
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

    setRepositories(data || []);

    const selected = (data || [])
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

      setRepositories(data || []);

      const selected = (data || [])
        .filter((repo) => repo.isSelectedForPortfolio)
        .map((repo) => repo.repositoryId);

      setSelectedIds(selected);
      setSuccess("GitHub repositories synced successfully.");
    } catch (err) {
      console.error("Failed to sync repositories:", err);
      setError(err.message || "Cannot sync repositories. Please connect GitHub first.");
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

  const handleSelectVisible = () => {
    const visibleIds = filteredRepositories.map((repo) => repo.repositoryId);
    setSelectedIds((prev) => Array.from(new Set([...prev, ...visibleIds])));
  };

  const handleClearSelection = () => {
    setSelectedIds([]);
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
      <main className="min-h-[calc(100vh-4rem)] bg-[#f6f8f4] px-4 py-8 sm:px-6">
        <div className="mx-auto max-w-6xl rounded-[2rem] border border-white bg-white/85 p-8 shadow-sm ring-1 ring-slate-200/70">
          <div className="flex items-center gap-3 text-slate-500">
            <Loader2 className="animate-spin" size={18} />
            Loading repositories...
          </div>
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
    <main className="min-h-[calc(100vh-4rem)] bg-[#f6f8f4] px-4 py-8 sm:px-6">
      <div className="mx-auto max-w-7xl space-y-6">
        <div className="flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={() => navigate("/portfolio")}
            className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 shadow-sm transition hover:bg-slate-50"
          >
            <ArrowLeft size={16} />
            Back to portfolio
          </button>

          <Link
            to="/portfolio"
            className="hidden items-center gap-2 rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-emerald-700 sm:inline-flex"
          >
            <Eye size={16} />
            Preview portfolio
          </Link>
        </div>

        <RepositoryPageHeader
          selectedCount={selectedCount}
          totalCount={repositories.length}
          syncing={syncing}
          saving={saving}
          onSync={handleSync}
          onSave={handleSave}
        />

        <RepositoryStatusMessage error={error} success={success} />

        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_340px]">
          <section className="space-y-5">
            <div className="rounded-[1.5rem] border border-slate-200 bg-white p-4 shadow-sm">
              <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                <div className="relative flex-1">
                  <Search className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                  <input
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                    placeholder="Search repository name, language, description..."
                    className="w-full rounded-2xl border border-slate-200 bg-slate-50 py-3 pl-11 pr-10 text-sm font-medium text-slate-700 outline-none transition placeholder:text-slate-400 focus:border-emerald-300 focus:bg-white focus:ring-4 focus:ring-emerald-100"
                  />
                  {searchTerm && (
                    <button
                      type="button"
                      onClick={() => setSearchTerm("")}
                      className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                    >
                      <X size={16} />
                    </button>
                  )}
                </div>

                <div className="flex flex-wrap gap-2">
                  <button
                    type="button"
                    onClick={handleSelectVisible}
                    disabled={filteredRepositories.length === 0}
                    className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-2.5 text-sm font-bold text-emerald-700 transition hover:bg-emerald-100 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Select visible
                  </button>
                  <button
                    type="button"
                    onClick={handleClearSelection}
                    disabled={selectedCount === 0}
                    className="rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-bold text-slate-600 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Clear all
                  </button>
                </div>
              </div>

              <p className="mt-3 text-sm font-medium text-slate-500">
                Showing {filteredRepositories.length} of {repositories.length} repositories.
              </p>
            </div>

            {repositories.length === 0 ? (
              <RepositoryEmptyState />
            ) : filteredRepositories.length === 0 ? (
              <section className="rounded-[2rem] border border-dashed border-slate-300 bg-white p-10 text-center shadow-sm">
                <h2 className="text-lg font-black text-slate-900">No matching repositories</h2>
                <p className="mt-2 text-sm text-slate-500">Try another keyword or clear the search box.</p>
              </section>
            ) : (
              <RepositorySelectList
                repositories={filteredRepositories}
                selectedIds={selectedIds}
                onToggleRepository={handleToggleRepository}
              />
            )}
          </section>

          <aside className="xl:sticky xl:top-24 xl:self-start">
            <section className="rounded-[2rem] border border-emerald-100 bg-white p-5 shadow-sm">
              <p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-700">Selection summary</p>
              <h2 className="mt-2 text-3xl font-black text-slate-950">{selectedCount}</h2>
              <p className="mt-1 text-sm font-medium text-slate-500">repositories will appear on your portfolio.</p>

              <div className="mt-5 space-y-3 border-t border-slate-100 pt-5">
                {selectedRepositories.length === 0 ? (
                  <p className="rounded-2xl bg-slate-50 p-4 text-sm font-medium text-slate-500">
                    Pick your strongest projects first. You can save an empty portfolio too.
                  </p>
                ) : (
                  selectedRepositories.slice(0, 5).map((repo) => (
                    <button
                      key={repo.repositoryId}
                      type="button"
                      onClick={() => handleToggleRepository(repo.repositoryId)}
                      className="flex w-full items-center justify-between gap-3 rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-left transition hover:border-red-100 hover:bg-red-50"
                    >
                      <span className="min-w-0">
                        <span className="block truncate text-sm font-black text-slate-800">{repo.name}</span>
                        <span className="block truncate text-xs font-medium text-slate-500">{repo.primaryLanguage || "No language"}</span>
                      </span>
                      <X className="shrink-0 text-slate-400" size={15} />
                    </button>
                  ))
                )}

                {selectedRepositories.length > 5 && (
                  <p className="text-sm font-bold text-slate-500">+{selectedRepositories.length - 5} more selected</p>
                )}
              </div>

              <button
                type="button"
                onClick={handleSave}
                disabled={saving}
                className="mt-5 w-full rounded-2xl bg-emerald-600 px-4 py-3 text-sm font-black text-white shadow-lg shadow-emerald-100 transition hover:-translate-y-0.5 hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {saving ? "Saving selection..." : "Save repository showcase"}
              </button>
            </section>
          </aside>
        </div>
      </div>
    </main>
  );
}
