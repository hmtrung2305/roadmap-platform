import { useEffect, useMemo, useRef, useState } from "react";
import { Loader2, Plus, X } from "lucide-react";

import AppSelect from "../../../components/common/AppSelect";
import {
  inputClass,
  ModuleButton,
  ModuleField,
  numberInputClass,
} from "../../learningModules/components/learningModuleUi";
import {
  checkpointTypeOptions,
  difficultyOptions,
  draftNodeTypeOptions,
  draftNodePositionOptions,
} from "../roadmapEditorConstants";
import { compareNodes } from "../roadmapEditorUtils";
import { getAllowedChildNodeTypes, getNodeLabel } from "../nodeRules";

function getNodeId(node) {
  return node?.roadmapNodeId || node?.id || "";
}

function getPhaseOptions(nodes = []) {
  return nodes
    .filter((node) => String(node?.nodeType || "").toLowerCase() === "phase")
    .slice()
    .sort(compareNodes)
    .map((node) => ({ value: getNodeId(node), label: node.title || "Untitled phase" }))
    .filter((option) => option.value);
}

const initialForm = {
  nodeType: "topic",
  title: "",
  description: "",
  reason: "",
  estimatedHours: "",
  difficultyLevel: "",
  checkpointType: "review",
  position: "end",
  referenceNodeId: "",
};

export default function NodeCreateModal({
  isOpen,
  nodes = [],
  selectedNode,
  createMode = "child",
  isSaving,
  onClose,
  onCreate,
}) {
  const panelRef = useRef(null);
  const [form, setForm] = useState(initialForm);

  const isPhaseCreate = createMode === "phase";
  const allowedNodeTypes = useMemo(() => (
    isPhaseCreate ? ["phase"] : getAllowedChildNodeTypes(selectedNode)
  ), [isPhaseCreate, selectedNode]);
  const nodeTypeOptions = useMemo(() => (
    draftNodeTypeOptions.filter((option) => allowedNodeTypes.includes(option.value))
  ), [allowedNodeTypes]);
  const phaseOptions = useMemo(() => getPhaseOptions(nodes), [nodes]);
  const selectedReferencePhase = phaseOptions.find((option) => option.value === form.referenceNodeId);

  useEffect(() => {
    if (!isOpen) return;

    setForm({
      ...initialForm,
      nodeType: isPhaseCreate ? "phase" : nodeTypeOptions[0]?.value || "topic",
    });
  }, [isOpen, isPhaseCreate, nodeTypeOptions]);

  useEffect(() => {
    if (!isOpen) return undefined;

    const onKeyDown = (event) => {
      if (event.key === "Escape") onClose();
    };

    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const parentNodeId = isPhaseCreate ? null : getNodeId(selectedNode);
  const canUseLearningFields = ["topic", "project", "checkpoint"].includes(form.nodeType);
  const setField = (key, value) => setForm((current) => {
    const next = { ...current, [key]: value };

    if (key === "position" && value === "end") {
      next.referenceNodeId = "";
    }

    return next;
  });

  const submit = async (event) => {
    event.preventDefault();
    await onCreate({
      nodeType: form.nodeType,
      parentNodeId,
      title: form.title,
      description: form.description,
      reason: form.reason,
      estimatedHours: form.estimatedHours === "" ? null : Number(form.estimatedHours),
      difficultyLevel: form.difficultyLevel || null,
      checkpointType: form.nodeType === "checkpoint" ? form.checkpointType : null,
      position: isPhaseCreate ? form.position : "end",
      referenceNodeId: isPhaseCreate && form.position !== "end" ? form.referenceNodeId || null : null,
    });
    onClose();
  };

  const isSubmitDisabled = (
    isSaving
    || !form.title.trim()
    || nodeTypeOptions.length === 0
    || (!isPhaseCreate && !parentNodeId)
    || (isPhaseCreate && form.position !== "end" && !form.referenceNodeId)
  );

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/35 p-4 backdrop-blur-sm"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <form
        ref={panelRef}
        onSubmit={submit}
        onMouseDown={(event) => event.stopPropagation()}
        className="flex max-h-[88vh] w-full max-w-2xl flex-col overflow-visible rounded-2xl border border-[#B9D8CC] bg-white shadow-2xl"
      >
        <div className="flex items-center justify-between gap-3 border-b border-[#B9D8CC]/70 p-4">
          <div className="flex items-center gap-2">
            <div className="grid h-9 w-9 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
              <Plus size={18} />
            </div>
            <h2 className="text-base font-extrabold text-[#18332D]">Add node</h2>
          </div>
          <button type="button" onClick={onClose} className="rounded-lg p-2 text-slate-500 transition hover:bg-[#F7F1E8] hover:text-[#18332D]">
            <X size={18} />
          </button>
        </div>

        <div className="min-h-0 flex-1 space-y-4 overflow-y-auto p-4 scrollbar-thin scrollbar-track-[#F7F1E8] scrollbar-thumb-[#B9D8CC]">
          {isPhaseCreate ? (
            <div className="rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/55 px-3 py-2">
              <p className="text-[11px] font-extrabold uppercase tracking-wide text-slate-500">Placement</p>
              <p className="mt-0.5 text-sm font-bold text-[#18332D]">
                {form.position === "end" ? "New phase will be added at the end." : `${draftNodePositionOptions.find((option) => option.value === form.position)?.label || "Position"}${selectedReferencePhase ? `: ${selectedReferencePhase.label}` : ""}`}
              </p>
            </div>
          ) : (
            <div className="rounded-xl border border-[#B9D8CC]/70 bg-[#F7F1E8]/55 px-3 py-2">
              <p className="text-[11px] font-extrabold uppercase tracking-wide text-slate-500">Parent node</p>
              <p className="mt-0.5 truncate text-sm font-bold text-[#18332D]">{getNodeLabel(selectedNode)}</p>
            </div>
          )}

          <ModuleField label="Node type">
            <AppSelect
              value={form.nodeType}
              options={nodeTypeOptions}
              onChange={(value) => setField("nodeType", value)}
              dropdownMode="fixed"
              disabled={isPhaseCreate}
            />
          </ModuleField>

          {isPhaseCreate ? (
            <div className="grid gap-3 sm:grid-cols-2">
              <ModuleField label="Position">
                <AppSelect
                  value={form.position}
                  options={draftNodePositionOptions}
                  onChange={(value) => setField("position", value)}
                  dropdownMode="fixed"
                />
              </ModuleField>
              {form.position !== "end" ? (
                <ModuleField label="Reference phase">
                  <AppSelect
                    value={form.referenceNodeId}
                    options={phaseOptions}
                    onChange={(value) => setField("referenceNodeId", value)}
                    dropdownMode="fixed"
                    placeholder="Select phase"
                  />
                </ModuleField>
              ) : null}
            </div>
          ) : null}

          <ModuleField label="Title">
            <input
              required
              type="text"
              value={form.title}
              onChange={(event) => setField("title", event.target.value)}
              className={inputClass}
            />
          </ModuleField>

          <ModuleField label="Description">
            <textarea
              value={form.description}
              onChange={(event) => setField("description", event.target.value)}
              rows={4}
              className={`${inputClass} min-h-28 resize-y`}
            />
          </ModuleField>

          {canUseLearningFields ? (
            <div className="grid gap-3 sm:grid-cols-2">
              <ModuleField label="Estimated hours">
                <input
                  type="number"
                  min="0"
                  value={form.estimatedHours}
                  onChange={(event) => setField("estimatedHours", event.target.value)}
                  className={numberInputClass}
                />
              </ModuleField>
              <ModuleField label="Difficulty">
                <AppSelect
                  value={form.difficultyLevel}
                  options={difficultyOptions}
                  onChange={(value) => setField("difficultyLevel", value)}
                  dropdownMode="fixed"
                />
              </ModuleField>
            </div>
          ) : null}

          {form.nodeType === "checkpoint" ? (
            <ModuleField label="Checkpoint type">
              <AppSelect
                value={form.checkpointType}
                options={checkpointTypeOptions}
                onChange={(value) => setField("checkpointType", value)}
                dropdownMode="fixed"
              />
            </ModuleField>
          ) : null}
        </div>

        <div className="flex justify-end gap-2 border-t border-[#B9D8CC]/70 p-4">
          <ModuleButton type="button" variant="secondary" onClick={onClose}>Cancel</ModuleButton>
          <ModuleButton type="submit" disabled={isSubmitDisabled}>
            {isSaving ? <Loader2 size={14} className="animate-spin" /> : <Plus size={14} />}
            Add node
          </ModuleButton>
        </div>
      </form>
    </div>
  );
}
