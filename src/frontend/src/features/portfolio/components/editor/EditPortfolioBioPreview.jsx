import { Sparkles } from "lucide-react";
import { getPortfolioBio } from "../../utils/portfolioEditUtils";

export default function EditPortfolioBioPreview({ portfolio }) {
  const bio = getPortfolioBio(portfolio);

  return (
    <section className="rounded-2xl border border-[#B9D8CC]/75 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-[#6F837C]">
            Bio preview
          </p>
          <p className="portfolio-editor-clamp-2 mt-2 text-sm font-semibold leading-6 text-[#34544C]">
            {bio}
          </p>
        </div>
        <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-[#EAF8F1] px-3 py-1 text-xs font-bold text-[#1F6F5F] ring-1 ring-[#B9D8CC]/75">
          <Sparkles size={14} /> Public bio
        </span>
      </div>
    </section>
  );
}
