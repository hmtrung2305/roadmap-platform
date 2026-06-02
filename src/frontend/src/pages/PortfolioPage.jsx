import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getMyPortfolioApi } from "../api/portfolioApi";

import PortfolioHeader from "../components/portfolio/PortfolioHeader";
import PortfolioAbout from "../components/portfolio/PortfolioAbout";
import PortfolioLinks from "../components/portfolio/PortfolioLink";
import PortfolioRepositoryList from "../components/portfolio/PortfolioRepositoryList";

export default function PortfolioPage() {
  const navigate = useNavigate();

  const [portfolio, setPortfolio] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function fetchPortfolio() {
      try {
        setLoading(true);
        setError("");

        const data = await getMyPortfolioApi();
        setPortfolio(data);
      } catch (err) {
        console.error("Failed to load portfolio:", err);
        setError(err.message || "Cannot load portfolio.");
      } finally {
        setLoading(false);
      }
    }

    fetchPortfolio();
  }, []);

  if (loading) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-slate-100 px-6 py-8">
        <div className="mx-auto max-w-6xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
          <p className="text-slate-500">Loading portfolio...</p>
        </div>
      </main>
    );
  }

  if (error || !portfolio) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-slate-100 px-6 py-8">
        <div className="mx-auto max-w-6xl rounded-2xl border border-red-200 bg-red-50 p-8 text-red-700">
          {error || "Portfolio not found."}
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] bg-slate-100 px-6 py-8">
      <div className="mx-auto max-w-6xl space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-slate-900">My Portfolio</h1>
            <p className="mt-1 text-sm text-slate-500">
              Preview and manage your public developer profile.
            </p>
          </div>

          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => navigate("/profile/edit")}
              className="rounded-lg bg-blue-700 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-800"
            >
              Edit Profile
            </button>

            <button
              type="button"
              onClick={() => navigate("/portfolio/repositories")}
              className="rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm hover:bg-slate-50"
            >
              Manage Repositories
            </button>
          </div>
        </div>

        <PortfolioHeader portfolio={portfolio} />

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_320px]">
          <div className="space-y-6">
            <PortfolioAbout portfolio={portfolio} />
            <PortfolioRepositoryList repositories={portfolio.repositories || []} />
          </div>

          <PortfolioLinks portfolio={portfolio} />
        </div>
      </div>
    </main>
  );
}