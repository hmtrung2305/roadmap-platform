import {
  Bot,
  ChevronDown,
  ChevronRight,
  Briefcase,
  Calendar,
  Clock3,
  Map,
  Pin,
  Plus,
  SendHorizontal,
  Sparkles,
  Trash2,
  UserRound,
  X,
} from "lucide-react";
import { FaGithub } from "react-icons/fa";
import { createPortal } from "react-dom";
import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../../stores/useAuthStore";

const starterPrompts = [
  {
    label: "Suggest roadmap",
    icon: Map,
    prompt: "Which roadmap should I choose if I want to become a full-stack developer?",
  },
  {
    label: "Weekly plan",
    icon: Calendar,
    prompt: "Create a weekly learning plan from my current roadmap progress.",
  },
  {
    label: "Portfolio ideas",
    icon: Briefcase,
    prompt: "What project should I build next for my portfolio?",
  },
  {
    label: "Skill assessment",
    icon: Sparkles,
    prompt: "Which skills should I improve first for internship preparation?",
  },
];

const initialSessions = [
  {
    id: "role-plan",
    title: "Career role plan",
    time: "Now",
    pinned: true,
  },
  {
    id: "github-review",
    title: "GitHub profile review",
    time: "2h ago",
  },
  {
    id: "internship-focus",
    title: "Internship focus",
    time: "Yesterday",
  },
  {
    id: "system-design",
    title: "System design prep",
    time: "2d ago",
  },
  {
    id: "portfolio-checklist",
    title: "Portfolio checklist",
    time: "5d ago",
  },
];

const initialMessagesBySession = {
  "role-plan": [
    {
      id: "m-1",
      role: "assistant",
      content:
        "Hi! I can help you choose a career role, plan your roadmap order, and turn learning progress into portfolio evidence.",
    },
  ],
  "github-review": [
    {
      id: "m-2",
      role: "assistant",
      content:
        "After GitHub is connected, I can review your public projects, README quality, tech stack, and portfolio gaps.",
    },
  ],
  "internship-focus": [
    {
      id: "m-3",
      role: "assistant",
      content:
        "For internship preparation, I can help you choose the highest-impact skills and convert them into a weekly plan.",
    },
  ],
  "system-design": [
    {
      id: "m-4",
      role: "assistant",
      content:
        "We can break system design into small concepts: APIs, databases, caching, queues, and deployment basics.",
    },
  ],
  "portfolio-checklist": [
    {
      id: "m-5",
      role: "assistant",
      content:
        "A strong portfolio should show the problem, features, tech stack, screenshots, deployment, and what you learned.",
    },
  ],
};

export default function CareerMentorWidget() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const [isOpen, setIsOpen] = useState(false);
  const [activeSessionId, setActiveSessionId] = useState("role-plan");
  const [sessions, setSessions] = useState(initialSessions);
  const [messagesBySession, setMessagesBySession] = useState(initialMessagesBySession);
  const [input, setInput] = useState("");
  const [isReplying, setIsReplying] = useState(false);
  const [activeStatus, setActiveStatus] = useState(null);
  const [isRecentCollapsed, setIsRecentCollapsed] = useState(false);
  const messagesEndRef = useRef(null);
  const statusAreaRef = useRef(null);

  const messages = messagesBySession[activeSessionId] ?? [];
  const activeSession = sessions.find((session) => session.id === activeSessionId);
  const visibleSessions = [...sessions].sort((first, second) => {
    if (first.pinned === second.pinned) return 0;
    return first.pinned ? -1 : 1;
  });

  const learnerName =
    user?.username ||
    user?.name ||
    user?.email?.split("@")[0] ||
    "learner";

  const connectionState = useMemo(
    () => ({
      githubConnected: false,
      roleSelected: false,
    }),
    [],
  );

  useEffect(() => {
    if (!isOpen) return;

    requestAnimationFrame(() => {
      messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
    });
  }, [isOpen, activeSessionId, messages.length, isReplying]);

  useEffect(() => {
    if (!activeStatus) return;

    function handlePointerDown(event) {
      if (statusAreaRef.current?.contains(event.target)) return;
      setActiveStatus(null);
    }

    function handleKeyDown(event) {
      if (event.key === "Escape") {
        setActiveStatus(null);
      }
    }

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [activeStatus]);

  useEffect(() => {
    if (!isOpen || typeof document === "undefined") return;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [isOpen]);

  function handleCreateSession() {
    const nextId = `session-${Date.now()}`;
    const nextSession = {
      id: nextId,
      title: "New conversation",
      time: "Now",
      pinned: false,
    };

    setSessions((current) => [nextSession, ...current]);
    setMessagesBySession((current) => ({
      ...current,
      [nextId]: [
        {
          id: `${nextId}-intro`,
          role: "assistant",
          content: "What career question do you want to explore today?",
        },
      ],
    }));
    setActiveSessionId(nextId);
  }


  function handleDeleteSession(sessionId) {
    setSessions((current) => {
      const remaining = current.filter((session) => session.id !== sessionId);

      if (remaining.length === 0) {
        const nextId = `session-${Date.now()}`;
        const nextSession = {
          id: nextId,
          title: "New conversation",
          time: "Now",
          pinned: false,
        };

        setMessagesBySession((currentMessages) => {
          const updated = { ...currentMessages };
          delete updated[sessionId];
          updated[nextId] = [
            {
              id: `${nextId}-intro`,
              role: "assistant",
              content: "What career question do you want to explore today?",
            },
          ];
          return updated;
        });
        setActiveSessionId(nextId);
        return [nextSession];
      }

      setMessagesBySession((currentMessages) => {
        const updated = { ...currentMessages };
        delete updated[sessionId];
        return updated;
      });

      if (activeSessionId === sessionId) {
        setActiveSessionId(remaining[0].id);
      }

      return remaining;
    });
  }

  function handleTogglePin(sessionId) {
    setSessions((current) =>
      current.map((session) =>
        session.id === sessionId
          ? {
              ...session,
              pinned: !session.pinned,
            }
          : session,
      ),
    );
  }

  function handlePrompt(prompt) {
    setInput(prompt);
  }

  function handleCloseWidget() {
    setActiveStatus(null);
    setIsOpen(false);
  }

  function handleNavigate(path) {
    setActiveStatus(null);
    setIsOpen(false);
    navigate(path);
  }

  function handleSubmit(event) {
    event.preventDefault();

    const text = input.trim();
    if (!text || isReplying) return;

    const userMessage = {
      id: `user-${Date.now()}`,
      role: "user",
      content: text,
    };

    setInput("");
    setMessagesBySession((current) => ({
      ...current,
      [activeSessionId]: [...(current[activeSessionId] ?? []), userMessage],
    }));

    setSessions((current) =>
      current.map((session) =>
        session.id === activeSessionId
          ? {
              ...session,
              title: text,
              time: "Now",
            }
          : session,
      ),
    );

    setIsReplying(true);

    window.setTimeout(() => {
      const assistantMessage = {
        id: `assistant-${Date.now()}`,
        role: "assistant",
        content: buildMockAnswer(text, learnerName),
      };

      setMessagesBySession((current) => ({
        ...current,
        [activeSessionId]: [...(current[activeSessionId] ?? []), assistantMessage],
      }));
      setIsReplying(false);
    }, 520);
  }

  if (typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <>
      <style>
        {`
          @keyframes mentorPillFloat {
            0%, 100% {
              transform: translateY(0);
            }

            50% {
              transform: translateY(-5px);
            }
          }

          @keyframes mentorPillGlow {
            0%, 100% {
              box-shadow: 0 18px 34px rgba(31, 111, 95, 0.22);
            }

            50% {
              box-shadow: 0 24px 46px rgba(31, 111, 95, 0.32);
            }
          }

          @keyframes mentorBotAttention {
            0%, 72%, 100% {
              transform: rotate(0deg) scale(1);
            }

            78% {
              transform: rotate(-7deg) scale(1.04);
            }

            84% {
              transform: rotate(7deg) scale(1.05);
            }

            90% {
              transform: rotate(-4deg) scale(1.03);
            }
          }

          @keyframes mentorBotWiggle {
            0%, 100% {
              transform: rotate(0deg) scale(1);
            }

            25% {
              transform: rotate(-9deg) scale(1.06);
            }

            50% {
              transform: rotate(9deg) scale(1.08);
            }

            75% {
              transform: rotate(-5deg) scale(1.04);
            }
          }

          @keyframes mentorDotPulse {
            0% {
              transform: scale(1);
              opacity: 0.9;
            }

            70% {
              transform: scale(2.35);
              opacity: 0;
            }

            100% {
              transform: scale(2.35);
              opacity: 0;
            }
          }

          @keyframes mentorMessageIn {
            0% {
              opacity: 0;
              transform: translateY(8px) scale(0.98);
            }

            100% {
              opacity: 1;
              transform: translateY(0) scale(1);
            }
          }

          @keyframes mentorActiveSessionGlow {
            0%, 100% {
              box-shadow: 0 8px 20px rgba(47, 160, 132, 0.08);
            }

            50% {
              box-shadow: 0 12px 26px rgba(47, 160, 132, 0.13);
            }
          }

          .mentor-message-enter {
            animation: mentorMessageIn 0.28s ease-out both;
          }

          .mentor-active-session {
            animation: mentorActiveSessionGlow 3.2s ease-in-out infinite;
          }

          .mentor-launcher-pill {
            animation:
              mentorPillFloat 4.2s ease-in-out infinite,
              mentorPillGlow 4.2s ease-in-out infinite;
          }

          .mentor-launcher-bot {
            animation: mentorBotAttention 3.8s ease-in-out infinite;
            transform-origin: center;
          }

          .mentor-launcher-pill:hover .mentor-launcher-bot {
            animation: mentorBotWiggle 0.6s ease-in-out;
          }

          .mentor-launcher-dot::after {
            content: "";
            position: absolute;
            inset: 0;
            border-radius: 999px;
            background: #6FCF97;
            animation: mentorDotPulse 2.2s ease-out infinite;
          }

          .mentor-scrollbar-hidden {
            scrollbar-width: none;
            -ms-overflow-style: none;
          }

          .mentor-scrollbar-hidden::-webkit-scrollbar {
            width: 0;
            height: 0;
            display: none;
          }

          @media (prefers-reduced-motion: reduce) {
            .mentor-launcher-pill,
            .mentor-launcher-bot,
            .mentor-launcher-dot::after,
            .mentor-message-enter,
            .mentor-active-session {
              animation: none;
            }
          }
        `}
      </style>

      <button
        type="button"
        onClick={() => setIsOpen(true)}
        className={`mentor-launcher-pill fixed bottom-6 right-6 z-40 inline-flex h-14 items-center gap-3 rounded-xl border border-white/80 bg-[#2FA084] px-4 pr-5 text-white shadow-xl transition duration-300 hover:-translate-y-1 hover:bg-[#1F6F5F] focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-[#6FCF97]/40 ${
          isOpen ? "pointer-events-none scale-95 opacity-0" : "opacity-100"
        }`}
        aria-label="Open AI career mentor"
      >
        <span className="mentor-launcher-bot grid h-9 w-9 place-items-center rounded-lg bg-white/15 text-white">
          <Bot size={22} strokeWidth={2.4} />
        </span>

        <span className="whitespace-nowrap text-base font-extrabold tracking-tight">
          AI Mentor
        </span>

        <span className="mentor-launcher-dot absolute -right-1 -top-1 h-3.5 w-3.5 rounded-full border-2 border-white bg-[#6FCF97]" />
      </button>

      {isOpen && (
        <div className="fixed inset-x-3 top-[76px] bottom-4 z-[999] flex justify-center sm:inset-x-4 lg:top-[76px]">
          <section className="grid h-full w-full max-w-[1500px] grid-cols-1 overflow-hidden rounded-2xl border border-[#B9D8CC] bg-[#FDFBF7] shadow-2xl shadow-emerald-950/20 lg:grid-cols-[300px_minmax(0,1fr)] xl:grid-cols-[320px_minmax(0,1fr)]">
            <aside className="flex min-h-0 flex-col border-b border-[#B9D8CC] bg-white/80 lg:border-b-0 lg:border-r">
              <div className="px-5 py-4">
                <div className="flex items-center gap-2 text-lg font-black tracking-tight text-[#18332D]">
                  <Sparkles size={18} className="text-[#1F6F5F]" />
                  AI Mentor
                </div>

                <div className="group mt-1 flex items-center justify-between">
                  <p className="text-sm font-semibold text-slate-500">Recent</p>

                  <button
                    type="button"
                    onClick={() => setIsRecentCollapsed((current) => !current)}
                    className={`grid h-7 w-7 place-items-center rounded-md border border-transparent bg-white/70 text-slate-400 transition hover:border-[#B9D8CC] hover:bg-[#F7F1E8] hover:text-[#1F6F5F] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]/20 ${
                      isRecentCollapsed ? "opacity-100" : "opacity-0 group-hover:opacity-100 focus-visible:opacity-100"
                    }`}
                    aria-label={isRecentCollapsed ? "Expand recent sessions" : "Collapse recent sessions"}
                    title={isRecentCollapsed ? "Expand recent" : "Collapse recent"}
                  >
                    {isRecentCollapsed ? <ChevronRight size={15} /> : <ChevronDown size={15} />}
                  </button>
                </div>
              </div>

              <div className="mentor-scrollbar-hidden min-h-0 flex-1 overflow-y-auto px-3 pb-3">
                {!isRecentCollapsed && (
                  <div className="space-y-1.5">
                    {visibleSessions.map((session) => {
                    const isActive = session.id === activeSessionId;

                    return (
                      <button
                        key={session.id}
                        type="button"
                        onClick={() => setActiveSessionId(session.id)}
                        className={`group relative flex w-full items-center gap-2 rounded-lg border px-3 py-2 text-left transition ${
                          isActive
                            ? "mentor-active-session border-[#2FA084] bg-[#EAF8F1]"
                            : "border-transparent bg-transparent hover:border-[#B9D8CC] hover:bg-[#FDFBF7]"
                        }`}
                      >
                        <span className="min-w-0 flex-1">
                          <span
                            className="block overflow-hidden !text-[13px] !font-black !leading-snug text-[#18332D]"
                            style={{
                              display: "-webkit-box",
                              WebkitLineClamp: 2,
                              WebkitBoxOrient: "vertical",
                            }}
                          >
                            {session.title}
                          </span>
                          <span className="mt-0.5 flex items-center gap-1 !text-[11px] !font-semibold text-slate-500">
                            <Clock3 size={11} />
                            {session.time}
                          </span>
                        </span>

                        <span className="flex shrink-0 items-center gap-1">
                          <button
                            type="button"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleTogglePin(session.id);
                            }}
                            className={`grid h-7 w-7 place-items-center rounded-md border transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]/20 ${
                              session.pinned
                                ? "border-[#B9D8CC] bg-[#EAF8F1] text-[#1F6F5F] opacity-100"
                                : "border-transparent bg-white/60 text-slate-400 opacity-0 hover:border-[#B9D8CC] hover:bg-[#F7F1E8] hover:text-[#1F6F5F] group-hover:opacity-100 focus-visible:opacity-100"
                            }`}
                            aria-label={`${session.pinned ? "Unpin" : "Pin"} ${session.title} chat`}
                            title={session.pinned ? "Unpin chat" : "Pin chat"}
                          >
                            <Pin size={12} />
                          </button>

                          <button
                            type="button"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleDeleteSession(session.id);
                            }}
                            className="grid h-7 w-7 place-items-center rounded-md border border-transparent bg-white/60 text-slate-400 opacity-0 transition hover:border-[#EF4444] hover:bg-[#FEF2F2] hover:text-[#DC2626] focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#EF4444]/20 group-hover:opacity-100"
                            aria-label={`Delete ${session.title} chat`}
                            title="Delete chat"
                          >
                            <Trash2 size={12} />
                          </button>
                        </span>
                      </button>
                    );
                    })}
                  </div>
                )}
              </div>

              <div className="border-t border-[#B9D8CC] p-3">
                <button
                  type="button"
                  onClick={handleCreateSession}
                  className="flex w-full items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] px-4 py-2.5 !text-[12px] font-extrabold text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#EAF8F1]"
                >
                  <Plus size={14} />
                  New conversation
                </button>
              </div>
            </aside>

            <main className="flex min-h-0 flex-col overflow-visible bg-[#FDFBF7]">
              <header className="relative z-20 border-b border-[#B9D8CC] bg-white/95 px-5 py-4 backdrop-blur-xl">
                <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
                  <div className="min-w-0 flex-1">
                    <h2 className="max-w-3xl text-lg font-black leading-snug tracking-tight text-[#18332D]">
                      {activeSession?.title || "Career mentor"}
                    </h2>
                    <p className="mt-1 text-sm font-semibold text-slate-500">
                      Ask about roles, projects, GitHub, or career growth.
                    </p>
                  </div>

                  <div ref={statusAreaRef} className="flex flex-wrap items-center gap-2 overflow-visible">
                    <MentorStatusPill
                      icon={FaGithub}
                      label={connectionState.githubConnected ? "GitHub connected" : "GitHub not connected"}
                      connected={connectionState.githubConnected}
                      reason="Connect GitHub so the mentor can understand your public projects, tech stack, README quality, and portfolio evidence."
                      actionLabel="Connect GitHub"
                      isOpen={activeStatus === "github"}
                      onToggle={() =>
                        setActiveStatus((current) => (current === "github" ? null : "github"))
                      }
                      onAction={() => handleNavigate("/settings/account")}
                    />

                    <MentorStatusPill
                      icon={UserRound}
                      label={connectionState.roleSelected ? "Role selected" : "Role not selected"}
                      connected={connectionState.roleSelected}
                      reason="Choose a target role so the mentor can recommend the right roadmap order, missing skills, and project ideas."
                      actionLabel="Choose role"
                      isOpen={activeStatus === "role"}
                      onToggle={() =>
                        setActiveStatus((current) => (current === "role" ? null : "role"))
                      }
                      onAction={() => handleNavigate("/settings/profile")}
                    />

                    

                    <button
                      type="button"
                      onClick={handleCloseWidget}
                      className="grid h-10 w-10 place-items-center rounded-lg border border-[#B9D8CC] bg-white text-slate-500 transition hover:border-[#2FA084] hover:bg-[#F7F1E8] hover:text-[#18332D]"
                      aria-label="Close AI mentor"
                    >
                      <X size={17} />
                    </button>
                  </div>
                </div>
              </header>

              <div className="mentor-scrollbar-hidden min-h-0 flex-1 overflow-y-auto px-4 py-4">
                <div className="flex w-full max-w-none flex-col gap-4">
                  {messages.map((message) => (
                    <MentorMessage key={message.id} message={message} />
                  ))}

                  {isReplying && (
                    <div className="flex justify-start">
                      <div className="inline-flex items-center gap-2 rounded-xl border border-[#B9D8CC] bg-white px-4 py-3 text-sm font-semibold text-slate-500 shadow-sm">
                        <span className="h-2 w-2 animate-pulse rounded-full bg-[#2FA084]" />
                        Mentor is thinking...
                      </div>
                    </div>
                  )}

                  <div ref={messagesEndRef} />
                </div>
              </div>

              <div className="border-t border-[#B9D8CC] bg-white px-5 py-4">
                <div className="mb-3">
                  <p className="mb-2 !text-[11px] font-black uppercase tracking-[0.16em] text-slate-500">
                    Quick actions
                  </p>

                  <div className="mentor-scrollbar-hidden flex gap-2 overflow-x-auto pb-1">
                    {starterPrompts.map((item) => {
                      const Icon = item.icon;

                      return (
                        <button
                          key={item.label}
                          type="button"
                          onClick={() => handlePrompt(item.prompt)}
                          className="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-[#B9D8CC] bg-[#FDFBF7] px-2.5 py-1.5 !text-[12px] font-bold text-slate-600 transition hover:border-[#2FA084] hover:bg-[#EAF8F1] hover:text-[#1F6F5F]"
                        >
                          <Icon size={14} className="text-[#1F6F5F]" />
                          {item.label}
                        </button>
                      );
                    })}
                  </div>
                </div>

                <form
                  onSubmit={handleSubmit}
                  className="flex items-end gap-2 rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] p-2 shadow-sm focus-within:border-[#2FA084] focus-within:ring-4 focus-within:ring-[#2FA084]/10"
                >
                  <textarea
                    rows={2}
                    value={input}
                    disabled={isReplying}
                    onChange={(event) => setInput(event.target.value)}
                    placeholder="Ask anything about roles, projects, GitHub, or career growth..."
                    className="max-h-28 min-h-10 flex-1 resize-none bg-transparent px-2 py-2 text-sm font-semibold text-[#18332D] outline-none placeholder:text-slate-400 disabled:cursor-not-allowed"
                    onKeyDown={(event) => {
                      if (event.key === "Enter" && !event.shiftKey) {
                        event.preventDefault();
                        event.currentTarget.form?.requestSubmit();
                      }
                    }}
                  />

                  <button
                    type="submit"
                    disabled={isReplying || !input.trim()}
                    className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#2FA084] text-white shadow-sm transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-[#6FCF97]/60"
                    aria-label="Send mentor question"
                  >
                    <SendHorizontal size={18} />
                  </button>
                </form>
              </div>
            </main>
          </section>
        </div>
      )}
    </>,
    document.body,
  );
}

function MentorStatusPill({
  icon: Icon,
  label,
  connected,
  reason,
  actionLabel,
  isOpen,
  onToggle,
  onAction,
}) {
  return (
    <div className="relative overflow-visible">
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={isOpen}
        aria-haspopup="dialog"
        className={`inline-flex h-9 items-center gap-1.5 rounded-lg border px-2.5 !text-[13px] font-bold shadow-sm transition focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-[#2FA084]/15 ${
          connected
            ? "border-[#6FCF97] bg-[#EAF8F1] text-[#1F6F5F]"
            : isOpen
              ? "border-[#2FA084] bg-[#F7F1E8] text-[#1F6F5F]"
              : "border-[#B9D8CC] bg-white text-slate-600 hover:border-[#2FA084] hover:bg-[#F7F1E8]"
        }`}
      >
        <Icon size={14} className={connected || isOpen ? "text-[#1F6F5F]" : "text-slate-500"} />
        <span className="!text-[13px] !leading-none">{label}</span>
        <span
          className={`h-1.5 w-1.5 rounded-full ${connected ? "bg-[#2FA084]" : "bg-amber-400"}`}
        />
      </button>

      {!connected && isOpen && (
        <div className="absolute right-0 top-[calc(100%+10px)] z-[70] w-72 rounded-xl border border-[#B9D8CC] bg-white p-4 text-left shadow-xl shadow-emerald-950/15">
          <p className="!text-[13px] font-black text-[#18332D]">{label}</p>
          <p className="mt-1 !text-[12px] font-semibold leading-5 text-slate-500">{reason}</p>

          <button
            type="button"
            onClick={onAction}
            className="mt-3 rounded-lg border border-[#B9D8CC] bg-[#EAF8F1] px-3 py-1.5 !text-[12px] font-extrabold text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#DDF3E8]"
          >
            {actionLabel}
          </button>
        </div>
      )}
    </div>
  );
}

function MentorMessage({ message }) {
  const isUser = message.role === "user";

  return (
    <div className={`mentor-message-enter flex ${isUser ? "justify-end" : "justify-start"}`}>
      <div className={`flex max-w-[88%] gap-2 ${isUser ? "flex-row-reverse" : "flex-row"}`}>
        <div
          className={`mt-1 grid h-9 w-9 shrink-0 place-items-center rounded-lg ${
            isUser ? "bg-[#18332D] text-white" : "bg-[#6FCF97]/20 text-[#1F6F5F]"
          }`}
        >
          {isUser ? <UserRound size={17} /> : <Bot size={17} />}
        </div>

        <div
          className={`rounded-xl border px-4 py-3 text-sm font-semibold leading-6 shadow-sm ${
            isUser
              ? "border-[#E7DDCF] bg-[#F7F1E8] text-[#465875]"
              : "border-[#B9D8CC] bg-[#EAF8F1] text-[#18332D]"
          }`}
        >
          <p>{message.content}</p>
        </div>
      </div>
    </div>
  );
}

function buildMockAnswer(question, learnerName) {
  const lowerQuestion = question.toLowerCase();

  if (lowerQuestion.includes("github") || lowerQuestion.includes("repo")) {
    return `For ${learnerName}, improve GitHub in this order: pin your strongest repositories, write clearer README files, add screenshots, and connect each project to one career skill. Connecting GitHub will make this advice more specific.`;
  }

  if (lowerQuestion.includes("project") || lowerQuestion.includes("portfolio")) {
    return "A strong next portfolio project should prove one target role skill. For a full-stack direction, build one project with authentication, CRUD, database design, API error handling, deployment, and a short case-study README.";
  }

  if (lowerQuestion.includes("roadmap") || lowerQuestion.includes("learn")) {
    return "Choose the roadmap closest to your target job, then prioritize nodes that create visible evidence: projects, GitHub commits, and portfolio summaries. Avoid learning too many disconnected tools before building one complete project.";
  }

  return "I would turn this into a simple plan: define your target role, compare required skills with your current projects, choose one missing skill, then build a small public project that proves it.";
}
