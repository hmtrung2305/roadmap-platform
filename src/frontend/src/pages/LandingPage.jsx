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
import { useState } from "react";
import { Link } from "react-router-dom";
import { useAuthStore } from "../stores/useAuthStore";

const navItems = [
  ["AI Mentor", "#mentor"],
  ["Roadmap", "#roadmap"],
  ["Market Pulse", "#market"],
  ["Portfolio", "#portfolio"],
];

const capabilityTabs = [
  {
    id: "mentor",
    label: "AI Mentor",
    icon: Sparkles,
    title: "Ask career questions in natural language.",
    text: "Students can ask about roles, skills, projects, and interview direction. The mentor can personalize answers with uploaded transcripts, selected skills, and public GitHub profile signals.",
    points: ["Natural language chat", "Transcript-aware guidance", "GitHub profile context"],
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
    points: ["Target role selection", "Hierarchical skill tree", "Node completion tracking"],
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
    points: ["Skill selection", "Role requirement mapping", "Priority report"],
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
    points: ["Daily job data pipeline", "Keyword frequency analysis", "Interactive trend charts"],
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
    points: ["GitHub repository sync", "README summarization", "Shareable profile URL"],
    previewTitle: "Portfolio card",
    preview:
      "react-learning-dashboard: React, REST API, resource reader. Objective: help learners continue roadmap tasks.",
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
  "Public landing remains separate from the protected /dashboard workspace.",
];

export default function LandingPage() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [activeCapabilityId, setActiveCapabilityId] = useState("mentor");
  const user = useAuthStore((state) => state.user);
  const primaryPath = user ? "/dashboard" : "/register";
  const primaryLabel = user ? "Open dashboard" : "Start building";
  const activeCapability =
    capabilityTabs.find((item) => item.id === activeCapabilityId) ||
    capabilityTabs[0];

  const closeMenu = () => setIsMenuOpen(false);

  return (
    <div className="min-h-screen bg-[#05070d] text-white">
      <header className="fixed inset-x-0 top-0 z-50 border-b border-white/10 bg-[#05070d]/90 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-5 sm:px-6 lg:px-8">
          <a
            href="#top"
            onClick={closeMenu}
            className="flex items-center gap-3 text-white no-underline"
            aria-label="TechMap home"
          >
            <span className="grid size-9 place-items-center rounded-md bg-white text-sm font-black text-[#05070d]">
              TM
            </span>
            <span className="text-xl font-bold">TechMap</span>
          </a>

          <nav className="hidden items-center gap-7 text-sm font-semibold text-slate-300 md:flex">
            {navItems.map(([label, href]) => (
              <a key={href} href={href} className="hover:text-white">
                {label}
              </a>
            ))}
          </nav>

          <div className="hidden items-center gap-3 md:flex">
            {!user && (
              <Link
                to="/login"
                className="rounded-md px-4 py-2 text-sm font-semibold text-slate-200 no-underline hover:bg-white/10"
              >
                Sign in
              </Link>
            )}
            <Link
              to={primaryPath}
              className="inline-flex items-center gap-2 rounded-md bg-[#2FA084] px-4 py-2 text-sm font-bold text-white no-underline shadow-sm shadow-blue-950/40 transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/150"
            >
              {primaryLabel}
              <ArrowRight size={16} aria-hidden="true" />
            </Link>
          </div>

          <button
            type="button"
            onClick={() => setIsMenuOpen((current) => !current)}
            className="grid size-10 place-items-center rounded-md border border-white/15 bg-white/5 text-white md:hidden"
            aria-expanded={isMenuOpen}
            aria-label="Toggle navigation"
          >
            {isMenuOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>

        {isMenuOpen && (
          <div className="border-t border-white/10 bg-[#05070d] px-5 py-4 md:hidden">
            <div className="flex flex-col gap-3 text-sm font-semibold text-slate-200">
              {navItems.map(([label, href]) => (
                <a key={href} href={href} onClick={closeMenu}>
                  {label}
                </a>
              ))}
              {!user && (
                <Link to="/login" onClick={closeMenu}>
                  Sign in
                </Link>
              )}
              <Link
                to={primaryPath}
                onClick={closeMenu}
                className="inline-flex w-fit items-center gap-2 rounded-md bg-[#2FA084] px-4 py-2 text-white no-underline"
              >
                {primaryLabel}
                <ArrowRight size={16} aria-hidden="true" />
              </Link>
            </div>
          </div>
        )}
      </header>

      <main id="top">
        <section className="relative isolate overflow-hidden pt-16">
          <div className="landing-glow absolute inset-0 -z-10 bg-[radial-gradient(circle_at_22%_12%,rgba(37,99,235,0.52),transparent_31%),radial-gradient(circle_at_78%_18%,rgba(109,40,217,0.22),transparent_26%),linear-gradient(180deg,#05070d_0%,#0f172a_64%,#ffffff_64%,#ffffff_100%)]" />
          <img
            src="/7372499.png"
            alt=""
            className="landing-float pointer-events-none absolute right-[-140px] top-28 -z-10 w-[460px] opacity-20 sm:w-[560px] lg:right-[-60px]"
          />

          <div className="mx-auto max-w-7xl px-5 pb-20 pt-20 sm:px-6 lg:px-8 lg:pb-28 lg:pt-24">
            <div className="mx-auto max-w-4xl text-center">
              <div className="inline-flex items-center gap-2 rounded-md border border-blue-400/40 bg-[#6FCF97]/150/10 px-3 py-2 text-sm font-semibold text-blue-100 shadow-sm shadow-blue-950/30">
                <Sparkles
                  size={16}
                  className="text-violet-300"
                  aria-hidden="true"
                />
                AI-powered career roadmap platform
              </div>

              <h1 className="mt-7 text-5xl font-black leading-[1.05] text-white sm:text-6xl lg:text-7xl">
                Plan smarter careers with AI, market data, and portfolio proof.
              </h1>

              <p className="mx-auto mt-6 max-w-3xl text-lg leading-8 text-slate-300">
                TechMap helps students choose a target role, understand skill
                gaps, follow a dynamic roadmap, learn with an AI mentor, and
                publish employer-ready evidence from GitHub.
              </p>

              <div className="mt-9 flex flex-col justify-center gap-3 sm:flex-row">
                <Link
                  to={primaryPath}
                  className="inline-flex items-center justify-center gap-2 rounded-md bg-[#2FA084] px-5 py-3 text-sm font-bold text-white no-underline shadow-lg shadow-blue-950/40 transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/150"
                >
                  {primaryLabel}
                  <ArrowRight size={18} aria-hidden="true" />
                </Link>
                <a
                  href="#mentor"
                  className="inline-flex items-center justify-center rounded-md border border-white/20 bg-white px-5 py-3 text-sm font-bold text-[#05070d] no-underline transition hover:-translate-y-0.5 hover:bg-slate-100"
                >
                  Explore capabilities
                </a>
              </div>
            </div>

            <div className="relative mx-auto mt-14 max-w-6xl">
              <div className="landing-float overflow-hidden rounded-lg border border-blue-400/20 bg-[#0b1220] shadow-2xl shadow-blue-950/50">
                <div className="flex items-center justify-between border-b border-white/10 px-4 py-3">
                  <div className="flex items-center gap-2">
                    <span className="size-3 rounded-full bg-red-400" />
                    <span className="size-3 rounded-full bg-amber-300" />
                    <span className="size-3 rounded-full bg-violet-400" />
                  </div>
                  <div className="hidden items-center gap-2 rounded-md border border-white/10 bg-white/5 px-3 py-1 text-xs font-semibold text-slate-300 sm:flex">
                    <Lock size={13} aria-hidden="true" />
                    /dashboard
                  </div>
                </div>

                <div className="grid min-h-[560px] lg:grid-cols-[260px_1fr]">
                  <aside className="border-b border-white/10 bg-[#05070d] p-5 lg:border-b-0 lg:border-r">
                    <div className="flex items-center gap-3">
                      <span className="grid size-10 place-items-center rounded-md bg-[#2FA084] text-sm font-black text-white">
                        AI
                      </span>
                      <div>
                        <p className="text-sm font-bold text-white">
                          Career Mentor
                        </p>
                        <p className="text-xs text-slate-400">
                          Personalized guidance
                        </p>
                      </div>
                    </div>

                    <div className="mt-7 space-y-2">
                      {["Ask Mentor", "Roadmap", "Skill Gap", "Market Pulse"].map(
                        (item, index) => (
                          <div
                            key={item}
                            className={`rounded-md px-3 py-2 text-sm font-semibold ${
                              index === 0
                                ? "border border-blue-400/20 bg-[#2FA084]/20 text-blue-100"
                                : "text-slate-300"
                            }`}
                          >
                            {item}
                          </div>
                        ),
                      )}
                    </div>

                    <div className="mt-8 rounded-lg border border-blue-400/25 bg-[#6FCF97]/150/10 p-4 shadow-sm shadow-blue-950/20">
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
                      <div className="rounded-lg border border-white/10 bg-white p-5 text-slate-950">
                        <div className="flex flex-col gap-4 border-b border-slate-200 pb-5 sm:flex-row sm:items-center sm:justify-between">
                          <div>
                            <p className="text-sm font-semibold text-[#1F6F5F]">
                              AI Mentor
                            </p>
                            <h2 className="mt-1 text-3xl font-bold text-slate-900">
                              What should I learn next?
                            </h2>
                          </div>
                          <span className="w-fit rounded-md bg-[#6FCF97]/15 px-3 py-1 text-sm font-bold text-[#1F6F5F]">
                            transcript + GitHub
                          </span>
                        </div>

                        <div className="mt-5 space-y-3">
                          <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
                            <p className="text-sm font-semibold text-slate-900">
                              Student question
                            </p>
                            <p className="mt-2 text-sm leading-6 text-slate-600">
                              I want to become a DevOps Engineer. I know basic
                              Linux and JavaScript. What should I prioritize?
                            </p>
                          </div>
                          <div className="rounded-lg border border-[#B9D8CC] bg-[#6FCF97]/15 p-4">
                            <p className="text-sm font-semibold text-[#1F6F5F]">
                              Mentor answer
                            </p>
                            <p className="mt-2 text-sm leading-6 text-slate-700">
                              Start with Docker and CI/CD. Your GitHub profile
                              shows frontend work, so add one deployment project
                              to prove cloud and automation skills.
                            </p>
                          </div>
                        </div>

                        <div className="mt-6 grid gap-3 sm:grid-cols-3">
                          {["Transcript", "Skills", "GitHub"].map((item) => (
                            <div
                              key={item}
                              className="rounded-lg border border-slate-200 bg-white p-4"
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
                        <div className="rounded-lg border border-white/10 bg-[#05070d] p-5">
                          <div className="flex items-center gap-2 text-sm font-bold text-slate-300">
                            <Terminal size={17} aria-hidden="true" />
                            Roadmap generated
                          </div>
                          <div className="mt-4 space-y-3">
                            {roadmapNodes.map(([number, title, meta, status]) => (
                              <div
                                key={number}
                                className="rounded-md border border-white/10 bg-white/5 p-3"
                              >
                                <div className="flex items-center justify-between gap-3">
                                  <span className="text-xs font-black text-blue-300">
                                    {number} {title}
                                  </span>
                                  <span className="text-xs font-semibold text-slate-400">
                                    {status}
                                  </span>
                                </div>
                                <p className="mt-1 text-sm text-white">{meta}</p>
                              </div>
                            ))}
                          </div>
                        </div>

                        <div className="rounded-lg border border-white/10 bg-[#05070d] p-5">
                          <div className="flex items-center justify-between">
                            <p className="text-sm font-bold text-white">
                              Market demand trend
                            </p>
                            <LineChart size={18} className="text-blue-300" />
                          </div>
                          <div className="mt-5 flex h-28 items-end gap-2">
                            {marketBars.map((height, index) => (
                              <span
                                key={index}
                                className={`landing-bar-pulse flex-1 rounded-t ${
                                  index === 5 ? "bg-violet-500" : "bg-[#6FCF97]/150"
                                }`}
                                style={{ height: `${height}%` }}
                              />
                            ))}
                          </div>
                          <p className="mt-3 text-xs font-semibold text-slate-400">
                            Daily keyword analysis from job descriptions.
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="mx-auto mt-6 grid max-w-3xl grid-cols-3 gap-3">
                {[
                  ["5", "career modules"],
                  ["2+", "resources per node"],
                  ["24h", "market refresh"],
                ].map(([value, label]) => (
                  <div
                    key={label}
                    className="rounded-lg border border-slate-200 bg-white p-4 text-center shadow-sm"
                  >
                    <strong className="block text-2xl font-black text-[#05070d]">
                      {value}
                    </strong>
                    <span className="mt-1 block text-sm font-semibold text-slate-600">
                      {label}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>

        <section id="mentor" className="bg-white py-24 text-slate-950">
          <div className="mx-auto max-w-7xl px-5 sm:px-6 lg:px-8">
            <div className="grid gap-10 lg:grid-cols-[0.88fr_1.12fr] lg:items-end">
              <div>
                <p className="text-sm font-black uppercase text-[#1F6F5F]">
                  Platform capabilities
                </p>
                <h2 className="mt-3 text-4xl font-black leading-tight sm:text-5xl">
                  One platform for guidance, planning, market insight, and proof.
                </h2>
              </div>
              <p className="text-base leading-7 text-slate-600">
                TechMap connects the tools students usually use separately:
                chat advice, role roadmaps, skill gap reports, job market
                signals, and a shareable developer portfolio.
              </p>
            </div>

            <div className="mt-12 grid gap-6 lg:grid-cols-[340px_1fr]">
              <div className="space-y-3">
                {capabilityTabs.map(({ id, label, icon: Icon }) => {
                  const isActive = id === activeCapability.id;

                  return (
                    <button
                      key={id}
                      type="button"
                      onClick={() => setActiveCapabilityId(id)}
                      className={`landing-lift flex w-full items-center gap-3 rounded-lg border p-4 text-left ${
                        isActive
                          ? "border-blue-300 bg-[#6FCF97]/15 shadow-md"
                          : "border-slate-200 bg-white hover:border-[#B9D8CC] hover:shadow-sm"
                      }`}
                    >
                      <span
                        className={`grid size-11 place-items-center rounded-md ${
                          isActive
                            ? "bg-[#2FA084] text-white"
                            : "bg-slate-100 text-slate-700"
                        }`}
                      >
                        <Icon size={21} aria-hidden="true" />
                      </span>
                      <span className="font-bold text-slate-900">{label}</span>
                    </button>
                  );
                })}
              </div>

              <div className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
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
                      {activeCapability.points.map((point) => (
                        <div
                          key={point}
                          className="flex items-start gap-3 rounded-lg bg-slate-50 p-3 text-sm font-semibold text-slate-700"
                        >
                          <CheckCircle2
                            size={18}
                            className="mt-0.5 shrink-0 text-[#1F6F5F]"
                          />
                          {point}
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="rounded-lg border border-slate-200 bg-slate-950 p-5 text-white">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-bold text-blue-200">
                        {activeCapability.previewTitle}
                      </p>
                      <Sparkles size={18} className="text-violet-300" />
                    </div>
                    <p className="mt-5 text-sm leading-7 text-slate-200">
                      {activeCapability.preview}
                    </p>
                    <div className="mt-6 rounded-md bg-white/5 p-4 font-mono text-xs leading-6 text-blue-200">
                      model.context = transcript + skills + github + market
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="roadmap" className="bg-slate-100 py-24 text-slate-950">
          <div className="mx-auto grid max-w-7xl gap-8 px-5 sm:px-6 lg:grid-cols-[1fr_0.9fr] lg:px-8">
            <div className="landing-lift rounded-lg border border-slate-200 bg-white p-6 shadow-sm hover:border-blue-300 hover:shadow-md">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-sm font-black uppercase text-[#1F6F5F]">
                    Dynamic Roadmap
                  </p>
                  <h2 className="mt-2 text-3xl font-black text-slate-950">
                    Role-based skill trees with real learning resources.
                  </h2>
                </div>
                <span className="grid size-12 place-items-center rounded-md bg-[#6FCF97]/15 text-[#1F6F5F]">
                  <FileText size={24} aria-hidden="true" />
                </span>
              </div>

              <div className="mt-6 grid gap-3">
                {[
                  ["Docker Fundamentals", "Documentation + YouTube", "Required"],
                  ["CI/CD Pipeline", "GitHub Actions + Tutorial", "High priority"],
                  ["Cloud Deployment", "AWS guide + project lab", "Next"],
                ].map(([title, resources, priority]) => (
                  <div
                    key={title}
                    className="landing-lift rounded-lg border border-slate-200 bg-white p-4 hover:border-[#B9D8CC] hover:shadow-sm"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <h3 className="font-semibold text-slate-900">{title}</h3>
                      <span className="rounded-full bg-[#6FCF97]/15 px-2.5 py-1 text-xs font-bold text-[#1F6F5F]">
                        {priority}
                      </span>
                    </div>
                    <p className="mt-2 text-sm text-slate-600">{resources}</p>
                  </div>
                ))}
              </div>
            </div>

            <div
              id="portfolio"
              className="landing-lift rounded-lg border border-slate-200 bg-white shadow-sm hover:border-blue-300 hover:shadow-md"
            >
              <div className="h-44 rounded-t-lg bg-[linear-gradient(90deg,#1d4ed8,#6d28d9,#020617)]" />
              <div className="px-6 pb-6">
                <div className="-mt-14 flex items-end justify-between gap-4">
                  <div className="grid size-28 place-items-center rounded-full border-4 border-white bg-[#6FCF97]/25 text-[#1F6F5F] shadow-md">
                    <UserRound size={44} />
                  </div>
                  <span className="rounded-md border border-[#B9D8CC] bg-[#6FCF97]/15 px-4 py-2 text-xs font-bold uppercase text-[#1F6F5F]">
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
                  AI summarizes README files and turns repositories into
                  employer-friendly project cards.
                </p>

                <div className="landing-lift mt-6 rounded-lg border border-slate-200 bg-white p-5 shadow-sm hover:border-[#B9D8CC]">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-bold text-[#1F6F5F]">
                        react-learning-dashboard
                      </p>
                      <p className="mt-1 text-sm text-slate-500">
                        an/techmap-dashboard
                      </p>
                    </div>
                    <Terminal size={18} className="text-slate-400" />
                  </div>
                  <p className="mt-4 text-sm leading-6 text-slate-600">
                    Objective: help learners continue roadmap tasks with a
                    resource reader and progress dashboard.
                  </p>
                  <div className="mt-5 flex flex-wrap gap-2 border-t border-slate-200 pt-4">
                    <span className="rounded-md bg-[#6FCF97]/15 px-3 py-1 text-sm font-medium text-[#1F6F5F]">
                      React
                    </span>
                    <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
                      <Star size={14} /> 8
                    </span>
                    <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-3 py-1 text-sm text-slate-700">
                      <GitFork size={14} /> 2
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="market" className="bg-white py-24 text-slate-950">
          <div className="mx-auto max-w-7xl px-5 sm:px-6 lg:px-8">
            <div className="mx-auto max-w-3xl text-center">
              <p className="text-sm font-black uppercase text-[#1F6F5F]">
                Market Pulse
              </p>
              <h2 className="mt-3 text-4xl font-black leading-tight sm:text-5xl">
                Let the job market influence what students learn next.
              </h2>
              <p className="mt-4 text-base leading-7 text-slate-600">
                Daily job data collection and keyword analysis highlight
                technologies that are growing, declining, or becoming essential
                for specific IT roles.
              </p>
            </div>

            <div className="mt-12 grid gap-4 lg:grid-cols-3">
              {workflowCards.map(({ icon: Icon, eyebrow, title, text }) => (
                <article
                  key={title}
                  className="landing-lift rounded-lg border border-slate-200 bg-white p-6 shadow-sm hover:border-blue-300 hover:shadow-md"
                >
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-black uppercase text-slate-500">
                      {eyebrow}
                    </span>
                    <Icon size={24} className="text-[#1F6F5F]" />
                  </div>
                  <h3 className="mt-8 text-2xl font-bold text-slate-900">
                    {title}
                  </h3>
                  <p className="mt-3 text-sm leading-6 text-slate-600">{text}</p>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className="bg-[#05070d] py-24">
          <div className="mx-auto grid max-w-7xl gap-10 px-5 sm:px-6 lg:grid-cols-[0.85fr_1.15fr] lg:items-center lg:px-8">
            <div>
              <p className="text-sm font-black uppercase text-blue-300">
                Secure user workspace
              </p>
              <h2 className="mt-3 text-4xl font-black leading-tight text-white sm:text-5xl">
                Personalized career data stays persistent.
              </h2>
              <p className="mt-5 text-base leading-7 text-slate-300">
                TechMap supports authenticated accounts and stores the
                long-running data students care about: chat history, assessments,
                roadmaps, progress, and portfolio state.
              </p>
              <Link
                to={primaryPath}
                className="mt-8 inline-flex items-center gap-2 rounded-md bg-[#2FA084] px-5 py-3 text-sm font-bold text-white no-underline transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/150"
              >
                {primaryLabel}
                <ArrowRight size={18} aria-hidden="true" />
              </Link>
            </div>

            <div className="rounded-lg border border-white/10 bg-white/5 p-5">
              <div className="grid gap-3">
                {trustItems.map((item) => (
                  <div
                    key={item}
                    className="flex items-start gap-3 rounded-lg border border-white/10 bg-white/5 p-4 text-sm font-semibold leading-6 text-slate-200"
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

        <section className="bg-white px-5 py-20 text-center text-slate-950 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-3xl">
            <Boxes size={36} className="mx-auto text-[#1F6F5F]" />
            <h2 className="mt-5 text-4xl font-black leading-tight">
              Build a career platform students can actually use.
            </h2>
            <p className="mx-auto mt-4 max-w-2xl text-base leading-7 text-slate-600">
              Replace scattered advice, static roadmap images, and manual
              portfolio writing with one AI-assisted workspace.
            </p>
            <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
              <Link
                to={primaryPath}
                className="inline-flex items-center justify-center gap-2 rounded-md bg-[#2FA084] px-5 py-3 text-sm font-bold text-white no-underline transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
              >
                {primaryLabel}
                <ArrowRight size={18} aria-hidden="true" />
              </Link>
              {!user && (
                <Link
                  to="/login"
                  className="inline-flex items-center justify-center rounded-md border border-slate-300 px-5 py-3 text-sm font-bold text-slate-800 no-underline hover:bg-slate-50"
                >
                  Sign in
                </Link>
              )}
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
