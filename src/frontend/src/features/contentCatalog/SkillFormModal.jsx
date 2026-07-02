import { useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Loader2, Save, X } from "lucide-react";

import AppSelect from "../../components/common/AppSelect";
import {
  inputClass,
  ModuleButton,
  ModuleField,
} from "../learningModules/components/learningModuleUi";
import {
  areSkillFormsEqual,
  buildSkillPayload,
  normalizeSkillForm,
} from "./catalogConstants";

const customCategoryValue = "__custom_category__";

function createSkillFormState(key, baseForm, categoryOptions) {
  const hasExistingCategory = categoryOptions.some(
    (item) => item === baseForm.category,
  );
  const usesCustomCategory = Boolean(baseForm.category && !hasExistingCategory);

  return {
    key,
    form: baseForm,
    isCustomCategory: usesCustomCategory,
    customCategory: usesCustomCategory ? baseForm.category : "",
  };
}

export default function SkillFormModal({
  isOpen,
  skill = null,
  categories = [],
  initialName = "",
  isSaving = false,
  error = "",
  onClose,
  onSubmit,
}) {
  const baseForm = useMemo(
    () => normalizeSkillForm(skill, initialName),
    [initialName, skill],
  );
  const categoryOptions = useMemo(
    () => categories.filter(Boolean),
    [categories],
  );
  const formKey = useMemo(
    () => JSON.stringify({ baseForm, categoryOptions }),
    [baseForm, categoryOptions],
  );
  const [formState, setFormState] = useState(() => (
    createSkillFormState(formKey, baseForm, categoryOptions)
  ));
  const resolvedFormState = formState.key === formKey
    ? formState
    : createSkillFormState(formKey, baseForm, categoryOptions);
  const { form, isCustomCategory, customCategory } = resolvedFormState;

  const updateFormState = (updater) => {
    setFormState((current) => {
      const currentState = current.key === formKey
        ? current
        : createSkillFormState(formKey, baseForm, categoryOptions);
      const patch = typeof updater === "function" ? updater(currentState) : updater;
      return {
        ...currentState,
        ...patch,
        key: formKey,
      };
    });
  };
  const setForm = (updater) => {
    updateFormState((current) => ({
      form: typeof updater === "function" ? updater(current.form) : updater,
    }));
  };

  const isEdit = Boolean(skill?.skillId);
  const isDirty = !areSkillFormsEqual(form, baseForm);
  const canSave =
    form.name.trim() &&
    form.category.trim() &&
    (isEdit ? isDirty : true) &&
    !isSaving;
  const isReadOnly = isSaving || (isEdit && !skill?.canEdit);
  const selectedCategoryValue = isCustomCategory
    ? customCategoryValue
    : form.category || "";

  const categorySelectOptions = useMemo(
    () => [
      { value: "", label: "Select category", disabled: true },
      ...categoryOptions.map((category) => ({
        value: category,
        label: category,
      })),
      { value: customCategoryValue, label: "Add new category..." },
    ],
    [categoryOptions],
  );

  const submit = (event) => {
    event.preventDefault();
    if (!canSave) return;
    onSubmit?.(buildSkillPayload(form));
  };

  const handleCategoryChange = (value) => {
    if (value === customCategoryValue) {
      updateFormState((current) => ({
        isCustomCategory: true,
        form: { ...current.form, category: current.customCategory },
      }));
      return;
    }

    updateFormState((current) => ({
      isCustomCategory: false,
      form: { ...current.form, category: value },
    }));
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
            className="w-full max-w-xl rounded-xl border border-[#B9D8CC] bg-white shadow-2xl"
            role="dialog"
            aria-modal="true"
            initial={{ opacity: 0, scale: 0.97, y: 12 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.97, y: 12 }}
            transition={{ duration: 0.16 }}
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
              <div>
                <h2 className="text-base font-extrabold text-[#18332D]">
                  {isEdit ? "Edit skill" : "Create skill"}
                </h2>
                {isEdit && !skill?.canEdit && (
                  <p className="mt-1 text-xs font-semibold text-slate-600">
                    Used skills are read-only here.
                  </p>
                )}
              </div>
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
              <ModuleField label="Name">
                <input
                  type="text"
                  value={form.name}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      name: event.target.value,
                    }))
                  }
                  className={inputClass}
                  disabled={isReadOnly}
                  maxLength={100}
                  placeholder="Example: GraphQL"
                />
              </ModuleField>

              <ModuleField label="Category">
                <AppSelect
                  value={selectedCategoryValue}
                  options={categorySelectOptions}
                  onChange={handleCategoryChange}
                  ariaLabel="Select skill category"
                  disabled={isReadOnly}
                  dropdownMode="fixed"
                />
              </ModuleField>

              {isCustomCategory && (
                <ModuleField label="New category">
                  <input
                    type="text"
                    value={customCategory}
                    onChange={(event) => {
                      const nextValue = event.target.value;
                      updateFormState((current) => ({
                        customCategory: nextValue,
                        form: {
                          ...current.form,
                          category: nextValue,
                        },
                      }));
                    }}
                    className={inputClass}
                    disabled={isReadOnly}
                    maxLength={100}
                    placeholder="Example: Backend"
                  />
                </ModuleField>
              )}

              <ModuleField label="Description">
                <textarea
                  value={form.description}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      description: event.target.value,
                    }))
                  }
                  className={`${inputClass} min-h-28 resize-y`}
                  disabled={isReadOnly}
                  placeholder="What this skill covers."
                />
              </ModuleField>

              {error && (
                <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-bold text-red-700">
                  {error}
                </div>
              )}
            </div>

            <div className="flex justify-end gap-2 border-t border-[#B9D8CC]/70 p-4">
              <ModuleButton
                variant="secondary"
                onClick={onClose}
                disabled={isSaving}
              >
                Cancel
              </ModuleButton>
              <ModuleButton
                type="submit"
                disabled={!canSave || (isEdit && !skill?.canEdit)}
              >
                {isSaving ? (
                  <Loader2 size={14} className="animate-spin" />
                ) : (
                  <Save size={14} />
                )}
                {isEdit ? "Save" : "Create"}
              </ModuleButton>
            </div>
          </motion.form>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
