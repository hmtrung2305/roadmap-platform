import { useEffect, useRef, useState } from "react";
import {
  parseMetadata,
  isManuallyTrackableNode,
  isComputedNodeType,
  getSortedResources,
  getLockedReason,
  formatSkillLabel,
  getDisplayKey,
  getResourceUrl,
  getDisplayText,
  groupResourcesByType,
} from "./roadmapUtils";

const STATUS_META = {
  pending: {
    label: "Available",
    menuLabel: "Available",
    shortcut: "A",
    dot: "#CBD5E1",
    text: "text-slate-600 !text-[13px]",
  },
  in_progress: {
    label: "In progress",
    menuLabel: "In progress",
    shortcut: "L",
    dot: "#24B1B1",
    text: "text-[#137979] !text-[13px]",
  },
  completed: {
    label: "Done",
    menuLabel: "Done",
    shortcut: "D",
    dot: "#22C55E",
    text: "text-[#1F6F5F] !text-[13px]",
  },
  skipped: {
    label: "Skip",
    menuLabel: "Skip",
    shortcut: "S",
    dot: "#4F6F73",
    text: "text-[#4F6F73] !text-[13px]",
  },
  locked: {
    label: "Locked",
    menuLabel: "Locked",
    shortcut: "",
    dot: "#94A3B8",
    text: "text-slate-500 !text-[13px]",
  },
};

const RESOURCE_TYPE_STYLES = {
  course: {
    label: "Course",
    dot: "bg-[#2FA084]",
    badge: "border-[#A8D3C4] bg-[#EAF8F1] text-[#1F6F5F]",
    card: "border-[#A8D3C4] bg-[#F8FFFC] hover:bg-[#DFF4EA]",
  },
  article: {
    label: "Article",
    dot: "bg-[#3B82F6]",
    badge: "border-blue-200 bg-blue-50 text-blue-700",
    card: "border-blue-200 bg-blue-50/40 hover:bg-blue-100",
  },
  video: {
    label: "Video",
    dot: "bg-[#F59E0B]",
    badge: "border-amber-200 bg-amber-50 text-amber-700",
    card: "border-amber-200 bg-amber-50/40 hover:bg-amber-50",
  },
  book: {
    label: "Book",
    dot: "bg-[#8B5CF6]",
    badge: "border-violet-200 bg-violet-50 text-violet-700",
    card: "border-violet-200 bg-violet-50/40 hover:bg-violet-100",
  },
  documentation: {
    label: "Documentation",
    dot: "bg-slate-500",
    badge: "border-slate-200 bg-slate-50 text-slate-600",
    card: "border-slate-200 bg-slate-50/60 hover:bg-slate-200/70",
  },
  docs: {
    label: "Documentation",
    dot: "bg-slate-500",
    badge: "border-slate-200 bg-slate-50 text-slate-600",
    card: "border-slate-200 bg-slate-50/60 hover:bg-slate-200/70",
  },
  default: {
    label: "Resource",
    dot: "bg-[#2FA084]",
    badge: "border-[#B9D8CC] bg-white text-slate-600",
    card: "border-[#B9D8CC] bg-white hover:bg-[#EAF8F1]",
  },
};

const REQUIREMENT_TAG_STYLES = {
  required: "border-[#B9D8CC] bg-white text-[#1F6F5F]",
  optional: "border-[#F1BA88] bg-[#FFF7ED] text-[#9A5A22]",
};

const NODE_TYPE_LABELS = {
  phase: "Phase overview",
  topic: "Topic",
  project: "Project",
  checkpoint: "Checkpoint",
  choice_group: "Choice group",
  choice_option: "Choice option",
  resource_group: "Resource group",
};

export default function RoadmapDetailDrawer({ node, isEnrolled, isUpdating, isLoadingDetail, onClose, onProgressChange }) {
  const status = node.progress?.status || "pending";
  const metadata = parseMetadata(node.metadata);
  const nodeType = getNodeType(node);
  const isManualNode = isManuallyTrackableNode(node);
  const isComputedNode = isComputedNodeType(node.nodeType);
  const canTrack = isEnrolled && isManualNode && node.isTrackable !== false && status !== "locked";
  const resources = getSortedResources(normalizeArray(node.resources || node.Resources));
  const resourceGroups = groupResourcesByType(resources);
  const learningModules = normalizeArray(node.learningModules || node.LearningModules);
  const skills = normalizeArray(node.skills || node.Skills);
  const learningOutcomes = normalizeArray(node.learningOutcomes || node.LearningOutcomes);
  const completionCriteria = normalizeArray(node.completionCriteria || node.CompletionCriteria);
  const lockedReason = getLockedReason(node);
  const isOptional = node.isRequired === false;
  const description = buildDescription(node, metadata);

  function handleStatusChange(nextStatus) {
    if (!nextStatus || nextStatus === status || !canTrack) return;
    onProgressChange(nextStatus);
  }

  return (
    <div className="pointer-events-none absolute inset-0 z-30">
      <style>{`
        @keyframes roadmapPaneOverlayIn { from { opacity: 0; } to { opacity: 1; } }
        @keyframes roadmapPaneIn { from { opacity: 0; transform: translateX(20px); } to { opacity: 1; transform: translateX(0); } }
        .roadmap-drawer-scrollbar-hide { scrollbar-width: none; -ms-overflow-style: none; }
        .roadmap-drawer-scrollbar-hide::-webkit-scrollbar { display: none; }
      `}</style>

      <button
        type="button"
        aria-label="Close node details"
        onClick={onClose}
        className="pointer-events-auto absolute inset-0 bg-[#18332D]/18 backdrop-blur-[1px]"
        style={{ animation: "roadmapPaneOverlayIn 140ms ease-out both" }}
      />

      <aside
        className="pointer-events-auto relative ml-auto mr-2 mt-2 flex h-[calc(100vh-80px)] w-[min(620px,calc(100vw-32px))] flex-col overflow-hidden rounded-md border border-[#A8D3C4] bg-white shadow-[-10px_0_22px_rgba(0,0,0,0.14)]"
        style={{ animation: "roadmapPaneIn 180ms ease-out both" }}
      >
        <header className="shrink-0 border-b border-[#A8D3C4] bg-[#EAF8F1] px-3 py-1.5">
          <div className="flex items-center justify-between gap-2">
            <div className="flex flex-wrap items-center gap-2">
              <RequirementTag isOptional={isOptional} />
              <NodeTypeTag nodeType={nodeType} />
            </div>

            <div className="flex shrink-0 items-center gap-1.5">
              <StatusSelect
                status={status}
                disabled={!canTrack || isUpdating}
                isOptional={isOptional}
                onChange={handleStatusChange}
              />

              <button
                type="button"
                aria-label="Close node details"
                onClick={onClose}
                className="inline-flex h-7 w-7 items-center justify-center rounded-md border border-[#A8D3C4] bg-white text-xs font-black leading-none text-[#18332D] shadow-sm transition-colors hover:bg-[#EAF8F1] focus-visible:ring-4 focus-visible:ring-[#2FA084]/15"
              >
                <span className="-mt-px">×</span>
              </button>
            </div>
          </div>

          <h2 className="mt-1.5 min-w-0 text-[22px] font-black leading-tight text-[#18332D]">
            {node.title}
          </h2>

          {description && (
            <p className="mt-1 w-full pr-0 text-sm font-semibold leading-6 text-slate-700">
              {description}
            </p>
          )}
        </header>

        <div className="roadmap-drawer-scrollbar-hide min-h-0 flex-1 overflow-y-auto px-3.5 pb-2 pt-2.5">
          {isLoadingDetail && (
            <CompactNotice>Loading full node details…</CompactNotice>
          )}

          {status === "locked" && (
            <section className="rounded-md border border-slate-300 bg-slate-100 px-3 py-2.5 shadow-sm">
              <h3 className="text-[11px] font-extrabold uppercase tracking-[0.18em] text-slate-500">
                Locked
              </h3>
              <p className="mt-1.5 text-sm font-bold leading-6 text-slate-700">
                {lockedReason || "Complete the required prerequisites before starting this node."}
              </p>
            </section>
          )}

          {isComputedNode && status !== "locked" && (
            <CompactNotice>Progress updates automatically from related roadmap items.</CompactNotice>
          )}

          {skills.length > 0 && shouldShowSkills(nodeType) && (
            <Section title={nodeType === "phase" ? "Skill areas" : "Skills covered"}>
              <div className="flex flex-wrap gap-2">
                {skills.map((skill, index) => (
                  <span
                    key={getDisplayKey(skill, index)}
                    className="rounded-md border border-[#A8D3C4] bg-[#EAF8F1] px-2.5 py-1 text-xs font-black text-[#18332D]"
                  >
                    {formatSkillLabel(skill)}
                  </span>
                ))}
              </div>
            </Section>
          )}

          <NodeSpecificContent
            node={node}
            metadata={metadata}
            nodeType={nodeType}
            completionCriteria={completionCriteria}
          />

          {learningModules.length > 0 && shouldShowLearningModules(nodeType) && (
            <Section
              title="Curated learning modules"
              badge={
                <span className="inline-flex items-center gap-1 rounded-md border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-extrabold uppercase tracking-[0.12em] text-amber-700">
                  <span aria-hidden="true">✦</span>
                  Recommended
                </span>
              }
            >
              <p className="mb-2 text-xs font-bold leading-5 text-slate-600">
                Internal modules connected to this roadmap item.
              </p>

              <div className="grid gap-2.5">
                {learningModules.map((module, index) => (
                  <LearningModuleRow
                    key={module.skillModuleId || module.SkillModuleId || module.slug || index}
                    module={module}
                  />
                ))}
              </div>
            </Section>
          )}

          {resourceGroups.length > 0 && shouldShowResources(nodeType) && (
            <Section title="Learning resources">
              <div className="grid gap-2.5">
                {resourceGroups.map((group) => (
                  <ResourceGroup key={group.type} group={group} />
                ))}
              </div>
            </Section>
          )}

          {learningOutcomes.length > 0 && shouldShowLearningOutcomes(nodeType) && (
            <PanelSection title={getOutcomeSectionTitle(nodeType)} items={learningOutcomes} />
          )}

          {completionCriteria.length > 0 && shouldShowGenericCompletionCriteria(nodeType) && (
            <PanelSection title="Completion criteria" items={completionCriteria} />
          )}
        </div>
      </aside>
    </div>
  );
}

function NodeSpecificContent({ node, metadata, nodeType, completionCriteria }) {
  if (nodeType === "project") {
    return <ProjectNodeContent node={node} metadata={metadata} completionCriteria={completionCriteria} />;
  }

  if (nodeType === "checkpoint") {
    return <CheckpointNodeContent node={node} metadata={metadata} completionCriteria={completionCriteria} />;
  }

  if (nodeType === "choice_option") {
    return <ChoiceOptionNodeContent metadata={metadata} completionCriteria={completionCriteria} />;
  }

  if (nodeType === "choice_group") {
    return <ChoiceGroupNodeContent metadata={metadata} />;
  }

  if (nodeType === "phase") {
    return <PhaseNodeContent metadata={metadata} />;
  }

  if (nodeType === "resource_group") {
    return <ResourceGroupNodeContent metadata={metadata} />;
  }

  return <TopicNodeContent metadata={metadata} />;
}

function ProjectNodeContent({ node, metadata, completionCriteria }) {
  const projectPurpose = getFirstMetadataText(metadata, [
    "projectBrief",
    "brief",
    "goal",
    "objective",
    "purpose",
  ]);
  const suggestedSteps = getFirstMetadataList(metadata, [
    "suggestedSteps",
    "buildSteps",
    "implementationSteps",
    "steps",
  ]);
  const expectedEvidence = getFirstMetadataList(metadata, [
    "expectedEvidence",
    "evidence",
    "deliverables",
    "artifacts",
    "outputs",
  ]);
  const fallbackPurpose = node.isRequired === false
    ? "Use this optional project to practice the related skills in a concrete artifact."
    : "Use this project to connect the related topics into a reviewable artifact.";

  return (
    <Section
      title="Project guide"
      badge={node.isRequired === false ? <SubtleBadge>Optional practice</SubtleBadge> : <SubtleBadge>Milestone</SubtleBadge>}
    >
      <TextBlock title="What to build">
        {projectPurpose || fallbackPurpose}
      </TextBlock>

      <ListBlock title="Build steps" items={suggestedSteps} />
      <ListBlock title="Evidence to submit" items={expectedEvidence} />
      <ListBlock title="Done when" items={completionCriteria} />
    </Section>
  );
}

function CheckpointNodeContent({ metadata, completionCriteria }) {
  const reviewFocus = getFirstMetadataText(metadata, [
    "reviewFocus",
    "checkpointFocus",
    "focus",
    "purpose",
    "reviewPurpose",
  ]);
  const expectedEvidence = getFirstMetadataList(metadata, [
    "expectedEvidence",
    "evidence",
    "reviewEvidence",
    "artifacts",
  ]);
  const reviewQuestions = getFirstMetadataList(metadata, [
    "reviewQuestions",
    "questions",
    "reflectionQuestions",
    "selfCheckQuestions",
  ]);
  const nextActions = getFirstMetadataList(metadata, [
    "nextActions",
    "followUpActions",
    "followUps",
    "nextSteps",
  ]);

  return (
    <Section title="Checkpoint guide" badge={<SubtleBadge>Progress review</SubtleBadge>}>
      <TextBlock title="Review focus">
        {reviewFocus || "Use this checkpoint to review the current roadmap segment, collect evidence, and decide whether to continue or revisit selected topics."}
      </TextBlock>

      <ListBlock title="Review criteria" items={completionCriteria} />
      <ListBlock title="Evidence to prepare" items={expectedEvidence} />
      <ListBlock title="Reflection prompts" items={reviewQuestions} />
      <ListBlock title="Next action options" items={nextActions} />
    </Section>
  );
}

function ChoiceOptionNodeContent({ metadata, completionCriteria }) {
  const optionUse = getFirstMetadataText(metadata, [
    "whenToChoose",
    "optionUseCase",
    "useCase",
    "purpose",
    "focus",
  ]);
  const suggestedSteps = getFirstMetadataList(metadata, [
    "suggestedSteps",
    "steps",
    "practiceSteps",
  ]);
  const expectedEvidence = getFirstMetadataList(metadata, [
    "expectedEvidence",
    "evidence",
    "deliverables",
    "outputs",
  ]);

  if (!optionUse && suggestedSteps.length === 0 && expectedEvidence.length === 0 && completionCriteria.length === 0) {
    return null;
  }

  return (
    <Section title="Choice option guide" badge={<SubtleBadge>Selectable path</SubtleBadge>}>
      {optionUse && <TextBlock title="When to choose this">{optionUse}</TextBlock>}
      <ListBlock title="Practice steps" items={suggestedSteps} />
      <ListBlock title="Evidence to submit" items={expectedEvidence} />
      <ListBlock title="Done when" items={completionCriteria} />
    </Section>
  );
}

function ChoiceGroupNodeContent({ metadata }) {
  const guidance = getFirstMetadataText(metadata, [
    "choiceGuidance",
    "guidance",
    "purpose",
    "selectionGuidance",
  ]);
  const selectionNotes = getFirstMetadataList(metadata, [
    "selectionNotes",
    "options",
    "considerations",
  ]);

  if (!guidance && selectionNotes.length === 0) {
    return (
      <CompactNotice>Select one or more options from this group based on your goal and the roadmap instructions.</CompactNotice>
    );
  }

  return (
    <Section title="Choice guidance" badge={<SubtleBadge>Decision point</SubtleBadge>}>
      {guidance && <TextBlock title="How to use this choice">{guidance}</TextBlock>}
      <ListBlock title="Considerations" items={selectionNotes} />
    </Section>
  );
}

function PhaseNodeContent({ metadata }) {
  const focus = getFirstMetadataText(metadata, [
    "phaseFocus",
    "focus",
    "purpose",
    "summary",
  ]);
  const milestones = getFirstMetadataList(metadata, [
    "milestones",
    "phaseMilestones",
    "reviewPoints",
  ]);

  if (!focus && milestones.length === 0) return null;

  return (
    <Section title="Phase overview" badge={<SubtleBadge>Roadmap segment</SubtleBadge>}>
      {focus && <TextBlock title="Phase focus">{focus}</TextBlock>}
      <ListBlock title="Focus areas" items={milestones} />
    </Section>
  );
}

function ResourceGroupNodeContent({ metadata }) {
  const purpose = getFirstMetadataText(metadata, [
    "purpose",
    "summary",
    "resourcePurpose",
    "focus",
  ]);

  if (!purpose) return null;

  return (
    <Section title="Resource group overview">
      <TextBlock title="Purpose">{purpose}</TextBlock>
    </Section>
  );
}

function TopicNodeContent({ metadata }) {
  const focus = getFirstMetadataText(metadata, [
    "focus",
    "topicFocus",
    "purpose",
    "summary",
  ]);
  const practice = getFirstMetadataList(metadata, [
    "practice",
    "practiceIdeas",
    "suggestedPractice",
  ]);

  if (!focus && practice.length === 0) return null;

  return (
    <Section title="Topic guide">
      {focus && <TextBlock title="Focus">{focus}</TextBlock>}
      <ListBlock title="Suggested practice" items={practice} />
    </Section>
  );
}

function RequirementTag({ isOptional }) {
  const label = isOptional ? "Optional" : "Required";
  const style = isOptional ? REQUIREMENT_TAG_STYLES.optional : REQUIREMENT_TAG_STYLES.required;

  return (
    <span className={`inline-flex h-8 items-center rounded-md border px-2.5 text-[11px] font-extrabold ${style}`}>
      {label}
    </span>
  );
}

function NodeTypeTag({ nodeType }) {
  const label = NODE_TYPE_LABELS[nodeType] || formatLabel(nodeType || "node");

  return (
    <span className="inline-flex h-8 items-center rounded-md border border-[#D6E4DE] bg-white/75 px-2.5 text-[11px] font-extrabold text-slate-600">
      {label}
    </span>
  );
}

function StatusSelect({ status, disabled, isOptional, onChange }) {
  const [isOpen, setIsOpen] = useState(false);
  const wrapperRef = useRef(null);
  const current = STATUS_META[status] || STATUS_META.pending;
  const options = getStatusOptions(status, isOptional);

  useEffect(() => {
    if (!isOpen) return;

    function handlePointerDown(event) {
      if (!wrapperRef.current?.contains(event.target)) {
        setIsOpen(false);
      }
    }

    function handleKeyDown(event) {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    }

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen]);

  function selectStatus(nextStatus) {
    setIsOpen(false);
    onChange(nextStatus);
  }

  return (
    <div ref={wrapperRef} className="relative">
      <button
        type="button"
        disabled={disabled}
        onClick={() => setIsOpen((currentOpen) => !currentOpen)}
        className="inline-flex h-8 min-w-[128px] items-center justify-between gap-2 rounded-md border border-[#B9D8CC] bg-white px-2.5 text-sm font-semibold text-slate-600 shadow-sm outline-none transition-colors hover:bg-[#F7F1E8] focus-visible:ring-4 focus-visible:ring-[#2FA084]/15 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500 disabled:opacity-80"
      >
        <span className="inline-flex min-w-0 items-center gap-2">
          <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: current.dot }} />
          <span className="truncate !text-[13px]">{current.label}</span>
        </span>
        <span className="text-xs text-slate-400">▾</span>
      </button>

      {isOpen && !disabled && (
        <div className="absolute right-0 top-9 z-50 w-44 overflow-hidden rounded-md border border-[#D6E4DE] bg-white py-1 shadow-xl">
          {options.map((option) => {
            const meta = STATUS_META[option] || STATUS_META.pending;
            const isSelected = option === status;

            return (
              <button
                key={option}
                type="button"
                onClick={() => selectStatus(option)}
                className={["flex w-full items-center justify-between gap-3 px-2.5 py-1.5 text-left text-sm font-semibold transition-colors hover:bg-[#EAF8F1]", isSelected ? "bg-[#F3FBEF]" : ""].join(" ")}
              >
                <span className="inline-flex items-center gap-2">
                  <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: meta.dot }} />
                  <span className={meta.text}>{meta.menuLabel}</span>
                </span>
                {meta.shortcut && <span className="text-xs font-bold text-slate-400">{meta.shortcut}</span>}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

function getStatusOptions(status, isOptional) {
  if (status === "locked") return ["locked"];

  const baseOptions = ["pending", "in_progress", "completed"];
  if (isOptional || status === "skipped") return [...baseOptions, "skipped"];

  return baseOptions;
}

function buildDescription(node, metadata) {
  const description = getDisplayText(node.description || "");
  const reason = getDisplayText(node.reason || metadata?.why || metadata?.whyItMatters || "");

  if (!reason) return description;

  const cleanedReason = reason.replace(/^why\s+this\s+matters:?\s*/i, "").trim();
  const sentence = cleanedReason.endsWith(".") ? cleanedReason : `${cleanedReason}.`;

  if (!description) return `This item helps you understand ${sentence.charAt(0).toLowerCase()}${sentence.slice(1)}`;

  return `${description} In practice, this helps you see where the idea is useful: ${sentence}`;
}

function Section({ title, badge, children }) {
  return (
    <section className="mt-2 rounded-md border border-[#A8D3C4] bg-white px-3 py-2 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <h3 className="min-w-0 text-[11px] font-extrabold uppercase tracking-[0.18em] text-slate-500">
          {title}
        </h3>
        {badge && <div className="shrink-0">{badge}</div>}
      </div>
      <div className="mt-1.5">{children}</div>
    </section>
  );
}

function SubtleBadge({ children }) {
  return (
    <span className="inline-flex items-center rounded-md border border-[#D6E4DE] bg-slate-50 px-2 py-0.5 text-[10px] font-extrabold uppercase tracking-[0.12em] text-slate-600">
      {children}
    </span>
  );
}

function CompactNotice({ children }) {
  return (
    <div className="mb-2 rounded-md border border-[#A8D3C4] bg-[#EAF8F1] px-3 py-2 text-sm font-black text-[#18332D] shadow-sm">
      {children}
    </div>
  );
}

function TextBlock({ title, children }) {
  if (!children) return null;

  return (
    <div>
      <h4 className="text-[11px] font-extrabold uppercase tracking-[0.14em] text-slate-500">
        {title}
      </h4>
      <p className="mt-1 text-sm font-semibold leading-6 text-slate-700">
        {children}
      </p>
    </div>
  );
}

function ListBlock({ title, items }) {
  const normalizedItems = normalizeArray(items).map(getDisplayText).filter(Boolean);

  if (normalizedItems.length === 0) return null;

  return (
    <div className="mt-2">
      <h4 className="text-[11px] font-extrabold uppercase tracking-[0.14em] text-slate-500">
        {title}
      </h4>
      <ul className="mt-1 space-y-1.5">
        {normalizedItems.map((item, index) => (
          <li key={`${title}-${index}`} className="flex gap-2 text-sm font-semibold leading-6 text-slate-700">
            <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-[#2FA084]" />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function LearningModuleRow({ module }) {
  const title = module.title || module.Title || "Learning module";
  const slug = module.slug || module.Slug;
  const url = slug ? `/learning-modules/${slug}/overview` : null;

  const content = (
    <div className="truncate text-sm font-black leading-5 text-[#18332D]">
      <span>{title}</span>
      <span className="font-bold text-slate-500"> - Roadmap Platform</span>
    </div>
  );

  const className =
    "block rounded-md border border-[#A8D3C4] bg-[#F8FFFC] px-3 py-2.5 shadow-sm transition-colors hover:bg-[#DFF4EA]";

  if (!url) {
    return <div className={className}>{content}</div>;
  }

  return (
    <a href={url} className={className}>
      {content}
    </a>
  );
}

function ResourceGroup({ group }) {
  const typeStyle = getResourceTypeStyle(group.type);

  return (
    <div className="rounded-md border border-[#E3EEE8] bg-[#FAFCFB] p-2">
      <div className="mb-2 flex items-center gap-2">
        <span className={`h-2.5 w-2.5 rounded-full ${typeStyle.dot}`} />
        <span className={`rounded-md border px-2 py-0.5 text-[10px] font-extrabold uppercase tracking-[0.12em] ${typeStyle.badge}`}>
          {typeStyle.label}
        </span>
      </div>

      <div className="grid gap-2">
        {group.resources.map((resource, index) => (
          <ResourceRow
            key={resource.resourceId || resource.learningResourceId || resource.url || `${group.type}-${index}`}
            resource={resource}
            typeStyle={typeStyle}
          />
        ))}
      </div>
    </div>
  );
}

function ResourceRow({ resource, typeStyle }) {
  const url = getResourceUrl(resource);
  const title = resource.title || resource.Title || resource.name || url || "Resource";
  const provider = resource.provider || resource.Provider || resource.source || "Learning resource";
  const className = `block rounded-md border px-3 py-2 text-sm font-black leading-5 text-[#18332D] shadow-sm transition-colors ${typeStyle.card}`;

  const content = (
    <div>{title} <span className="font-bold text-slate-500">- {provider}</span></div>
  );

  if (!url) {
    return <div className={className}>{content}</div>;
  }

  return (
    <a href={url} target="_blank" rel="noreferrer" className={className}>
      {content}
    </a>
  );
}

function getResourceTypeStyle(type) {
  const normalizedType = String(type || "default").trim().toLowerCase();
  return RESOURCE_TYPE_STYLES[normalizedType] || RESOURCE_TYPE_STYLES.default;
}

function PanelSection({ title, items }) {
  if (!items?.length) return null;

  return (
    <Section title={title}>
      <ul className="space-y-2">
        {items.map((item, index) => (
          <li
            key={getDisplayKey(item, index)}
            className="flex gap-2 text-sm font-semibold leading-6 text-slate-700"
          >
            <span className="mt-2 h-2 w-2 shrink-0 rounded-full bg-[#2FA084]" />
            <span>{getDisplayText(item)}</span>
          </li>
        ))}
      </ul>
    </Section>
  );
}

function shouldShowSkills(nodeType) {
  return nodeType !== "choice_group" && nodeType !== "resource_group";
}

function shouldShowLearningModules(nodeType) {
  return nodeType !== "phase" && nodeType !== "choice_group" && nodeType !== "resource_group";
}

function shouldShowResources(nodeType) {
  return nodeType !== "phase" && nodeType !== "choice_group" && nodeType !== "resource_group";
}

function shouldShowLearningOutcomes(nodeType) {
  return nodeType !== "choice_group" && nodeType !== "resource_group";
}

function shouldShowGenericCompletionCriteria(nodeType) {
  return nodeType !== "project" && nodeType !== "checkpoint" && nodeType !== "choice_option";
}

function getOutcomeSectionTitle(nodeType) {
  if (nodeType === "project") return "Project outcomes";
  if (nodeType === "checkpoint") return "Review outcomes";
  if (nodeType === "choice_option") return "Option outcomes";
  return "Learning outcomes";
}

function getNodeType(node) {
  return node?.nodeType || node?.NodeType || node?.node_type || "topic";
}

function getFirstMetadataText(metadata, keys) {
  for (const key of keys) {
    const value = getMetadataValue(metadata, key);
    const text = getDisplayText(value);

    if (isDisplayableMetadataText(text)) return text;
  }

  return "";
}

function getFirstMetadataList(metadata, keys) {
  for (const key of keys) {
    const list = normalizeArray(getMetadataValue(metadata, key))
      .map(getDisplayText)
      .filter(isDisplayableMetadataText);

    if (list.length > 0) return list;
  }

  return [];
}

function getMetadataValue(metadata, key) {
  if (!metadata || !key) return undefined;

  if (Object.prototype.hasOwnProperty.call(metadata, key)) {
    return metadata[key];
  }

  const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
  if (Object.prototype.hasOwnProperty.call(metadata, pascalKey)) {
    return metadata[pascalKey];
  }

  const snakeKey = key.replace(/[A-Z]/g, (letter) => `_${letter.toLowerCase()}`);
  if (Object.prototype.hasOwnProperty.call(metadata, snakeKey)) {
    return metadata[snakeKey];
  }

  return undefined;
}

function isDisplayableMetadataText(value) {
  const text = String(value || "").trim();

  if (!text) return false;

  const looksLikeInternalToken = /^[a-z0-9]+(?:_[a-z0-9]+)+$/.test(text);
  if (looksLikeInternalToken) return false;

  return true;
}

function normalizeArray(value) {
  if (!value) return [];
  if (Array.isArray(value)) return value;

  if (typeof value === "string") {
    const trimmed = value.trim();
    if (!trimmed) return [];

    try {
      const parsed = JSON.parse(trimmed);
      if (Array.isArray(parsed)) return parsed;
    } catch {
      // Fall back to line-based parsing below.
    }

    return trimmed
      .split(/\r?\n|\s*;\s*/)
      .map((item) => item.replace(/^[-*•]\s*/, "").trim())
      .filter(Boolean);
  }

  return [value];
}

function formatLabel(value) {
  return String(value || "")
    .replace(/_/g, " ")
    .replace(/-/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .trim()
    .replace(/\b\w/g, (char) => char.toUpperCase());
}
