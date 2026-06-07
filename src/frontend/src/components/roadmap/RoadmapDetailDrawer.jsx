import {
  parseMetadata,
  isManuallyTrackableNode,
  isComputedNodeType,
  getSortedResources,
  getUsefulMetadata,
  getProgressSummary,
  getLockedReason,
  getProgressMessage,
  formatStatusLabel,
  formatNodeTypeLabel,
  formatReadableLabel,
  formatSelectionRule,
  getParentChoiceRule,
  formatMetadataValue,
  formatSkillLabel,
  getDisplayKey,
  getResourceUrl,
  formatResourceType,
  formatResourceDuration,
  getDisplayText
} from "./roadmapUtils";

export default function RoadmapDetailDrawer({ node, isEnrolled, isUpdating, isLoadingDetail, onClose, onProgressChange }) {
  const status = node.progress?.status || "pending";
  const metadata = parseMetadata(node.metadata);
  const isManualNode = isManuallyTrackableNode(node);
  const isComputedNode = isComputedNodeType(node.nodeType);
  const canTrack = isEnrolled && isManualNode && node.isTrackable !== false && status !== "locked";
  const resources = getSortedResources(node.resources || []);
  const usefulMetadata = getUsefulMetadata(metadata);
  const progressSummary = getProgressSummary(node);
  const lockedReason = getLockedReason(node);

  return (
    <div className="pointer-events-none absolute inset-0 z-30">
      <button
        type="button"
        aria-label="Close node details"
        onClick={onClose}
        className="pointer-events-auto absolute inset-0 bg-[#1F6F5F]/20 backdrop-blur-[1px] transition-opacity"
      />

      <aside className="pointer-events-auto relative ml-auto flex h-full w-[min(720px,calc(100vw-24px))] flex-col border-l border-[#B9D8CC] bg-white shadow-[-18px_0_35px_rgba(0,0,0,0.22)]">
        <header className="shrink-0 border-b border-[#B9D8CC] bg-[#EAF8F1] p-5">
          <button
            type="button"
            onClick={onClose}
            className="mb-3 text-xs font-extrabold tracking-[0.16em] text-[#1F6F5F] hover:underline"
          >
            Close details →
          </button>

          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge>{formatNodeTypeLabel(node.nodeType)}</StatusBadge>
            <StatusBadge>{formatStatusLabel(status)}</StatusBadge>
            {node.checkpointType && <StatusBadge>{formatReadableLabel(node.checkpointType)}</StatusBadge>}
          </div>

          <h2 className="mt-3 text-2xl font-black leading-tight text-[#18332D]">
            {node.title}
          </h2>

          {node.description && (
            <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">
              {node.description}
            </p>
          )}
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto p-5">
          {isLoadingDetail && (
            <div className="mb-4 rounded-xl border border-[#B9D8CC] bg-[#EAF8F1] px-4 py-3 text-sm font-black text-[#18332D] shadow-[4px_4px_0_rgba(0,0,0,0.18)]">
              Loading full node details…
            </div>
          )}

          <section className="rounded-xl border border-[#B9D8CC] bg-[#EAF8F1] p-4 shadow-[4px_4px_0_rgba(0,0,0,0.22)]">
            <div className="flex items-center justify-between gap-3">
              <div>
                <h3 className="text-xs font-extrabold tracking-[0.16em] text-slate-500">
                  Progress
                </h3>
                <p className="mt-1 text-sm font-bold text-slate-700">
                  {getProgressMessage(status, isComputedNode)}
                </p>
              </div>

              <StatusBadge>{formatStatusLabel(status)}</StatusBadge>
            </div>

            {progressSummary && (
              <p className="mt-3 rounded-xl border border-[#B9D8CC] bg-white p-3 text-sm font-black text-[#18332D]">
                {progressSummary}
              </p>
            )}

            {status === "locked" && (
              <p className="mt-3 rounded-xl border border-[#B9D8CC] bg-[#E5E7EB] p-3 text-sm font-bold leading-6 text-slate-700">
                {lockedReason || "This node is locked until the required prerequisites are completed."}
              </p>
            )}

            {canTrack && (
              <div className="mt-4 grid gap-2 sm:grid-cols-2">
                {status === "pending" && (
                  <ActionButton disabled={isUpdating} onClick={() => onProgressChange("in_progress")} variant="secondary">
                    Start
                  </ActionButton>
                )}

                <ActionButton
                  disabled={isUpdating}
                  onClick={() => onProgressChange(status === "completed" ? "in_progress" : "completed")}
                  variant="primary"
                >
                  {status === "completed" ? "Mark In Progress" : "Mark Completed"}
                </ActionButton>

                {status !== "pending" && (
                  <ActionButton disabled={isUpdating} onClick={() => onProgressChange("pending")} variant="secondary">
                    Reset to Pending
                  </ActionButton>
                )}

                {!node.isRequired && (
                  <ActionButton disabled={isUpdating} onClick={() => onProgressChange("skipped")} variant="secondary">
                    Skip Node
                  </ActionButton>
                )}
              </div>
            )}
          </section>

          <section className="mt-5 rounded-xl border border-[#B9D8CC] bg-white p-4 shadow-[4px_4px_0_rgba(0,0,0,0.18)]">
            <h3 className="text-xs font-extrabold tracking-[0.16em] text-slate-500">
              Planning info
            </h3>

            <div className="mt-3 grid grid-cols-2 gap-3">
              {node.difficultyLevel || metadata.difficulty ? (
                <DetailStat label="Difficulty" value={formatReadableLabel(node.difficultyLevel || metadata.difficulty)} />
              ) : null}

              {node.estimatedHours ? (
                <DetailStat label="Estimated time" value={`${node.estimatedHours}h`} />
              ) : null}

              <DetailStat label="Required" value={node.isRequired ? "Yes" : "Optional"} />
              <DetailStat label="Tracking" value={isComputedNode ? "Computed" : "Manual"} />
            </div>
          </section>

          {(node.nodeType === "choice_group" || node.selectionType || node.nodeType === "choice_option" || node.nodeType === "checkpoint" || usefulMetadata.length > 0) && (
            <section className="mt-5 rounded-xl border border-[#B9D8CC] bg-white p-4 shadow-[4px_4px_0_rgba(0,0,0,0.18)]">
              <h3 className="text-xs font-extrabold tracking-[0.16em] text-slate-500">
                Requirements
              </h3>

              <div className="mt-3 grid gap-3">
                {(node.nodeType === "choice_group" || node.selectionType) && (
                  <RequirementRow label="Selection rule" value={formatSelectionRule(node)} />
                )}

                {node.nodeType === "choice_option" && getParentChoiceRule(node) && (
                  <RequirementRow label="Parent rule" value={getParentChoiceRule(node)} />
                )}

                {node.nodeType === "checkpoint" && node.checkpointType && (
                  <RequirementRow label="Checkpoint type" value={formatReadableLabel(node.checkpointType)} />
                )}

                {usefulMetadata.map((item) => (
                  <RequirementRow key={item.label} label={item.label} value={item.value} />
                ))}
              </div>
            </section>
          )}

          {node.skills?.length > 0 && (
            <section className="mt-5 rounded-xl border border-[#B9D8CC] bg-white p-4 shadow-[4px_4px_0_rgba(0,0,0,0.18)]">
              <h3 className="text-xs font-extrabold tracking-[0.16em] text-slate-500">
                Skills covered
              </h3>

              <div className="mt-3 flex flex-wrap gap-2">
                {node.skills.map((skill, index) => (
                  <span
                    key={getDisplayKey(skill, index)}
                    className="rounded-xl border border-[#B9D8CC] bg-[#EAF8F1] px-3 py-1.5 text-xs font-black text-[#18332D]"
                  >
                    {formatSkillLabel(skill)}
                  </span>
                ))}
              </div>
            </section>
          )}

          {resources.length > 0 && (
            <section className="mt-5 rounded-xl border border-[#B9D8CC] bg-white p-4 shadow-[4px_4px_0_rgba(0,0,0,0.18)]">
              <div className="flex items-center justify-between gap-3">
                <h3 className="text-xs font-extrabold tracking-[0.16em] text-slate-500">
                  Learning resources
                </h3>
                <span className="rounded-xl border border-[#B9D8CC] bg-[#EAF8F1] px-2 py-1 text-[10px] font-extrabold text-[#18332D]">
                  {resources.length} item{resources.length === 1 ? "" : "s"}
                </span>
              </div>

              <div className="mt-4 grid gap-3">
                {resources.map((resource, index) => (
                  <ResourceCard
                    key={resource.resourceId || resource.url || `resource-${index}`}
                    resource={resource}
                    index={index}
                    isPrimary={index < 3}
                  />
                ))}
              </div>
            </section>
          )}

          <PanelSection title="Completion criteria" items={node.completionCriteria} compact />
          <PanelSection title="Learning outcomes" items={node.learningOutcomes} compact />

          {node.reason && status !== "locked" && (
            <InfoCard title="Why this matters">{node.reason}</InfoCard>
          )}

          {isComputedNode && (
            <InfoCard title="Computed tracking">
              This item is completed automatically based on its child nodes. Manual progress buttons are hidden.
            </InfoCard>
          )}
        </div>
      </aside>
    </div>
  );
}

function InfoCard({ title, children }) {
  return (
    <section className="mt-5 rounded-xl border border-[#B9D8CC] bg-white p-4 shadow-[4px_4px_0_rgba(0,0,0,0.18)]">
      <h3 className="text-xs font-extrabold tracking-[0.16em] text-slate-500">
        {title}
      </h3>
      <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">{children}</p>
    </section>
  );
}

function ResourceCard({ resource, index = 0, isPrimary = false }) {
  const url = getResourceUrl(resource);
  const title = resource.title || resource.name || url || "Resource";
  const provider = resource.provider || resource.source || "Learning resource";
  const type = formatResourceType(resource.resourceType || resource.type || "link");
  const duration = resource.estimatedMinutes || resource.durationMinutes || resource.duration || null;
  const difficulty = resource.difficultyLevel || resource.difficulty || null;
  const description = resource.description || resource.summary || null;
  const isFree = resource.isFree ?? resource.free ?? null;

  const content = (
    <>
      <div className="flex items-start gap-3">
        <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl border border-[#B9D8CC] bg-white text-xs font-black text-[#18332D]">
          {index + 1}
        </span>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            {isPrimary && (
              <span className="rounded-xl border border-[#B9D8CC] bg-[#2FA084] px-2 py-0.5 text-[9px] font-extrabold text-white">
                Recommended
              </span>
            )}

            <span className="rounded-xl border border-[#B9D8CC] bg-white px-2 py-0.5 text-[9px] font-extrabold text-[#18332D]">
              {type}
            </span>

            {isFree === true && <ResourceBadge>Free</ResourceBadge>}
          </div>

          <p className="mt-2 text-sm font-black leading-5 text-[#18332D]">
            {title}
          </p>

          <p className="mt-1 text-xs font-bold text-slate-500">
            {provider} · {type}
          </p>
        </div>

        {url && (
          <span className="shrink-0 rounded-xl border border-[#B9D8CC] bg-white px-3 py-1 text-[10px] font-extrabold text-[#18332D]">
            Open
          </span>
        )}
      </div>

      {description && (
        <p className="mt-3 text-xs font-semibold leading-5 text-slate-600">
          {description}
        </p>
      )}

      {(difficulty || duration) && (
        <div className="mt-3 flex flex-wrap gap-2">
          {difficulty && <ResourceBadge>{formatReadableLabel(difficulty)}</ResourceBadge>}
          {duration && <ResourceBadge>{formatResourceDuration(duration)}</ResourceBadge>}
        </div>
      )}
    </>
  );

  if (!url) {
    return (
      <div className="rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] p-3">
        {content}
      </div>
    );
  }

  return (
    <a
      href={url}
      target="_blank"
      rel="noreferrer"
      className="block rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] p-3 transition-transform hover:-translate-y-0.5 hover:bg-[#EAF8F1]"
    >
      {content}
    </a>
  );
}

function StatusBadge({ children }) {
  return (
    <span className="rounded-xl border border-[#B9D8CC] bg-white px-2 py-1 text-[10px] font-extrabold tracking-tight text-[#18332D]">
      {children}
    </span>
  );
}

function ActionButton({ children, disabled, onClick, variant = "secondary" }) {
  const className = variant === "primary"
    ? "rounded-xl border border-[#B9D8CC] bg-[#2FA084] px-4 py-3 text-sm font-extrabold tracking-[0.08em] text-white shadow-sm disabled:opacity-60"
    : "rounded-xl border border-[#B9D8CC] bg-white px-4 py-3 text-sm font-extrabold tracking-[0.08em] text-[#18332D] shadow-sm disabled:opacity-60";

  return (
    <button type="button" disabled={disabled} onClick={onClick} className={className}>
      {children}
    </button>
  );
}

function RequirementRow({ label, value }) {
  if (!value) return null;

  const values = Array.isArray(value) ? value : [value];

  return (
    <div className="rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] p-3">
      <p className="text-[10px] font-extrabold tracking-tight text-slate-500">
        {label}
      </p>

      {values.length > 1 ? (
        <ul className="mt-2 space-y-1 text-sm font-bold leading-6 text-[#18332D]">
          {values.map((item, index) => (
            <li key={`${label}-${index}`} className="flex gap-2">
              <span className="mt-2 h-2 w-2 shrink-0 rounded-xl border border-[#B9D8CC] bg-[#2FA084]" />
              <span>{formatMetadataValue(item)}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-1 text-sm font-bold leading-6 text-[#18332D]">{formatMetadataValue(values[0])}</p>
      )}
    </div>
  );
}

function ResourceBadge({ children }) {
  return (
    <span className="rounded-xl border border-[#B9D8CC] bg-white px-2 py-0.5 text-[10px] font-extrabold text-[#18332D]">
      {children}
    </span>
  );
}

function HeaderBadge({ children }) {
  return (
    <span className="rounded-xl border border-[#B9D8CC] bg-[#EAF8F1] px-2 py-1 text-[10px] font-extrabold tracking-[0.1em] text-[#18332D]">
      {children}
    </span>
  );
}

function DetailStat({ label, value }) {
  return (
    <div className="rounded-xl border border-[#B9D8CC] bg-white p-3">
      <p className="text-[10px] font-extrabold tracking-tight text-slate-500">
        {label}
      </p>

      <p className="mt-1 text-sm font-black text-[#18332D]">{value}</p>
    </div>
  );
}

function PanelSection({ title, items }) {
  if (!items?.length) return null;

  return (
    <section className="mt-5 rounded-xl border border-[#B9D8CC] bg-white p-4 shadow-[4px_4px_0_rgba(0,0,0,0.18)]">
      <h3 className="text-xs font-extrabold tracking-[0.16em] text-slate-500">
        {title}
      </h3>

      <ul className="mt-3 space-y-2">
        {items.map((item, index) => (
          <li
            key={getDisplayKey(item, index)}
            className="flex gap-2 text-sm font-semibold leading-6 text-slate-700"
          >
            <span className="mt-2 h-2 w-2 shrink-0 rounded-xl border border-[#B9D8CC] bg-[#2FA084]" />
            <span>{getDisplayText(item)}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}
