import { Edit3, Trash2 } from "lucide-react";

import { ModuleButton } from "../../learningModules/components/learningModuleUi";

export default function MappingList({ title, icon: Icon, items, getId, getLabel, emptyText, onRemove, onEdit }) {
  return (
    <div className="rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/45 p-3">
      <div className="mb-2 flex items-center gap-2 text-xs font-extrabold uppercase tracking-wide text-slate-600">
        <Icon size={14} /> {title}
      </div>
      {items.length === 0 ? (
        <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-white p-3 text-xs font-semibold text-slate-500">
          {emptyText}
        </div>
      ) : (
        <div className="space-y-2">
          {items.map((item) => (
            <div
              key={getId(item)}
              className="flex items-center justify-between gap-2 rounded-lg border border-[#B9D8CC]/70 bg-white px-3 py-2"
            >
              <span className="min-w-0 truncate text-sm font-bold text-[#18332D]">{getLabel(item)}</span>
              {(onEdit || onRemove) && (
                <div className="flex shrink-0 items-center gap-1">
                  {onEdit && (
                    <ModuleButton variant="ghost" size="icon" onClick={() => onEdit(item)}>
                      <Edit3 size={14} />
                    </ModuleButton>
                  )}
                  {onRemove && (
                    <ModuleButton variant="ghost" size="icon" onClick={() => onRemove(getId(item))}>
                      <Trash2 size={14} />
                    </ModuleButton>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
