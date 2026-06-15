import { useEffect, useMemo, useRef, useState } from "react";
import { Search, X } from "lucide-react";
import { counselorLearningModuleApi } from "../../api/learningModuleApi";
import { inputClass } from "./learningModuleUi";

export default function SkillSearchPicker({
  value,
  initialSkill = null,
  onChange,
  placeholder = "Search skills",
  disabled = false,
}) {
  const [selectedSkill, setSelectedSkill] = useState(initialSkill);
  const [query, setQuery] = useState(initialSkill?.name || "");
  const [results, setResults] = useState([]);
  const [isSearching, setIsSearching] = useState(!initialSkill && !value);
  const [isLoading, setIsLoading] = useState(false);
  const searchRef = useRef(null);

  useEffect(() => {
    if (initialSkill) {
      setSelectedSkill(initialSkill);
      setQuery(initialSkill.name || "");
      setIsSearching(false);
    }
  }, [initialSkill]);

  useEffect(() => {
    if (isSearching) {
      window.setTimeout(() => searchRef.current?.focus(), 0);
    }
  }, [isSearching]);

  useEffect(() => {
    let ignore = false;
    const term = query.trim();

    if (!isSearching || term.length < 2) {
      setResults([]);
      return undefined;
    }

    async function searchSkills() {
      try {
        setIsLoading(true);
        const data = await counselorLearningModuleApi.searchSkills(term);
        if (!ignore) setResults(data);
      } catch {
        if (!ignore) setResults([]);
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    const timeoutId = window.setTimeout(searchSkills, 250);

    return () => {
      ignore = true;
      window.clearTimeout(timeoutId);
    };
  }, [query, isSearching]);

  const hasSelectedSkill = Boolean(selectedSkill || value);

  const selectedLabel = useMemo(() => {
    if (selectedSkill?.name) return selectedSkill.name;
    return value ? "Selected skill" : "";
  }, [selectedSkill, value]);

  const toggleSearch = () => {
    if (disabled) return;

    setIsSearching((current) => {
      const next = !current;
      if (next && selectedSkill?.name) setQuery(selectedSkill.name);
      return next;
    });
  };

  const chooseSkill = (skill) => {
    setSelectedSkill(skill);
    setQuery(skill.name || "");
    setIsSearching(false);
    onChange?.(skill.skillId, skill);
  };

  const clearSkill = () => {
    if (disabled) return;

    setSelectedSkill(null);
    setQuery("");
    setResults([]);
    setIsSearching(true);
    onChange?.("", null);
  };

  return (
    <div className="space-y-3">
      {hasSelectedSkill && (
        <div className="rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/35 p-3">
          <div className="flex items-center justify-between gap-3">
            <button
              type="button"
              onClick={toggleSearch}
              disabled={disabled}
              className="min-w-0 flex-1 rounded-md px-1 py-1 text-left transition hover:bg-white/60 disabled:pointer-events-none disabled:opacity-70"
            >
              <div className="text-[11px] font-extrabold uppercase tracking-[0.14em] text-[#1F6F5F]">
                Selected skill
              </div>
              <div className="mt-0.5 truncate text-sm font-extrabold text-[#18332D]">
                {selectedLabel}
              </div>
            </button>

            <div className="flex shrink-0 items-center gap-1.5">
              <button
                type="button"
                onClick={toggleSearch}
                disabled={disabled}
                className="inline-flex h-8 items-center justify-center rounded-md border border-[#B9D8CC] bg-white px-2.5 text-xs font-extrabold leading-none text-[#1F6F5F] transition hover:bg-[#F7F1E8] disabled:pointer-events-none disabled:opacity-60"
              >
                {isSearching ? "Close" : "Change"}
              </button>

              <button
                type="button"
                onClick={clearSkill}
                disabled={disabled}
                className="grid h-8 w-8 place-items-center rounded-md border border-slate-200 bg-white text-slate-500 transition hover:bg-slate-100 hover:text-slate-800 disabled:pointer-events-none disabled:opacity-60"
                aria-label="Clear selected skill"
              >
                <X size={14} />
              </button>
            </div>
          </div>
        </div>
      )}

      {isSearching && (
        <div className="relative z-30">
          <label className="relative block">
            <Search
              size={16}
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-500"
            />
            <input
              ref={searchRef}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={placeholder}
              disabled={disabled}
              className={`${inputClass} pl-9`}
            />
          </label>

          <div className="absolute left-0 right-0 top-[calc(100%+6px)] z-40 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white shadow-lg">
            {query.trim().length < 2 ? (
              <div className="px-3 py-3 text-sm font-semibold text-slate-500">
                Type at least 2 characters to search.
              </div>
            ) : isLoading ? (
              <div className="px-3 py-3 text-sm font-semibold text-slate-500">
                Searching...
              </div>
            ) : results.length === 0 ? (
              <div className="px-3 py-3 text-sm font-semibold text-slate-500">
                No matching skills found.
              </div>
            ) : (
              <div className="max-h-64 overflow-y-auto">
                {results.map((skill) => (
                  <button
                    key={skill.skillId}
                    type="button"
                    onClick={() => chooseSkill(skill)}
                    className="flex w-full items-start justify-between gap-3 border-b border-[#B9D8CC]/60 px-3 py-3 text-left transition last:border-b-0 hover:bg-[#F7F1E8]"
                  >
                    <div className="min-w-0">
                      <div className="truncate text-sm font-extrabold text-[#18332D]">
                        {skill.name}
                      </div>
                      {skill.category && (
                        <div className="mt-0.5 text-xs font-semibold text-slate-500">
                          {skill.category}
                        </div>
                      )}
                    </div>

                    {String(value) === String(skill.skillId) && (
                      <span className="rounded-full bg-[#6FCF97]/18 px-2 py-0.5 text-[11px] font-extrabold text-[#1F6F5F]">
                        Selected
                      </span>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {!hasSelectedSkill && !isSearching && (
        <button
          type="button"
          onClick={toggleSearch}
          disabled={disabled}
          className="w-full rounded-lg border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/35 px-4 py-4 text-left text-sm font-bold text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:pointer-events-none disabled:opacity-60"
        >
          Choose a skill
        </button>
      )}
    </div>
  );
}
