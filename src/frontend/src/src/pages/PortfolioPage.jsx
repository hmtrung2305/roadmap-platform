import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Loader2, SlidersHorizontal } from "lucide-react";

import { getPortfolioByUsernameApi } from "../api/portfolioApi";
import { useAuthStore } from "../stores/useAuthStore";

import PortfolioHeader from "../components/portfolio/PortfolioHeader";
import PortfolioRepositoryList from "../components/portfolio/PortfolioRepositoryList";
import { FaGithub } from "react-icons/fa";

export default function PortfolioPage() {
  const { username: routeUsername } = useParams();
  const user = useAuthStore((state) => state.user);

  const [portfolio, setPortfolio] = useState(null);
  const [portfolioLoading, setPortfolioLoading] = useState(true);
  const [portfolioError, setPortfolioError] = useState("");

  const isOwnPortfolio = !routeUsername;

  const username = useMemo(() => {
    return (
      routeUsername ||
      user?.username ||
      user?.userName ||
      user?.Username ||
      user?.email?.split("@")[0]
    );
  }, [routeUsername, user]);

  useEffect(() => {
    const fetchPortfolio = async () => {
      if (!username) {
        setPortfolioLoading(false);
        setPortfolioError("Username was not found.");
        return;
      }

      try {
        setPortfolioLoading(true);
        setPortfolioError("");

        const data = await getPortfolioByUsernameApi(username);
        setPortfolio(data);
      } catch (error) {
        console.error("Load portfolio failed:", error);
        setPortfolioError(error?.message || "Could not load this portfolio.");
      } finally {
        setPortfolioLoading(false);
      }
    };

    fetchPortfolio();
  }, [username]);

  if (portfolioLoading) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-[#f8faf8] px-4 py-8 sm:px-6">
        <div className="mx-auto max-w-6xl">
          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex items-center gap-3 text-slate-500">
              <Loader2 className="animate-spin" size={18} />
              Loading portfolio...
            </div>
            <div className="mt-6 animate-pulse space-y-4">
              <div className="h-56 rounded-3xl bg-slate-100" />
              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <div className="h-36 rounded-2xl bg-slate-100" />
                <div className="h-36 rounded-2xl bg-slate-100" />
                <div className="h-36 rounded-2xl bg-slate-100" />
              </div>
            </div>
          </div>
        </div>
      </main>
    );
  }

  if (portfolioError) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-[#f8faf8] px-4 py-8 sm:px-6">
        <section className="mx-auto max-w-3xl rounded-3xl border border-red-100 bg-white p-8 text-center shadow-sm">
          <p className="text-lg font-black text-slate-950">Portfolio unavailable</p>
          <p className="mt-2 text-sm font-medium text-red-600">{portfolioError}</p>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#f8faf8] px-4 py-7 text-slate-950 sm:px-6">
      <div className="mx-auto max-w-6xl space-y-6">
        {isOwnPortfolio && (
          <section className="flex flex-col gap-3 rounded-[1.35rem] border border-emerald-100 bg-white px-5 py-4 shadow-sm md:flex-row md:items-center md:justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-700">
                <FaGithub size={18} />
              </div>
              <div>
                <p className="text-sm font-extrabold text-slate-950">Portfolio control center</p>
                <p className="mt-1 text-sm text-slate-500">
                  Choose the repositories that should appear publicly and keep your portfolio focused.
                </p>
              </div>
            </div>

            <Link
              to="/portfolio/repositories"
              className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-emerald-700"
            >
              <SlidersHorizontal size={16} />
              Manage repositories
            </Link>
          </section>
        )}

        <PortfolioHeader portfolio={portfolio} editable={isOwnPortfolio} />
        <PortfolioRepositoryList repositories={portfolio?.repositories || []} editable={isOwnPortfolio} />
      </div>
    </main>
  );
}
