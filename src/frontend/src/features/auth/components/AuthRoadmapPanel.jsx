const UI_FONT =
  "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";

/* ====================== LOGO ====================== */

function AuthLogo() {
  return (
    <div className="flex shrink-0 items-center gap-3.5">
      <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-[14px] bg-gradient-to-br from-[#7F9E8B] to-[#4D705E] shadow-[0_8px_22px_rgba(65,101,82,0.2)]">
        <svg
          width="23"
          height="23"
          viewBox="0 0 24 24"
          fill="none"
          aria-hidden="true"
        >
          <rect
            x="3"
            y="3"
            width="7.5"
            height="7.5"
            rx="2"
            fill="#FFFFFF"
            opacity="0.78"
          />

          <rect
            x="13.5"
            y="13.5"
            width="7.5"
            height="7.5"
            rx="2"
            fill="#FFFFFF"
          />

          <path
            d="M10.5 6.75H15.3V10.3"
            stroke="#FFFFFF"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          <path
            d="M17.25 13.5V10.3H13"
            stroke="#FFFFFF"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </div>

      <div className="leading-none">
        <p className="text-[21px] font-black tracking-[-0.04em] text-[#19372B]">
          TechMap
        </p>

        <p className="mt-1 text-[8px] font-semibold uppercase tracking-[0.18em] text-[#527661]">
          ENGINEER YOUR FUTURE
        </p>
      </div>
    </div>
  );
}

/* ====================== ROADMAP DATA ====================== */

const ROADMAP_NODES = [
  {
    id: "01",
    x: 55,
    y: 350,
    width: 190,
    height: 72,
    title: "01 Foundation",
    subtitle: "Build your basics",
    type: "standard",
  },
  {
    id: "02",
    x: 355,
    y: 350,
    width: 190,
    height: 72,
    title: "02 Core Concepts",
    subtitle: "Understand the key ideas",
    type: "standard",
  },
  {
    id: "03",
    x: 655,
    y: 350,
    width: 190,
    height: 72,
    title: "03 Practice",
    subtitle: "Apply and strengthen your skills",
    type: "standard",
  },
  {
    id: "04",
    x: 150,
    y: 570,
    width: 190,
    height: 72,
    title: "04 Workflow",
    subtitle: "Learn how to work effectively",
    type: "standard",
  },
  {
    id: "05",
    x: 450,
    y: 570,
    width: 190,
    height: 72,
    title: "05 Projects",
    subtitle: "Build real-world projects",
    type: "standard",
  },
  {
    id: "06",
    x: 750,
    y: 570,
    width: 190,
    height: 72,
    title: "06 Review",
    subtitle: "Reflect and improve",
    type: "standard",
  },
  {
    id: "07",
    x: 590,
    y: 820,
    width: 215,
    height: 94,
    title: "07 Milestone",
    subtitle: "Plan your next learning step",
    type: "milestone",
  },
];

const ROADMAP_EDGES = [
  {
    from: "01",
    to: "02",
    fromSide: "right",
    toSide: "left",
  },
  {
    from: "02",
    to: "03",
    fromSide: "right",
    toSide: "left",
  },
  {
    from: "03",
    to: "05",
    fromSide: "bottom",
    toSide: "top",
  },
  {
    from: "04",
    to: "05",
    fromSide: "right",
    toSide: "left",
  },
  {
    from: "05",
    to: "06",
    fromSide: "right",
    toSide: "left",
  },
  {
    from: "06",
    to: "07",
    fromSide: "bottom",
    toSide: "top",
  },
];

const NODE_STYLE = {
  standard: {
    stroke: "#91AA99",
    hoverStroke: "#5F846E",
    dot: "#62866F",
    title: "#244638",
    subtitle: "#65776E",
  },

  milestone: {
    stroke: "#C4A36D",
    hoverStroke: "#A97B3B",
    dot: "#AD7D3C",
    title: "#604822",
    subtitle: "#806A48",
  },
};

/* ====================== MAIN COMPONENT ====================== */

export default function AuthRoadmapPanel() {
  return (
    <aside className="relative hidden h-dvh min-h-0 overflow-hidden border-r border-[#E4E0D7] bg-[#F7F5EF] font-sans lg:block">
      <NodeAnimationStyles />
      <PanelBackground />
      <DecorativeElements />

      {/* Header */}
      <header className="absolute left-[clamp(28px,3.5vw,52px)] right-[clamp(24px,3vw,40px)] top-[clamp(28px,4vh,42px)] z-40 flex items-start justify-between gap-6">
        <AuthLogo />

        <div className="mt-0.5 flex h-10 shrink-0 items-center gap-2.5 whitespace-nowrap rounded-full border border-[#C7D6CC] bg-white/80 px-4 shadow-[0_6px_20px_rgba(62,95,77,0.08)] backdrop-blur-md">
          <div className="h-2.5 w-2.5 shrink-0 animate-pulse rounded-full bg-[#688B75] ring-2 ring-[#688B75]/25" />

          <span className="text-[7px] font-semibold uppercase tracking-[0.1em] text-[#456A55] xl:text-[10px]">
            LEARNING ROADMAP PLATFORM
          </span>
        </div>
      </header>

      {/* Introduction */}
      <section className="absolute left-[clamp(28px,3.5vw,52px)] top-[clamp(105px,11.5vh,125px)] z-40 max-w-[650px]">
        <h2 className="max-w-[610px] text-[clamp(34px,3.2vw,42px)] font-bold leading-[1.06] tracking-[-0.04em] text-[#1C3A2E]">
          Learn with a calm, clear{" "}
          <span className="text-[#688D75]">roadmap.</span>
        </h2>

        <p className="mt-3.5 max-w-[530px] text-[12px] leading-[1.6] text-[#5C6E64] xl:text-[13.5px]">
          TechMap connects topics, learning steps, and practical projects into
          one clear path, helping you focus on the next meaningful goal.
        </p>
      </section>

      <RoadmapArtwork />
    </aside>
  );
}

/* ====================== ANIMATION ====================== */

function NodeAnimationStyles() {
  return (
    <style>{`
      .roadmap-node {
        transition:
          transform 0.35s cubic-bezier(0.4, 0, 0.2, 1),
          filter 0.35s ease;

        transform-box: fill-box;
        transform-origin: center;
        cursor: default;
        filter: drop-shadow(0 7px 9px rgba(56, 83, 68, 0.1));
      }

      .roadmap-node:hover {
        transform: translateY(-7px) scale(1.04);
        filter: drop-shadow(0 14px 15px rgba(56, 83, 68, 0.17));
      }

      .roadmap-node .node-border {
        transition:
          stroke 0.3s ease,
          stroke-width 0.3s ease;
      }

      .roadmap-node:hover .node-border {
        stroke-width: 2.8;
      }

      .roadmap-edge {
        transition:
          stroke 0.3s ease,
          stroke-width 0.3s ease;
      }

      .roadmap-group:hover .roadmap-edge {
        stroke: #5E826C;
        stroke-width: 5.2;
      }

      .float-up {
        animation: floatUp 5s ease-in-out infinite;
      }

      .float-down {
        animation: floatDown 6s ease-in-out infinite;
      }

      .float-slow {
        animation: floatUp 8s ease-in-out infinite;
      }

      .rotate-slow {
        animation: rotateSlow 22s linear infinite;
      }

      .rotate-reverse {
        animation: rotateReverse 28s linear infinite;
      }

      .pulse-soft {
        animation: pulseSoft 4s ease-in-out infinite;
      }

      .shine-soft {
        animation: shineSoft 5s ease-in-out infinite;
      }

      @keyframes floatUp {
        0%,
        100% {
          transform: translateY(0);
        }

        50% {
          transform: translateY(-8px);
        }
      }

      @keyframes floatDown {
        0%,
        100% {
          transform: translateY(0);
        }

        50% {
          transform: translateY(8px);
        }
      }

      @keyframes rotateSlow {
        from {
          transform: rotate(0deg);
        }

        to {
          transform: rotate(360deg);
        }
      }

      @keyframes rotateReverse {
        from {
          transform: rotate(360deg);
        }

        to {
          transform: rotate(0deg);
        }
      }

      @keyframes pulseSoft {
        0%,
        100% {
          opacity: 0.35;
          transform: scale(1);
        }

        50% {
          opacity: 0.7;
          transform: scale(1.08);
        }
      }

      @keyframes shineSoft {
        0%,
        100% {
          opacity: 0.4;
        }

        50% {
          opacity: 0.9;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .float-up,
        .float-down,
        .float-slow,
        .rotate-slow,
        .rotate-reverse,
        .pulse-soft,
        .shine-soft {
          animation: none;
        }
      }
    `}</style>
  );
}

/* ====================== BACKGROUND ====================== */

function PanelBackground() {
  return (
    <>
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_18%_26%,rgba(222,234,226,0.94),transparent_47%),radial-gradient(circle_at_82%_72%,rgba(240,232,215,0.88),transparent_54%)]" />

      <div className="absolute -left-24 top-14 h-[520px] w-[520px] rounded-[42%] bg-[#E2ECE5] opacity-[0.65] blur-[90px]" />

      <div className="absolute bottom-6 right-0 h-[520px] w-[520px] rounded-[45%] bg-[#EFE6D6] opacity-[0.72] blur-[100px]" />

      <div className="absolute left-1/2 top-[34%] h-[340px] w-[340px] -translate-x-1/2 rounded-[42%] bg-[#DCE8E0] opacity-[0.45] blur-[85px]" />

      <div className="absolute right-[18%] top-[10%] h-[220px] w-[220px] rounded-full bg-[#E9DFCC]/50 blur-[70px]" />

      <div
        className="absolute inset-0 opacity-[0.1]"
        style={{
          backgroundImage:
            "radial-gradient(circle, rgba(75,105,87,0.3) 1px, transparent 1px)",
          backgroundSize: "30px 30px",
        }}
      />

      <div
        className="absolute inset-0 opacity-[0.08]"
        style={{
          backgroundImage:
            "linear-gradient(rgba(88,116,99,0.18) 1px, transparent 1px), linear-gradient(90deg, rgba(88,116,99,0.18) 1px, transparent 1px)",
          backgroundSize: "120px 120px",
        }}
      />
    </>
  );
}

/* ====================== DECORATIONS ====================== */

function DecorativeElements() {
  return (
    <div className="pointer-events-none absolute inset-0 z-10 overflow-hidden">
      {/* Orbit góc phải */}
      <div className="absolute right-[5%] top-[16%] h-44 w-44">
        <div className="rotate-slow absolute inset-0 rounded-full border border-dashed border-[#9FB5A7]/35">
          <span className="absolute left-1/2 top-[-5px] h-2.5 w-2.5 -translate-x-1/2 rounded-full bg-[#7E9C89] shadow-[0_0_0_5px_rgba(126,156,137,0.1)]" />

          <span className="absolute bottom-[18px] right-[8px] h-2 w-2 rounded-full bg-[#B69A6E]" />
        </div>

        <div className="rotate-reverse absolute inset-[24px] rounded-full border border-[#AABEB1]/32">
          <span className="absolute left-[2px] top-1/2 h-2 w-2 -translate-y-1/2 rounded-full bg-[#94AD9D]" />
        </div>

        <div className="pulse-soft absolute inset-[52px] rounded-full border border-[#B9CABF]/50 bg-[#E4ECE7]/40" />

        <div className="absolute inset-[73px] rounded-full bg-[#7E9B88]/20 shadow-[0_0_30px_rgba(104,139,117,0.2)]" />
      </div>

      {/* Họa tiết dọc bên phải */}
      <div className="absolute right-[3.5%] top-[39%] flex flex-col gap-3 opacity-70">
        <span className="h-8 w-2 rounded-full bg-[#89A392]/55" />
        <span className="h-4 w-2 rounded-full bg-[#C2A77A]/55" />
        <span className="h-12 w-2 rounded-full bg-[#AFC0B5]/45" />
      </div>

      {/* Lưới chấm bên trái */}
      <div className="absolute left-[5%] top-[47%] grid grid-cols-5 gap-2.5 opacity-[0.42]">
        {Array.from({ length: 25 }).map((_, index) => (
          <span
            key={index}
            className="h-1.5 w-1.5 rounded-full bg-[#809C89]"
            style={{
              opacity: 0.35 + (index % 5) * 0.12,
            }}
          />
        ))}
      </div>

      {/* Vòng tròn bên trái */}
      <div className="absolute -left-20 top-[54%] h-48 w-48 rounded-full border-[20px] border-[#DCE7DF]/40" />

      <div className="absolute -left-11 top-[58%] h-28 w-28 rounded-full border border-dashed border-[#90A998]/35" />

      {/* Build by doing */}
      <div className="float-down absolute right-[22%] top-[25%] z-30 flex items-center gap-2 rounded-xl border border-[#D5DED8] bg-[#FBFCF9]/80 px-3 py-2 shadow-[0_7px_22px_rgba(68,94,79,0.08)] backdrop-blur-md">
        <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-[#E8EFEA]">
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M8 8L4 12L8 16"
              stroke="#637F6D"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            <path
              d="M16 8L20 12L16 16"
              stroke="#637F6D"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            <path
              d="M14 5L10 19"
              stroke="#637F6D"
              strokeWidth="1.8"
              strokeLinecap="round"
            />
          </svg>
        </div>

        <span className="text-[9px] font-semibold text-[#587060]">
          Build by doing
        </span>
      </div>

      {/* Focus card */}
      <div className="float-up absolute right-[7%] top-[42%] flex items-center gap-2.5 rounded-2xl border border-[#D3DED7] bg-white/70 px-3.5 py-3 shadow-[0_8px_26px_rgba(65,92,76,0.08)] backdrop-blur-md">
        <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-[#E5EEE8]">
          <svg
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <circle
              cx="12"
              cy="12"
              r="7"
              stroke="#5F816C"
              strokeWidth="1.8"
            />

            <circle cx="12" cy="12" r="2.4" fill="#5F816C" />

            <path
              d="M12 2V5"
              stroke="#5F816C"
              strokeWidth="1.8"
              strokeLinecap="round"
            />

            <path
              d="M12 19V22"
              stroke="#5F816C"
              strokeWidth="1.8"
              strokeLinecap="round"
            />

            <path
              d="M2 12H5"
              stroke="#5F816C"
              strokeWidth="1.8"
              strokeLinecap="round"
            />

            <path
              d="M19 12H22"
              stroke="#5F816C"
              strokeWidth="1.8"
              strokeLinecap="round"
            />
          </svg>
        </span>

        <div>
          <p className="text-[8px] font-semibold uppercase tracking-[0.13em] text-[#84978C]">
            Focus mode
          </p>

          <p className="mt-0.5 text-[10px] font-semibold text-[#405C4C]">
            One step at a time
          </p>
        </div>
      </div>

      {/* Progress card */}
      <div className="float-slow absolute bottom-[11%] left-[6%] w-[170px] rounded-2xl border border-[#D6E0D9] bg-[#FCFBF7]/75 p-3.5 shadow-[0_8px_24px_rgba(64,90,75,0.07)] backdrop-blur-md">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-[8px] font-semibold uppercase tracking-[0.12em] text-[#84978C]">
              Progress
            </p>

            <p className="mt-0.5 text-[10px] font-semibold text-[#496151]">
              Keep moving forward
            </p>
          </div>

          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[#E8EFEA] text-[9px] font-bold text-[#567560]">
            72%
          </div>
        </div>

        <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-[#E1E9E3]">
          <div className="h-full w-[72%] rounded-full bg-gradient-to-r from-[#7E9C89] to-[#5D816A]" />
        </div>
      </div>

      {/* Goal card */}
      <div className="float-down absolute bottom-[7%] right-[7%] flex items-center gap-2 rounded-full border border-[#DED7C9] bg-[#FFFDF8]/75 px-3.5 py-2 shadow-[0_7px_20px_rgba(98,77,45,0.06)] backdrop-blur-sm">
        <span className="flex h-7 w-7 items-center justify-center rounded-full bg-[#F2E8D7]">
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <circle
              cx="12"
              cy="12"
              r="8"
              stroke="#A4834E"
              strokeWidth="1.7"
            />

            <circle
              cx="12"
              cy="12"
              r="4"
              stroke="#A4834E"
              strokeWidth="1.7"
            />

            <circle cx="12" cy="12" r="1.6" fill="#A4834E" />
          </svg>
        </span>

        <span className="text-[9px] font-semibold text-[#745B35]">
          Reach your goal
        </span>
      </div>

      {/* Chấm góc dưới */}
      <div className="absolute bottom-[12%] right-[3%] grid grid-cols-6 gap-2 opacity-30">
        {Array.from({ length: 30 }).map((_, index) => (
          <span
            key={index}
            className="h-1.5 w-1.5 rounded-full bg-[#B39A73]"
          />
        ))}
      </div>

      <Sparkle className="absolute left-[10%] top-[35%]" />
      <Sparkle className="absolute right-[34%] top-[18%]" />
      <Sparkle className="absolute bottom-[22%] right-[28%]" gold />
      <Sparkle className="absolute bottom-[10%] left-[38%]" />

      <svg
        className="absolute inset-0 h-full w-full"
        viewBox="0 0 1000 1000"
        preserveAspectRatio="none"
        aria-hidden="true"
      >
        <path
          d="M700 110 C810 150 910 235 985 350"
          fill="none"
          stroke="#A9BEB0"
          strokeWidth="1.2"
          strokeDasharray="5 10"
          opacity="0.3"
        />

        <path
          d="M24 690 C130 605 238 610 326 683"
          fill="none"
          stroke="#BDA77F"
          strokeWidth="1.2"
          strokeDasharray="5 11"
          opacity="0.27"
        />

        <path
          d="M590 82 C650 112 690 142 720 188"
          fill="none"
          stroke="#9DB4A5"
          strokeWidth="1"
          opacity="0.26"
        />

        <path
          d="M770 850 C840 810 925 806 995 836"
          fill="none"
          stroke="#B9A27A"
          strokeWidth="1"
          opacity="0.24"
        />
      </svg>
    </div>
  );
}

/* ====================== SPARKLE ====================== */

function Sparkle({ className = "", gold = false }) {
  const color = gold ? "#B39768" : "#789783";

  return (
    <svg
      className={`shine-soft h-5 w-5 ${className}`}
      viewBox="0 0 20 20"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M10 1.5C10.8 6.6 13.4 9.2 18.5 10C13.4 10.8 10.8 13.4 10 18.5C9.2 13.4 6.6 10.8 1.5 10C6.6 9.2 9.2 6.6 10 1.5Z"
        fill={color}
        opacity="0.55"
      />
    </svg>
  );
}

/* ====================== ROADMAP SVG ====================== */

function RoadmapArtwork() {
  return (
    <svg
      aria-hidden="true"
      className="absolute -bottom-10 left-0 z-20 h-[86%] w-full"
      viewBox="0 250 1200 1050"
      preserveAspectRatio="none"
      shapeRendering="geometricPrecision"
    >
      <defs>
        <filter
          id="edgeShadow"
          x="-30%"
          y="-40%"
          width="170%"
          height="190%"
        >
          <feDropShadow
            dx="0"
            dy="6"
            stdDeviation="5"
            floodColor="#405E4D"
            floodOpacity="0.1"
          />
        </filter>

        <filter
          id="nodeShadow"
          x="-30%"
          y="-40%"
          width="170%"
          height="190%"
        >
          <feDropShadow
            dx="0"
            dy="7"
            stdDeviation="6"
            floodColor="#405E4D"
            floodOpacity="0.11"
          />
        </filter>

        <linearGradient
          id="standardNodeGradient"
          x1="0"
          y1="0"
          x2="1"
          y2="1"
        >
          <stop offset="0%" stopColor="#F4F8F5" />
          <stop offset="100%" stopColor="#DFEAE2" />
        </linearGradient>

        <linearGradient
          id="milestoneNodeGradient"
          x1="0"
          y1="0"
          x2="1"
          y2="1"
        >
          <stop offset="0%" stopColor="#FCF8EF" />
          <stop offset="100%" stopColor="#EEE0C7" />
        </linearGradient>

        <radialGradient id="softGreenGlow">
          <stop offset="0%" stopColor="#A9BDAF" stopOpacity="0.3" />
          <stop offset="100%" stopColor="#A9BDAF" stopOpacity="0" />
        </radialGradient>

        <radialGradient id="softGoldGlow">
          <stop offset="0%" stopColor="#C5AB7D" stopOpacity="0.28" />
          <stop offset="100%" stopColor="#C5AB7D" stopOpacity="0" />
        </radialGradient>
      </defs>

      <g className="roadmap-group" transform="translate(0 140)">
        <circle
          cx="430"
          cy="510"
          r="230"
          fill="url(#softGreenGlow)"
        />

        <circle
          cx="740"
          cy="850"
          r="190"
          fill="url(#softGoldGlow)"
        />

        {/* Đường trang trí */}
        <g fill="none" strokeLinecap="round" opacity="0.22">
          <path
            d="M40 300 C190 245 340 260 470 320"
            stroke="#8FA99A"
            strokeWidth="1.4"
            strokeDasharray="6 12"
          />

          <path
            d="M500 940 C690 1005 875 975 1035 890"
            stroke="#A99065"
            strokeWidth="1.4"
            strokeDasharray="6 12"
          />

          <path
            d="M835 295 C950 250 1080 280 1165 355"
            stroke="#91A99A"
            strokeWidth="1.1"
          />

          <path
            d="M30 750 C145 835 280 830 375 765"
            stroke="#B5A076"
            strokeWidth="1.1"
          />
        </g>

        {/* Vòng trang trí */}
        <g opacity="0.25">
          <circle
            cx="1085"
            cy="405"
            r="50"
            fill="none"
            stroke="#B5A27E"
            strokeWidth="1.2"
          />

          <circle
            cx="1085"
            cy="405"
            r="34"
            fill="none"
            stroke="#B5A27E"
            strokeWidth="1.2"
            strokeDasharray="5 8"
          />

          <circle
            cx="70"
            cy="730"
            r="40"
            fill="none"
            stroke="#8FA99A"
            strokeWidth="1.2"
          />

          <circle
            cx="70"
            cy="730"
            r="26"
            fill="none"
            stroke="#8FA99A"
            strokeWidth="1"
            strokeDasharray="4 7"
          />
        </g>

        {/* Đường nền */}
        <g
          opacity="0.2"
          stroke="#95AE9E"
          strokeWidth="1.5"
          strokeLinecap="round"
        >
          <path
            d="M35 390 Q250 300 480 390 Q720 300 930 390"
            fill="none"
          />

          <path
            d="M90 620 Q330 500 570 620 Q850 500 1080 620"
            fill="none"
          />

          <path
            d="M430 870 Q700 730 950 875"
            fill="none"
            stroke="#B59F76"
          />
        </g>

        {/* Connections */}
        <g filter="url(#edgeShadow)">
          {ROADMAP_EDGES.map((edge, index) => (
            <RoadmapEdge
              key={`${edge.from}-${edge.to}-${index}`}
              edge={edge}
            />
          ))}
        </g>

        {/* Nodes */}
        <g filter="url(#nodeShadow)">
          {ROADMAP_NODES.map((node, index) => (
            <RoadmapNode
              key={node.id}
              node={node}
              index={index}
            />
          ))}
        </g>
      </g>
    </svg>
  );
}

/* ====================== HELPERS ====================== */

function getNodeAnchor(node, side) {
  const centerX = node.x + node.width / 2;
  const centerY = node.y + node.height / 2;

  switch (side) {
    case "left":
      return {
        x: node.x,
        y: centerY,
      };

    case "right":
      return {
        x: node.x + node.width,
        y: centerY,
      };

    case "top":
      return {
        x: centerX,
        y: node.y,
      };

    case "bottom":
      return {
        x: centerX,
        y: node.y + node.height,
      };

    default:
      return {
        x: centerX,
        y: centerY,
      };
  }
}

/* ====================== ROADMAP EDGE ====================== */

function RoadmapEdge({ edge }) {
  const fromNode = ROADMAP_NODES.find(
    (node) => node.id === edge.from,
  );

  const toNode = ROADMAP_NODES.find(
    (node) => node.id === edge.to,
  );

  if (!fromNode || !toNode) {
    return null;
  }

  const start = getNodeAnchor(fromNode, edge.fromSide);
  const end = getNodeAnchor(toNode, edge.toSide);

  const isVerticalConnection =
    edge.fromSide === "top" ||
    edge.fromSide === "bottom" ||
    edge.toSide === "top" ||
    edge.toSide === "bottom";

  const path = isVerticalConnection
    ? (() => {
        const middleY = (start.y + end.y) / 2;

        return `
          M ${start.x} ${start.y}
          C ${start.x} ${middleY},
            ${end.x} ${middleY},
            ${end.x} ${end.y}
        `;
      })()
    : (() => {
        const middleX = (start.x + end.x) / 2;

        return `
          M ${start.x} ${start.y}
          C ${middleX} ${start.y},
            ${middleX} ${end.y},
            ${end.x} ${end.y}
        `;
      })();

  return (
    <>
      <path
        d={path}
        fill="none"
        stroke="#DEE8E1"
        strokeWidth="14"
        strokeLinecap="round"
        strokeLinejoin="round"
      />

      <path
        className="roadmap-edge"
        d={path}
        fill="none"
        stroke="#74947F"
        strokeWidth="4.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />

      <path
        d={path}
        fill="none"
        stroke="#ADC1B3"
        strokeWidth="1.4"
        strokeLinecap="round"
        strokeLinejoin="round"
        opacity="0.72"
      />
    </>
  );
}

/* ====================== ROADMAP NODE ====================== */

function RoadmapNode({ node, index }) {
  const style = NODE_STYLE[node.type];
  const centerX = node.x + node.width / 2;
  const isMilestone = node.type === "milestone";

  const titleY = isMilestone ? node.y + 48 : node.y + 42;
  const subtitleY = isMilestone ? node.y + 69 : node.y + 62;

  return (
    <g
      className="roadmap-node"
      style={{
        transitionDelay: `${index * 45}ms`,
      }}
    >
      <rect
        className="node-border"
        x={node.x}
        y={node.y}
        width={node.width}
        height={node.height}
        rx={isMilestone ? "22" : "19"}
        fill={
          isMilestone
            ? "url(#milestoneNodeGradient)"
            : "url(#standardNodeGradient)"
        }
        stroke={style.stroke}
        strokeWidth={isMilestone ? "2.3" : "2"}
      />

      <rect
        x={node.x + 2}
        y={node.y + 2}
        width={node.width - 4}
        height={node.height * 0.45}
        rx={isMilestone ? "20" : "17"}
        fill="#FFFFFF"
        opacity={isMilestone ? "0.32" : "0.4"}
      />

      <circle
        cx={node.x + 22}
        cy={node.y + 22}
        r="8.8"
        fill="#FFFFFF"
        stroke={style.stroke}
        strokeWidth="1.1"
      />

      <circle
        cx={node.x + 22}
        cy={node.y + 22}
        r="5.8"
        fill={style.dot}
      />

      <circle
        cx={node.x + 20.2}
        cy={node.y + 20.2}
        r="1.6"
        fill="#FFFFFF"
        opacity="0.76"
      />

      <text
        x={centerX}
        y={titleY}
        textAnchor="middle"
        fontSize={isMilestone ? "14.5" : "14"}
        fontWeight="700"
        fill={style.title}
        fontFamily={UI_FONT}
      >
        {node.title}
      </text>

      <text
        x={centerX}
        y={subtitleY}
        textAnchor="middle"
        fontSize={isMilestone ? "9.2" : "9"}
        fontWeight="500"
        fill={style.subtitle}
        fontFamily={UI_FONT}
      >
        {node.subtitle}
      </text>
    </g>
  );
}