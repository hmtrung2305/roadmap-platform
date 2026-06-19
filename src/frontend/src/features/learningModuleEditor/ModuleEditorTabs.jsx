import { CheckCircle2 } from "lucide-react";
import { editorTabs, isEditorTabComplete } from "./editorUtils";

export default function ModuleEditorTabs({ activeTab, detail, onSelect }) {
  return (
    <div className="rounded-xl border border-[#B9D8CC] bg-white p-2 shadow-sm">
      <div className="grid gap-2 md:grid-cols-5">
        {editorTabs.map((tab, index) => {
          const isActive = activeTab === tab.key;
          const isComplete = isEditorTabComplete(tab.key, detail);

          return (
            <button
              key={tab.key}
              type="button"
              onClick={() => onSelect(tab.key)}
              className={`flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-left text-xs font-extrabold transition ${
                isActive
                  ? "border-[#1F6F5F] bg-[#2FA084] text-white shadow-sm"
                  : isComplete
                    ? "border-[#B9D8CC] bg-white text-[#1F6F5F]"
                    : "border-transparent bg-white text-slate-600 hover:bg-[#F7F1E8]"
              }`}
            >
              <span className={`grid h-6 w-6 shrink-0 place-items-center rounded-full text-[11px] ${
                isActive
                  ? "bg-white/20 text-white"
                  : isComplete
                    ? "border border-[#6FCF97] bg-[#6FCF97]/14 text-[#1F6F5F]"
                    : "bg-slate-100 text-slate-600"
              }`}>
                {isComplete && !isActive ? <CheckCircle2 size={14} /> : index + 1}
              </span>
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
