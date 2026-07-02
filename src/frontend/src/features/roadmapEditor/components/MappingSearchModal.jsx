import { useEffect } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Loader2, Search, X } from "lucide-react";

import {
  ModuleButton,
  ModuleEmptyState,
} from "../../learningModules/components/learningModuleUi";

export default function MappingSearchModal({
  isOpen,
  title,
  searchValue,
  setSearchValue,
  isSearching,
  onSearch,
  items,
  getId,
  getTitle,
  getSubtitle,
  onChoose,
  onClose,
  emptyTitle = "No results",
  emptyBody = null,
  emptyActionLabel = "",
  onEmptyAction,
}) {
  useEffect(() => {
    if (!isOpen) return undefined;

    const handleKeyDown = (event) => {
      if (event.key === "Escape") onClose();
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm"
          role="presentation"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.16 }}
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) onClose();
          }}
        >
          <motion.div
            className="flex h-[560px] w-full max-w-2xl flex-col rounded-xl border border-[#B9D8CC] bg-white shadow-2xl"
            role="dialog"
            aria-modal="true"
            aria-label={title}
            initial={{ opacity: 0, scale: 0.97, y: 12 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.97, y: 12 }}
            transition={{ duration: 0.16 }}
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
              <h2 className="text-base font-extrabold text-[#18332D]">{title}</h2>
              <button
                type="button"
                onClick={onClose}
                className="inline-grid h-8 w-8 place-items-center rounded-lg text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                aria-label="Close"
              >
                <X size={17} />
              </button>
            </div>

            <div className="border-b border-[#B9D8CC]/70 p-4">
              <div className="flex gap-2">
                <label className="relative block min-w-0 flex-1">
                  <span className="sr-only">Search</span>
                  <Search
                    size={16}
                    className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
                  />
                  <input
                    type="search"
                    value={searchValue}
                    onChange={(event) => setSearchValue(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") onSearch();
                    }}
                    placeholder="Search existing items"
                    className="h-10 w-full rounded-lg border border-[#B9D8CC] bg-white pl-9 pr-3 text-sm font-semibold text-[#18332D] outline-none transition placeholder:text-slate-400 focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
                  />
                </label>
                <ModuleButton variant="secondary" onClick={onSearch} disabled={isSearching}>
                  {isSearching ? <Loader2 size={14} className="animate-spin" /> : <Search size={14} />}
                  Search
                </ModuleButton>
              </div>
            </div>

            <div className="min-h-0 flex-1 overflow-y-auto p-4">
              {items.length === 0 ? (
                <ModuleEmptyState title={emptyTitle}>
                  <div className="space-y-3">
                    {emptyBody && <div>{emptyBody}</div>}
                    {emptyActionLabel && onEmptyAction && (
                      <ModuleButton variant="primary" onClick={onEmptyAction}>
                        {emptyActionLabel}
                      </ModuleButton>
                    )}
                  </div>
                </ModuleEmptyState>
              ) : (
                <div className="space-y-2">
                  {items.map((item) => (
                    <button
                      key={getId(item)}
                      type="button"
                      onClick={() => onChoose(getId(item))}
                      className="block w-full rounded-lg border border-[#B9D8CC]/70 bg-white px-3 py-2 text-left transition hover:border-[#2FA084] hover:bg-[#F7F1E8]"
                    >
                      <div className="text-sm font-bold text-[#18332D]">{getTitle(item)}</div>
                      {getSubtitle && (
                        <div className="mt-0.5 text-xs font-semibold text-slate-500">
                          {getSubtitle(item)}
                        </div>
                      )}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="flex justify-end border-t border-[#B9D8CC]/70 p-4">
              <ModuleButton variant="secondary" onClick={onClose}>Close</ModuleButton>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
