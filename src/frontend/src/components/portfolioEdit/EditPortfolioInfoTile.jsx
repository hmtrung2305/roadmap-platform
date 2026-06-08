export default function EditPortfolioInfoTile({ icon, label, value, helper }) {
  return (
    <article className="rounded-lg border border-[#B9D8CC] bg-white p-4 shadow-[0_12px_30px_rgba(31,111,95,0.06)] transition hover:-translate-y-0.5 hover:shadow-[0_18px_38px_rgba(31,111,95,0.10)]">
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
