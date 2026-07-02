import { useEffect, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Loader2, Save, X } from "lucide-react";

import AppSelect from "../../components/common/AppSelect";
import {
  inputClass,
  ModuleButton,
  ModuleField,
} from "../learningModules/components/learningModuleUi";
import {
  areResourceFormsEqual,
  buildResourcePayload,
  normalizeResourceForm,
  resourceDifficultyOptions,
  resourceTypeOptions,
} from "./catalogConstants";

export default function LearningResourceFormModal({
  isOpen,
  resource = null,
  initialTitle = "",
  initialUrl = "",
  isSaving = false,
  error = "",
  onClose,
  onSubmit,
}) {
  const baseForm = useMemo(
    () => normalizeResourceForm(resource, initialTitle, initialUrl),
    [initialTitle, initialUrl, resource],
  );
  const [form, setForm] = useState(baseForm);

  useEffect(() => {
    if (isOpen) setForm(baseForm);
  }, [baseForm, isOpen]);

  const isEdit = Boolean(resource?.resourceId);
  const isDirty = !areResourceFormsEqual(form, baseForm);
  const canSave = form.title.trim() && form.url.trim() && form.resourceType && (isEdit ? isDirty : true) && !isSaving;

  const submit = (event) => {
    event.preventDefault();
    if (!canSave) return;
    onSubmit?.(buildResourcePayload(form));
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          className="fixed inset-0 z-[120] grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.16 }}
          onMouseDown={(event) => {
            if (event.target === event.currentTarget && !isSaving) onClose?.();
          }}
        >
          <motion.form
            onSubmit={submit}
            className="w-full max-w-2xl rounded-xl border border-[#B9D8CC] bg-white shadow-2xl"
            role="dialog"
            aria-modal="true"
            initial={{ opacity: 0, scale: 0.97, y: 12 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.97, y: 12 }}
            transition={{ duration: 0.16 }}
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
              <h2 className="text-base font-extrabold text-[#18332D]">
                {isEdit ? "Edit resource" : "Create resource"}
              </h2>
              <button
                type="button"
                onClick={onClose}
                disabled={isSaving}
                className="inline-grid h-8 w-8 place-items-center rounded-lg text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
                aria-label="Close"
              >
                <X size={17} />
              </button>
            </div>

            <div className="space-y-4 p-4">
              <ModuleField label="Title">
                <input
                  type="text"
                  value={form.title}
                  onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
                  className={inputClass}
                  disabled={isSaving}
                  maxLength={200}
                  placeholder="Example: React Docs"
                />
              </ModuleField>

              <ModuleField label="URL">
                <input
                  type="url"
                  value={form.url}
                  onChange={(event) => setForm((current) => ({ ...current, url: event.target.value }))}
                  className={inputClass}
                  disabled={isSaving}
                  placeholder="https://example.com/resource"
                />
              </ModuleField>

              <div className="grid gap-4 md:grid-cols-2">
                <ModuleField label="Type">
                  <AppSelect
                    value={form.resourceType}
                    options={resourceTypeOptions}
                    onChange={(value) => setForm((current) => ({ ...current, resourceType: value }))}
                    ariaLabel="Select resource type"
                    disabled={isSaving}
                    dropdownMode="fixed"
                  />
                </ModuleField>

                <ModuleField label="Difficulty">
                  <AppSelect
                    value={form.difficultyLevel}
                    options={resourceDifficultyOptions}
                    onChange={(value) => setForm((current) => ({ ...current, difficultyLevel: value }))}
                    ariaLabel="Select resource difficulty"
                    disabled={isSaving}
                    dropdownMode="fixed"
                  />
                </ModuleField>
              </div>

              <ModuleField label="Provider">
                <input
                  type="text"
                  value={form.provider}
                  onChange={(event) => setForm((current) => ({ ...current, provider: event.target.value }))}
                  className={inputClass}
                  disabled={isSaving}
                  maxLength={100}
                  placeholder="Example: React"
                />
              </ModuleField>

              <ModuleField label="Description">
                <textarea
                  value={form.description}
                  onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                  className={`${inputClass} min-h-28 resize-y`}
                  disabled={isSaving}
                  placeholder="What this resource is useful for."
                />
              </ModuleField>

              {error && (
                <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-bold text-red-700">
                  {error}
                </div>
              )}
            </div>

            <div className="flex justify-end gap-2 border-t border-[#B9D8CC]/70 p-4">
              <ModuleButton variant="secondary" onClick={onClose} disabled={isSaving}>
                Cancel
              </ModuleButton>
              <ModuleButton type="submit" disabled={!canSave}>
                {isSaving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
                {isEdit ? "Save" : "Create"}
              </ModuleButton>
            </div>
          </motion.form>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
