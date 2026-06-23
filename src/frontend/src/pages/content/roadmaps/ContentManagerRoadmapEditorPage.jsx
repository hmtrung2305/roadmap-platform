import { useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Layers3, Loader2, Map as MapIcon } from "lucide-react";

import AppSelect from "../../../components/common/AppSelect";
import {
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
} from "../../../components/learningModules/learningModuleUi";
import EditorStat from "../../../features/roadmapEditor/components/EditorStat";
import MetadataEditor from "../../../features/roadmapEditor/components/MetadataEditor";
import NodeDetailsPanel from "../../../features/roadmapEditor/components/NodeDetailsPanel";
import NodeSearchCombobox from "../../../features/roadmapEditor/components/NodeSearchCombobox";
import RoadmapGraphCanvas from "../../../features/roadmapEditor/components/RoadmapGraphCanvas";
import useContentRoadmapEditor from "../../../features/roadmapEditor/hooks/useContentRoadmapEditor";
import { prettyStatus } from "../../../features/roadmapEditor/roadmapEditorUtils";

export default function ContentManagerRoadmapEditorPage() {
  const navigate = useNavigate();
  const { roadmapId } = useParams();
  const editor = useContentRoadmapEditor(roadmapId);

  const {
    detail,
    selectedNodeId,
    setSelectedNodeId,
    selectedNode,
    metadataForm,
    setMetadataForm,
    nodeForm,
    setNodeForm,
    graphFocusRequest,
    skillSearch,
    setSkillSearch,
    skillResults,
    resourceSearch,
    setResourceSearch,
    resourceResults,
    isLoading,
    showLoadingState,
    isSavingMetadata,
    isSavingNode,
    isSearchingSkills,
    isSearchingResources,
    error,
    workspaceMode,
    setWorkspaceMode,
    allNodes,
    missingMappingCount,
    versionOptions,
    hasMetadataChanges,
    hasNodeDetailsChanges,
    saveMetadata,
    saveNode,
    loadSkillSuggestions,
    loadResourceSuggestions,
    searchSkills,
    searchResources,
    addSkill,
    removeSkill,
    addResource,
    removeResource,
    handleVersionChange,
    focusNodeFromSearch,
  } = editor;

  if (isLoading && !detail) {
    if (!showLoadingState) {
      return (
        <ModulePageShell compact>
          <div className="min-h-[280px]" />
        </ModulePageShell>
      );
    }

    return (
      <ModulePageShell compact>
        <ModuleCard className="flex items-center justify-center gap-2 p-10 text-center text-sm font-bold text-slate-600">
          <Loader2 size={16} className="animate-spin text-[#1F6F5F]" />
          Loading roadmap editor...
        </ModuleCard>
      </ModulePageShell>
    );
  }

  if (error) {
    return (
      <ModulePageShell compact>
        <div className="space-y-4">
          <button
            type="button"
            onClick={() => navigate("/content/roadmaps")}
            className="inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to roadmaps
          </button>
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            {error}
          </ModuleCard>
        </div>
      </ModulePageShell>
    );
  }

  if (!detail) {
    return (
      <ModulePageShell compact>
        <ModuleEmptyState title="Roadmap not found">
          The roadmap could not be loaded.
        </ModuleEmptyState>
      </ModulePageShell>
    );
  }

  return (
    <ModulePageShell compact>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <button
            type="button"
            onClick={() => navigate("/content/roadmaps")}
            className="inline-flex cursor-pointer items-center gap-2 text-sm font-bold text-[#1F6F5F]"
          >
            <ArrowLeft size={16} /> Back to roadmap selection
          </button>

          {isLoading && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-white px-2.5 py-1 text-xs font-bold text-slate-600 shadow-sm">
              <Loader2 size={13} className="animate-spin text-[#1F6F5F]" />
              Updating...
            </span>
          )}
        </div>

        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <MapIcon size={22} />
              </div>
              <div className="min-w-0">
                <h1 className="truncate text-2xl font-extrabold text-[#18332D]">{detail.title}</h1>
              </div>
            </div>

            {versionOptions.length > 0 && (
              <div className="w-44 shrink-0 self-center">
                <AppSelect
                  value={detail.roadmapVersionId}
                  options={versionOptions}
                  ariaLabel="Select roadmap version"
                  onChange={handleVersionChange}
                />
              </div>
            )}
          </div>
        </section>

        <div className="grid gap-3 md:grid-cols-4">
          <EditorStat label="Career role" value={detail.careerRole?.name || detail.slug || "Not set"} />
          <EditorStat label="Current status" value={prettyStatus(detail.status)} />
          <EditorStat label="Node count" value={allNodes.length} />
          <EditorStat label="Needs review" value={missingMappingCount} hint="Missing mappings" />
        </div>

        <div className="rounded-xl border border-[#B9D8CC] bg-white p-2 shadow-sm">
          <div className="grid gap-2 sm:grid-cols-2">
            <button
              type="button"
              onClick={() => setWorkspaceMode("nodes")}
              className={[
                "rounded-lg px-4 py-2.5 text-center transition",
                workspaceMode === "nodes" ? "bg-[#1F6F5F] text-white" : "text-[#18332D] hover:bg-[#F7F1E8]",
              ].join(" ")}
            >
              <div className="text-sm font-extrabold">Node editor</div>
            </button>
            <button
              type="button"
              onClick={() => setWorkspaceMode("metadata")}
              className={[
                "rounded-lg px-4 py-2.5 text-center transition",
                workspaceMode === "metadata" ? "bg-[#1F6F5F] text-white" : "text-[#18332D] hover:bg-[#F7F1E8]",
              ].join(" ")}
            >
              <div className="text-sm font-extrabold">Roadmap metadata</div>
            </button>
          </div>
        </div>

        {workspaceMode === "metadata" ? (
          <MetadataEditor
            detail={detail}
            form={metadataForm}
            setForm={setMetadataForm}
            isSaving={isSavingMetadata}
            isDirty={hasMetadataChanges}
            onSave={saveMetadata}
          />
        ) : (
          <ModuleCard className="overflow-visible">
            <div className="border-b border-[#B9D8CC]/70 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
                    <Layers3 size={16} />
                  </div>
                  <h2 className="text-base font-extrabold text-[#18332D]">Node editor</h2>
                </div>

                <NodeSearchCombobox nodes={allNodes} onSelect={focusNodeFromSearch} />
              </div>
            </div>

            <div className="grid gap-4 p-4 xl:grid-cols-[minmax(340px,0.72fr)_minmax(560px,1.15fr)]">
              <div className="min-w-0">
                <RoadmapGraphCanvas
                  detail={detail}
                  nodes={allNodes}
                  selectedNodeId={selectedNodeId}
                  focusNodeRequest={graphFocusRequest}
                  onSelect={setSelectedNodeId}
                />
              </div>

              <NodeDetailsPanel
                selectedNode={selectedNode}
                nodeForm={nodeForm}
                setNodeForm={setNodeForm}
                isSavingNode={isSavingNode}
                isDirty={hasNodeDetailsChanges}
                onSaveNode={saveNode}
                skillSearch={skillSearch}
                setSkillSearch={setSkillSearch}
                skillResults={skillResults}
                isSearchingSkills={isSearchingSkills}
                onSearchSkills={searchSkills}
                onOpenSkillSearch={loadSkillSuggestions}
                onAddSkill={addSkill}
                onRemoveSkill={removeSkill}
                resourceSearch={resourceSearch}
                setResourceSearch={setResourceSearch}
                resourceResults={resourceResults}
                isSearchingResources={isSearchingResources}
                onSearchResources={searchResources}
                onOpenResourceSearch={loadResourceSuggestions}
                onAddResource={addResource}
                onRemoveResource={removeResource}
              />
            </div>
          </ModuleCard>
        )}
      </div>
    </ModulePageShell>
  );
}
