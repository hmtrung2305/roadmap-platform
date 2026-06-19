import { useState } from "react";
import { CheckCircle2, ChevronDown } from "lucide-react";

export function DirtyStateBadge({ isDirty, label = "Unsaved changes" }) {
  if (!isDirty) return null;

  return (
    <span className="inline-flex items-center gap-1 rounded-md border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-extrabold uppercase tracking-[0.12em] text-amber-700">
      <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
      {label}
    </span>
  );
}

export function CustomSelect({ value, onChange, options, placeholder = "Select an option" }) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedOption = options.find((option) => String(option.value) === String(value));

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="flex h-10 w-full cursor-pointer items-center justify-between gap-3 rounded-lg border border-[#B9D8CC] bg-white px-3 text-left text-sm font-semibold text-[#18332D] outline-none transition hover:border-[#2FA084] focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
      >
        <span className={selectedOption ? "truncate" : "truncate text-slate-400"}>
          {selectedOption?.label || placeholder}
        </span>
        <ChevronDown
          size={16}
          className={`shrink-0 text-[#1F6F5F] transition ${isOpen ? "rotate-180" : ""}`}
        />
      </button>

      {isOpen && (
        <div className="absolute left-0 right-0 top-[calc(100%+6px)] z-30 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 shadow-lg">
          {options.map((option) => {
            const isSelected = String(option.value) === String(value);

            return (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  onChange(option.value);
                  setIsOpen(false);
                }}
                className={`flex w-full cursor-pointer items-center justify-between gap-3 px-3 py-2 text-left text-sm font-bold transition ${
                  isSelected
                    ? "bg-[#6FCF97]/18 text-[#1F6F5F]"
                    : "text-slate-700 hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                }`}
              >
                <span>{option.label}</span>
                {isSelected && <CheckCircle2 size={15} className="text-[#1F6F5F]" />}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
