const GUIDE_STEPS = [
  {
    title: "Start your roadmap",
    eyebrow: "Step 1",
    targetLabel: "Click Start roadmap",
    body:
      "Click Start roadmap to begin. Your progress will be saved from this point.",
    tips: [
      "After starting, the roadmap will guide you to the first phase.",
    ],
  },
  {
    title: "Follow the first phase",
    eyebrow: "Step 2",
    targetLabel: "First phase",
    body:
      "A phase is a main milestone in your learning path. Learn phases from top to bottom.",
    tips: [
      "Click Continue to move to the first learning node.",
    ],
  },
  {
    title: "Open a learning node",
    eyebrow: "Step 3",
    targetLabel: "Click this node",
    body:
      "Click the highlighted node to view what you need to learn next.",
    tips: [
      "Prioritize nodes from top to bottom. Nodes on the same row can be learned in any order if they are unlocked.",
    ],
  },
  {
    title: "Open the module",
    eyebrow: "Step 4",
    targetLabel: "Open module",
    body:
      "Click Open module to view the recommended learning module for this node.",
    tips: [
      "Review the module overview first, then start learning from there.",
    ],
  },
  {
    title: "Learn and practice",
    eyebrow: "Step 5",
    targetLabel: "Start learning",
    body:
      "Study the lessons, ask AI when you need help, and complete the quiz when you are ready.",
    tips: [
      "Finish the module before returning to the roadmap.",
    ],
  },
  {
    title: "Set status to Done",
    eyebrow: "Step 6",
    targetLabel: "Set Done",
    body:
      "Open the status menu and choose Done after you finish the module.",
    tips: [
      "Then continue with the next available node.",
    ],
  },
];

export default function RoadmapStudyGuide({
  isOpen,
  stepIndex,
  targetRect,
  isEnrolled,
  isEnrolling,
  recommendedPhaseTitle,
  recommendedTopicTitle,
  hasSelectedNodeModule,
  onClose,
  onStepChange,
  onStartRoadmap,
  onFocusPhase,
  onMoveToTopic,
  onFocusTopic,
  onOpenSelectedModule,
}) {
  if (!isOpen) return null;

  const currentIndex = Math.min(Math.max(stepIndex, 0), GUIDE_STEPS.length - 1);
  const step = GUIDE_STEPS[currentIndex];
  const isFirst = currentIndex === 0;
  const isLast = currentIndex === GUIDE_STEPS.length - 1;

  const primaryAction = getPrimaryAction({
    currentIndex,
    isEnrolled,
    isEnrolling,
    hasSelectedNodeModule,
    onStartRoadmap,
    onFocusPhase,
    onMoveToTopic,
    onFocusTopic,
    onOpenSelectedModule,
    onStepChange,
  });

  return (
    <div className="pointer-events-none fixed inset-0 z-50">
      <GuideMotionStyles />
      {targetRect && <GuideTargetHighlight rect={targetRect} label={step.targetLabel} />}

      <aside className="roadmap-guide-card pointer-events-auto absolute bottom-5 left-5 w-[min(440px,calc(100vw-40px))] overflow-hidden rounded-xl border border-[#A8D3C4] bg-white shadow-[0_24px_80px_rgba(24,51,45,0.22)]">
        <header className="border-b border-[#D6E4DE] bg-[#EAF8F1] px-4 py-3">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-[11px] font-black uppercase tracking-[0.18em] text-[#1F6F5F]">
                Learning guide
              </p>
              <h2 className="mt-1 text-xl font-black leading-tight text-[#18332D]">
                {step.title}
              </h2>
            </div>

            <button
              type="button"
              onClick={onClose}
              className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-[#A8D3C4] bg-white text-sm font-black text-[#18332D] shadow-sm transition hover:bg-[#F7F1E8]"
            >
              ×
            </button>
          </div>
        </header>

        <div key={currentIndex} className="roadmap-guide-step px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <span className="rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-1 text-[11px] font-black uppercase tracking-[0.14em] text-slate-600">
              {step.eyebrow}
            </span>
            <span className="text-xs font-extrabold text-slate-500">
              {currentIndex + 1}/{GUIDE_STEPS.length}
            </span>
          </div>

          <p className="mt-3 text-sm font-semibold leading-6 text-slate-700">
            {step.body}
          </p>

          {(currentIndex === 1 || currentIndex === 2) && (
            <div className="mt-3 rounded-lg border border-[#D6E4DE] bg-[#FAFCFB] px-3 py-2 text-xs font-bold leading-5 text-slate-600">
              {currentIndex === 1 ? (
                <>Highlighted phase: {recommendedPhaseTitle || "first phase"}</>
              ) : (
                <>Highlighted node: {recommendedTopicTitle || "first available topic"}</>
              )}
            </div>
          )}

          <ul className="mt-3 space-y-2">
            {step.tips.map((tip, tipIndex) => (
              <li
                key={tip}
                className="roadmap-guide-tip flex gap-2 text-xs font-bold leading-5 text-slate-600"
                style={{ "--guide-tip-delay": `${tipIndex * 45}ms` }}
              >
                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-[#2FA084]" />
                <span>{tip}</span>
              </li>
            ))}
          </ul>

          <div className="mt-4 grid grid-cols-6 gap-1.5">
            {GUIDE_STEPS.map((item, index) => (
              <button
                key={item.title}
                type="button"
                aria-label={`Open ${item.eyebrow}`}
                onClick={() => onStepChange(index)}
                className={`roadmap-guide-step-dot h-1.5 rounded-full transition ${
                  index === currentIndex
                    ? "is-active bg-[#2FA084]"
                    : "bg-[#D6E4DE] hover:bg-[#A8D3C4]"
                }`}
              />
            ))}
          </div>

          <div className="mt-4 flex flex-wrap items-center justify-between gap-2">
            <button
              type="button"
              onClick={() => onStepChange(Math.max(0, currentIndex - 1))}
              disabled={isFirst}
              className="rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-xs font-extrabold text-[#18332D] shadow-sm transition hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-40"
            >
              Previous
            </button>

            <div className="flex flex-wrap justify-end gap-2">
              {!isLast && (
                <button
                  type="button"
                  onClick={() => onStepChange(currentIndex + 1)}
                  className="rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-xs font-extrabold text-[#1F6F5F] shadow-sm transition hover:bg-[#F7F1E8]"
                >
                  Skip step
                </button>
              )}

              {primaryAction && (
                <button
                  type="button"
                  onClick={primaryAction.onClick}
                  disabled={primaryAction.disabled}
                  className="rounded-lg bg-[#2FA084] px-3 py-2 text-xs font-extrabold text-white shadow-sm transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600"
                >
                  {primaryAction.label}
                </button>
              )}
            </div>
          </div>
        </div>
      </aside>
    </div>
  );
}

function GuideMotionStyles() {
  return (
    <style>{`
      @keyframes roadmapGuideCardIn {
        from { opacity: 0; transform: translate3d(-42px, 52px, 0) scale(0.9); filter: blur(2.5px); }
        60% { opacity: 1; transform: translate3d(7px, -7px, 0) scale(1.026); filter: blur(0); }
        to { opacity: 1; transform: translate3d(0, 0, 0) scale(1); filter: blur(0); }
      }

      @keyframes roadmapGuideStepIn {
        from { opacity: 0; transform: translate3d(0, 36px, 0) scale(0.91); filter: blur(2.25px); }
        58% { opacity: 1; transform: translate3d(0, -7px, 0) scale(1.026); filter: blur(0); }
        to { opacity: 1; transform: translate3d(0, 0, 0) scale(1); filter: blur(0); }
      }

      @keyframes roadmapGuideTipIn {
        from { opacity: 0; transform: translate3d(-32px, 12px, 0); }
        70% { opacity: 1; transform: translate3d(4px, -2px, 0); }
        to { opacity: 1; transform: translate3d(0, 0, 0); }
      }

      @keyframes roadmapGuideDotActive {
        0% { transform: scaleX(0.3) scaleY(0.75); opacity: 0.4; }
        62% { transform: scaleX(1.38) scaleY(1.45); opacity: 1; }
        100% { transform: scaleX(1) scaleY(1); opacity: 1; }
      }

      @keyframes roadmapGuideTargetPulse {
        0%, 100% { opacity: 0.9; transform: scale(1); }
        50% { opacity: 0.08; transform: scale(1.3); }
      }

      @keyframes roadmapGuideBadgeFloat {
        0%, 100% { transform: translate3d(0, 0, 0); }
        50% { transform: translate3d(0, -6px, 0); }
      }

      @keyframes roadmapGuideNodeRing {
        0%, 100% { opacity: 0.98; transform: scale(1); }
        50% { opacity: 0.5; transform: scale(1.07); }
      }

      @keyframes roadmapGuideNodeHalo {
        0%, 100% { opacity: 0.62; transform: scale(1); }
        50% { opacity: 0.12; transform: scale(1.16); }
      }

      @keyframes roadmapGuideNodeLabelIn {
        from { opacity: 0; transform: translate(-50%, calc(-100% - 2px)); }
        to { opacity: 1; transform: translate(-50%, calc(-100% - 10px)); }
      }

      .roadmap-guide-card {
        animation: roadmapGuideCardIn 520ms cubic-bezier(0.16, 1, 0.3, 1) both;
        backface-visibility: hidden;
        transform-origin: left bottom;
      }

      .roadmap-guide-step {
        transform-origin: left bottom;
        animation: roadmapGuideStepIn 500ms cubic-bezier(0.16, 1, 0.3, 1) both;
      }

      .roadmap-guide-tip {
        opacity: 0;
        animation: roadmapGuideTipIn 420ms cubic-bezier(0.16, 1, 0.3, 1) both;
        animation-delay: var(--guide-tip-delay, 0ms);
      }

      .roadmap-guide-step-dot.is-active {
        animation: roadmapGuideDotActive 520ms cubic-bezier(0.16, 1, 0.3, 1) both;
        transform-origin: center;
      }

      .roadmap-guide-target,
      .roadmap-guide-target-badge,
      .roadmap-node-guide-ring,
      .roadmap-node-guide-halo,
      .roadmap-node-guide-label {
        backface-visibility: hidden;
        will-change: transform, opacity;
      }

      .roadmap-guide-target-pulse {
        animation: roadmapGuideTargetPulse 1100ms ease-in-out infinite;
        transform-origin: center;
      }

      .roadmap-guide-target-badge {
        animation: roadmapGuideBadgeFloat 1250ms ease-in-out infinite;
      }

      .roadmap-node-guide-ring {
        animation: roadmapGuideNodeRing 1250ms ease-in-out infinite;
        transform-origin: center;
      }

      .roadmap-node-guide-halo {
        animation: roadmapGuideNodeHalo 1250ms ease-in-out infinite;
        transform-origin: center;
      }

      .roadmap-node-guide-label {
        transform: translate(-50%, calc(-100% - 10px));
        animation: roadmapGuideNodeLabelIn 180ms ease-out both;
      }

      @media (prefers-reduced-motion: reduce) {
        .roadmap-guide-card,
        .roadmap-guide-step,
        .roadmap-guide-tip,
        .roadmap-guide-step-dot.is-active,
        .roadmap-guide-target-pulse,
        .roadmap-guide-target-badge,
        .roadmap-node-guide-ring,
        .roadmap-node-guide-halo,
        .roadmap-node-guide-label {
          animation: none !important;
        }
      }
    `}</style>
  );
}

function GuideTargetHighlight({ rect, label }) {
  const highlightPadding = label === "Set Done" ? 5 : 8;
  const highlightStyle = {
    left: Math.max(8, rect.left - highlightPadding),
    top: Math.max(8, rect.top - highlightPadding),
    width: rect.width + highlightPadding * 2,
    height: rect.height + highlightPadding * 2,
  };
  const badgeStyle = getBadgeStyle(rect);

  return (
    <>
      <div
        className="roadmap-guide-target pointer-events-none absolute rounded-lg border-2 border-[#2FA084] shadow-[0_0_0_4px_rgba(47,160,132,0.14)]"
        style={highlightStyle}
      >
        <span className="roadmap-guide-target-pulse absolute inset-0 rounded-lg border-2 border-[#2FA084]/60" />
      </div>

      <div
        className="roadmap-guide-target-badge pointer-events-none absolute rounded-full border border-[#A8D3C4] bg-white px-3 py-1.5 text-[11px] font-black uppercase tracking-[0.12em] text-[#1F6F5F] shadow-lg"
        style={badgeStyle}
      >
        {label}
      </div>
    </>
  );
}

function getBadgeStyle(rect) {
  const viewportWidth = typeof window === "undefined" ? 1200 : window.innerWidth;
  const badgeWidth = 164;
  const left = Math.min(
    viewportWidth - badgeWidth - 12,
    Math.max(12, rect.left + rect.width / 2 - badgeWidth / 2),
  );
  const top = rect.top > 54 ? rect.top - 38 : rect.bottom + 10;

  return { left, top: Math.max(12, top), width: badgeWidth };
}

function getPrimaryAction({
  currentIndex,
  isEnrolled,
  isEnrolling,
  hasSelectedNodeModule,
  onStartRoadmap,
  onFocusPhase,
  onMoveToTopic,
  onFocusTopic,
  onOpenSelectedModule,
  onStepChange,
}) {
  if (currentIndex === 0) {
    return {
      label: isEnrolled ? "Go to first phase" : isEnrolling ? "Starting..." : "Start roadmap",
      disabled: isEnrolling,
      onClick: async () => {
        if (!isEnrolled) {
          await onStartRoadmap();
          return;
        }
        onStepChange(1);
        onFocusPhase();
      },
    };
  }

  if (currentIndex === 1) {
    return {
      label: "Show next node",
      onClick: onMoveToTopic || onFocusPhase,
    };
  }

  if (currentIndex === 2) {
    return {
      label: "Open highlighted node",
      onClick: onFocusTopic,
    };
  }

  if (currentIndex === 3) {
    return {
      label: hasSelectedNodeModule ? "Open module overview" : "Open a node first",
      disabled: !hasSelectedNodeModule,
      onClick: onOpenSelectedModule,
    };
  }

  return null;
}
