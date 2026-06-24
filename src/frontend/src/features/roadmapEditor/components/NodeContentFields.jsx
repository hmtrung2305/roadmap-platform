import { ModuleField } from "../../../components/learningModules/learningModuleUi";
import { inputClass } from "../../../components/learningModules/learningModuleUi";
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
        <ListEditorField
          label="Completion criteria"
          value={nodeForm.completionCriteriaText}
          addLabel="Add criterion"
          placeholder="Done when..."
          onChange={(value) => setNodeForm((current) => ({ ...current, completionCriteriaText: value }))}
        />
      </SectionCard>

      {nodeType === "project" && (
        <SectionCard title="Project guide">
          <SingleTextField
            label="Project brief"
            value={nodeForm.guide?.projectBrief}
            onChange={(value) => updateGuideField(setNodeForm, "projectBrief", value)}
            placeholder="Describe the artifact learners should build."
          />
          <ListEditorField
            label="Build steps"
            value={nodeForm.guide?.suggestedStepsText}
            addLabel="Add step"
            placeholder="Step to complete"
            onChange={(value) => updateGuideField(setNodeForm, "suggestedStepsText", value)}
          />
          <ListEditorField
            label="Evidence to submit"
            value={nodeForm.guide?.expectedEvidenceText}
            addLabel="Add evidence"
            placeholder="Evidence item"
            onChange={(value) => updateGuideField(setNodeForm, "expectedEvidenceText", value)}
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
            label="Evidence to prepare"
            value={nodeForm.guide?.expectedEvidenceText}
            addLabel="Add evidence"
            placeholder="Evidence item"
            onChange={(value) => updateGuideField(setNodeForm, "expectedEvidenceText", value)}
          />
          <ListEditorField
            label="Reflection prompts"
            value={nodeForm.guide?.reviewQuestionsText}
            addLabel="Add prompt"
            placeholder="Reflection prompt"
            onChange={(value) => updateGuideField(setNodeForm, "reviewQuestionsText", value)}
          />
          <ListEditorField
            label="Next action options"
            value={nodeForm.guide?.nextActionsText}
            addLabel="Add action"
            placeholder="Next action"
            onChange={(value) => updateGuideField(setNodeForm, "nextActionsText", value)}
          />
        </SectionCard>
      )}
    </div>
  );
}
