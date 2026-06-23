import AuthLogo from "./AuthLogo";
import {
  getEdgeStyle,
  getNodeTypeClass,
  getStatusColor,
} from "../../roadmaps/utils/roadmapUtils";

const previewNodes = [
  // Top layer
  {
    id: "html-basics",
    title: "HTML Basics",
    nodeType: "topic",
    status: "pending",
    x: 26,
    y: 68,
    width: 142,
    height: 46,
  },
  {
    id: "semantic-html",
    title: "Writing Semantic HTML",
    nodeType: "topic",
    status: "pending",
    x: 26,
    y: 138,
    width: 142,
    height: 46,
  },
  {
    id: "forms-validation",
    title: "Forms and Validation",
    nodeType: "topic",
    status: "pending",
    x: 26,
    y: 208,
    width: 142,
    height: 46,
  },
  {
    id: "html-core",
    title: "HTML Core",
    nodeType: "choice_group",
    status: "pending",
    x: 194,
    y: 134,
    width: 156,
    height: 56,
    selectionType: "complete_all",
  },
  {
    id: "html-phase",
    title: "HTML, Semantics, Forms, Accessibility, and SEO",
    nodeType: "phase",
    status: "in_progress",
    x: 388,
    y: 126,
    width: 176,
    height: 68,
  },
  {
    id: "accessibility-group",
    title: "Accessibility and SEO Basics",
    nodeType: "choice_group",
    status: "pending",
    x: 600,
    y: 134,
    width: 160,
    height: 56,
    selectionType: "complete_all",
  },
  {
    id: "accessibility-basics",
    title: "Accessibility Basics",
    nodeType: "topic",
    status: "pending",
    x: 792,
    y: 92,
    width: 138,
    height: 46,
  },
  {
    id: "seo-basics",
    title: "SEO Basics",
    nodeType: "topic",
    status: "pending",
    x: 792,
    y: 186,
    width: 138,
    height: 46,
  },

  // Middle layer
  {
    id: "responsive-phase",
    title: "Responsive Layout Foundations",
    nodeType: "phase",
    status: "pending",
    x: 388,
    y: 318,
    width: 176,
    height: 60,
  },
  {
    id: "responsive-core",
    title: "Responsive Core",
    nodeType: "choice_group",
    status: "pending",
    x: 194,
    y: 320,
    width: 156,
    height: 56,
    selectionType: "complete_all",
  },
  {
    id: "flexbox-basics",
    title: "Flexbox Basics",
    nodeType: "topic",
    status: "pending",
    x: 26,
    y: 284,
    width: 142,
    height: 46,
  },
  {
    id: "responsive-breakpoints",
    title: "Responsive Breakpoints",
    nodeType: "topic",
    status: "pending",
    x: 26,
    y: 356,
    width: 142,
    height: 46,
  },
  {
    id: "signup-project",
    title: "Required Accessible Signup Page",
    nodeType: "project",
    status: "pending",
    x: 602,
    y: 322,
    width: 158,
    height: 52,
  },

  // Bottom layer - new phase + right choice group
  {
    id: "practice-phase",
    title: "Layout Practice and Component Building",
    nodeType: "phase",
    status: "pending",
    x: 388,
    y: 500,
    width: 176,
    height: 62,
  },
  {
    id: "layout-practice-group",
    title: "Layout Practice",
    nodeType: "choice_group",
    status: "pending",
    x: 600,
    y: 504,
    width: 160,
    height: 56,
    selectionType: "complete_all",
  },
  {
    id: "landing-layout",
    title: "Landing Page Structure",
    nodeType: "topic",
    status: "pending",
    x: 792,
    y: 462,
    width: 138,
    height: 46,
  },
  {
    id: "responsive-nav",
    title: "Responsive Navbar",
    nodeType: "topic",
    status: "pending",
    x: 792,
    y: 556,
    width: 138,
    height: 46,
  },
];

const previewEdges = [
  // Top layer edges
  {
    id: "html-1",
    edgeType: "choice",
    d: "M168 91 H180 C188 91 188 162 194 162",
  },
  {
    id: "html-2",
    edgeType: "choice",
    d: "M168 161 H180 C188 161 188 162 194 162",
  },
  {
    id: "html-3",
    edgeType: "choice",
    d: "M168 231 H180 C188 231 188 162 194 162",
  },
  { id: "html-to-phase", edgeType: "sequence", d: "M350 162 H388" },
  { id: "phase-to-right", edgeType: "sequence", d: "M564 160 H600" },
  {
    id: "right-to-a11y",
    edgeType: "choice",
    d: "M760 162 H776 C784 162 784 115 792 115",
  },
  {
    id: "right-to-seo",
    edgeType: "choice",
    d: "M760 162 H776 C784 162 784 209 792 209",
  },

  // Vertical flow between phases
  { id: "phase-1-to-2", edgeType: "sequence", d: "M476 194 V318" },
  { id: "phase-2-to-3", edgeType: "sequence", d: "M476 378 V500" },

  // Middle layer edges
  {
    id: "flex-to-group",
    edgeType: "choice",
    d: "M168 307 H180 C188 307 188 348 194 348",
  },
  {
    id: "breakpoint-to-group",
    edgeType: "choice",
    d: "M168 379 H180 C188 379 188 348 194 348",
  },
  { id: "group-to-responsive-phase", edgeType: "sequence", d: "M350 348 H388" },
  {
    id: "responsive-phase-to-project",
    edgeType: "sequence",
    d: "M564 348 H602",
  },

  // Bottom layer edges
  { id: "practice-phase-to-group", edgeType: "sequence", d: "M564 531 H600" },
  {
    id: "practice-1",
    edgeType: "choice",
    d: "M760 532 H776 C784 532 784 485 792 485",
  },
  {
    id: "practice-2",
    edgeType: "choice",
    d: "M760 532 H776 C784 532 784 579 792 579",
  },
];

const WORLD_WIDTH = 960;
const WORLD_HEIGHT = 660;
const PREVIEW_SCALE = 0.75;
const PREVIEW_OFFSET_X = -10;
const PREVIEW_OFFSET_Y = 10;

export default function AuthRoadmapPanel() {
  return (
    <aside className="relative hidden h-dvh overflow-hidden border-transparent shadow-sm bg-[#F4F3EF] px-5 py-4 shadow-none lg:flex lg:flex-col">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(255,255,255,0.72),transparent_56%),radial-gradient(circle_at_bottom_right,rgba(185,216,204,0.16),transparent_46%),linear-gradient(135deg,rgba(244,243,239,0.92),rgba(247,241,232,0.72))]" />

      <div className="relative z-10 shrink-0">
        <AuthLogo compact />

        <div className="mt-4 max-w-sm">
          <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-[#1F6F5F]">
            Guided Learning Path
          </p>

          <h2 className="mt-2 text-[24px] font-bold leading-tight tracking-tight text-[#18332D]">
            Learn through clear milestones.
          </h2>

          <p className="mt-2 text-xs leading-5 text-slate-600">
            A subtle preview of how TechMap connects phases, choices, and
            practice tasks.
          </p>
        </div>
      </div>

      <div className="relative z-10 mt-4 flex min-h-0 flex-1 items-center justify-center">
        <RoadmapPreviewCanvas />
      </div>
    </aside>
  );
}

function RoadmapPreviewCanvas() {
  return (
    <div className="relative mb-2 h-130 w-full max-w-[700px] overflow-hidden rounded-[1.75rem] border-transparent shadow-sm bg-white/18 shadow-none backdrop-blur-[1px]">
      <div className="absolute inset-0 opacity-[0.26] [background-image:radial-gradient(rgba(185,216,204,0.75)_1px,transparent_1px)] [background-size:24px_24px]" />
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(255,255,255,0.34),rgba(244,243,239,0.12)_58%,rgba(244,243,239,0)_84%)]" />
      <div className="pointer-events-none absolute inset-0 z-[1] bg-[radial-gradient(circle_at_center,transparent_60%,rgba(244,243,239,0.20)_100%)]" />

      <div
        className="absolute left-0 top-0"
        style={{
          width: WORLD_WIDTH,
          height: WORLD_HEIGHT,
          transform: `translate(${PREVIEW_OFFSET_X}px, ${PREVIEW_OFFSET_Y}px) scale(${PREVIEW_SCALE})`,
          transformOrigin: "top left",
        }}
      >
        <svg
          className="pointer-events-none absolute inset-0 z-10"
          viewBox={`0 0 ${WORLD_WIDTH} ${WORLD_HEIGHT}`}
          fill="none"
          preserveAspectRatio="none"
          style={{ width: WORLD_WIDTH, height: WORLD_HEIGHT }}
        >
          {previewEdges.map((edge) => {
            const style = getEdgeStyle(edge.edgeType);

            return (
              <path
                key={edge.id}
                d={edge.d}
                stroke={style.stroke}
                strokeWidth={Math.max(
                  (Number.parseFloat(style.strokeWidth) || 2) * 0.52,
                  1.05,
                )}
                strokeDasharray={style.strokeDasharray}
                strokeLinecap="round"
                strokeLinejoin="round"
                opacity="0.74"
              />
            );
          })}
        </svg>

        {previewNodes.map((node) => (
          <PreviewNode key={node.id} node={node} />
        ))}
      </div>
    </div>
  );
}

function PreviewNode({ node }) {
  const baseClass = getNodeTypeClass(node.nodeType);
  const statusColor = getStatusColor(node.status, node.nodeType);
  const isCompleted = node.status === "completed";

  return (
    <div
      style={{
        left: node.x,
        top: node.y,
        width: node.width,
        height: node.height,
      }}
      className={`absolute z-20 flex items-center justify-center rounded-lg border px-3 text-center shadow-[0_10px_22px_rgba(31,111,95,0.08)] ${baseClass}`}
    >
      <span
        className="absolute left-2 top-2 h-2.5 w-2.5 rounded-full border border-white/80"
        style={{ backgroundColor: statusColor }}
      />

      <div className="min-w-0 px-1.5">
        <p
          className={`line-clamp-3 text-[11.5px] font-black leading-[1.14] tracking-tight ${
            isCompleted ? "line-through decoration-current decoration-2" : ""
          }`}
        >
          {node.title}
        </p>

        {node.selectionType && (
          <p className="mx-auto mt-1 w-fit rounded-full bg-white/80 px-2 py-0.5 text-[8px] font-black lowercase tracking-tight text-[#18332D]">
            {formatAuthSelectionText(node)}
          </p>
        )}
      </div>
    </div>
  );
}

function formatAuthSelectionText(node) {
  if (node.selectionType === "choose_many")
    return `choose ${node.requiredCount || 1}`;
  if (node.selectionType === "choose_one") return "choose 1";
  if (node.selectionType === "complete_all") return "complete all";
  return "group";
}
