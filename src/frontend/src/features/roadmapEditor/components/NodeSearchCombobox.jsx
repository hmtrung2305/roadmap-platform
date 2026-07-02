import { useEffect, useMemo, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Search } from "lucide-react";

import { normalizeNodes } from "../roadmapEditorUtils";

export default function NodeSearchCombobox({ nodes, onSelect }) {
  const [value, setValue] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const wrapperRef = useRef(null);

  const results = useMemo(() => {
    const query = value.trim().toLowerCase();
    const normalized = normalizeNodes(nodes);

    if (!query) return normalized.slice(0, 24);

    return normalized
      .filter((node) => {
        const haystack = [node.title, node.nodeType, node.description].join(" ").toLowerCase();
        return haystack.includes(query);
      })
      .slice(0, 24);
  }, [nodes, value]);

  useEffect(() => {
    function handlePointerDown(event) {
      if (!wrapperRef.current?.contains(event.target)) {
        setIsOpen(false);
      }
    }

    window.addEventListener("pointerdown", handlePointerDown);
    return () => window.removeEventListener("pointerdown", handlePointerDown);
  }, []);

  const chooseNode = (node) => {
    if (!node) return;

    setValue(node.title || "");
    setIsOpen(false);
    onSelect(node.roadmapNodeId);
  };

  return (
    <div ref={wrapperRef} className="relative w-full sm:w-80">
      <label className="relative block">
        <Search
          size={15}
          className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
        />
        <input
          type="search"
          value={value}
          onFocus={() => setIsOpen(true)}
          onChange={(event) => {
            setValue(event.target.value);
            setIsOpen(true);
          }}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              chooseNode(results[0]);
            }
            if (event.key === "Escape") {
              setIsOpen(false);
            }
          }}
          placeholder="Search nodes"
          className="h-9 w-full rounded-lg border border-[#B9D8CC] bg-white pl-8 pr-3 text-xs font-semibold text-[#18332D] outline-none focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
        />
      </label>

      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 6 }}
            transition={{ duration: 0.14 }}
            className="absolute right-0 top-11 z-30 w-full overflow-hidden rounded-xl border border-[#B9D8CC] bg-white shadow-xl"
          >
            {results.length === 0 ? (
              <div className="px-3 py-3 text-xs font-bold text-slate-500">No matching nodes</div>
            ) : (
              <div className="max-h-[260px] overflow-y-auto pr-1 [scrollbar-color:#9DCDBC_transparent] [scrollbar-width:thin] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-[#9DCDBC] [&::-webkit-scrollbar-track]:bg-transparent">
                {results.map((node) => (
                  <button
                    key={node.roadmapNodeId}
                    type="button"
                    onMouseDown={(event) => event.preventDefault()}
                    onClick={() => chooseNode(node)}
                    className="block w-full border-b border-[#B9D8CC]/50 px-3 py-2 text-left last:border-b-0 hover:bg-[#F7F1E8]"
                  >
                    <div className="truncate text-sm font-extrabold text-[#18332D]">{node.title || "Untitled node"}</div>
                    <div className="mt-0.5 text-[11px] font-bold uppercase tracking-wide text-slate-500">
                      {node.nodeType || "node"}
                    </div>
                  </button>
                ))}
              </div>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
