export default function PortfolioEmptyState() {
  return (
    <section className="rounded-2xl border border-dashed border-[#B9D8CC] bg-white p-8 text-center shadow-sm">
      <h3 className="text-lg font-extrabold text-[#18332D]">No projects selected yet</h3>
      <p className="mx-auto mt-2 max-w-md text-sm font-semibold leading-6 text-slate-600">
        Selected repositories will appear here as public portfolio projects.
      </p>
    </section>
  );
}
