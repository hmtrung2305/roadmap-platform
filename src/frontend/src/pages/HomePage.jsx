
import { useAuthStore } from "../stores/useAuthStore";
import { useStreakStore } from "../stores/useStreakStore";

export default function HomePage() {

  const trackStreak = useStreakStore((state) => state.trackStreak);
  const user = useAuthStore((state) => state.user);

  const userDisplayName =
    user?.displayName || user?.username || user?.email || "User";

  const handleOpenDemoResource = async () => {

    await trackStreak();

  }
  return (
    <section className="mx-auto max-w-7xl px-6 py-12">
      <div className="rounded-3xl border border-slate-200 bg-white p-8 shadow-sm">
        <p className="text-sm font-semibold text-emerald-600">Welcome back</p>

        <h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-900">
          Hi, {userDisplayName}
        </h1>

        <p className="mt-4 max-w-2xl text-base leading-7 text-slate-600">
          Welcome to your learning dashboard. From here, you can continue your
          roadmap, update your skills, connect GitHub, and build your portfolio.
        </p>

        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <InfoCard title="Roadmap" description="Follow your learning path." />
          <InfoCard title="Skills" description="Track your current skills." />
          <InfoCard title="GitHub" description="Analyze your repositories." />
          <InfoCard title="Portfolio" description="Build your public profile." />
        </div>

        <div className="mt-8 rounded-2xl border border-emerald-100 bg-emerald-50 p-6">
          <p className="text-sm font-semibold text-emerald-700">
            Demo learning resource
          </p>

          <h2 className="mt-2 text-xl font-bold text-slate-900">
            Introduction to React State
          </h2>

          <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
            Click this demo resource to test daily streak tracking. The real
            document page will be implemented later.
          </p>

          <button
            type="button"
            onClick={handleOpenDemoResource}
            className="mt-5 rounded-xl bg-emerald-700 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-emerald-800"
          >
            Open demo resource
          </button>
        </div>
      </div>
    </section>
  );
}

function InfoCard({ title, description }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-[#F7F1E8] p-5">
      <h3 className="font-semibold text-slate-900">{title}</h3>
      <p className="mt-2 text-sm leading-6 text-slate-500">{description}</p>
    </div>
  );
}