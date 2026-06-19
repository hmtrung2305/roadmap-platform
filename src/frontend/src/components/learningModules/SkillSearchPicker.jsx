import { useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { Check, Search, X } from "lucide-react";
import { skillApi } from "../../api/skillApi";
import { inputClass } from "./learningModuleUi";

function normalizeCareerRoles(skill) {
  return Array.isArray(skill?.careerRoles)
    ? skill.careerRoles.filter(Boolean)
    : [];
}

function formatCareerRoles(skill) {
  const roles = normalizeCareerRoles(skill);

  if (roles.length === 0) {
    return "No published roadmap usage";
  }

  const visibleRoles = roles.slice(0, 3);
  const remainingCount = roles.length - visibleRoles.length;

  return remainingCount > 0
    ? `${visibleRoles.join(", ")} +${remainingCount} more`
    : visibleRoles.join(", ");
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function HighlightedText({ text, query }) {
  const value = String(text ?? "");
  const term = query.trim();

  if (!term) {
    return value;
  }

  const expression = new RegExp(`(${escapeRegExp(term)})`, "ig");
  const normalizedTerm = term.toLocaleLowerCase();

  return value.split(expression).map((part, index) => {
    const isMatch = part.toLocaleLowerCase() === normalizedTerm;

    return isMatch ? (
      <mark
        key={`${part}-${index}`}
        className="rounded-sm bg-[#F4D06F]/45 px-0.5 text-inherit"
      >
        {part}
      </mark>
    ) : (
      part
    );
  });
}

function SkillDetails({ skill, query = "", compact = false }) {
  return (
    <div className={`space-y-1 ${compact ? "mt-1.5" : "mt-2"}`}>
      <p className="text-xs font-semibold text-slate-600">
        <span className="font-extrabold text-slate-500">Category:</span>{" "}
        <HighlightedText
          text={skill?.category || "Uncategorized"}
          query={query}
        />
      </p>
      <p className="text-xs font-semibold text-slate-600">
        <span className="font-extrabold text-slate-500">Used in:</span>{" "}
        <HighlightedText text={formatCareerRoles(skill)} query={query} />
      </p>
    </div>
  );
}

export default function SkillSearchPicker({
  value,
  initialSkill = null,
  onChange,
  placeholder = "Search skills",
  disabled = false,
}) {
  const [selectedSkill, setSelectedSkill] = useState(initialSkill);
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [suggestions, setSuggestions] = useState([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isSuggestionsLoading, setIsSuggestionsLoading] = useState(false);
  const [searchFailed, setSearchFailed] = useState(false);
  const [suggestionsFailed, setSuggestionsFailed] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);

  const searchRef = useRef(null);
  const previousFocusRef = useRef(null);

  const normalizedQuery = query.trim();
  const isSearching = normalizedQuery.length > 0;
  const displayedSkills = isSearching ? results : suggestions;
  const displayedLoading = isSearching ? isLoading : isSuggestionsLoading;
  const displayedFailed = isSearching ? searchFailed : suggestionsFailed;

  useEffect(() => {
    let ignore = false;

    if (!value) {
      setSelectedSkill(null);
      return undefined;
    }

    if (initialSkill && String(initialSkill.skillId) === String(value)) {
      setSelectedSkill(initialSkill);

      if (Array.isArray(initialSkill.careerRoles)) {
        return undefined;
      }
    }

    async function loadSelectedSkill() {
      try {
        const skill = await skillApi.getSkillById(value);
        if (!ignore) {
          setSelectedSkill(skill);
        }
      } catch {
        // Preserve the module-provided skill details when enrichment is unavailable.
      }
    }

    loadSelectedSkill();

    return () => {
      ignore = true;
    };
  }, [initialSkill, value]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    window.setTimeout(() => searchRef.current?.focus(), 0);

    const handleKeyDown = (event) => {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    };

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus?.();
    };
  }, [isOpen]);

  useEffect(() => {
    let ignore = false;

    if (!isOpen) {
      return undefined;
    }

    async function loadSuggestions() {
      try {
        setIsSuggestionsLoading(true);
        setSuggestionsFailed(false);

        const items = await skillApi.getSuggestions({ limit: 6 });

        if (!ignore) {
          setSuggestions(items);
          if (!query.trim()) {
            setActiveIndex(items.length > 0 ? 0 : -1);
          }
        }
      } catch {
        if (!ignore) {
          setSuggestions([]);
          setSuggestionsFailed(true);
          if (!query.trim()) {
            setActiveIndex(-1);
          }
        }
      } finally {
        if (!ignore) {
          setIsSuggestionsLoading(false);
        }
      }
    }

    loadSuggestions();

    return () => {
      ignore = true;
    };
  }, [isOpen]);

  useEffect(() => {
    let ignore = false;
    const term = query.trim();

    if (!isOpen || term.length === 0) {
      setResults([]);
      setIsLoading(false);
      setSearchFailed(false);
      setActiveIndex(suggestions.length > 0 ? 0 : -1);
      return undefined;
    }

    async function searchSkills() {
      try {
        setIsLoading(true);
        setSearchFailed(false);

        const result = await skillApi.searchSkills({
          search: term,
          limit: 20,
        });

        if (!ignore) {
          setResults(result.items);
          setActiveIndex(result.items.length > 0 ? 0 : -1);
        }
      } catch {
        if (!ignore) {
          setResults([]);
          setSearchFailed(true);
          setActiveIndex(-1);
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
        }
      }
    }

    const timeoutId = window.setTimeout(searchSkills, 250);

    return () => {
      ignore = true;
      window.clearTimeout(timeoutId);
    };
  }, [isOpen, query, suggestions.length]);

  const hasSelectedSkill = Boolean(selectedSkill || value);

  const selectedLabel = useMemo(() => {
    if (selectedSkill?.name) return selectedSkill.name;
    return value ? "Selected skill" : "";
  }, [selectedSkill, value]);

  const openDialog = () => {
    if (disabled) return;

    previousFocusRef.current = document.activeElement;
    setQuery("");
    setResults([]);
    setSearchFailed(false);
    setSuggestionsFailed(false);
    setActiveIndex(-1);
    setIsOpen(true);
  };

  const closeDialog = () => {
    setIsOpen(false);
  };

  const chooseSkill = (skill) => {
    setSelectedSkill(skill);
    setIsOpen(false);
    onChange?.(skill.skillId, skill);
  };

  const handleSearchKeyDown = (event) => {
    if (displayedSkills.length === 0) return;

    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((current) => (current + 1) % displayedSkills.length);
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((current) =>
        current <= 0 ? displayedSkills.length - 1 : current - 1,
      );
      return;
    }

    if (event.key === "Enter" && activeIndex >= 0) {
      event.preventDefault();
      chooseSkill(displayedSkills[activeIndex]);
    }
  };

  const dialog = isOpen && typeof document !== "undefined"
    ? createPortal(
        <div
          className="fixed inset-0 z-[100] grid place-items-center bg-[#18332D]/40 px-4 py-6 backdrop-blur-sm"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              closeDialog();
            }
          }}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="skill-picker-title"
            className="flex h-[calc(100vh-3rem)] max-h-[680px] w-full max-w-2xl flex-col overflow-hidden rounded-2xl border border-[#B9D8CC] bg-white shadow-2xl"
          >
            <div className="flex items-center justify-between border-b border-[#B9D8CC] px-5 py-4">
              <h2 id="skill-picker-title" className="text-lg font-extrabold text-[#18332D]">
                Choose skill
              </h2>
              <button
                type="button"
                onClick={closeDialog}
                className="grid h-9 w-9 place-items-center rounded-lg text-slate-500 transition hover:bg-slate-100 hover:text-slate-800"
                aria-label="Close skill picker"
              >
                <X size={18} />
              </button>
            </div>

            <div className="border-b border-[#B9D8CC] p-5">
              <label className="relative block">
                <Search
                  size={17}
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
                />
                <input
                  ref={searchRef}
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  onKeyDown={handleSearchKeyDown}
                  placeholder={placeholder}
                  className={`${inputClass} pl-10`}
                  role="combobox"
                  aria-expanded="true"
                  aria-controls="skill-picker-results"
                  aria-activedescendant={
                    activeIndex >= 0 && displayedSkills[activeIndex]
                      ? `skill-option-${displayedSkills[activeIndex].skillId}`
                      : undefined
                  }
                />
              </label>
            </div>

            <div className="flex min-h-0 flex-1 flex-col">
              <div className="flex h-11 shrink-0 items-center justify-between border-b border-[#B9D8CC]/70 px-5">
                <p className="text-xs font-extrabold uppercase tracking-[0.12em] text-slate-500">
                  {isSearching ? "Search results" : "Suggested skills"}
                </p>
                {!displayedLoading && !displayedFailed && displayedSkills.length > 0 && (
                  <span className="text-xs font-bold text-slate-400">
                    {displayedSkills.length}
                  </span>
                )}
              </div>

              <div
                id="skill-picker-results"
                role="listbox"
                className="min-h-0 flex-1 overflow-y-auto [scrollbar-gutter:stable]"
              >
                {displayedLoading ? (
                  <div className="grid h-full place-items-center px-6 text-sm font-semibold text-slate-500">
                    Loading...
                  </div>
                ) : displayedFailed ? (
                  <div className="grid h-full place-items-center px-6 text-sm font-semibold text-rose-700">
                    Unable to load skills.
                  </div>
                ) : displayedSkills.length === 0 ? (
                  <div className="grid h-full place-items-center px-6 text-center">
                    <div>
                      <p className="text-sm font-extrabold text-[#18332D]">
                        {isSearching ? "No skills found" : "Search for a skill"}
                      </p>
                    </div>
                  </div>
                ) : (
                  displayedSkills.map((skill, index) => {
                    const isSelected = String(value) === String(skill.skillId);
                    const isActive = index === activeIndex;

                    return (
                      <button
                        id={`skill-option-${skill.skillId}`}
                        key={skill.skillId}
                        type="button"
                        role="option"
                        aria-selected={isSelected}
                        onMouseEnter={() => setActiveIndex(index)}
                        onClick={() => chooseSkill(skill)}
                        className={`w-full border-b border-[#B9D8CC]/60 px-5 py-4 text-left transition last:border-b-0 ${
                          isSelected
                            ? "bg-[#6FCF97]/12"
                            : isActive
                              ? "bg-[#F7F1E8]"
                              : "hover:bg-[#F7F1E8]"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div className="min-w-0 flex-1">
                            <div className="truncate text-sm font-extrabold text-[#18332D]">
                              <HighlightedText
                                text={skill.name}
                                query={normalizedQuery}
                              />
                            </div>
                            <SkillDetails
                              skill={skill}
                              query={normalizedQuery}
                              compact
                            />
                          </div>

                          {isSelected && (
                            <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-[#6FCF97]/20 px-2 py-0.5 text-[11px] font-extrabold text-[#1F6F5F]">
                              <Check size={12} /> Selected
                            </span>
                          )}
                        </div>
                      </button>
                    );
                  })
                )}
              </div>
            </div>

            <p className="sr-only" aria-live="polite">
              {displayedLoading
                ? "Loading skills"
                : `${displayedSkills.length} skills available`}
            </p>

            <div className="flex justify-end border-t border-[#B9D8CC] px-5 py-4">
              <button
                type="button"
                onClick={closeDialog}
                className="inline-flex h-10 items-center justify-center rounded-lg border border-[#B9D8CC] bg-white px-4 text-sm font-extrabold text-[#1F6F5F] transition hover:bg-[#F7F1E8]"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>,
        document.body,
      )
    : null;

  return (
    <div className="space-y-3">
      {hasSelectedSkill ? (
        <div className="rounded-xl border border-[#B9D8CC] bg-[#F7F1E8]/35 p-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0 flex-1">
              <div className="text-[11px] font-extrabold uppercase tracking-[0.14em] text-[#1F6F5F]">
                Selected skill
              </div>
              <div className="mt-1 truncate text-base font-extrabold text-[#18332D]">
                {selectedLabel}
              </div>
              <SkillDetails skill={selectedSkill} />
            </div>

            <button
              type="button"
              onClick={openDialog}
              disabled={disabled}
              className="inline-flex h-9 shrink-0 items-center justify-center rounded-lg border border-[#B9D8CC] bg-white px-3 text-xs font-extrabold text-[#1F6F5F] transition hover:bg-[#F7F1E8] disabled:pointer-events-none disabled:opacity-60"
            >
              Change
            </button>
          </div>
        </div>
      ) : (
        <button
          type="button"
          onClick={openDialog}
          disabled={disabled}
          className="w-full rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/35 px-4 py-4 text-left text-sm font-bold text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:pointer-events-none disabled:opacity-60"
        >
          Choose a skill
        </button>
      )}

      {dialog}
    </div>
  );
}
