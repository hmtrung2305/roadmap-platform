import { useEffect, useRef } from "react";
import { AlertCircle, CheckCircle2, X } from "lucide-react";

import { ModuleButton } from "../../learningModules/components/learningModuleUi";

function ValidationList({ title, items, tone }) {
  if (!items?.length) return null;

  const toneClass = tone === "error" ? "text-red-700 bg-red-50 border-red-200" : "text-amber-800 bg-amber-50 border-amber-200";

  return (
    <section className="space-y-2">
      <h3 className="text-sm font-extrabold text-[#18332D]">{title}</h3>
      <div className="space-y-2">
        {items.map((item, index) => (
          <div key={`${item.code}-${item.roadmapNodeId || index}`} className={`rounded-lg border px-3 py-2 text-sm font-semibold ${toneClass}`}>
            <div>{item.message}</div>
            {item.nodeTitle ? <div className="mt-1 text-xs opacity-75">{item.nodeTitle}</div> : null}
          </div>
        ))}
      </div>
    </section>
  );
}

export default function DraftValidationModal({ isOpen, result, onClose, onPublish, isPublishing }) {
  const panelRef = useRef(null);

  useEffect(() => {
    if (!isOpen) return undefined;

    const onKeyDown = (event) => {
      if (event.key === "Escape") onClose();
    };

    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const errors = result?.errors || [];
  const warnings = result?.warnings || [];
  const isValid = result?.isValid && errors.length === 0;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/35 p-4 backdrop-blur-sm"
      onMouseDown={(event) => {
        if (panelRef.current && !panelRef.current.contains(event.target)) onClose();
      }}
    >
      <div ref={panelRef} className="flex max-h-[82vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl border border-[#B9D8CC] bg-white shadow-2xl">
        <div className="flex items-center justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
          <div className="flex items-center gap-2">
            {isValid ? <CheckCircle2 size={18} className="text-[#1F6F5F]" /> : <AlertCircle size={18} className="text-amber-600" />}
            <h2 className="text-base font-extrabold text-[#18332D]">Draft validation</h2>
          </div>
          <button type="button" onClick={onClose} className="rounded-lg p-2 text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D]">
            <X size={18} />
          </button>
        </div>

        <div className="min-h-0 flex-1 space-y-5 overflow-y-auto p-4 scrollbar-thin scrollbar-track-[#F7F1E8] scrollbar-thumb-[#B9D8CC]">
          {isValid && warnings.length === 0 ? (
            <div className="rounded-xl border border-[#B9D8CC] bg-[#F4FBF8] p-4 text-sm font-bold text-[#1F6F5F]">
              This draft is ready to publish.
            </div>
          ) : null}
          <ValidationList title="Errors" items={errors} tone="error" />
          <ValidationList title="Warnings" items={warnings} tone="warning" />
        </div>

        <div className="flex justify-end gap-2 border-t border-[#B9D8CC]/70 p-4">
          <ModuleButton variant="secondary" onClick={onClose}>Close</ModuleButton>
          <ModuleButton onClick={onPublish} disabled={!isValid || isPublishing}>Publish draft</ModuleButton>
        </div>
      </div>
    </div>
  );
}
