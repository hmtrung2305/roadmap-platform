export default function PortfolioAbout({ portfolio }) {
  return (
    <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
      <div className="flex items-center gap-3">
        <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
          About
        </p>
      </div>

      <p className="mt-4 max-w-4xl whitespace-pre-line text-sm font-semibold leading-7 text-slate-700">
        {portfolio?.bio || "No profile description has been added yet."}
      </p>
    </section>
  );
}
