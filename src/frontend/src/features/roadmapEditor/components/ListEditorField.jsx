import { useEffect, useState } from "react";
import { Plus, X } from "lucide-react";

import { ModuleButton, ModuleField } from "../../learningModules/components/learningModuleUi";
import { listToText, normalizeTextList } from "../roadmapEditorUtils";

function updateItem(items, index, value) {
  const nextItems = [...items];
  nextItems[index] = value;
  return nextItems;
}

function removeItem(items, index) {
  return items.filter((_, itemIndex) => itemIndex !== index);
}

function normalizeDraftItems(value) {
  const items = normalizeTextList(value);
  return items.length > 0 ? items : [""];
}

export default function ListEditorField({
  label,
  value,
  onChange,
  addLabel = "Add item",
  placeholder = "List item",
  disabled = false,
}) {
  const [draftItems, setDraftItems] = useState(() => normalizeDraftItems(value));

  useEffect(() => {
    setDraftItems(normalizeDraftItems(value));
  }, [value]);

  function commit(nextItems) {
    const visibleItems = nextItems.length > 0 ? nextItems : [""];
    setDraftItems(visibleItems);
    onChange(listToText(visibleItems));
  }

  return (
    <ModuleField label={label}>
      <div className="space-y-2 rounded-2xl border border-[#D6E4DE] bg-white p-2.5 shadow-sm">
        <div className="space-y-2">
          {draftItems.map((item, index) => (
            <div key={`${label}-${index}`} className="flex items-start gap-2">
              <div className="mt-2 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[#F7F1E8] text-[11px] font-extrabold text-slate-500">
                {index + 1}
              </div>
              <textarea
                value={item}
                rows={2}
                placeholder={placeholder}
                disabled={disabled}
                onPaste={(event) => {
                  const pastedText = event.clipboardData.getData("text");
                  const pastedItems = normalizeTextList(pastedText);
                  if (pastedItems.length <= 1) return;

                  event.preventDefault();
                  commit([
                    ...draftItems.slice(0, index),
                    ...pastedItems,
                    ...draftItems.slice(index + 1),
                  ]);
                }}
                onChange={(event) => commit(updateItem(draftItems, index, event.target.value))}
                className="min-h-[68px] flex-1 rounded-xl border border-[#D6E4DE] bg-white px-3 py-2 text-sm font-semibold text-[#18332D] outline-none transition focus:border-[#1F6F5F] focus:ring-4 focus:ring-[#2FA084]/15"
              />
              <button
                type="button"
                onClick={() => commit(removeItem(draftItems, index))}
                disabled={disabled || (draftItems.length === 1 && !item)}
                className="mt-2 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-xl border border-[#D6E4DE] bg-white text-slate-500 transition hover:bg-red-50 hover:text-red-600 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-white disabled:hover:text-slate-500"
                aria-label={`Remove ${label} item ${index + 1}`}
              >
                <X size={14} />
              </button>
            </div>
          ))}
        </div>
        <ModuleButton
          type="button"
          size="xs"
          variant="secondary"
          onClick={() => setDraftItems((current) => [...current, ""])}
          disabled={disabled}
        >
          <Plus size={14} /> {addLabel}
        </ModuleButton>
      </div>
    </ModuleField>
  );
}
