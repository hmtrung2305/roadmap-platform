import { Sparkles } from "lucide-react";
import { getPortfolioBio } from "./portfolioEditUtils";

export default function EditPortfolioBioPreview({ portfolio }) {
  const bio = getPortfolioBio(portfolio);

  return (
    <div className="mt-5 rounded-lg border border-[#DCEBE5] bg-[#F7F1E8]/55 p-4">
      <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-[#2FA084]">Bio preview</p>
          <p className="portfolio-editor-clamp-2 mt-2 text-sm font-semibold leading-6 text-[#34544C]">{bio}</p>
        </div>
        <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-white px-3 py-1 text-xs font-bold text-[#1F6F5F]">
          <Sparkles size={14} /> Public bio
        </span>
      </div>
    </div>
  );
}
