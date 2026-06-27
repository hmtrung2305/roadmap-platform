import { useEffect, useMemo } from "react";
import { Loader2 } from "lucide-react";
import { useParams } from "react-router-dom";

import { useAuthStore } from "../stores/useAuthStore";
import { usePortfolioStore } from "../stores/usePortfolioStore";
import PortfolioHeader from "../features/portfolio/components/public/PortfolioHeader";
import PortfolioStats from "../features/portfolio/components/public/PortfolioStats";
import PortfolioAbout from "../features/portfolio/components/public/PortfolioAbout";
import PortfolioRepositoryList from "../features/portfolio/components/public/PortfolioRepositoryList";
import PortfolioSkillGroups from "../features/portfolio/components/public/PortfolioSkillGroups";

export default function PortfolioPage() {
  const { username: routeUsername } = useParams();
  const user = useAuthStore((state) => state.user);

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

  const portfolio = usePortfolioStore((state) =>
    state.getPortfolioSnapshot({ username, isOwnPortfolio }),
  );
  const portfolioLoading = usePortfolioStore((state) =>
    state.getPortfolioLoading({ username, isOwnPortfolio }),
  );
  const portfolioLoaded = usePortfolioStore((state) =>
    state.getPortfolioLoaded({ username, isOwnPortfolio }),
  );
  const portfolioError = usePortfolioStore((state) =>
    state.getPortfolioError({ username, isOwnPortfolio }),
  );
  const loadPortfolio = usePortfolioStore((state) => state.loadPortfolio);

  useEffect(() => {
    loadPortfolio({ username, isOwnPortfolio }).catch((error) => {
      console.error("Load portfolio failed:", error);
    });
  }, [isOwnPortfolio, loadPortfolio, username]);

  const shouldShowInitialLoading = portfolioLoading || (!portfolioLoaded && !portfolioError);

  if (shouldShowInitialLoading) {
    return (
      <main className="min-h-[calc(100vh-4rem)] px-6 py-12">
        <div className="mx-auto max-w-7xl rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
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
      <main className="min-h-[calc(100vh-4rem)] px-6 py-12">
        <section className="mx-auto max-w-3xl rounded-lg border border-[#B9D8CC] bg-white p-8 text-center shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
          <p className="text-lg font-extrabold text-[#18332D]">Portfolio unavailable</p>
          <p className="mt-2 text-sm font-bold text-red-600">{portfolioError}</p>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] px-6 py-12">
      <div className="mx-auto max-w-7xl space-y-4">
        <PortfolioHeader portfolio={portfolio} username={username} isOwnPortfolio={isOwnPortfolio} />
        <PortfolioAbout portfolio={portfolio} />
        <PortfolioStats portfolio={portfolio} />
        <PortfolioRepositoryList repositories={portfolio?.repositories || []} />
        <PortfolioDetectedTechnologies repositories={portfolio?.repositories || []} />
        <PortfolioSkillGroups skillGroups={portfolio?.skillGroups || []} />
      </div>
    </main>
  );
}


function toPortfolioTagArray(value) {
  if (!value) return [];
  if (Array.isArray(value)) return value;
  if (typeof value === "string") {
    return value
      .split(/[;,]/)
      .map((item) => item.trim())
      .filter(Boolean);
  }
  return [];
}

function getRepositoryTechStackTags(repository) {
  const insight = repository?.insight;
  const insightStatus = String(insight?.analysisStatus || insight?.status || "").toLowerCase();
  const publicInsight = insightStatus === "completed" ? insight : null;

  return [
    ...toPortfolioTagArray(publicInsight?.techStack),
    ...toPortfolioTagArray(publicInsight?.techStacks),
    ...toPortfolioTagArray(repository?.techStack),
    ...toPortfolioTagArray(repository?.techStacks),
  ].filter(Boolean);
}

function PortfolioDetectedTechnologies({ repositories = [] }) {
  const tags = useMemo(() => {
    const collected = repositories.flatMap(getRepositoryTechStackTags);
    return Array.from(
      new Set(collected.map((tag) => String(tag).trim()).filter(Boolean))
    ).slice(0, 14);
  }, [repositories]);

  if (tags.length === 0) return null;

  return (
    <section className="rounded-2xl border border-[#B9D8CC]/75 bg-white p-5 shadow-[0_8px_18px_rgba(31,111,95,0.05)] transition duration-200 hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md">
      <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
        Languages & technologies
      </p>

      <div className="mt-4 flex flex-wrap gap-2">
        {tags.map((tag) => (
          <span
            key={tag}
            className="rounded-lg bg-[#F7F1E8] px-3 py-1.5 text-xs font-bold text-slate-700 ring-1 ring-[#B9D8CC]"
          >
            {tag}
          </span>
        ))}
      </div>
    </section>
  );
}
