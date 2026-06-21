export default function EditPortfolioInfoTile({ icon, label, value, helper, onClick, actionLabel }) {
  const TileTag = onClick ? "button" : "article";

  return (
    <TileTag
      type={onClick ? "button" : undefined}
      onClick={onClick}
      className={`group rounded-2xl border border-[#C9DCD4]/70 bg-white/80 p-4 text-left shadow-[0_10px_24px_rgba(31,111,95,0.035)] transition duration-200 ${
        onClick
          ? "cursor-pointer hover:-translate-y-0.5 hover:border-[#AFCFC3] hover:bg-white hover:shadow-[0_14px_30px_rgba(31,111,95,0.07)]"
          : ""
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-[#6F837C]">{label}</p>
          <p className="portfolio-editor-nowrap mt-2 text-xl font-bold text-[#18332D]">{value}</p>
        </div>
        {icon && (
          <span className="grid size-10 place-items-center rounded-xl bg-[#EAF8F1] text-[#1F6F5F] ring-1 ring-[#D8EAE2] transition group-hover:bg-[#E1F4EC]">
            {icon}
          </span>
        )}
      </div>
      <p className="mt-3 text-xs font-medium leading-5 text-[#667A73]">{helper}</p>
      {actionLabel && (
        <p className="mt-2 text-xs font-bold text-[#1F6F5F] opacity-80 transition group-hover:opacity-100">
          {actionLabel}
        </p>
      )}
    </TileTag>
  );
}
