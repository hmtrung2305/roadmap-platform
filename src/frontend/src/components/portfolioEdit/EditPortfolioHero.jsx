import { Copy, ExternalLink, UserRound } from "lucide-react";
import { Link } from "react-router-dom";

export default function EditPortfolioHero({
  portfolio,
  displayName,
  headline,
  copied = false,
  onCopyPublicLink,
}) {
  return (
    <section className="overflow-hidden rounded-[1.6rem] border border-[#B9D8CC] bg-white shadow-[0_22px_60px_rgba(31,111,95,0.10)]">
      <div className="relative h-44 bg-[#1F6F5F] sm:h-52">
        {portfolio?.coverImageUrl ? (
          <img src={portfolio.coverImageUrl} alt={`${displayName} cover`} className="h-full w-full object-cover" />
        ) : (
          <div className="h-full w-full">
            <div className="h-1/3 bg-[#1F6F5F]" />
            <div className="h-1/3 bg-[#2FA084]" />
            <div className="h-1/3 bg-[#6FCF97]" />
          </div>
        )}
        <div className="absolute inset-0 bg-gradient-to-r from-[#18332D]/15 via-transparent to-white/10" />
      </div>

      <div className="relative px-5 pb-6 pt-0 sm:px-6">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div className="flex min-w-0 flex-col gap-4 sm:flex-row sm:items-end">
            <div className="-mt-12 grid size-24 shrink-0 place-items-center overflow-hidden rounded-lg border-4 border-white bg-[#F7F1E8] text-[#1F6F5F] shadow-[0_12px_28px_rgba(31,111,95,0.16)] sm:size-28">
              {portfolio?.avatarUrl ? (
                <img src={portfolio.avatarUrl} alt={displayName} className="h-full w-full object-cover" />
              ) : (
                <UserRound size={42} />
              )}
            </div>

            <div className="min-w-0 pb-1">
              <p className="mb-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                Public portfolio editor
              </p>
              <h1 className="portfolio-editor-nowrap text-3xl font-bold tracking-tight text-[#18332D] sm:text-4xl">{displayName}</h1>
              <p className="portfolio-editor-nowrap mt-1 max-w-2xl text-sm font-semibold leading-6 text-[#34544C]">{headline}</p>
            </div>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row">
            <button
              type="button"
              onClick={onCopyPublicLink}
              className="inline-flex shrink-0 items-center justify-center gap-1.5 rounded-lg border border-[#B9D8CC] bg-[#EEEEEE] px-4 py-2 !text-[12px] font-bold text-[#18332D] shadow-sm transition-colors hover:bg-[#6FCF97]/35"
            >
              <Copy size={14} />
              {copied ? "Copied" : "Copy public link"}
            </button>

            <Link
              to="/portfolio"
              className="inline-flex shrink-0 items-center justify-center gap-1.5 rounded-lg border border-[#B9D8CC] bg-[#EAF8F1] px-4 py-2 !text-[12px] font-extrabold text-[#1F6F5F] no-underline shadow-sm transition-colors hover:border-[#1F6F5F] hover:bg-[#18332D] hover:text-white"
            >
              Preview portfolio
              <ExternalLink size={14} />
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
