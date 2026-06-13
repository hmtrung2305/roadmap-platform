import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { toast } from "react-toastify";
import { counselorLearningModuleApi } from "../../../api/learningModuleApi";
import SkillSearchPicker from "../../../components/learningModules/SkillSearchPicker";
import {
  inputClass,
  ModuleButton,
  ModuleCard,
  ModuleField,
  ModulePageShell,
  selectClass,
} from "../../../components/learningModules/learningModuleUi";

export default function AdminLearningModuleCreatePage() {
  const navigate = useNavigate();
  const [selectedSkill, setSelectedSkill] = useState(null);
  const [form, setForm] = useState({
    skillId: "",
    title: "",
    description: "",
    difficultyLevel: "beginner",
    estimatedHours: "",
  });
  const [isSaving, setIsSaving] = useState(false);

  const updateForm = (key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const handleSkillChange = (skillId, skill) => {
    setSelectedSkill(skill);
    updateForm("skillId", skillId);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!form.skillId.trim()) {
      toast.error("Choose a skill first.");
      return;
    }

    if (!form.title.trim()) {
      toast.error("Module title is required.");
      return;
    }

    try {
      setIsSaving(true);

      const created = await counselorLearningModuleApi.createModule({
        skillId: form.skillId.trim(),
        title: form.title.trim(),
        slug: null,
        description: form.description.trim() || null,
        difficultyLevel: form.difficultyLevel || null,
        estimatedHours: form.estimatedHours ? Number(form.estimatedHours) : null,
      });

      toast.success("Module draft created.");
      navigate(`/admin/learning-modules/${created.skillModuleId}/edit`);
    } catch (err) {
      toast.error(err?.message || "Unable to create module.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <ModulePageShell>
      <div className="space-y-5">
        <button
          type="button"
          onClick={() => navigate("/admin/learning-modules")}
          className="inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
        >
          <ArrowLeft size={16} /> Back to module management
        </button>

        <ModuleCard className="p-6">
          <h1 className="text-2xl font-extrabold text-[#18332D]">Create learning module</h1>
          <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">
            Set up the module basics first. Lessons and quiz questions can be added after the draft is created.
          </p>

          <form onSubmit={handleSubmit} className="mt-6 grid gap-4 lg:grid-cols-2">
            <div className="lg:col-span-2">
              <ModuleField label="Skill">
                <SkillSearchPicker
                  value={form.skillId}
                  initialSkill={selectedSkill}
                  onChange={handleSkillChange}
                  placeholder="Search for a skill"
                />
              </ModuleField>
            </div>

            <ModuleField label="Module title">
              <input
                value={form.title}
                onChange={(event) => updateForm("title", event.target.value)}
                className={inputClass}
                placeholder="Build a Portfolio Website"
              />
            </ModuleField>

            <ModuleField label="Difficulty">
              <select
                value={form.difficultyLevel}
                onChange={(event) => updateForm("difficultyLevel", event.target.value)}
                className={selectClass}
              >
                <option value="beginner">Beginner</option>
                <option value="intermediate">Intermediate</option>
                <option value="advanced">Advanced</option>
              </select>
            </ModuleField>

            <ModuleField label="Estimated hours">
              <input
                type="number"
                step="0.5"
                min="0"
                value={form.estimatedHours}
                onChange={(event) => updateForm("estimatedHours", event.target.value)}
                className={inputClass}
                placeholder="8"
              />
            </ModuleField>

            <div className="lg:col-span-2">
              <ModuleField label="Description">
                <textarea
                  value={form.description}
                  onChange={(event) => updateForm("description", event.target.value)}
                  className={`${inputClass} min-h-28 resize-none`}
                  placeholder="Describe what this module teaches."
                />
              </ModuleField>
            </div>

            <div className="flex justify-end gap-3 lg:col-span-2">
              <ModuleButton
                type="button"
                variant="secondary"
                onClick={() => navigate("/admin/learning-modules")}
              >
                Cancel
              </ModuleButton>
              <ModuleButton type="submit" disabled={isSaving}>
                {isSaving ? "Creating..." : "Create draft"}
              </ModuleButton>
            </div>
          </form>
        </ModuleCard>
      </div>
    </ModulePageShell>
  );
}
