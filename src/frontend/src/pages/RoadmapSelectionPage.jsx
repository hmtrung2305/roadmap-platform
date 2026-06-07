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
        <div className="rounded-xl border border-[#B9D8CC] bg-white p-8 text-center shadow-lg">
          <div className="mx-auto mb-4 h-7 w-7 animate-spin rounded-xl border border-[#B9D8CC] border-t-[#2FA084]" />
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
        <div className="max-w-md rounded-xl border border-[#B9D8CC] bg-white p-8 text-center shadow-lg">
          <h1 className="text-2xl font-black text-[#18332D]">Couldn&apos;t load roadmaps</h1>
          <p className="mt-3 text-sm font-semibold leading-6 text-slate-600">{message}</p>

          <button
            type="button"
            onClick={loadPage}
            className="mt-6 rounded-xl border border-[#B9D8CC] bg-[#2FA084] px-5 py-2 text-sm font-extrabold text-white shadow-sm transition-transform duration-150 hover:-translate-y-0.5 hover:shadow-[0_14px_30px_rgba(31,111,95,0.10)] active:translate-y-0 active:shadow-sm"
          >
            Retry
          </button>
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell>
      <main className="mx-auto max-w-7xl space-y-12 px-6 py-8 pb-16">
        <section className="overflow-visible rounded-xl border border-[#B9D8CC] bg-white shadow-lg">
          <div className="border-b border-[#B9D8CC] bg-[#EAF8F1] px-6 py-3">
            <p className="text-xs font-extrabold tracking-[0.2em] text-[#1F6F5F]">
              Learning paths
            </p>
          </div>

          <div className="grid gap-8 p-7 lg:grid-cols-[1.1fr_0.9fr] lg:items-end">
            <div>
              <h1 className="text-4xl font-black tracking-tight text-[#18332D] sm:text-5xl">
                Choose your roadmap
              </h1>

              <p className="mt-4 max-w-2xl text-sm font-semibold leading-7 text-slate-600">
                Pick a learning path, then open the graph to track topics, projects,
                prerequisites, resources, and skills.
              </p>
            </div>

            <div className="rounded-xl border border-[#B9D8CC] bg-white p-4 shadow-sm">
              <label className="mb-2 block text-xs font-extrabold tracking-[0.16em] text-slate-500">
                Search roadmaps
              </label>

              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="frontend, backend, api, fullstack..."
                className="w-full rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] px-4 py-3 text-sm font-bold outline-none placeholder:text-slate-500 transition-colors duration-150 focus:bg-[#EAF8F1]"
              />
            </div>
          </div>
        </section>

        {filteredRoadmaps.length === 0 ? (
          <section className="rounded-xl border-2 border-dashed border-[#B9D8CC] bg-white p-10 text-center">
            <p className="text-sm font-black text-[#18332D]">No roadmaps found.</p>
            <p className="mt-2 text-sm font-semibold text-slate-500">
              Try a different search term.
            </p>
          </section>
        ) : (
          <section className="grid items-stretch gap-3 overflow-visible pb-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {filteredRoadmaps.map((roadmap) => (
              <RoadmapCard
                key={roadmap.roadmapId || roadmap.roadmapVersionId || roadmap.slug}
                roadmap={roadmap}
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
