import { ModuleField } from "../../learningModules/components/learningModuleUi";
import { inputClass } from "../../learningModules/components/learningModuleUi";
import { normalizeNodeType } from "../nodeRules";
import ListEditorField from "./ListEditorField";

function updateGuideField(setNodeForm, field, value) {
  setNodeForm((current) => ({
    ...current,
    guide: {
      ...(current.guide || {}),
      [field]: value,
    },
  }));
}

function SingleTextField({ label, value, onChange, rows = 3, placeholder = "" }) {
  return (
    <ModuleField label={label}>
      <textarea
        value={value || ""}
        onChange={(event) => onChange(event.target.value)}
        rows={rows}
        placeholder={placeholder}
        className={`${inputClass} min-h-24 resize-y`}
      />
    </ModuleField>
  );
}

function SectionCard({ title, children }) {
  return (
    <section className="space-y-3 rounded-2xl border border-[#D6E4DE] bg-[#FCFAF7] p-3.5 shadow-sm">
      <div className="text-xs font-extrabold uppercase tracking-wide text-slate-500">{title}</div>
      <div className="space-y-3">{children}</div>
    </section>
  );
}

export default function NodeContentFields({ selectedNode, nodeForm, setNodeForm }) {
  const nodeType = normalizeNodeType(selectedNode);
  const showGenericCompletionCriteria = nodeType !== "checkpoint";

  return (
    <div className="space-y-4">
      <SectionCard title="Learner outcomes">
        <ListEditorField
          label="Learning outcomes"
          value={nodeForm.learningOutcomesText}
          addLabel="Add outcome"
          placeholder="Learner should be able to..."
          onChange={(value) => setNodeForm((current) => ({ ...current, learningOutcomesText: value }))}
        />
        {showGenericCompletionCriteria && (
          <ListEditorField
            label="Completion criteria"
            value={nodeForm.completionCriteriaText}
            addLabel="Add criterion"
            placeholder="Completion criterion"
            onChange={(value) => setNodeForm((current) => ({ ...current, completionCriteriaText: value }))}
          />
        )}
      </SectionCard>

      {nodeType === "project" && (
        <SectionCard title="Project guide">
          <SingleTextField
            label="What to build"
            value={nodeForm.guide?.whatToBuild}
            onChange={(value) => updateGuideField(setNodeForm, "whatToBuild", value)}
            placeholder="Describe the artifact learners should build."
          />
          <ListEditorField
            label="Build steps"
            value={nodeForm.guide?.buildStepsText}
            addLabel="Add step"
            placeholder="Step to complete"
            onChange={(value) => updateGuideField(setNodeForm, "buildStepsText", value)}
          />
        </SectionCard>
      )}

      {nodeType === "checkpoint" && (
        <SectionCard title="Checkpoint guide">
          <SingleTextField
            label="Review focus"
            value={nodeForm.guide?.reviewFocus}
            onChange={(value) => updateGuideField(setNodeForm, "reviewFocus", value)}
            placeholder="Describe what learners should review."
          />
          <ListEditorField
            label="Review criteria"
            value={nodeForm.guide?.reviewCriteriaText}
            addLabel="Add criterion"
            placeholder="Review criterion"
            onChange={(value) => updateGuideField(setNodeForm, "reviewCriteriaText", value)}
          />
        </SectionCard>
      )}
    </div>
  );
}
