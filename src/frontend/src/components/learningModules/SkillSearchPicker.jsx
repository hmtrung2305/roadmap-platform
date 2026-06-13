import { useEffect, useMemo, useRef, useState } from "react";
import { Check, Loader2, Search, X } from "lucide-react";
import { skillApi } from "../../api/skillApi";
import { inputClass, ModuleBadge, ModuleButton } from "./learningModuleUi";

function useDebouncedValue(value, delay = 250) {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(timeoutId);
  }, [value, delay]);

  return debounced;
}

export default function SkillSearchPicker({
  value,
  onChange,
  initialSkill = null,
  disabled = false,
  placeholder = "Search skills",
}) {
  const containerRef = useRef(null);
  const [searchText, setSearchText] = useState("");
  const [selectedSkill, setSelectedSkill] = useState(initialSkill);
  const [results, setResults] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const debouncedSearch = useDebouncedValue(searchText);

  useEffect(() => {
    if (initialSkill?.skillId && initialSkill.skillId === value) {
      setSelectedSkill(initialSkill);
    }
  }, [initialSkill, value]);

  useEffect(() => {
    if (disabled) return;

    let ignore = false;

    async function loadSkills() {
      try {
        setIsLoading(true);
        const data = await skillApi.searchSkills({
          search: debouncedSearch.trim(),
          limit: 20,
          offset: 0,
        });

        if (!ignore) {
          setResults(data.items);
          setTotalCount(data.totalCount);
        }
      } catch {
        if (!ignore) {
          setResults([]);
          setTotalCount(0);
        }
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    loadSkills();

    return () => {
      ignore = true;
    };
  }, [debouncedSearch, disabled]);

  useEffect(() => {
    function handleClickOutside(event) {
      if (!containerRef.current?.contains(event.target)) {
        setIsOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const normalizedValue = useMemo(() => value || "", [value]);

  const selectSkill = (skill) => {
    setSelectedSkill(skill);
    setSearchText("");
    setIsOpen(false);
    onChange(skill.skillId, skill);
  };

  const clearSkill = () => {
    setSelectedSkill(null);
    setSearchText("");
    setIsOpen(false);
    onChange("", null);
  };

  const openSearch = () => {
    if (disabled) return;
    setSearchText("");
    setIsOpen(true);
  };

  return (
    <div ref={containerRef} className="relative space-y-2">
      {selectedSkill && normalizedValue ? (
        <div className="rounded-xl border border-[#B9D8CC] bg-[#6FCF97]/10 p-3">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <span className="grid h-6 w-6 place-items-center rounded-full bg-[#1F6F5F] text-white">
                  <Check size={13} />
                </span>
                <span className="truncate text-sm font-extrabold text-[#18332D]">
                  {selectedSkill.name}
                </span>
                {selectedSkill.category && (
                  <ModuleBadge tone="slate">{selectedSkill.category}</ModuleBadge>
                )}
              </div>

              <div className="mt-1 text-xs font-semibold text-slate-500">
                {selectedSkill.slug}
              </div>

              {selectedSkill.description && (
                <p className="mt-2 line-clamp-2 text-xs font-medium leading-5 text-slate-600">
                  {selectedSkill.description}
                </p>
              )}
            </div>

            <div className="flex shrink-0 items-center gap-2">
              <ModuleButton type="button" variant="secondary" onClick={openSearch} disabled={disabled}>
                Change
              </ModuleButton>
              <button
                type="button"
                onClick={clearSkill}
                disabled={disabled}
                className="grid h-8 w-8 place-items-center rounded-lg border border-slate-200 bg-white text-slate-500 hover:bg-slate-100 hover:text-slate-800 disabled:pointer-events-none disabled:opacity-60"
                aria-label="Clear selected skill"
              >
                <X size={15} />
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {(!selectedSkill || isOpen) && (
        <div className="relative">
          <Search
            size={15}
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
          />
          <input
            value={searchText}
            disabled={disabled}
            onFocus={() => setIsOpen(true)}
            onChange={(event) => {
              setSearchText(event.target.value);
              setIsOpen(true);
            }}
            className={`${inputClass} pl-9 pr-10`}
            placeholder={placeholder}
          />
          {isLoading ? (
            <Loader2
              size={15}
              className="absolute right-3 top-1/2 -translate-y-1/2 animate-spin text-slate-500"
            />
          ) : searchText ? (
            <button
              type="button"
              onClick={() => setSearchText("")}
              disabled={disabled}
              className="absolute right-2 top-1/2 grid h-7 w-7 -translate-y-1/2 place-items-center rounded-md text-slate-500 hover:bg-slate-100 hover:text-slate-800 disabled:pointer-events-none"
              aria-label="Clear search"
            >
              <X size={14} />
            </button>
          ) : null}
        </div>
      )}

      {isOpen && !disabled && (
        <div className="absolute z-30 mt-2 max-h-80 w-full overflow-y-auto rounded-xl border border-[#B9D8CC] bg-white p-2 shadow-xl">
          {results.length === 0 && !isLoading ? (
            <div className="px-3 py-6 text-center text-sm font-semibold text-slate-600">
              No skills found.
            </div>
          ) : (
            <div className="space-y-1">
              {results.map((skill) => (
                <button
                  key={skill.skillId}
                  type="button"
                  onClick={() => selectSkill(skill)}
                  className={`w-full rounded-lg px-3 py-2 text-left transition hover:bg-[#F7F1E8] ${
                    skill.skillId === normalizedValue ? "bg-[#6FCF97]/15" : "bg-white"
                  }`}
                >
                  <div className="flex items-center justify-between gap-3">
                    <div className="min-w-0">
                      <div className="truncate text-sm font-extrabold text-[#18332D]">
                        {skill.name}
                      </div>
                      <div className="truncate text-xs font-semibold text-slate-500">
                        {skill.slug}
                      </div>
                    </div>
                    {skill.category && <ModuleBadge tone="slate">{skill.category}</ModuleBadge>}
                  </div>

                  {skill.description && (
                    <p className="mt-1 line-clamp-2 text-xs font-medium leading-5 text-slate-600">
                      {skill.description}
                    </p>
                  )}
                </button>
              ))}
            </div>
          )}

          {totalCount > results.length ? (
            <div className="mt-2 border-t border-slate-100 px-3 pt-2 text-[11px] font-semibold text-slate-500">
              Keep typing to narrow the results.
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
}
