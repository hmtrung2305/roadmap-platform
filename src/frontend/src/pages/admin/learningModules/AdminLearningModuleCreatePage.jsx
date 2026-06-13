import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowLeft, CheckCircle2, ChevronDown } from "lucide-react";
import { toast } from "react-toastify";
import { counselorLearningModuleApi } from "../../../api/learningModuleApi";
import SkillSearchPicker from "../../../components/learningModules/SkillSearchPicker";
import {
  inputClass,
  ModuleButton,
  ModuleCard,
  ModuleField,
  ModulePageShell,
} from "../../../components/learningModules/learningModuleUi";

function CustomSelect({ value, onChange, options, placeholder = "Select an option" }) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedOption = options.find((option) => String(option.value) === String(value));

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="flex h-10 w-full cursor-pointer items-center justify-between gap-3 rounded-lg border border-[#B9D8CC] bg-white px-3 text-left text-sm font-semibold text-[#18332D] outline-none transition hover:border-[#2FA084] focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
      >
        <span className={selectedOption ? "truncate" : "truncate text-slate-400"}>
          {selectedOption?.label || placeholder}
        </span>
        <ChevronDown
          size={16}
          className={`shrink-0 text-[#1F6F5F] transition ${isOpen ? "rotate-180" : ""}`}
        />
      </button>

      {isOpen && (
        <div className="absolute left-0 right-0 top-[calc(100%+6px)] z-30 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 shadow-lg">
          {options.map((option) => {
            const isSelected = String(option.value) === String(value);

            return (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  onChange(option.value);
                  setIsOpen(false);
                }}
                className={`flex w-full cursor-pointer items-center justify-between gap-3 px-3 py-2 text-left text-sm font-bold transition ${
                  isSelected
                    ? "bg-[#6FCF97]/18 text-[#1F6F5F]"
                    : "text-slate-700 hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                }`}
              >
                <span>{option.label}</span>
                {isSelected && <CheckCircle2 size={15} className="text-[#1F6F5F]" />}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

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
          className="inline-flex cursor-pointer items-center gap-2 text-sm font-bold text-[#1F6F5F]"
        >
          <ArrowLeft size={16} /> Back to module management
        </button>

        <ModuleCard className="p-6">
          <h1 className="text-2xl font-extrabold text-[#18332D]">Create learning module</h1>
          <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">
            Set the basics now. Lessons and quiz questions can be added after the draft is created.
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
              <CustomSelect
                value={form.difficultyLevel}
                onChange={(value) => updateForm("difficultyLevel", value)}
                options={[
                  { value: "beginner", label: "Beginner" },
                  { value: "intermediate", label: "Intermediate" },
                  { value: "advanced", label: "Advanced" },
                ]}
              />
            </ModuleField>

            <ModuleField label="Estimated hours">
              <input
                type="number"
                step="0.5"
                min="0"
                value={form.estimatedHours}
                onChange={(event) => updateForm("estimatedHours", event.target.value)}
                className={`${inputClass} [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none`}
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
