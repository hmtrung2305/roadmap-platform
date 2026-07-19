import { ChevronDown, Filter, RotateCcw, X } from "lucide-react";
import { useEffect, useState } from "react";
import AppSelect from "../../../components/common/AppSelect";
import { PERIOD_OPTIONS, toSelectOptions } from "../marketPulseViewModel";

const facetLabels = {
  category: "Category",
  location: "Location",
  seniority: "Seniority",
};

export default function MarketPulseFilters({
  days,
  appliedFilters,
  optionCatalog,
  resetKey,
  onDaysChange,
  onApply,
  onClear,
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [draft, setDraft] = useState(appliedFilters);
  const activeEntries = Object.entries(appliedFilters).filter(([, value]) => value);

  useEffect(() => {
    setDraft(appliedFilters);
    setIsOpen(false);
  }, [appliedFilters, resetKey]);

  const apply = () => {
    onApply(draft);
    setIsOpen(false);
  };

  const cancel = () => {
    setDraft(appliedFilters);
    setIsOpen(false);
  };

  return (
    <section aria-label="Market filters" className="mt-6 rounded-2xl border border-[#B9D8CC] bg-white p-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <div className="text-xs font-extrabold uppercase tracking-[0.1em] text-slate-500">Time range</div>
          <div className="mt-2 flex flex-wrap gap-2" role="group" aria-label="Time range">
            {PERIOD_OPTIONS.map((option) => (
              <button
                key={option}
                type="button"
                aria-pressed={days === option}
                onClick={() => onDaysChange(option)}
                className={`min-h-11 rounded-xl border px-3 text-sm font-extrabold transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] ${
                  days === option
                    ? "border-[#1F6F5F] bg-[#1F6F5F] text-white"
                    : "border-[#B9D8CC] bg-white text-slate-600 hover:border-[#2FA084] hover:bg-[#EAF8F1]"
                }`}
              >
                {option} days
              </button>
            ))}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            aria-expanded={isOpen}
            aria-controls="market-pulse-advanced-filters"
            onClick={() => setIsOpen((current) => !current)}
            className="inline-flex min-h-11 items-center gap-2 rounded-xl border border-[#B9D8CC] bg-white px-4 text-sm font-extrabold text-[#18332D] hover:border-[#2FA084] hover:bg-[#EAF8F1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
          >
            <Filter size={16} aria-hidden="true" />
            Filters
            {activeEntries.length > 0 && (
              <span className="grid h-6 min-w-6 place-items-center rounded-full bg-[#1F6F5F] px-1 text-xs text-white">
                {activeEntries.length}
              </span>
            )}
            <ChevronDown size={16} className={isOpen ? "rotate-180" : ""} aria-hidden="true" />
          </button>
          {(activeEntries.length > 0 || days !== 30) && (
            <button
              type="button"
              onClick={onClear}
              className="inline-flex min-h-11 items-center gap-2 rounded-xl px-3 text-sm font-bold text-slate-600 hover:bg-[#F7F1E8] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
            >
              <RotateCcw size={15} aria-hidden="true" />
              Clear all
            </button>
          )}
        </div>
      </div>

      {isOpen && (
        <div id="market-pulse-advanced-filters" className="mt-4 border-t border-[#DCEBE5] pt-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {Object.entries(facetLabels).map(([key, label]) => (
              <div key={key}>
                <div className="mb-1.5 text-xs font-bold uppercase tracking-wide text-slate-500">{label}</div>
                <AppSelect
                  ariaLabel={label}
                  value={draft[key]}
                  options={toSelectOptions(optionCatalog[key])}
                  onChange={(value) => setDraft((current) => ({ ...current, [key]: value }))}
                  dropdownMode="fixed"
                  buttonClassName="!h-11 min-h-11"
                />
              </div>
            ))}
          </div>
          <div className="mt-4 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={cancel}
              className="min-h-11 rounded-xl px-4 text-sm font-bold text-slate-600 hover:bg-[#F7F1E8] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={apply}
              className="min-h-11 rounded-xl bg-[#1F6F5F] px-5 text-sm font-extrabold text-white hover:bg-[#18594D] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] focus-visible:ring-offset-2"
            >
              Apply filters
            </button>
          </div>
        </div>
      )}

      {activeEntries.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2 border-t border-[#DCEBE5] pt-3" aria-label="Applied filters">
          {activeEntries.map(([key, value]) => (
            <button
              key={key}
              type="button"
              onClick={() => onApply({ ...appliedFilters, [key]: "" })}
              aria-label={`Remove ${facetLabels[key]} filter ${value}`}
              className="inline-flex min-h-11 items-center gap-2 rounded-full bg-[#EAF8F1] px-3 text-xs font-extrabold text-[#1F6F5F] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
            >
              <span>{facetLabels[key]}: {value}</span>
              <X size={14} aria-hidden="true" />
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
