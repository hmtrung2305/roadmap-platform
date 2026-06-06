import { useEffect, useMemo, useState } from "react";
import { Copy, Loader2, PencilLine } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { FaGithub } from "react-icons/fa";

import { getMyPortfolioApi, getPortfolioByUsernameApi } from "../api/portfolioApi";
import { useAuthStore } from "../stores/useAuthStore";
import PortfolioHeader from "../components/portfolio/PortfolioHeader";
import PortfolioStats from "../components/portfolio/PortfolioStats";
import PortfolioAbout from "../components/portfolio/PortfolioAbout";
import PortfolioRepositoryList from "../components/portfolio/PortfolioRepositoryList";
import PortfolioSkillGroups from "../components/portfolio/PortfolioSkillGroups";

export default function PortfolioPage() {
  const { username: routeUsername } = useParams();
  const user = useAuthStore((state) => state.user);

  const [portfolio, setPortfolio] = useState(null);
  const [portfolioLoading, setPortfolioLoading] = useState(true);
  const [portfolioError, setPortfolioError] = useState("");
  const [copied, setCopied] = useState(false);

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
      if (!isOwnPortfolio && !username) {
        setPortfolioLoading(false);
        setPortfolioError("Username was not found.");
        return;
      }

      try {
        setPortfolioLoading(true);
        setPortfolioError("");

        const data = isOwnPortfolio
          ? await getMyPortfolioApi()
          : await getPortfolioByUsernameApi(username);

        setPortfolio(data);
      } catch (error) {
        console.error("Load portfolio failed:", error);
        setPortfolioError(error?.message || "Could not load this portfolio.");
      } finally {
        setPortfolioLoading(false);
      }
    };

    fetchPortfolio();
  }, [username, isOwnPortfolio]);

  const publicLink = username
    ? `${window.location.origin}/portfolio/${username}`
    : "";

  const handleCopy = async () => {
    if (!publicLink) return;

    try {
      await navigator.clipboard.writeText(publicLink);
      setCopied(true);
      setTimeout(() => setCopied(false), 1800);
    } catch (error) {
      console.error("Copy failed:", error);
    }
  };

  if (portfolioLoading) {
    return (
      <main className="min-h-[calc(100vh-4rem)] px-4 py-8 sm:px-6">
        <div className="mx-auto max-w-6xl rounded-2xl border border-[#B9D8CC] bg-white p-6 shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
          <div className="flex items-center gap-3 text-slate-600">
            <Loader2 className="animate-spin" size={18} />
            Loading portfolio...
          </div>
        </div>
      </main>
    );
  }

  if (portfolioError) {
    return (
      <main className="min-h-[calc(100vh-4rem)] px-4 py-8 sm:px-6">
        <section className="mx-auto max-w-3xl rounded-2xl border border-[#B9D8CC] bg-white p-8 text-center shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
          <p className="text-lg font-extrabold text-[#18332D]">Portfolio unavailable</p>
          <p className="mt-2 text-sm font-bold text-red-600">{portfolioError}</p>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] px-4 py-7 sm:px-6">
      <div className="mx-auto max-w-6xl space-y-6">
        {isOwnPortfolio && (
          <section className="rounded-2xl border border-[#B9D8CC] bg-white p-5 shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
              <div className="flex items-center gap-3">
                <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-[#2FA084] text-white shadow-sm">
                  <FaGithub size={20} />
                </div>
                <div>
                  <p className="text-sm font-extrabold text-[#18332D]">Public portfolio editor</p>
                  <p className="mt-1 text-sm font-semibold text-slate-600">
                    Choose repositories and share your public page without requiring visitors to sign in.
                  </p>
                </div>
              </div>

              <div className="flex flex-col gap-2 sm:flex-row">
                <button
                  type="button"
                  onClick={handleCopy}
                  className="inline-flex items-center justify-center gap-2 rounded-xl border border-[#B9D8CC] bg-[#EEEEEE] px-4 py-2 text-sm font-extrabold text-[#18332D] shadow-sm transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/35"
                >
                  <Copy size={15} />
                  {copied ? "Copied" : "Copy public link"}
                </button>

                <Link
                  to="/portfolio/repositories"
                  className="inline-flex items-center justify-center gap-2 rounded-xl bg-[#2FA084] px-4 py-2 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5"
                >
                  <PencilLine size={15} />
                  Manage repositories
                </Link>
              </div>
            </div>
          </section>
        )}

        <PortfolioHeader portfolio={portfolio} username={username} />
        <PortfolioStats portfolio={portfolio} />
        <PortfolioAbout portfolio={portfolio} />
        <PortfolioRepositoryList repositories={portfolio?.repositories || []} />
        <PortfolioSkillGroups skillGroups={portfolio?.skillGroups || []} />
      </div>
    </main>
  );
}
