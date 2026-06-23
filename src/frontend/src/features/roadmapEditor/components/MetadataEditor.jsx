import { Info, Save } from "lucide-react";

import { DirtyStateBadge } from "../../learningModuleEditor/EditorControls";
import {
  inputClass,
  ModuleButton,
  ModuleCard,
  ModuleField,
  numberInputClass,
} from "../../../components/learningModules/learningModuleUi";

export default function MetadataEditor({ detail, form, setForm, isSaving, isDirty, onSave }) {
  return (
    <ModuleCard className="p-4">
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-center gap-2">
          <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
            <Info size={16} />
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-base font-extrabold text-[#18332D]">Roadmap metadata</h2>
            <DirtyStateBadge isDirty={isDirty} />
          </div>
        </div>
        <ModuleButton onClick={onSave} disabled={!detail || isSaving || !isDirty}>
          <Save size={14} /> {isSaving ? "Saving" : "Save metadata"}
        </ModuleButton>
      </div>

      <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_180px]">
        <ModuleField label="Title">
          <input
            type="text"
            value={form.title}
            onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
            className={inputClass}
          />
        </ModuleField>
        <ModuleField label="Total hours">
          <input
            type="number"
            min="0"
            value={form.estimatedTotalHours}
            onChange={(event) => setForm((current) => ({ ...current, estimatedTotalHours: event.target.value }))}
            className={numberInputClass}
          />
        </ModuleField>
        <div className="lg:col-span-2">
          <ModuleField label="Description">
            <textarea
              value={form.description}
              onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              rows={4}
              className={`${inputClass} min-h-28 resize-y`}
            />
          </ModuleField>
        </div>
      </div>
    </ModuleCard>
  );
}
