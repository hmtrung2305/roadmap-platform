import {
  ArrowRight,
  Boxes,
  CheckCircle2,
  Code2,
  FileText,
  GitFork,
  LineChart,
  Lock,
  Map,
  Menu,
  Rocket,
  Sparkles,
  Star,
  Terminal,
  UserRound,
  X,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { CgListTree } from "react-icons/cg";

import Footer from "../components/layout/Footer";
import { useAuthStore } from "../stores/useAuthStore";
import { getDefaultAuthenticatedRoute } from "../utils/navigationUtils";

const navItems = [
  { label: "AI Mentor", target: "mentor" },
  { label: "Roadmap", target: "roadmap" },
  { label: "Market Pulse", target: "market" },
  { label: "Portfolio", target: "portfolio" },
];

const capabilityTabs = [
  {
    id: "mentor",
    label: "AI Mentor",
    icon: Sparkles,
    title: "Ask career questions in natural language.",
    text: "Students can ask about roles, skills, projects, and interview direction. The mentor can personalize answers with uploaded transcripts, selected skills, and public GitHub profile signals.",
    points: [
      "Natural language chat",
      "Transcript-aware guidance",
      "GitHub profile context",
    ],
    previewTitle: "Mentor response",
    preview:
      "You are close to a Frontend Developer path. Prioritize React state, API integration, and one portfolio project with a clear README.",
  },
  {
    id: "roadmap",
    label: "Dynamic Roadmap",
    icon: Map,
    title: "Generate a prioritized skill tree for any target role.",
    text: "Users select a target career role and TechMap turns it into a hierarchical roadmap with technical nodes, learning sequence, curated resources, and live completion progress.",
    points: [
      "Target role selection",
      "Hierarchical skill tree",
      "Node completion tracking",
    ],
    previewTitle: "Roadmap node",
    preview:
      "Frontend Developer > React > Component State: 2 resources, 1 practice task, marked in progress.",
  },
  {
    id: "gap",
    label: "Skill Gap",
    icon: CheckCircle2,
    title: "Compare current skills against role requirements.",
    text: "Learners can manually select skills, then receive a visual gap report that highlights missing skills and urgent priorities before they spend time on the wrong topic.",
    points: [
      "Skill selection",
      "Role requirement mapping",
      "Priority report",
    ],
    previewTitle: "Gap summary",
    preview:
      "Missing: Docker, CI/CD, SQL indexing. Urgent priority: Docker fundamentals before deployment projects.",
  },
  {
    id: "market",
    label: "Market Pulse",
    icon: LineChart,
    title: "Track what the job market is asking for.",
    text: "Daily scheduled collection from job portals feeds keyword analysis, trend charts, and role demand signals so the roadmap can stay aligned with real hiring demand.",
    points: [
      "Daily job data pipeline",
      "Keyword frequency analysis",
      "Interactive trend charts",
    ],
    previewTitle: "Trend signal",
    preview:
      "React demand +18%, Docker +24%, PostgreSQL +11% across recent junior-to-mid postings.",
  },
  {
    id: "portfolio",
    label: "E-Portfolio",
    icon: Code2,
    title: "Turn GitHub repositories into employer-ready proof.",
    text: "Users can link GitHub, sync public repositories, summarize README files with AI, and publish a shareable portfolio URL for applications.",
    points: [
      "GitHub repository sync",
      "README summarization",
      "Shareable profile URL",
    ],
    previewTitle: "Portfolio card",
    preview:
      "react-learning-roadmap: React, REST API, resource reader. Objective: help learners continue roadmap tasks.",
  },
];

const roadmapNodes = [
  ["01", "Target Role", "Frontend Developer", "Selected"],
  ["02", "Core Skills", "JavaScript, React, APIs", "In progress"],
  ["03", "Portfolio Proof", "GitHub project + README", "Next"],
  ["04", "Market Check", "Validate against job trends", "Queued"],
];

const marketBars = [58, 72, 46, 84, 64, 78, 92];

const workflowCards = [
  {
    icon: Sparkles,
    eyebrow: "01 Understand",
    title: "Start with an AI career conversation",
    text: "Ask questions, upload academic context, and connect public GitHub signals so advice is not generic.",
  },
  {
    icon: Map,
    eyebrow: "02 Plan",
    title: "Generate a roadmap that can be completed",
    text: "Skill nodes, resource links, and completion state keep the path practical instead of motivational only.",
  },
  {
    icon: Rocket,
    eyebrow: "03 Prove",
    title: "Convert learning into portfolio evidence",
    text: "Repositories, README summaries, and shareable profiles make progress visible to employers.",
  },
];

const trustItems = [
  "Email/password and Google OAuth authentication.",
  "Persistent chat history, roadmap progress, skill assessments, and portfolio data.",
  "Public landing remains separate from the protected roadmap workspace.",
];

function useCountUp(target, start = false, duration = 1200) {
  const [value, setValue] = useState(0);

  useEffect(() => {
    if (!start) {
      return undefined;
    }

    let frameId;
    const startedAt = performance.now();

    const tick = (now) => {
      const progress = Math.min((now - startedAt) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);

      setValue(Math.round(target * eased));

      if (progress < 1) {
        frameId = requestAnimationFrame(tick);
      }
    };

    frameId = requestAnimationFrame(tick);

    return () => cancelAnimationFrame(frameId);
  }, [duration, start, target]);

  return value;
}

function StatCard({ value, suffix = "", label }) {
  const cardRef = useRef(null);
  const [start, setStart] = useState(false);
  const count = useCountUp(value, start);

  useEffect(() => {
    const element = cardRef.current;

    if (!element) {
      return undefined;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setStart(true);
          observer.unobserve(element);
        }
      },
      { threshold: 0.45 },
    );

    observer.observe(element);

    return () => observer.disconnect();
  }, []);

  return (
    <div
      ref={cardRef}
      className="landing-reveal rounded-2xl border border-slate-200 bg-white/95 p-5 text-center shadow-lg shadow-slate-200/60 backdrop-blur"
    >
      <strong className="block text-3xl font-black text-[#05070d]">
        {count}
        {suffix}
      </strong>

      <span className="mt-1 block text-sm font-semibold text-slate-600">
        {label}
      </span>
    </div>
  );
}

export default function LandingPage() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [activeCapabilityId, setActiveCapabilityId] =
    useState("mentor");
  const [isNavHidden, setIsNavHidden] = useState(false);

  const lastScrollYRef = useRef(0);

  const user = useAuthStore((state) => state.user);

  const primaryPath = user
    ? getDefaultAuthenticatedRoute(user)
    : "/register";

  const primaryLabel = user
    ? "Open workspace"
    : "Start building";

  const activeCapability = useMemo(
    () =>
      capabilityTabs.find(
        (item) => item.id === activeCapabilityId,
      ) || capabilityTabs[0],
    [activeCapabilityId],
  );

  useEffect(() => {
    const revealElements =
      document.querySelectorAll(".landing-reveal");

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add(
              "landing-reveal-visible",
            );

            observer.unobserve(entry.target);
          }
        });
      },
      {
        threshold: 0.16,
        rootMargin: "0px 0px -70px 0px",
      },
    );

    revealElements.forEach((element) =>
      observer.observe(element),
    );

    return () => observer.disconnect();
  }, [activeCapabilityId]);

  useEffect(() => {
    const handleScroll = () => {
      const currentScrollY = window.scrollY;

      const isScrollingDown =
        currentScrollY > lastScrollYRef.current;

      if (currentScrollY < 80) {
        setIsNavHidden(false);
      } else if (
        isScrollingDown &&
        currentScrollY > 120
      ) {
        setIsNavHidden(true);
        setIsMenuOpen(false);
      } else if (!isScrollingDown) {
        setIsNavHidden(false);
      }

      lastScrollYRef.current = Math.max(
        currentScrollY,
        0,
      );
    };

    window.addEventListener("scroll", handleScroll, {
      passive: true,
    });

    return () =>
      window.removeEventListener("scroll", handleScroll);
  }, []);

  const closeMenu = () => {
    setIsMenuOpen(false);
  };

  const scrollToSection = (target) => {
    const element = document.getElementById(target);

    if (!element) {
      return;
    }

    closeMenu();

    element.scrollIntoView({
      behavior: "smooth",
      block: "start",
    });
  };

  return (
    <div className="landing-page min-h-screen bg-[#F7F1E8] text-[#18332D]">
      <style>{`
        html {
          scroll-behavior: smooth;
        }

        /*
          Hiển thị con trỏ bàn tay cho toàn bộ thành phần
          có thể nhấn trong Landing Page.
        */
        .landing-page button:not(:disabled),
        .landing-page a[href],
        .landing-page [role="button"] {
          cursor: pointer;
        }

        /*
          Hiển thị biểu tượng cấm khi nút bị vô hiệu hóa.
        */
        .landing-page button:disabled {
          cursor: not-allowed;
        }

        @keyframes landingFloat {
          0%,
          100% {
            transform: translate3d(0, 0, 0);
          }

          50% {
            transform: translate3d(0, -12px, 0);
          }
        }

        @keyframes landingSoftFloat {
          0%,
          100% {
            transform: translate3d(0, 0, 0) rotate(-1deg);
          }

          50% {
            transform: translate3d(0, -9px, 0) rotate(1deg);
          }
        }

        @keyframes landingShine {
          0% {
            background-position: -220% 0;
          }

          100% {
            background-position: 220% 0;
          }
        }

        @keyframes landingBarPulse {
          0%,
          100% {
            opacity: 0.78;
            transform: scaleY(0.96);
          }

          50% {
            opacity: 1;
            transform: scaleY(1);
          }
        }

        .landing-float {
          animation: landingFloat 5.5s ease-in-out infinite;
        }

        .landing-soft-float {
          animation: landingSoftFloat 6.2s ease-in-out infinite;
        }

        .landing-bar-pulse {
          animation: landingBarPulse 2.2s ease-in-out infinite;
          transform-origin: bottom;
        }

        .landing-reveal {
          opacity: 0;
          transform: translateY(28px);
          transition:
            opacity 700ms ease,
            transform 700ms ease;
        }

        .landing-reveal-visible {
          opacity: 1;
          transform: translateY(0);
        }

        .landing-roadmap-shell {
          position: relative;
          animation: landingFloat 6.5s ease-in-out infinite;
          border-color: rgba(185, 216, 204, 0.85) !important;
          box-shadow:
            0 0 0 1px rgba(185, 216, 204, 0.85),
            0 0 28px rgba(47, 160, 132, 0.22),
            0 28px 90px rgba(47, 160, 132, 0.18),
            0 0 90px rgba(47, 160, 132, 0.2);
        }

        .landing-roadmap-shell::before {
          content: "";
          position: absolute;
          inset: -2px;
          z-index: 0;
          border-radius: 1.6rem;
          background: linear-gradient(
            110deg,
            rgba(47, 160, 132, 0),
            rgba(47, 160, 132, 0.45),
            rgba(47, 160, 132, 0.55),
            rgba(47, 160, 132, 0)
          );
          background-size: 240% 100%;
          animation: landingShine 3.8s linear infinite;
          pointer-events: none;
        }

        .landing-roadmap-shell > * {
          position: relative;
          z-index: 1;
        }

        .landing-section {
          position: relative;
          isolation: isolate;
          overflow: hidden;
          background:
            radial-gradient(
              circle at 14% 4%,
              rgba(111, 207, 151, 0.13),
              transparent 34%
            ),
            radial-gradient(
              circle at 86% 8%,
              rgba(185, 216, 204, 0.16),
              transparent 32%
            ),
            linear-gradient(
              180deg,
              rgba(247, 241, 232, 0.98) 0%,
              rgba(255, 255, 255, 0.48) 48%,
              rgba(247, 241, 232, 0.98) 100%
            );
        }

        .landing-section::before,
        .landing-section::after {
          content: "";
          position: absolute;
          left: 0;
          right: 0;
          z-index: -1;
          pointer-events: none;
        }

        .landing-section::before {
          top: 0;
          height: 220px;
          background: linear-gradient(
            180deg,
            #F7F1E8 0%,
            rgba(247, 241, 232, 0.88) 36%,
            rgba(247, 241, 232, 0.42) 68%,
            rgba(247, 241, 232, 0) 100%
          );
        }

        .landing-section::after {
          bottom: 0;
          height: 240px;
          background: linear-gradient(
            180deg,
            rgba(247, 241, 232, 0) 0%,
            rgba(247, 241, 232, 0.42) 28%,
            rgba(247, 241, 232, 0.86) 70%,
            #F7F1E8 100%
          );
        }

        .landing-card-soft {
          border-color: rgba(185, 216, 204, 0.88) !important;
          background: rgba(255, 255, 255, 0.82) !important;
          box-shadow: 0 10px 24px rgba(15, 23, 42, 0.035);
          backdrop-filter: blur(18px);
        }

        .landing-main
          :where(
            .landing-card-soft,
            .landing-hover-soft,
            article.rounded-3xl.border,
            div.rounded-3xl.border,
            div.rounded-2xl.border
          ) {
          transform: translate3d(0, 0, 0);
          transition:
            transform 260ms cubic-bezier(0.22, 1, 0.36, 1),
            box-shadow 260ms cubic-bezier(0.22, 1, 0.36, 1),
            border-color 260ms cubic-bezier(0.22, 1, 0.36, 1),
            background-color 260ms
              cubic-bezier(0.22, 1, 0.36, 1);
          will-change: transform;
        }

        .landing-main
          :where(
            .landing-card-soft,
            .landing-hover-soft,
            article.rounded-3xl.border,
            div.rounded-3xl.border,
            div.rounded-2xl.border
          ):hover {
          transform: translate3d(0, -2px, 0) !important;
          border-color: rgba(185, 216, 204, 1) !important;
          box-shadow: 0 12px 28px rgba(15, 23, 42, 0.07) !important;
        }

        .landing-main :where(.landing-roadmap-shell):hover {
          transform: translate3d(0, -2px, 0) !important;
        }

        .landing-overlap-glow {
          position: absolute;
          inset-inline: 6%;
          top: -110px;
          height: 220px;
          border-radius: 999px;
          background: radial-gradient(
            circle,
            rgba(111, 207, 151, 0.14),
            rgba(185, 216, 204, 0.08) 42%,
            rgba(247, 241, 232, 0) 72%
          );
          filter: blur(6px);
          pointer-events: none;
          z-index: -1;
        }

        @media (prefers-reduced-motion: reduce) {
          .landing-main
            :where(
              .landing-card-soft,
              .landing-hover-soft,
              article.rounded-3xl.border,
              div.rounded-3xl.border,
              div.rounded-2xl.border
            ),
          .landing-main
            :where(
              .landing-card-soft,
              .landing-hover-soft,
              article.rounded-3xl.border,
              div.rounded-3xl.border,
              div.rounded-2xl.border
            ):hover {
            transform: none !important;
            transition: none !important;
          }
        }
      `}</style>

      <header
        className={`fixed inset-x-0 top-0 z-50 border-transparent bg-[#F7F1E8]/92 shadow-sm backdrop-blur-xl transition-transform duration-300 ${
          isNavHidden
            ? "-translate-y-full"
            : "translate-y-0"
        }`}
      >
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-5 sm:px-6 lg:px-8">
          <button
            type="button"
            onClick={() => scrollToSection("top")}
            className="flex cursor-pointer items-center gap-3 text-[#18332D]"
            aria-label="TechMap home"
          >
            <span className="grid size-10 place-items-center rounded-2xl bg-[#2FA084] text-white shadow-lg shadow-emerald-950/30 ring-1 ring-[#6FCF97]/40">
              <CgListTree
                className="text-xl"
                aria-hidden="true"
              />
            </span>

            <span className="hidden text-lg font-black tracking-tight text-[#18332D] sm:inline">
              TechMap
            </span>
          </button>

          <nav className="hidden items-center gap-7 text-sm font-semibold text-slate-600 md:flex">
            {navItems.map((item) => (
              <button
                key={item.target}
                type="button"
                onClick={() =>
                  scrollToSection(item.target)
                }
                className="cursor-pointer transition hover:text-[#18332D]"
              >
                {item.label}
              </button>
            ))}
          </nav>

          <div className="hidden items-center gap-3 md:flex">
            {!user && (
              <Link
                to="/login"
                className="cursor-pointer rounded-xl px-4 py-2 text-sm font-semibold text-slate-700 no-underline transition hover:bg-white/70"
              >
                Sign in
              </Link>
            )}

            <Link
              to={primaryPath}
              className="inline-flex cursor-pointer items-center gap-2 rounded-xl bg-[#2FA084] px-4 py-2 text-sm font-bold text-white no-underline shadow-sm shadow-blue-950/40 transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
            >
              {primaryLabel}

              <ArrowRight
                size={16}
                aria-hidden="true"
              />
            </Link>
          </div>

          <button
            type="button"
            onClick={() =>
              setIsMenuOpen((current) => !current)
            }
            className="grid size-10 cursor-pointer place-items-center rounded-xl border border-[#B9D8CC] bg-white/70 text-[#18332D] md:hidden"
            aria-expanded={isMenuOpen}
            aria-label="Toggle navigation"
          >
            {isMenuOpen ? (
              <X size={20} />
            ) : (
              <Menu size={20} />
            )}
          </button>
        </div>

        {isMenuOpen && (
          <div className="border-t border-[#B9D8CC] bg-[#F7F1E8] px-5 py-4 md:hidden">
            <div className="flex flex-col gap-3 text-sm font-semibold text-slate-700">
              {navItems.map((item) => (
                <button
                  key={item.target}
                  type="button"
                  onClick={() =>
                    scrollToSection(item.target)
                  }
                  className="cursor-pointer text-left"
                >
                  {item.label}
                </button>
              ))}

              {!user && (
                <Link
                  to="/login"
                  onClick={closeMenu}
                  className="cursor-pointer"
                >
                  Sign in
                </Link>
              )}

              <Link
                to={primaryPath}
                onClick={closeMenu}
                className="inline-flex w-fit cursor-pointer items-center gap-2 rounded-xl bg-[#2FA084] px-4 py-2 text-white no-underline"
              >
                {primaryLabel}

                <ArrowRight
                  size={16}
                  aria-hidden="true"
                />
              </Link>
            </div>
          </div>
        )}
      </header>

      <main
        id="top"
        className="landing-main relative overflow-hidden bg-[#F7F1E8]"
      >
        <section className="relative isolate overflow-hidden pt-16 text-[#18332D]">
          <div className="absolute inset-0 -z-10 bg-[radial-gradient(circle_at_20%_15%,rgba(111,207,151,0.32),transparent_30%),radial-gradient(circle_at_78%_18%,rgba(185,216,204,0.30),transparent_30%),linear-gradient(180deg,#F7F1E8_0%,#F7F1E8_48%,rgba(255,255,255,0.72)_76%,#F7F1E8_100%)]" />

          <img
            src="/7372499.png"
            alt=""
            className="landing-soft-float pointer-events-none absolute right-[-140px] top-28 -z-10 w-[460px] opacity-20 sm:w-[560px] lg:right-[-60px]"
          />

          <div className="mx-auto max-w-7xl px-5 pb-16 pt-16 sm:px-6 lg:px-8 lg:pb-[5.5rem] lg:pt-20">
            <div className="landing-reveal mx-auto max-w-4xl text-center">
              <div className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-white/80 px-4 py-2 text-sm font-semibold text-[#1F6F5F] shadow-sm shadow-blue-950/30 backdrop-blur">
                <Sparkles
                  size={16}
                  className="text-[#2FA084]"
                  aria-hidden="true"
                />

                AI-powered career roadmap platform
              </div>

              <h1 className="mt-7 text-5xl font-black leading-[1.05] text-[#18332D] sm:text-6xl lg:text-7xl">
                Plan smarter careers with AI, market data,
                and portfolio proof.
              </h1>

              <p className="mx-auto mt-6 max-w-3xl text-lg leading-8 text-slate-600">
                TechMap helps students choose a target role,
                understand skill gaps, follow a dynamic
                roadmap, learn with an AI mentor, and publish
                employer-ready evidence from GitHub.
              </p>

              <div className="mt-9 flex flex-col justify-center gap-3 sm:flex-row">
                <Link
                  to={primaryPath}
                  className="inline-flex cursor-pointer items-center justify-center gap-2 rounded-xl bg-[#2FA084] px-5 py-3 text-sm font-bold text-white no-underline shadow-lg shadow-blue-950/40 transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
                >
                  {primaryLabel}

                  <ArrowRight
                    size={18}
                    aria-hidden="true"
                  />
                </Link>

                <button
                  type="button"
                  onClick={() =>
                    scrollToSection("mentor")
                  }
                  className="inline-flex cursor-pointer items-center justify-center rounded-xl border border-[#B9D8CC] bg-white px-5 py-3 text-sm font-bold text-[#18332D] transition hover:-translate-y-0.5 hover:bg-slate-100"
                >
                  Explore capabilities
                </button>
              </div>
            </div>

            <div className="relative mx-auto mt-10 max-w-6xl">
              <div className="landing-soft-float pointer-events-none absolute -left-3 top-7 z-20 hidden rounded-2xl border border-[#B9D8CC] bg-white/95 px-4 py-3 text-slate-900 shadow-2xl shadow-blue-950/30 backdrop-blur md:block">
                <p className="text-xs font-black uppercase tracking-[0.18em] text-[#1F6F5F]">
                  AI insight
                </p>

                <p className="mt-1 text-sm font-bold">
                  Docker should be next
                </p>
              </div>

              <div className="landing-soft-float pointer-events-none absolute -right-4 top-28 z-20 hidden rounded-2xl border border-[#B9D8CC] bg-white/95 px-4 py-3 text-slate-900 shadow-2xl shadow-blue-950/30 backdrop-blur lg:block">
                <p className="text-xs font-black uppercase tracking-[0.18em] text-blue-600">
                  Market pulse
                </p>

                <p className="mt-1 text-sm font-bold">
                  React demand +18%
                </p>
              </div>

              <div className="landing-reveal landing-roadmap-shell overflow-hidden rounded-[1.5rem] border border-[#B9D8CC] bg-white/40 p-2 backdrop-blur-xl">
                <div className="flex items-center justify-between border-b border-white/10 bg-[#05070d]/60 px-4 py-3">
                  <div className="flex items-center gap-2">
                    <span className="size-3 rounded-full bg-red-400" />
                    <span className="size-3 rounded-full bg-amber-300" />
                    <span className="size-3 rounded-full bg-violet-400" />
                  </div>

                  <div className="landing-soft-float hidden items-center gap-2 rounded-full border border-blue-300/70 bg-blue-400/15 px-3 py-1 text-xs font-semibold text-blue-100 shadow-lg shadow-blue-500/30 sm:flex">
                    <Lock
                      size={13}
                      aria-hidden="true"
                    />

                    /roadmaps
                  </div>
                </div>

                <div className="grid min-h-[560px] lg:grid-cols-[260px_1fr]">
                  <aside className="border-b border-white/10 bg-[#05070d] p-5 lg:border-b-0 lg:border-r lg:border-white/10">
                    <div className="landing-soft-float rounded-2xl border border-blue-300/70 bg-blue-400/15 p-4 shadow-2xl shadow-blue-500/35 ring-1 ring-blue-200/20">
                      <div className="flex items-center gap-3">
                        <span className="grid size-10 place-items-center rounded-xl bg-[#2FA084] text-sm font-black text-white shadow-lg shadow-emerald-950/30">
                          AI
                        </span>

                        <div>
                          <p className="text-sm font-bold text-white">
                            Career Mentor
                          </p>

                          <p className="text-xs text-blue-100/80">
                            Personalized guidance
                          </p>
                        </div>
                      </div>
                    </div>

                    <div className="mt-7 space-y-2">
                      {[
                        "Ask Mentor",
                        "Roadmap",
                        "Skill Gap",
                        "Market Pulse",
                      ].map((item, index) => (
                        <div
                          key={item}
                          className={`rounded-xl px-3 py-2 text-sm font-semibold ${
                            index === 0
                              ? "border border-blue-400/30 bg-blue-400/15 text-blue-100 shadow-sm shadow-blue-500/10"
                              : "text-slate-300"
                          }`}
                        >
                          {item}
                        </div>
                      ))}
                    </div>

                    <div className="mt-8 rounded-2xl border border-blue-400/30 bg-[#2FA084]/15 p-4 shadow-sm shadow-blue-950/20">
                      <p className="text-xs font-bold uppercase text-blue-100">
                        Current target role
                      </p>

                      <p className="mt-2 text-2xl font-black text-white">
                        DevOps
                      </p>

                      <p className="mt-1 text-sm leading-6 text-slate-400">
                        11 missing skills detected
                      </p>
                    </div>
                  </aside>

                  <div className="bg-[#0f172a] p-5">
                    <div className="grid gap-4 xl:grid-cols-[1fr_340px]">
                      <div className="rounded-2xl border border-white/10 bg-white p-5 text-slate-950 shadow-xl shadow-black/20">
                        <div className="flex flex-col gap-4 border-b border-slate-200 pb-5 sm:flex-row sm:items-center sm:justify-between">
                          <div>
                            <p className="text-sm font-semibold text-[#1F6F5F]">
                              AI Mentor
                            </p>

                            <h2 className="mt-1 text-3xl font-bold text-slate-900">
                              What should I learn next?
                            </h2>
                          </div>

                          <span className="w-fit rounded-full bg-[#6FCF97]/15 px-3 py-1 text-sm font-bold text-[#1F6F5F]">
                            transcript + GitHub
                          </span>
                        </div>

                        <div className="mt-5 space-y-3">
                          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                            <p className="text-sm font-semibold text-slate-900">
                              Student question
                            </p>

                            <p className="mt-2 text-sm leading-6 text-slate-600">
                              I want to become a DevOps
                              Engineer. I know basic Linux and
                              JavaScript. What should I
                              prioritize?
                            </p>
                          </div>

                          <div className="rounded-2xl border border-[#B9D8CC] bg-[#6FCF97]/15 p-4 shadow-sm shadow-emerald-100">
                            <p className="text-sm font-semibold text-[#1F6F5F]">
                              Mentor answer
                            </p>

                            <p className="mt-2 text-sm leading-6 text-slate-700">
                              Start with Docker and CI/CD. Your
                              GitHub profile shows frontend
                              work, so add one deployment
                              project to prove cloud and
                              automation skills.
                            </p>
                          </div>
                        </div>

                        <div className="mt-6 grid gap-3 sm:grid-cols-3">
                          {[
                            "Transcript",
                            "Skills",
                            "GitHub",
                          ].map((item) => (
                            <div
                              key={item}
                              className="rounded-2xl border border-slate-200 bg-white p-4"
                            >
                              <p className="font-semibold text-slate-900">
                                {item}
                              </p>

                              <p className="mt-2 text-xs leading-5 text-slate-500">
                                Used for personalization.
                              </p>
                            </div>
                          ))}
                        </div>
                      </div>

                      <div className="space-y-4">
                        <div className="rounded-2xl border border-white/10 bg-[#05070d] p-5">
                          <div className="flex items-center gap-2 text-sm font-bold text-slate-300">
                            <Terminal
                              size={17}
                              aria-hidden="true"
                            />

                            Roadmap generated
                          </div>

                          <div className="mt-4 space-y-3">
                            {roadmapNodes.map(
                              ([
                                number,
                                title,
                                meta,
                                status,
                              ]) => (
                                <div
                                  key={number}
                                  className="rounded-xl border border-white/10 bg-white/5 p-3"
                                >
                                  <div className="flex items-center justify-between gap-3">
                                    <span className="text-xs font-black text-blue-300">
                                      {number} {title}
                                    </span>

                                    <span className="text-xs font-semibold text-slate-400">
                                      {status}
                                    </span>
                                  </div>

                                  <p className="mt-1 text-sm text-white">
                                    {meta}
                                  </p>
                                </div>
                              ),
                            )}
                          </div>
                        </div>

                        <div className="rounded-2xl border border-white/10 bg-[#05070d] p-5">
                          <div className="flex items-center justify-between">
                            <p className="text-sm font-bold text-white">
                              Market demand trend
                            </p>

                            <LineChart
                              size={18}
                              className="text-blue-300"
                            />
                          </div>

                          <div className="mt-5 flex h-28 items-end gap-2">
                            {marketBars.map(
                              (height, index) => (
                                <span
                                  key={index}
                                  className={`landing-bar-pulse flex-1 rounded-t ${
                                    index === 5
                                      ? "bg-violet-500"
                                      : "bg-[#2FA084]"
                                  }`}
                                  style={{
                                    height: `${height}%`,
                                    animationDelay: `${
                                      index * 120
                                    }ms`,
                                  }}
                                />
                              ),
                            )}
                          </div>

                          <p className="mt-3 text-xs font-semibold text-slate-400">
                            Daily keyword analysis from job
                            descriptions.
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="mx-auto mt-5 grid max-w-3xl grid-cols-1 gap-3 sm:grid-cols-3">
                <StatCard
                  value={5}
                  label="career modules"
                />

                <StatCard
                  value={2}
                  suffix="+"
                  label="resources per node"
                />

                <StatCard
                  value={24}
                  suffix="h"
                  label="market refresh"
                />
              </div>
            </div>
          </div>
        </section>

        <section
          id="mentor"
          className="landing-section scroll-mt-24 py-16 text-[#18332D] lg:py-20"
        >
          <div className="landing-overlap-glow" />

          <div className="mx-auto max-w-7xl px-5 sm:px-6 lg:px-8">
            <div className="landing-reveal grid gap-10 lg:grid-cols-[0.88fr_1.12fr] lg:items-end">
              <div>
                <p className="text-sm font-black uppercase text-[#1F6F5F]">
                  Platform capabilities
                </p>

                <h2 className="mt-3 text-4xl font-black leading-tight sm:text-5xl">
                  One platform for guidance, planning, market
                  insight, and proof.
                </h2>
              </div>

              <p className="text-base leading-7 text-slate-600">
                TechMap connects the tools students usually use
                separately: chat advice, role roadmaps, skill
                gap reports, job market signals, and a shareable
                developer portfolio.
              </p>
            </div>

            <div className="mt-9 grid gap-6 lg:grid-cols-[340px_1fr]">
              <div className="landing-reveal space-y-3">
                {capabilityTabs.map(
                  ({ id, label, icon: Icon }) => {
                    const isActive =
                      id === activeCapability.id;

                    return (
                      <button
                        key={id}
                        type="button"
                        onClick={() =>
                          setActiveCapabilityId(id)
                        }
                        className={`flex w-full cursor-pointer items-center gap-3 rounded-2xl border p-4 text-left transition duration-300 hover:-translate-y-0.5 ${
                          isActive
                            ? "border-[#B9D8CC] bg-[#6FCF97]/15 shadow-sm"
                            : "border-slate-200 bg-white hover:border-[#B9D8CC] hover:shadow-[0_10px_24px_rgba(15,23,42,0.06)]"
                        }`}
                      >
                        <span
                          className={`grid size-11 place-items-center rounded-xl ${
                            isActive
                              ? "bg-[#2FA084] text-white"
                              : "bg-slate-100 text-slate-700"
                          }`}
                        >
                          <Icon
                            size={21}
                            aria-hidden="true"
                          />
                        </span>

                        <span className="font-bold text-slate-900">
                          {label}
                        </span>
                      </button>
                    );
                  },
                )}
              </div>

              <div
                key={activeCapability.id}
                className="landing-reveal landing-card-soft rounded-3xl border p-6"
              >
                <div className="grid gap-8 lg:grid-cols-[1fr_0.9fr]">
                  <div>
                    <p className="text-sm font-black uppercase text-[#1F6F5F]">
                      {activeCapability.label}
                    </p>

                    <h3 className="mt-3 text-3xl font-black leading-tight text-slate-950">
                      {activeCapability.title}
                    </h3>

                    <p className="mt-4 text-base leading-7 text-slate-600">
                      {activeCapability.text}
                    </p>

                    <div className="mt-6 grid gap-3">
                      {activeCapability.points.map(
                        (point) => (
                          <div
                            key={point}
                            className="flex items-start gap-3 rounded-2xl bg-slate-50 p-3 text-sm font-semibold text-slate-700"
                          >
                            <CheckCircle2
                              size={18}
                              className="mt-0.5 shrink-0 text-[#1F6F5F]"
                            />

                            {point}
                          </div>
                        ),
                      )}
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-200 bg-slate-950 p-5 text-white shadow-inner shadow-blue-950/30">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-bold text-blue-200">
                        {activeCapability.previewTitle}
                      </p>

                      <Sparkles
                        size={18}
                        className="text-[#2FA084]"
                      />
                    </div>

                    <p className="mt-5 text-sm leading-7 text-slate-200">
                      {activeCapability.preview}
                    </p>

                    <div className="mt-6 rounded-xl bg-white/5 p-4 font-mono text-xs leading-6 text-blue-200">
                      model.context = transcript + skills +
                      github + market
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section
          id="roadmap"
          className="landing-section scroll-mt-24 py-16 text-[#18332D] lg:py-20"
        >
          <div className="landing-overlap-glow" />

          <div className="mx-auto grid max-w-7xl gap-8 px-5 sm:px-6 lg:grid-cols-[1fr_0.9fr] lg:px-8">
            <div className="landing-reveal landing-card-soft rounded-3xl border p-6 transition hover:-translate-y-1">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-sm font-black uppercase text-[#1F6F5F]">
                    Dynamic Roadmap
                  </p>

                  <h2 className="mt-2 text-3xl font-black text-slate-950">
                    Role-based skill trees with real learning
                    resources.
                  </h2>
                </div>

                <span className="grid size-12 place-items-center rounded-2xl bg-[#6FCF97]/15 text-[#1F6F5F]">
                  <FileText
                    size={24}
                    aria-hidden="true"
                  />
                </span>
              </div>

              <div className="mt-6 grid gap-3">
                {[
                  [
                    "Docker Fundamentals",
                    "Documentation + YouTube",
                    "Required",
                  ],
                  [
                    "CI/CD Pipeline",
                    "GitHub Actions + Tutorial",
                    "High priority",
                  ],
                  [
                    "Cloud Deployment",
                    "AWS guide + project lab",
                    "Next",
                  ],
                ].map(([title, resources, priority]) => (
                  <div
                    key={title}
                    className="rounded-2xl border border-slate-200 bg-white p-4 transition hover:-translate-y-0.5 hover:border-[#B9D8CC] hover:shadow-[0_10px_24px_rgba(15,23,42,0.06)]"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <h3 className="font-semibold text-slate-900">
                        {title}
                      </h3>

                      <span className="rounded-full bg-[#6FCF97]/15 px-2.5 py-1 text-xs font-bold text-[#1F6F5F]">
                        {priority}
                      </span>
                    </div>

                    <p className="mt-2 text-sm text-slate-600">
                      {resources}
                    </p>
                  </div>
                ))}
              </div>
            </div>

            <div
              id="portfolio"
              className="landing-reveal landing-card-soft scroll-mt-24 rounded-3xl border transition hover:-translate-y-1"
            >
              <div className="h-44 rounded-t-3xl bg-[linear-gradient(180deg,#1f7567_0%,#1f7567_33%,#33a388_33%,#33a388_66%,#6fcf97_66%,#6fcf97_100%)]" />

              <div className="px-6 pb-6">
                <div className="-mt-14 flex items-end justify-between gap-4">
                  <div className="z-10 grid size-28 place-items-center rounded-full border-4 border-white bg-[#6FCF97]/25 text-[#1F6F5F] shadow-md">
                    <UserRound size={44} />
                  </div>

                  <span className="rounded-full border border-[#B9D8CC] bg-[#6FCF97]/15 px-4 py-2 text-xs font-bold uppercase text-[#1F6F5F]">
                    Shareable URL
                  </span>
                </div>

                <h2 className="mt-5 text-3xl font-bold text-slate-900">
                  An Nguyen
                </h2>

                <p className="mt-1 text-lg font-bold text-[#1F6F5F]">
                  Frontend Developer in progress
                </p>

                <p className="mt-4 text-sm leading-6 text-slate-600">
                  AI summarizes README files and turns
                  repositories into employer-friendly project
                  cards.
                </p>

                <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-[#B9D8CC]">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-bold text-[#1F6F5F]">
                        react-learning-roadmap
                      </p>

                      <p className="mt-1 text-sm text-slate-500">
                        an/techmap-roadmap
                      </p>
                    </div>

                    <Terminal
                      size={18}
                      className="text-slate-400"
                    />
                  </div>

                  <p className="mt-4 text-sm leading-6 text-slate-600">
                    Objective: help learners continue roadmap
                    tasks with a resource reader and roadmap
                    progress.
                  </p>

                  <div className="mt-5 flex flex-wrap gap-2 border-t border-slate-200 pt-4">
                    <span className="rounded-xl bg-[#6FCF97]/15 px-3 py-1 text-sm font-medium text-[#1F6F5F]">
                      React
                    </span>

                    <span className="inline-flex items-center gap-1 rounded-xl bg-slate-100 px-3 py-1 text-sm text-slate-700">
                      <Star size={14} /> 8
                    </span>

                    <span className="inline-flex items-center gap-1 rounded-xl bg-slate-100 px-3 py-1 text-sm text-slate-700">
                      <GitFork size={14} /> 2
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section
          id="market"
          className="landing-section scroll-mt-24 py-16 text-[#18332D] lg:py-20"
        >
          <div className="landing-overlap-glow" />

          <div className="mx-auto max-w-7xl px-5 sm:px-6 lg:px-8">
            <div className="landing-reveal mx-auto max-w-3xl text-center">
              <p className="text-sm font-black uppercase text-[#1F6F5F]">
                Market Pulse
              </p>

              <h2 className="mt-3 text-4xl font-black leading-tight sm:text-5xl">
                Let the job market influence what students
                learn next.
              </h2>

              <p className="mt-4 text-base leading-7 text-slate-600">
                Daily job data collection and keyword analysis
                highlight technologies that are growing,
                declining, or becoming essential for specific
                IT roles.
              </p>
            </div>

            <div className="mt-9 grid gap-4 lg:grid-cols-3">
              {workflowCards.map(
                ({ icon: Icon, eyebrow, title, text }) => (
                  <article
                    key={title}
                    className="landing-reveal landing-card-soft rounded-3xl border p-6 transition hover:-translate-y-1"
                  >
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-black uppercase text-slate-500">
                        {eyebrow}
                      </span>

                      <Icon
                        size={24}
                        className="text-[#1F6F5F]"
                      />
                    </div>

                    <h3 className="mt-8 text-2xl font-bold text-slate-900">
                      {title}
                    </h3>

                    <p className="mt-3 text-sm leading-6 text-slate-600">
                      {text}
                    </p>
                  </article>
                ),
              )}
            </div>
          </div>
        </section>

        <section className="landing-section py-16 text-[#18332D] lg:py-20">
          <div className="landing-overlap-glow" />

          <div className="mx-auto grid max-w-7xl gap-10 px-5 sm:px-6 lg:grid-cols-[0.85fr_1.15fr] lg:items-center lg:px-8">
            <div className="landing-reveal">
              <p className="text-sm font-black uppercase text-[#1F6F5F]">
                Secure user workspace
              </p>

              <h2 className="mt-3 text-4xl font-black leading-tight text-[#18332D] sm:text-5xl">
                Personalized career data stays persistent.
              </h2>

              <p className="mt-5 text-base leading-7 text-slate-600">
                TechMap supports authenticated accounts and
                stores the long-running data students care
                about: chat history, assessments, roadmaps,
                progress, and portfolio state.
              </p>

              <Link
                to={primaryPath}
                className="mt-8 inline-flex cursor-pointer items-center gap-2 rounded-xl bg-[#2FA084] px-5 py-3 text-sm font-bold text-white no-underline transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
              >
                {primaryLabel}

                <ArrowRight
                  size={18}
                  aria-hidden="true"
                />
              </Link>
            </div>

            <div className="landing-reveal landing-card-soft rounded-3xl border p-5">
              <div className="grid gap-3">
                {trustItems.map((item) => (
                  <div
                    key={item}
                    className="flex items-start gap-3 rounded-2xl border border-[#B9D8CC] bg-white p-4 text-sm font-semibold leading-6 text-slate-700"
                  >
                    <CheckCircle2
                      size={18}
                      className="mt-0.5 shrink-0 text-blue-300"
                    />

                    {item}
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>

        <section className="landing-section px-5 py-14 text-center text-[#18332D] sm:px-6 lg:px-8 lg:py-16">
          <div className="landing-overlap-glow" />

          <div className="landing-reveal mx-auto max-w-3xl">
            <Boxes
              size={36}
              className="mx-auto text-[#1F6F5F]"
            />

            <h2 className="mt-5 text-4xl font-black leading-tight">
              Build a career platform students can actually
              use.
            </h2>

            <p className="mx-auto mt-4 max-w-2xl text-base leading-7 text-slate-600">
              Replace scattered advice, static roadmap images,
              and manual portfolio writing with one AI-assisted
              workspace.
            </p>

            <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
              <Link
                to={primaryPath}
                className="inline-flex cursor-pointer items-center justify-center gap-2 rounded-xl bg-[#2FA084] px-5 py-3 text-sm font-bold text-white no-underline transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
              >
                {primaryLabel}

                <ArrowRight
                  size={18}
                  aria-hidden="true"
                />
              </Link>

              {!user && (
                <Link
                  to="/login"
                  className="inline-flex cursor-pointer items-center justify-center rounded-xl border border-slate-300 px-5 py-3 text-sm font-bold text-slate-800 no-underline hover:bg-slate-50"
                >
                  Sign in
                </Link>
              )}
            </div>
          </div>
        </section>

        <Footer />
      </main>
    </div>
  );
}