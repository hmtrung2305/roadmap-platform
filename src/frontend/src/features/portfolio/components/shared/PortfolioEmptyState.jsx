export default function PortfolioEmptyState() {
  return (
    <section className="rounded-2xl border border-dashed border-[#B9D8CC]/75 bg-white p-8 text-center shadow-[0_8px_18px_rgba(31,111,95,0.05)] transition duration-200 hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md">
      <h3 className="text-lg font-extrabold text-[#18332D]">No projects selected yet</h3>
      <p className="mx-auto mt-2 max-w-md text-sm font-semibold leading-6 text-slate-600">
        Selected repositories will appear here as public portfolio projects.
      </p>
    </section>
  );
}
