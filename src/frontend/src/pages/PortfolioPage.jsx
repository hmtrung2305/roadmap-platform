import { useEffect, useMemo, useState } from "react";
import { Loader2 } from "lucide-react";
import { useParams } from "react-router-dom";

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

  if (portfolioLoading) {
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
      <div className="mx-auto max-w-7xl space-y-6">
        <PortfolioHeader portfolio={portfolio} username={username} isOwnPortfolio={isOwnPortfolio} />
        <PortfolioStats portfolio={portfolio} />
        <PortfolioAbout portfolio={portfolio} />
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

function getRepositoryDetectedTags(repository) {
  const insight = repository?.insight;

  return [
    insight?.projectType,
    ...toPortfolioTagArray(insight?.techStack),
    ...toPortfolioTagArray(insight?.detectedSkills),
    ...toPortfolioTagArray(insight?.skills),
    ...toPortfolioTagArray(repository?.detectedSkills),
    ...toPortfolioTagArray(repository?.techStack),
    ...toPortfolioTagArray(repository?.readmeSkills),
    ...toPortfolioTagArray(repository?.readmeTechnologies),
    repository?.primaryLanguage || repository?.language,
  ].filter(Boolean);
}

function PortfolioDetectedTechnologies({ repositories = [] }) {
  const tags = useMemo(() => {
    const collected = repositories.flatMap(getRepositoryDetectedTags);
    return Array.from(
      new Set(collected.map((tag) => String(tag).trim()).filter(Boolean))
    ).slice(0, 14);
  }, [repositories]);

  if (tags.length === 0) return null;

  return (
    <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
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
