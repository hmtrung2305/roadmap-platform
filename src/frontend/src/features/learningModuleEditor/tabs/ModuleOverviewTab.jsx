import { useEffect, useState } from "react";
import { Minus, Plus, Save } from "lucide-react";
import { toast } from "react-toastify";
import { contentManagerLearningModuleApi } from "../../../api/learningModuleApi";
import SkillSearchPicker from "../../../components/learningModules/SkillSearchPicker";
import { inputClass, ModuleButton, ModuleCard, ModuleField } from "../../../components/learningModules/learningModuleUi";
import { CustomSelect, DirtyStateBadge } from "../EditorControls";
import { hasOverviewDraftChanges } from "../editorUtils";

export default function ModuleOverviewTab({ module, onSaved, onDirtyStateChange }) {
  const [selectedSkill, setSelectedSkill] = useState({
    skillId: module.skillId,
    name: module.skillName,
    slug: module.skillSlug || "",
    category: null,
    description: null,
  });
  const [form, setForm] = useState({
    skillId: module.skillId || "",
    title: module.title || "",
    description: module.description || "",
    difficultyLevel: module.difficultyLevel || "beginner",
    estimatedHours: module.estimatedHours ?? "",
  });
  const [isSaving, setIsSaving] = useState(false);
  const isDirty = hasOverviewDraftChanges(form, module);

  useEffect(() => {
    onDirtyStateChange?.("overview", isDirty);
  }, [isDirty, onDirtyStateChange]);

  useEffect(() => () => {
    onDirtyStateChange?.("overview", false);
  }, [onDirtyStateChange]);

  useEffect(() => {
    setSelectedSkill({
      skillId: module.skillId,
      name: module.skillName,
      slug: module.skillSlug || "",
      category: null,
      description: null,
    });
    setForm({
      skillId: module.skillId || "",
      title: module.title || "",
      description: module.description || "",
      difficultyLevel: module.difficultyLevel || "beginner",
      estimatedHours: module.estimatedHours ?? "",
    });
  }, [
    module.skillModuleId,
    module.skillId,
    module.skillName,
    module.skillSlug,
    module.title,
    module.description,
    module.difficultyLevel,
    module.estimatedHours,
  ]);

  const update = (key, value) => setForm((current) => ({ ...current, [key]: value }));

  const adjustEstimatedHours = (delta) => {
    setForm((current) => {
      const currentValue = Number(current.estimatedHours || 0);
      const nextValue = Math.max(0, currentValue + delta);
      return { ...current, estimatedHours: nextValue };
    });
  };

  const updateSkill = (skillId, skill) => {
    setSelectedSkill(skill);
    update("skillId", skillId);
  };

  const save = async () => {
    if (!form.skillId) {
      toast.error("Choose a skill first.");
      return;
    }

    if (!form.title.trim()) {
      toast.error("Module title is required.");
      return;
    }

    try {
      setIsSaving(true);
      const updatedModule = await contentManagerLearningModuleApi.updateModule(module.skillModuleId, {
        skillId: form.skillId,
        title: form.title.trim(),
        slug: null,
        description: form.description.trim() || null,
        difficultyLevel: form.difficultyLevel || null,
        estimatedHours: form.estimatedHours === "" ? null : Number(form.estimatedHours),
      });

      toast.success("Overview saved.");
      onSaved(updatedModule);
    } catch (err) {
      toast.error(err?.message || "Unable to save overview.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <ModuleCard className="flex h-full min-h-0 flex-col overflow-hidden p-5">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b border-[#B9D8CC]/70 pb-4">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-lg font-extrabold text-[#18332D]">Overview</h2>
            <DirtyStateBadge isDirty={isDirty} />
          </div>
          <p className="mt-1 text-sm font-semibold text-slate-600">Set the basic module information learners will see.</p>
        </div>

        <ModuleButton onClick={save} disabled={isSaving || !isDirty}>
          <Save size={14} /> {isSaving ? "Saving..." : isDirty ? "Save overview" : "Saved"}
        </ModuleButton>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="lg:col-span-2">
          <ModuleField label="Skill">
            <SkillSearchPicker
              value={form.skillId}
              initialSkill={selectedSkill}
              onChange={updateSkill}
              placeholder="Search for a skill"
            />
          </ModuleField>
        </div>

        <ModuleField label="Title">
          <input
            value={form.title}
            onChange={(event) => update("title", event.target.value)}
            className={inputClass}
          />
        </ModuleField>

        <ModuleField label="Difficulty">
          <CustomSelect
            value={form.difficultyLevel}
            onChange={(value) => update("difficultyLevel", value)}
            options={[
              { value: "beginner", label: "Beginner" },
              { value: "intermediate", label: "Intermediate" },
              { value: "advanced", label: "Advanced" },
            ]}
          />
        </ModuleField>

        <ModuleField label="Estimated hours">
          <div className="flex h-10 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white">
            <button
              type="button"
              onClick={() => adjustEstimatedHours(-0.5)}
              className="grid w-10 place-items-center border-r border-[#B9D8CC] text-slate-600 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
              aria-label="Decrease estimated hours"
            >
              <Minus size={14} />
            </button>
            <input
              type="number"
              step="0.5"
              min="0"
              value={form.estimatedHours}
              onChange={(event) => update("estimatedHours", event.target.value)}
              className="min-w-0 flex-1 border-0 bg-white px-3 text-center text-sm font-semibold text-[#18332D] outline-none [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
            />
            <button
              type="button"
              onClick={() => adjustEstimatedHours(0.5)}
              className="grid w-10 place-items-center border-l border-[#B9D8CC] text-slate-600 transition hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
              aria-label="Increase estimated hours"
            >
              <Plus size={14} />
            </button>
          </div>
        </ModuleField>

        <div className="lg:col-span-2">
          <ModuleField label="Description">
            <textarea
              value={form.description}
              onChange={(event) => update("description", event.target.value)}
              className={`${inputClass} min-h-32 resize-none`}
            />
          </ModuleField>
        </div>
      </div>


    </ModuleCard>
  );
}


