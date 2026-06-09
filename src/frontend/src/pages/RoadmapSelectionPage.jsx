import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { roadmapApi } from "../api/roadmapApi";
import RoadmapCard from "../components/roadmap/RoadmapCard";

export default function RoadmapSelectionPage() {
  const navigate = useNavigate();
  const [roadmaps, setRoadmaps] = useState([]);
  const [status, setStatus] = useState("loading");
  const [message, setMessage] = useState("");
  const [query, setQuery] = useState("");

  const filteredRoadmaps = useMemo(() => {
    const value = query.trim().toLowerCase();

    if (!value) return roadmaps;

    return roadmaps.filter((roadmap) => {
      const searchable = [
        roadmap.title,
        roadmap.description,
        roadmap.slug,
        roadmap.careerRole?.name,
        roadmap.careerRole?.category,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return searchable.includes(value);
    });
  }, [roadmaps, query]);

  useEffect(() => {
    loadPage();
  }, []);

  async function loadPage() {
    setStatus("loading");
    setMessage("");

    try {
      const baseRoadmaps = await roadmapApi.getRoadmaps();

      setRoadmaps(baseRoadmaps);
      setStatus("success");

      loadEnrollmentProgress(baseRoadmaps);
    } catch (error) {
      console.error(error);
      setMessage(error?.message || "Failed to load roadmaps. Check that the API is running.");
      setStatus("error");
    }
  }

  async function loadEnrollmentProgress(baseRoadmaps) {
    const roadmapsWithVersion = baseRoadmaps.filter((roadmap) => roadmap.roadmapVersionId);

    if (roadmapsWithVersion.length === 0) return;

    try {
      const progressResults = await Promise.allSettled(
        roadmapsWithVersion.map(async (roadmap) => ({
          roadmapVersionId: roadmap.roadmapVersionId,
          enrollment: await roadmapApi.getCurrentEnrollment(roadmap.roadmapVersionId),
        }))
      );

      const enrollmentByVersionId = new Map();

      progressResults.forEach((result) => {
        if (result.status !== "fulfilled") return;

        enrollmentByVersionId.set(
          String(result.value.roadmapVersionId),
          result.value.enrollment
        );
      });

      setRoadmaps((current) =>
        current.map((roadmap) => {
          const key = String(roadmap.roadmapVersionId);

          if (!enrollmentByVersionId.has(key)) return roadmap;

          const enrollment = enrollmentByVersionId.get(key);

          return {
            ...roadmap,
            enrollment,
            progressPercent: enrollment?.progressPercent ?? 0,
          };
        })
      );
    } catch (error) {
      console.error(error);
    }
  }

  if (status === "loading") {
    return (
      <PageShell centered>
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-8 text-center shadow-lg">
          <div className="mx-auto mb-4 h-7 w-7 animate-spin rounded-full border-2 border-[#B9D8CC] border-t-[#2FA084]" />
          <p className="animate-pulse text-sm font-extrabold tracking-tight text-slate-600">
            Loading roadmaps
          </p>
        </div>
      </PageShell>
    );
  }

  if (status === "error") {
    return (
      <PageShell centered>
        <div className="max-w-md rounded-lg border border-[#B9D8CC] bg-white p-8 text-center shadow-lg">
          <h1 className="text-2xl font-black text-[#18332D]">Couldn&apos;t load roadmaps</h1>
          <p className="mt-3 text-sm font-semibold leading-6 text-slate-600">{message}</p>

          <button
            type="button"
            onClick={loadPage}
            className="mt-6 rounded-lg border border-[#B9D8CC] bg-[#2FA084] px-5 py-2 text-sm font-extrabold text-white shadow-sm transition-transform duration-150 hover:-translate-y-0.5 hover:shadow-[0_14px_30px_rgba(31,111,95,0.10)] active:translate-y-0 active:shadow-sm"
          >
            Retry
          </button>
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell>
      <style>{`
        @keyframes roadmapHeaderIn {
          from { opacity: 0; transform: translateY(16px); }
          to { opacity: 1; transform: translateY(0); }
        }

        @keyframes roadmapCardIn {
          from { opacity: 0; transform: translateY(14px) scale(0.985); }
          to { opacity: 1; transform: translateY(0) scale(1); }
        }
      `}</style>

      <main className="mx-auto max-w-7xl px-6 py-10 pb-16">
        <section
          className="mx-auto max-w-4xl text-center"
          style={{ animation: "roadmapHeaderIn 420ms ease-out both" }}
        >
          <p className="text-xs font-extrabold uppercase tracking-[0.2em] text-[#1F6F5F]">
            Learning paths
          </p>

          <h1 className="mt-3 text-4xl font-black tracking-tight text-[#18332D] sm:text-5xl">
            Choose a career role
          </h1>

          <p className="mx-auto mt-4 max-w-2xl text-sm font-semibold leading-7 text-slate-600 sm:text-base">
            Select a career path to open a curated learning roadmap.
          </p>

          <div className="relative mx-auto mt-7 max-w-4xl">
            <svg
              aria-hidden
              className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <circle cx="11" cy="11" r="7" />
              <path d="m20 20-3.5-3.5" />
            </svg>

            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search career role"
              className="w-full rounded-lg border border-[#B9D8CC] bg-white py-3 pl-12 pr-4 text-sm font-bold text-[#18332D] shadow-sm outline-none placeholder:text-slate-400 transition-colors duration-150 focus:border-[#2FA084] focus:bg-[#EAF8F1] focus:ring-4 focus:ring-[#2FA084]/10"
            />
          </div>
        </section>

        {filteredRoadmaps.length === 0 ? (
          <section className="mt-10 rounded-lg border-2 border-dashed border-[#B9D8CC] bg-white p-10 text-center">
            <p className="text-sm font-black text-[#18332D]">No roadmaps found.</p>
            <p className="mt-2 text-sm font-semibold text-slate-500">
              Try a different search term.
            </p>
          </section>
        ) : (
          <section className="mt-9 grid items-stretch gap-4 overflow-visible pb-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
            {filteredRoadmaps.map((roadmap, index) => (
              <RoadmapCard
                key={roadmap.roadmapId || roadmap.roadmapVersionId || roadmap.slug}
                roadmap={roadmap}
                index={index}
                onOpen={() => navigate(`/roadmaps/${encodeURIComponent(roadmap.slug)}`)}
              />
            ))}
          </section>
        )}
      </main>
    </PageShell>
  );
}

function PageShell({ centered = false, children }) {
  return (
    <div className="min-h-[calc(100vh-64px)] bg-[#F7F1E8] text-[#18332D]">
      {centered ? (
        <main className="mx-auto flex min-h-[calc(100vh-64px)] max-w-7xl items-center justify-center px-6 py-8">
          {children}
        </main>
      ) : (
        children
      )}
    </div>
  );
}
