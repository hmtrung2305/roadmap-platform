import { MAX_SHOWCASE_REPOSITORIES } from "../../constants/portfolioLimits";

export default function EditPortfolioStatusBar({
  selectedCount,
  availableCount,
}) {
  return (
    <section className="rounded-[1.35rem] border border-[#B9D8CC] bg-white/95 px-4 py-3 shadow-[0_10px_26px_rgba(31,111,95,0.06)]">
      <div className="flex flex-wrap items-center gap-2 text-sm font-semibold text-[#34544C]">
        <span className="h-2 w-2 rounded-full bg-[#6FCF97]" />
        {selectedCount}/{MAX_SHOWCASE_REPOSITORIES} featured repositories
        selected.
        {availableCount > 0 ? ` ${availableCount} repositories available.` : ""}
      </div>
    </section>
  );
}
