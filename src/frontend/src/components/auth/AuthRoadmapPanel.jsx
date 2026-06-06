import AuthLogo from "./AuthLogo";

const mainNodes = [
  {
    title: "Internet Basics",
    top: 58,
  },
  {
    title: "HTML & CSS",
    subtitle: "Fundamentals",
    top: 172,
  },
  {
    title: "JavaScript Core",
    top: 286,
  },
];

const skillNodes = [
  // Node 1: 3 skills bên trái
  {
    title: "HTTP / HTTPS",
    side: "left",
    top: 38,
    variant: "completed",
  },
  {
    title: "DNS",
    side: "left",
    top: 92,
    variant: "completed",
  },
  {
    title: "Browsers",
    side: "left",
    top: 146,
    variant: "warm",
  },

  // Node 2: 3 skills bên phải
  {
    title: "Semantic HTML",
    side: "right",
    top: 154,
    variant: "completed",
  },
  {
    title: "Forms",
    side: "right",
    top: 208,
    variant: "warm",
  },
  {
    title: "Flexbox",
    side: "right",
    top: 262,
    variant: "neutral",
  },

  // Node 3: 2 skills trái, 1 skill phải
  {
    title: "Async/Await",
    side: "left",
    top: 292,
    variant: "completed",
  },
  {
    title: "DOM API",
    side: "left",
    top: 346,
    variant: "warm",
  },
  {
    title: "ES6+",
    side: "right",
    top: 346,
    variant: "completed",
  },
];

export default function AuthRoadmapPanel() {
  return (
    <aside className="relative hidden h-dvh overflow-hidden border-r border-slate-200 bg-[#f8fafc] px-5 py-5 lg:flex lg:flex-col">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(37,99,235,0.07),transparent_32%),radial-gradient(circle_at_bottom_right,rgba(245,158,11,0.08),transparent_34%)]" />

      <div className="relative z-10 shrink-0">
        <AuthLogo compact />

        <div className="mt-4 max-w-sm">
          <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-[#1F6F5F]">
            Guided Learning Path
          </p>

          <h2 className="mt-2 text-[24px] font-bold leading-tight tracking-tight text-slate-950">
            Learn through clear milestones.
          </h2>

          <p className="mt-2 text-xs leading-5 text-slate-600">
            Follow a structured path, complete core skills, and grow step by
            step.
          </p>
        </div>
      </div>

      <div className="relative z-10 mt-4 flex min-h-0 flex-1 items-center justify-center">
        <div className="relative h-[430px] w-full max-w-[580px] rounded-[2rem] border border-slate-200 bg-white/95 shadow-[0_24px_70px_rgba(15,23,42,0.10)] backdrop-blur">
          <svg
            className="pointer-events-none absolute inset-0 h-full w-full"
            viewBox="0 0 580 430"
            fill="none"
            preserveAspectRatio="none"
          >
            {/* Main vertical line */}
            <path
              d="M290 34 V398"
              stroke="#2563eb"
              strokeWidth="2.4"
              strokeLinecap="round"
            />

            {/* Node 1: Internet Basics -> 3 left skills */}
            <path
              d="M240 86 C210 86 198 70 162 70"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeLinecap="round"
            />
            <path
              d="M240 86 C210 86 198 124 162 124"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeDasharray="6 6"
              strokeLinecap="round"
            />
            <path
              d="M240 86 C210 86 198 178 162 178"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeLinecap="round"
            />

            {/* Node 2: HTML & CSS -> 3 right skills */}
            <path
              d="M340 200 C375 200 390 186 420 186"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeLinecap="round"
            />
            <path
              d="M340 200 C375 200 390 240 420 240"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeDasharray="6 6"
              strokeLinecap="round"
            />
            <path
              d="M340 200 C375 200 390 294 420 294"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeLinecap="round"
            />

            {/* Node 3: JavaScript Core -> 2 left, 1 right */}
            <path
              d="M240 314 C208 314 198 324 162 324"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeDasharray="6 6"
              strokeLinecap="round"
            />
            <path
              d="M240 314 C208 314 198 378 162 378"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeLinecap="round"
            />
            <path
              d="M340 314 C375 314 390 378 420 378"
              stroke="#cbd5e1"
              strokeWidth="2.1"
              strokeDasharray="6 6"
              strokeLinecap="round"
            />
          </svg>

          {mainNodes.map((node) => (
            <MainNode key={node.title} node={node} />
          ))}

          {skillNodes.map((node) => (
            <SkillNode key={`${node.title}-${node.side}`} node={node} />
          ))}
        </div>
      </div>
    </aside>
  );
}

function MainNode({ node }) {
  return (
    <div
      style={{ top: node.top }}
      className="absolute left-1/2 z-20 flex h-[56px] w-[160px] -translate-x-1/2 items-center justify-center rounded-[20px] border-2 border-blue-700 bg-white px-4 text-center shadow-[0_10px_24px_rgba(37,99,235,0.12)]"
    >
      <div>
        <h3 className="text-[13px] font-semibold leading-tight text-slate-900">
          {node.title}
        </h3>

        {node.subtitle && (
          <p className="text-[13px] font-semibold leading-tight text-slate-900">
            {node.subtitle}
          </p>
        )}
      </div>
    </div>
  );
}

function SkillNode({ node }) {
  const positionClass = node.side === "left" ? "left-[28px]" : "right-[28px]";

  const variantClass =
    node.variant === "completed"
      ? "border-emerald-200 bg-emerald-50 text-emerald-800 shadow-[0_8px_20px_rgba(16,185,129,0.14)]"
      : node.variant === "warm"
      ? "border-amber-200 bg-amber-50 text-amber-800 shadow-[0_8px_20px_rgba(245,158,11,0.12)]"
      : "border-slate-200 bg-slate-50 text-slate-700 shadow-[0_8px_18px_rgba(15,23,42,0.06)]";

  return (
    <div
      style={{ top: node.top }}
      className={`absolute z-20 ${positionClass} flex h-[40px] min-w-[142px] items-center justify-center rounded-[14px] border px-4 text-sm font-medium transition hover:-translate-y-0.5 hover:shadow-md ${variantClass}`}
    >
      {node.variant === "completed" && (
        <span className="mr-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-emerald-500 text-[10px] font-bold text-white">
          ✓
        </span>
      )}
      {node.title}
    </div>
  );
}