import { useEffect, useId, useMemo, useRef, useState } from "react";
import { Check, ChevronDown } from "lucide-react";

export default function AppSelect({
  value,
  options,
  onChange,
  ariaLabel,
  className = "",
  disabled = false,
}) {
  const listboxId = useId();
  const rootRef = useRef(null);
  const [isOpen, setIsOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);

  const selectedIndex = useMemo(
    () => Math.max(options.findIndex((option) => option.value === value), 0),
    [options, value],
  );
  const selectedOption = options[selectedIndex] || options[0];

  useEffect(() => {
    if (!isOpen) return undefined;

    const handlePointerDown = (event) => {
      if (!rootRef.current?.contains(event.target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    return () => document.removeEventListener("mousedown", handlePointerDown);
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      setActiveIndex(selectedIndex);
    }
  }, [isOpen, selectedIndex]);

  const chooseOption = (option) => {
    if (!option || option.disabled) return;
    onChange(option.value);
    setIsOpen(false);
  };

  const moveActiveOption = (direction) => {
    if (options.length === 0) return;

    let nextIndex = activeIndex;

    do {
      nextIndex = (nextIndex + direction + options.length) % options.length;
    } while (options[nextIndex]?.disabled && nextIndex !== activeIndex);

    setActiveIndex(nextIndex);
  };

  const handleKeyDown = (event) => {
    if (disabled) return;

    if (!isOpen) {
      if (["ArrowDown", "ArrowUp", "Enter", " "].includes(event.key)) {
        event.preventDefault();
        setIsOpen(true);
      }
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      moveActiveOption(1);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      moveActiveOption(-1);
    } else if (event.key === "Home") {
      event.preventDefault();
      setActiveIndex(0);
    } else if (event.key === "End") {
      event.preventDefault();
      setActiveIndex(options.length - 1);
    } else if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      chooseOption(options[activeIndex]);
    } else if (event.key === "Escape") {
      event.preventDefault();
      setIsOpen(false);
    } else if (event.key === "Tab") {
      setIsOpen(false);
    }
  };

  return (
    <div ref={rootRef} className={`relative ${className}`}>
      <button
        type="button"
        aria-label={ariaLabel}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-controls={listboxId}
        disabled={disabled}
        onClick={() => setIsOpen((open) => !open)}
        onKeyDown={handleKeyDown}
        className={`flex h-10 w-full items-center justify-between gap-3 rounded-lg border bg-white px-3 text-left text-sm font-bold text-slate-700 shadow-sm outline-none transition focus:ring-2 focus:ring-[#6FCF97]/25 disabled:cursor-not-allowed disabled:opacity-60 ${
          isOpen
            ? "border-[#2FA084] ring-2 ring-[#6FCF97]/20"
            : "border-[#B9D8CC] hover:border-[#2FA084]"
        }`}
      >
        <span className="truncate">{selectedOption?.label || "Select"}</span>
        <ChevronDown
          size={16}
          className={`shrink-0 text-[#1F6F5F] transition-transform ${
            isOpen ? "rotate-180" : ""
          }`}
        />
      </button>

      {isOpen && (
        <div
          id={listboxId}
          role="listbox"
          aria-label={ariaLabel}
          className="absolute left-0 right-0 top-full z-50 mt-2 overflow-hidden rounded-xl border border-[#B9D8CC] bg-white p-1.5 shadow-[0_18px_45px_rgba(24,51,45,0.16)]"
        >
          {options.map((option, index) => {
            const isSelected = option.value === value;
            const isActive = index === activeIndex;

            return (
              <button
                key={option.value}
                id={`${listboxId}-option-${index}`}
                type="button"
                role="option"
                aria-selected={isSelected}
                disabled={option.disabled}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => chooseOption(option)}
                className={`flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2.5 text-left text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-50 ${
                  isSelected
                    ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
                    : isActive
                      ? "bg-[#F7F1E8] text-[#18332D]"
                      : "text-slate-700 hover:bg-[#F7F1E8] hover:text-[#18332D]"
                }`}
              >
                <span>{option.label}</span>
                {isSelected && <Check size={16} className="shrink-0" />}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
