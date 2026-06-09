import {
  parseMetadata,
  isManuallyTrackableNode,
  isComputedNodeType,
  getSortedResources,
  getUsefulMetadata,
  getLockedReason,
  formatSelectionRule,
  getParentChoiceRule,
  formatMetadataValue,
  formatSkillLabel,
  getDisplayKey,
  getResourceUrl,
  formatResourceDuration,
  getDisplayText,
  groupResourcesByType,
} from "./roadmapUtils";

export default function RoadmapDetailDrawer({ node, isEnrolled, isUpdating, isLoadingDetail, onClose, onProgressChange }) {
  const status = node.progress?.status || "pending";
  const metadata = parseMetadata(node.metadata);
  const isManualNode = isManuallyTrackableNode(node);
  const isComputedNode = isComputedNodeType(node.nodeType);
  const canTrack = isEnrolled && isManualNode && node.isTrackable !== false && status !== "locked";
  const resources = getSortedResources(node.resources || []);
  const resourceGroups = groupResourcesByType(resources);
  const usefulMetadata = getUsefulMetadata(metadata);
  const lockedReason = getLockedReason(node);
  const isOptional = node.isRequired === false;

  function handleStatusChange(event) {
    const nextStatus = event.target.value;
    if (!nextStatus || nextStatus === status || !canTrack) return;
    onProgressChange(nextStatus);
  }

  return (
    <div className="pointer-events-none absolute inset-0 z-30">
      <style>{`
        @keyframes roadmapPaneOverlayIn {
          from { opacity: 0; }
          to { opacity: 1; }
        }

        @keyframes roadmapPaneIn {
          from { opacity: 0; transform: translateX(28px); }
          to { opacity: 1; transform: translateX(0); }
        }
      `}</style>

      <button
        type="button"
        aria-label="Close node details"
        onClick={onClose}
        className="pointer-events-auto absolute inset-0 bg-[#18332D]/20 backdrop-blur-[1px]"
        style={{ animation: "roadmapPaneOverlayIn 180ms ease-out both" }}
      />

      <aside
        className="pointer-events-auto relative ml-auto flex h-full w-[min(720px,calc(100vw-24px))] flex-col border-l border-[#A8D3C4] bg-white shadow-[-18px_0_35px_rgba(0,0,0,0.18)]"
        style={{ animation: "roadmapPaneIn 240ms ease-out both" }}
      >
        <header className="shrink-0 border-b border-[#A8D3C4] bg-[#EAF8F1] p-5">
          <div className="flex items-center justify-between gap-3">
            <span className="inline-flex h-6 items-center rounded-md border border-[#A8D3C4] bg-white px-2 text-[10px] font-extrabold text-slate-600">
              {isOptional ? "Optional" : "Required"}
            </span>

            <div className="flex items-center gap-2">
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
                className="inline-flex h-7 w-7 items-center justify-center rounded-md border border-[#A8D3C4] bg-white text-base font-black leading-none text-[#18332D] shadow-sm transition-colors hover:bg-[#EAF8F1] focus-visible:ring-4 focus-visible:ring-[#2FA084]/15"
              >
                ×
              </button>
            </div>
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
            <div className="mb-4 rounded-lg border border-[#A8D3C4] bg-[#EAF8F1] px-4 py-3 text-sm font-black text-[#18332D] shadow-sm">
              Loading full node details…
            </div>
          )}

          {status === "locked" && (
            <section className="rounded-lg border border-slate-300 bg-slate-100 p-4 shadow-sm">
              <h3 className="text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">
                Locked
              </h3>
              <p className="mt-2 text-sm font-bold leading-6 text-slate-700">
                {lockedReason || "Complete the required prerequisites before starting this node."}
              </p>
            </section>
          )}

          {isComputedNode && status !== "locked" && (
            <section className="rounded-lg border border-[#A8D3C4] bg-white p-4 shadow-sm">
              <h3 className="text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">
                Progress
              </h3>
              <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">
                Progress updates automatically from child items.
              </p>
            </section>
          )}

          {(node.nodeType === "choice_group" || node.selectionType || node.nodeType === "choice_option" || usefulMetadata.length > 0) && (
            <section className="mt-5 rounded-lg border border-[#A8D3C4] bg-white p-4 shadow-sm">
              <h3 className="text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">
                Requirements
              </h3>

              <div className="mt-3 grid gap-3">
                {(node.nodeType === "choice_group" || node.selectionType) && (
                  <RequirementRow label="Selection rule" value={formatSelectionRule(node)} />
                )}

                {node.nodeType === "choice_option" && getParentChoiceRule(node) && (
                  <RequirementRow label="Parent rule" value={getParentChoiceRule(node)} />
                )}

                {usefulMetadata.map((item) => (
                  <RequirementRow key={item.label} label={item.label} value={item.value} />
                ))}
              </div>
            </section>
          )}

          {node.skills?.length > 0 && (
            <section className="mt-5 rounded-lg border border-[#A8D3C4] bg-white p-4 shadow-sm">
              <h3 className="text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">
                Skills covered
              </h3>

              <div className="mt-3 flex flex-wrap gap-2">
                {node.skills.map((skill, index) => (
                  <span
                    key={getDisplayKey(skill, index)}
                    className="rounded-md border border-[#A8D3C4] bg-[#EAF8F1] px-3 py-1.5 text-xs font-black text-[#18332D]"
                  >
                    {formatSkillLabel(skill)}
                  </span>
                ))}
              </div>
            </section>
          )}

          {resourceGroups.length > 0 && (
            <section className="mt-5 rounded-lg border border-[#A8D3C4] bg-white p-4 shadow-sm">
              <div className="flex items-center justify-between gap-3">
                <h3 className="text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">
                  Learning resources
                </h3>
                <span className="rounded-md border border-[#A8D3C4] bg-[#EAF8F1] px-2 py-1 text-[10px] font-extrabold text-[#18332D]">
                  {resources.length} item{resources.length === 1 ? "" : "s"}
                </span>
              </div>

              <div className="mt-4 grid gap-4">
                {resourceGroups.map((group) => (
                  <div key={group.type} className="rounded-lg border border-[#E3EEE8] bg-[#FAFCFB] p-3">
                    <h4 className="text-[10px] font-extrabold uppercase tracking-[0.16em] text-[#1F6F5F]">
                      {group.type}
                    </h4>

                    <div className="mt-3 grid gap-3">
                      {group.resources.map((resource, index) => (
                        <ResourceCard
                          key={resource.resourceId || resource.url || `${group.type}-${index}`}
                          resource={resource}
                          index={index}
                        />
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}

          <PanelSection title="Completion criteria" items={node.completionCriteria} />
          <PanelSection title="Learning outcomes" items={node.learningOutcomes} />

          {node.reason && status !== "locked" && (
            <InfoCard title="Why this matters">{node.reason}</InfoCard>
          )}
        </div>
      </aside>
    </div>
  );
}

function StatusSelect({ status, disabled, isOptional, onChange }) {
  return (
    <select
      aria-label="Node progress status"
      value={status}
      disabled={disabled}
      onChange={onChange}
      className="h-8 min-w-[128px] cursor-pointer rounded-lg border border-[#B9D8CC] bg-[#F3FBEF] px-2.5 text-[11px] font-extrabold text-[#18332D] shadow-sm outline-none transition-colors hover:bg-[#E9F5BE] focus:border-[#81E7AF] focus:ring-4 focus:ring-[#81E7AF]/30 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500 disabled:opacity-80"
    >
      {status === "locked" && <option value="locked">Locked</option>}
      <option value="pending">Available</option>
      <option value="in_progress">In progress</option>
      <option value="completed">Completed</option>
      {(isOptional || status === "skipped") && <option value="skipped">Skipped</option>}
    </select>
  );
}

function InfoCard({ title, children }) {
  return (
    <section className="mt-5 rounded-lg border border-[#A8D3C4] bg-white p-4 shadow-sm">
      <h3 className="text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">
        {title}
      </h3>
      <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">{children}</p>
    </section>
  );
}

function ResourceCard({ resource, index = 0 }) {
  const url = getResourceUrl(resource);
  const title = resource.title || resource.name || url || "Resource";
  const provider = resource.provider || resource.source || "Learning resource";
  const duration = resource.estimatedMinutes || resource.durationMinutes || resource.duration || null;
  const isFree = resource.isFree ?? resource.free ?? null;

  const content = (
    <div className="flex items-start gap-3">
      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-[#A8D3C4] bg-white text-xs font-black text-[#18332D]">
        {index + 1}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          {isFree === true && <ResourceBadge>Free</ResourceBadge>}
          {duration && <ResourceBadge>{formatResourceDuration(duration)}</ResourceBadge>}
        </div>

        <p className="mt-1 text-sm font-black leading-5 text-[#18332D]">
          {title}
        </p>

        <p className="mt-1 truncate text-xs font-bold text-slate-500">
          {provider}
        </p>
      </div>

      {url && (
        <span className="shrink-0 rounded-md border border-[#A8D3C4] bg-white px-3 py-1 text-[10px] font-extrabold text-[#18332D]">
          Open
        </span>
      )}
    </div>
  );

  if (!url) {
    return <div className="rounded-lg border border-[#A8D3C4] bg-white p-3">{content}</div>;
  }

  return (
    <a
      href={url}
      target="_blank"
      rel="noreferrer"
      className="block rounded-lg border border-[#A8D3C4] bg-white p-3 transition-transform hover:-translate-y-0.5 hover:bg-[#EAF8F1]"
    >
      {content}
    </a>
  );
}

function RequirementRow({ label, value }) {
  if (!value) return null;

  const values = Array.isArray(value) ? value : [value];

  return (
    <div className="rounded-lg border border-[#A8D3C4] bg-[#F7F1E8] p-3">
      <p className="text-[10px] font-extrabold tracking-tight text-slate-500">
        {label}
      </p>

      {values.length > 1 ? (
        <ul className="mt-2 space-y-1 text-sm font-bold leading-6 text-[#18332D]">
          {values.map((item, index) => (
            <li key={`${label}-${index}`} className="flex gap-2">
              <span className="mt-2 h-2 w-2 shrink-0 rounded-full border border-[#A8D3C4] bg-[#2FA084]" />
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
    <span className="rounded-md border border-[#A8D3C4] bg-white px-2 py-0.5 text-[10px] font-extrabold text-[#18332D]">
      {children}
    </span>
  );
}

function PanelSection({ title, items }) {
  if (!items?.length) return null;

  return (
    <section className="mt-5 rounded-lg border border-[#A8D3C4] bg-white p-4 shadow-sm">
      <h3 className="text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">
        {title}
      </h3>

      <ul className="mt-3 space-y-2">
        {items.map((item, index) => (
          <li
            key={getDisplayKey(item, index)}
            className="flex gap-2 text-sm font-semibold leading-6 text-slate-700"
          >
            <span className="mt-2 h-2 w-2 shrink-0 rounded-full border border-[#A8D3C4] bg-[#2FA084]" />
            <span>{getDisplayText(item)}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}
