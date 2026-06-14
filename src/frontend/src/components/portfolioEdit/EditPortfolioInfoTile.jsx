export default function EditPortfolioInfoTile({ icon, label, value, helper }) {
  return (
    <article className="tm-surface tm-surface-hover p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-[#667A73]">{label}</p>
          <p className="portfolio-editor-nowrap mt-2 text-xl font-bold text-[#18332D]">{value}</p>
        </div>
        {icon && (
          <span className="grid size-10 place-items-center rounded-lg bg-[#6FCF97]/18 text-[#1F6F5F]">
            {icon}
          </span>
        )}
      </div>
      <p className="mt-3 text-xs font-medium leading-5 text-[#667A73]">{helper}</p>
    </article>
  );
}
