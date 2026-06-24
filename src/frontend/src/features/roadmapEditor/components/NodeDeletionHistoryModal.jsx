import { History, RotateCcw, X } from "lucide-react";

import { ModuleButton } from "../../../components/learningModules/learningModuleUi";

export default function NodeDeletionHistoryModal({
  isOpen,
  pendingDeletions = [],
  isBusy = false,
  onClose,
  onUndo,
  onRestoreAll,
}) {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/35 p-4 backdrop-blur-sm"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose?.();
      }}
    >
      <div className="flex max-h-[82vh] w-full max-w-xl flex-col overflow-hidden rounded-2xl border border-[#B9D8CC] bg-white shadow-2xl">
        <div className="flex items-center justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
          <div className="flex items-center gap-2">
            <div className="grid h-9 w-9 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
              <History size={18} />
            </div>
            <h2 className="text-base font-extrabold text-[#18332D]">Deletion history</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D]"
            aria-label="Close deletion history"
          >
            <X size={18} />
          </button>
        </div>

        <div className="min-h-0 flex-1 space-y-3 overflow-y-auto p-4 scrollbar-thin scrollbar-track-[#F7F1E8] scrollbar-thumb-[#B9D8CC]">
          {pendingDeletions.length === 0 ? (
            <div className="rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/45 p-5 text-center text-sm font-bold text-slate-600">
              No staged deletions.
            </div>
          ) : (
            pendingDeletions.map((deletion) => (
              <div
                key={deletion.actionId}
                className="flex items-center justify-between gap-3 rounded-xl border border-[#B9D8CC] bg-white p-3 shadow-sm"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-extrabold text-[#18332D]">{deletion.nodeLabel}</p>
                  <p className="text-xs font-semibold text-slate-600">
                    {deletion.deletedCount > 1
                      ? `${deletion.deletedCount} nodes will be deleted`
                      : "1 node will be deleted"}
                  </p>
                </div>
                <ModuleButton
                  size="xs"
                  variant="secondary"
                  disabled={isBusy}
                  onClick={() => onUndo?.(deletion.actionId)}
                >
                  <RotateCcw size={13} /> Undo
                </ModuleButton>
              </div>
            ))
          )}
        </div>

        {pendingDeletions.length > 0 && (
          <div className="flex justify-end border-t border-[#B9D8CC]/70 p-4">
            <ModuleButton type="button" variant="ghost" disabled={isBusy} onClick={onRestoreAll}>
              Restore all
            </ModuleButton>
          </div>
        )}
      </div>
    </div>
  );
}
