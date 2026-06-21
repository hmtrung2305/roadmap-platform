export default function PortfolioStats({ portfolio }) {
  const repositories = portfolio?.repositories || [];
  const languages = new Set(
    repositories
      .map((repo) => repo.primaryLanguage || repo.language)
      .filter(Boolean)
  );

  const totalStars = repositories.reduce(
    (sum, repo) => sum + Number(repo.stars ?? repo.starCount ?? 0),
    0
  );

  return (
    <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard value={repositories.length} label="Projects" />
      <StatCard value={totalStars} label="Stars" />
      <StatCard value={languages.size} label="Languages" />
      <StatCard
        value={portfolio?.careerGoal || "Not updated"}
        label="Goal"
        text
      />
    </section>
  );
}

function StatCard({ value, label, text = false }) {
  return (
    <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-[0_14px_34px_rgba(31,111,95,0.08)] transition hover:-translate-y-0.5 hover:shadow-[0_18px_42px_rgba(31,111,95,0.12)]">
      <p className={`${text ? "text-base" : "text-3xl"} font-extrabold text-[#18332D]`}>
        {value}
      </p>
      <p className="mt-2 text-sm font-semibold text-slate-500">{label}</p>
    </div>
  );
}
