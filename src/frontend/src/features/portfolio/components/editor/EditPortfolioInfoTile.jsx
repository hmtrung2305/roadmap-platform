export default function EditPortfolioInfoTile({
  icon,
  label,
  value,
  helper,
  onClick,
  actionLabel,
}) {
  const TileTag = onClick ? "button" : "article";

  return (
    <TileTag
      type={onClick ? "button" : undefined}
      onClick={onClick}
      className="group rounded-2xl border border-[#B9D8CC]/75 bg-white p-3 text-left shadow-sm transition duration-200 hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md
      "
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-[11px] font-bold uppercase tracking-[0.14em] text-[#6F837C]">
            {label}
          </p>
          <p className="portfolio-editor-nowrap mt-1.5 text-lg font-bold text-[#18332D]">
            {value}
          </p>
        </div>
        {icon && (
          <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-[#EAF8F1] text-[#1F6F5F] ring-1 ring-[#D8EAE2] transition group-hover:bg-[#E1F4EC]">
            {icon}
          </span>
        )}
      </div>
      <p className="mt-2.5 text-[11px] font-medium leading-4 text-[#667A73]">
        {helper}
      </p>
      {actionLabel && (
        <p className="mt-2 text-[11px] font-bold text-[#1F6F5F] opacity-80 transition group-hover:opacity-100">
          {actionLabel}
        </p>
      )}
    </TileTag>
  );
}
