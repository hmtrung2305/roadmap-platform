import { FcAbout } from "react-icons/fc";

export default function PortfolioAbout({ portfolio }) {
  const repoCount = portfolio.repositories?.length || 0;

  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-7 shadow-sm">
      <h2 className="flex items-center gap-2 text-2xl font-bold text-slate-900">
        <FcAbout size={24}/>
        About
      </h2>

      <p className="mt-5 leading-7 text-slate-600">
        {portfolio.bio || "No profile description yet."}
      </p>

      <div className="mt-8 grid grid-cols-1 gap-4 border-t border-slate-200 pt-6 sm:grid-cols-3">
        <StatItem label="Repositories" value={repoCount} />
        <StatItem label="Current Role" value={portfolio.currentRole || "-"} />
        <StatItem label="Career Goal" value={portfolio.careerGoal || "-"} />
      </div>
    </section>
  );
}

function StatItem({ label, value }) {
  return (
    <div className="rounded-xl bg-slate-50 p-4 text-center">
      <p className="text-xl font-bold text-blue-700">{value}</p>
      <p className="mt-1 text-sm text-slate-500">{label}</p>
    </div>
  );
}