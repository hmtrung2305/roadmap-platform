import {
  Bot,
  ChevronDown,
  ChevronRight,
  Clock3,
  Coins,
  Pin,
  Plus,
  Loader2,
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
import { aiMentorApi } from "../../../api/aiMentorApi";
import { useAiCreditStore } from "../../../stores/useAiCreditStore";
import { useAuthProviderStore } from "../../../stores/useAuthProviderStore";
import { useAuthStore } from "../../../stores/useAuthStore";
import { useProfileStore } from "../../../stores/useProfileStore";
import {
  getCreditStatus,
  getFriendlyApiErrorMessage,
} from "../../../utils/apiErrorUtils";

const PAGE_CONTEXT = "roadmap_selection";
const LOCAL_SESSION_PREFIX = "local-session-";

const introMessage = {
  id: "mentor-intro",
  role: "assistant",
  content:
    "Hi! I can help you choose a career role, plan your roadmap order, and turn learning progress into portfolio evidence.",
};

function createLocalSession() {
  return {
    id: `${LOCAL_SESSION_PREFIX}${Date.now()}`,
    title: "New conversation",
    time: "Now",
    pinned: false,
    isLocal: true,
  };
}

function isLocalSessionId(sessionId) {
  return String(sessionId || "").startsWith(LOCAL_SESSION_PREFIX);
}

function formatRelativeTime(value) {
  if (!value) return "Now";

  const timestamp = new Date(value).getTime();

  if (!Number.isFinite(timestamp)) return "Now";

  const diffMs = Date.now() - timestamp;
  const minuteMs = 60 * 1000;
  const hourMs = 60 * minuteMs;
  const dayMs = 24 * hourMs;

  if (diffMs < minuteMs) return "Now";
  if (diffMs < hourMs) return `${Math.floor(diffMs / minuteMs)}m ago`;
  if (diffMs < dayMs) return `${Math.floor(diffMs / hourMs)}h ago`;
  if (diffMs < 2 * dayMs) return "Yesterday";
  if (diffMs < 7 * dayMs) return `${Math.floor(diffMs / dayMs)}d ago`;

  return new Date(timestamp).toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
  });
}

function getConversationId(conversation) {
  return conversation?.aiMentorConversationId || conversation?.conversationId || conversation?.id || "";
}

function normalizeConversation(conversation) {
  const id = getConversationId(conversation);

  return {
    id,
    title: conversation?.title || "New conversation",
    time: formatRelativeTime(conversation?.updatedAt || conversation?.createdAt),
    pinned: false,
    isLocal: false,
    pageContext: conversation?.pageContext || PAGE_CONTEXT,
    createdAt: conversation?.createdAt || null,
    updatedAt: conversation?.updatedAt || null,
  };
}

function normalizeMessage(message) {
  const id =
    message?.aiMentorMessageId ||
    message?.messageId ||
    message?.id ||
    `${message?.role || "message"}-${Date.now()}-${Math.random().toString(36).slice(2)}`;

  return {
    id,
    role: message?.role === "user" ? "user" : "assistant",
    content: message?.content || "",
    sources: Array.isArray(message?.sources) ? message.sources : [],
    aiModel: message?.aiModel || null,
    createdAt: message?.createdAt || null,
  };
}

function getProviderName(provider) {
  return String(provider?.provider || provider?.providerName || provider?.name || "").trim().toLowerCase();
}

function hasRoleProfileSignal(profile) {
  return Boolean(
    profile?.careerGoal ||
      profile?.currentRole ||
      profile?.targetRole ||
      profile?.headline,
  );
}

function renderInlineMentorMarkdown(content) {
  const parts = String(content || "").split(/(`[^`\n]+`|\*\*[\s\S]*?\*\*)/g);

  return parts.map((part, index) => {
    if (!part) return null;

    if (part.startsWith("**") && part.endsWith("**") && part.length > 4) {
      return (
        <strong key={`bold-${index}`} className="font-black text-inherit">
          {part.slice(2, -2)}
        </strong>
      );
    }

    if (part.startsWith("`") && part.endsWith("`") && part.length > 2) {
      return (
        <code
          key={`code-${index}`}
          className="rounded bg-white/70 px-1 py-0.5 font-mono !text-[0.85em] text-[#1F6F5F]"
        >
          {part.slice(1, -1)}
        </code>
      );
    }

    return part;
  });
}

export default function CareerMentorWidget() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const providers = useAuthProviderStore((state) => state.providers);
  const loadProviders = useAuthProviderStore((state) => state.loadProviders);
  const profile = useProfileStore((state) => state.profile);
  const loadProfile = useProfileStore((state) => state.loadProfile);
  const creditStatus = useAiCreditStore((state) => state.creditStatus);
  const isLoadingCreditStatus = useAiCreditStore((state) => state.isLoadingCreditStatus);
  const loadCreditStatus = useAiCreditStore((state) => state.loadCreditStatus);
  const patchCreditStatus = useAiCreditStore((state) => state.patchCreditStatus);
  const invalidateCreditStatus = useAiCreditStore((state) => state.invalidateCreditStatus);

  const initialSessionRef = useRef(createLocalSession());
  const [isOpen, setIsOpen] = useState(false);
  const [activeSessionId, setActiveSessionId] = useState(initialSessionRef.current.id);
  const [sessions, setSessions] = useState([initialSessionRef.current]);
  const [messagesBySession, setMessagesBySession] = useState({
    [initialSessionRef.current.id]: [introMessage],
  });
  const [input, setInput] = useState("");
  const [isReplying, setIsReplying] = useState(false);
  const [isLoadingConversations, setIsLoadingConversations] = useState(false);
  const [loadingMessageSessionId, setLoadingMessageSessionId] = useState(null);
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

  const connectionState = useMemo(
    () => ({
      githubConnected: providers.some(
        (provider) => getProviderName(provider) === "github" && Boolean(provider.isLinked),
      ),
      roleSelected: hasRoleProfileSignal(profile),
    }),
    [profile, providers],
  );

  useEffect(() => {
    if (!isOpen || !user) return;

    loadProviders().catch(() => {});
    loadProfile().catch(() => {});
    loadCreditStatus().catch(() => {});
  }, [isOpen, loadCreditStatus, loadProfile, loadProviders, user]);

  useEffect(() => {
    if (!isOpen || !user) return;

    let isCancelled = false;

    async function loadConversations() {
      try {
        setIsLoadingConversations(true);
        const data = await aiMentorApi.getConversations();
        if (isCancelled) return;

        const nextSessions = data
          .map(normalizeConversation)
          .filter((session) => session.id);

        if (nextSessions.length === 0) {
          const localSession = createLocalSession();
          setSessions([localSession]);
          setMessagesBySession({ [localSession.id]: [introMessage] });
          setActiveSessionId(localSession.id);
          return;
        }

        setSessions(nextSessions);
        setActiveSessionId((current) =>
          nextSessions.some((session) => session.id === current)
            ? current
            : nextSessions[0].id,
        );
      } catch (error) {
        if (isCancelled) return;

        const localSession = createLocalSession();
        setSessions([localSession]);
        setMessagesBySession({
          [localSession.id]: [
            introMessage,
            {
              id: `${localSession.id}-load-error`,
              role: "assistant",
              content: getFriendlyApiErrorMessage(
                error,
                "Unable to load AI mentor conversations right now.",
              ),
            },
          ],
        });
        setActiveSessionId(localSession.id);
      } finally {
        if (!isCancelled) {
          setIsLoadingConversations(false);
        }
      }
    }

    loadConversations();

    return () => {
      isCancelled = true;
    };
  }, [isOpen, user]);

  useEffect(() => {
    if (!isOpen || !activeSessionId || isLocalSessionId(activeSessionId)) return;
    if (messagesBySession[activeSessionId]) return;

    let isCancelled = false;

    async function loadMessages() {
      try {
        setLoadingMessageSessionId(activeSessionId);
        const data = await aiMentorApi.getMessages(activeSessionId);
        if (isCancelled) return;

        const nextMessages = data.map(normalizeMessage);
        setMessagesBySession((current) => ({
          ...current,
          [activeSessionId]: nextMessages.length > 0 ? nextMessages : [introMessage],
        }));
      } catch (error) {
        if (isCancelled) return;

        setMessagesBySession((current) => ({
          ...current,
          [activeSessionId]: [
            {
              id: `${activeSessionId}-message-load-error`,
              role: "assistant",
              content: getFriendlyApiErrorMessage(
                error,
                "Unable to load this AI mentor conversation.",
              ),
            },
          ],
        }));
      } finally {
        if (!isCancelled) {
          setLoadingMessageSessionId(null);
        }
      }
    }

    loadMessages();

    return () => {
      isCancelled = true;
    };
  }, [activeSessionId, isOpen, messagesBySession]);

  useEffect(() => {
    if (!isOpen) return;

    requestAnimationFrame(() => {
      messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
    });
  }, [isOpen, activeSessionId, messages.length, isReplying, loadingMessageSessionId]);

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
    const nextSession = createLocalSession();

    setSessions((current) => [nextSession, ...current]);
    setMessagesBySession((current) => ({
      ...current,
      [nextSession.id]: [
        {
          ...introMessage,
          id: "mentor-intro",
          content: "What career question do you want to explore today?",
        },
      ],
    }));
    setActiveSessionId(nextSession.id);
  }

  function removeSessionLocally(sessionId) {
    setSessions((current) => {
      const remaining = current.filter((session) => session.id !== sessionId);
      const nextSessions = remaining.length > 0 ? remaining : [createLocalSession()];
      const nextActiveId =
        activeSessionId === sessionId ? nextSessions[0].id : activeSessionId;

      setMessagesBySession((currentMessages) => {
        const updated = { ...currentMessages };
        delete updated[sessionId];

        if (nextSessions.length === 1 && nextSessions[0].isLocal && !updated[nextSessions[0].id]) {
          updated[nextSessions[0].id] = [
            {
              ...introMessage,
              id: "mentor-intro",
              content: "What career question do you want to explore today?",
            },
          ];
        }

        return updated;
      });

      setActiveSessionId(nextActiveId);
      return nextSessions;
    });
  }

  async function handleDeleteSession(sessionId) {
    const session = sessions.find((item) => item.id === sessionId);
    if (!session) return;

    try {
      if (!session.isLocal && !isLocalSessionId(sessionId)) {
        await aiMentorApi.archiveConversation(sessionId);
      }

      removeSessionLocally(sessionId);
    } catch (error) {
      setMessagesBySession((current) => ({
        ...current,
        [sessionId]: [
          ...(current[sessionId] ?? []),
          {
            id: `${sessionId}-delete-error-${Date.now()}`,
            role: "assistant",
            content: getFriendlyApiErrorMessage(
              error,
              "Unable to delete this conversation right now.",
            ),
          },
        ],
      }));
    }
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


  function handleCloseWidget() {
    setActiveStatus(null);
    setIsOpen(false);
  }

  function handleNavigate(path) {
    setActiveStatus(null);
    setIsOpen(false);
    navigate(path);
  }

  async function handleSubmit(event) {
    event.preventDefault();

    const text = input.trim();
    if (!text || isReplying) return;

    if (creditStatus?.remainingCreditsToday <= 0) {
      setMessagesBySession((current) => ({
        ...current,
        [activeSessionId]: [
          ...(current[activeSessionId] ?? []),
          {
            id: `assistant-no-credit-${Date.now()}`,
            role: "assistant",
            content:
              "You have no AI credits left today. Try again after the daily reset.",
          },
        ],
      }));
      return;
    }

    const sessionIdAtSend = activeSessionId;
    const isLocalSession = isLocalSessionId(sessionIdAtSend);
    const optimisticUserMessage = {
      id: `user-${Date.now()}`,
      role: "user",
      content: text,
      sources: [],
    };

    setInput("");
    setMessagesBySession((current) => ({
      ...current,
      [sessionIdAtSend]: [
        ...(current[sessionIdAtSend] ?? []).filter((message) => message.id !== "mentor-intro"),
        optimisticUserMessage,
      ],
    }));

    setSessions((current) =>
      current.map((session) =>
        session.id === sessionIdAtSend
          ? {
              ...session,
              title: session.title === "New conversation" ? text : session.title,
              time: "Now",
            }
          : session,
      ),
    );

    setIsReplying(true);

    try {
      const response = await aiMentorApi.sendMessage({
        conversationId: isLocalSession ? null : sessionIdAtSend,
        pageContext: PAGE_CONTEXT,
        message: text,
      });

      const nextConversation = normalizeConversation(response?.conversation);
      const nextConversationId = nextConversation.id || sessionIdAtSend;
      const nextUserMessage = response?.userMessage
        ? normalizeMessage(response.userMessage)
        : optimisticUserMessage;
      const nextAssistantMessage = response?.assistantMessage
        ? normalizeMessage(response.assistantMessage)
        : {
            id: `assistant-${Date.now()}`,
            role: "assistant",
            content: response?.answer || "No answer was generated.",
            sources: response?.sources || [],
          };

      setSessions((current) => {
        const baseSessions = isLocalSession
          ? current.filter((session) => session.id !== sessionIdAtSend)
          : current;
        const existingIndex = baseSessions.findIndex(
          (session) => session.id === nextConversationId,
        );

        if (existingIndex >= 0) {
          const updatedSession = {
            ...baseSessions[existingIndex],
            ...nextConversation,
            id: nextConversationId,
            pinned: baseSessions[existingIndex].pinned,
            time: "Now",
          };

          return [
            updatedSession,
            ...baseSessions.filter((session) => session.id !== nextConversationId),
          ];
        }

        return [
          {
            ...nextConversation,
            id: nextConversationId,
            title: nextConversation.title || text,
            time: "Now",
          },
          ...baseSessions,
        ];
      });

      setMessagesBySession((current) => {
        const previousMessages = (current[sessionIdAtSend] ?? [])
          .filter((message) => message.id !== optimisticUserMessage.id)
          .filter((message) => message.id !== "mentor-intro");
        const updated = { ...current };

        if (sessionIdAtSend !== nextConversationId) {
          delete updated[sessionIdAtSend];
        }

        updated[nextConversationId] = [
          ...previousMessages,
          nextUserMessage,
          nextAssistantMessage,
        ];

        return updated;
      });

      setActiveSessionId(nextConversationId);

      const responseCreditStatus = response?.creditStatus || response?.aiCreditStatus;
      if (responseCreditStatus) {
        patchCreditStatus(responseCreditStatus);
      } else {
        invalidateCreditStatus();
        loadCreditStatus({ force: true }).catch(() => {});
      }
    } catch (error) {
      const nextCreditStatus = getCreditStatus(error);

      if (nextCreditStatus) {
        patchCreditStatus(nextCreditStatus);
      }

      setMessagesBySession((current) => ({
        ...current,
        [sessionIdAtSend]: [
          ...(current[sessionIdAtSend] ?? []),
          {
            id: `assistant-error-${Date.now()}`,
            role: "assistant",
            content: getFriendlyApiErrorMessage(
              error,
              "Unable to send this question right now.",
            ),
          },
        ],
      }));
    } finally {
      setIsReplying(false);
    }
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
                  <p className="text-sm font-semibold text-slate-500">
                    {isLoadingConversations ? "Loading recent" : "Recent"}
                  </p>

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
                        <div
                          key={session.id}
                          className={`group relative flex w-full items-center gap-2 rounded-lg border px-3 py-2 text-left transition ${
                            isActive
                              ? "mentor-active-session border-[#2FA084] bg-[#EAF8F1]"
                              : "border-transparent bg-transparent hover:border-[#B9D8CC] hover:bg-[#FDFBF7]"
                          }`}
                        >
                          <button
                            type="button"
                            onClick={() => setActiveSessionId(session.id)}
                            className="min-w-0 flex-1 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]/20"
                            aria-current={isActive ? "true" : undefined}
                          >
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
                          </button>

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
                        </div>
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
                      Ask about roles, projects, synced GitHub, or career growth.
                    </p>
                  </div>

                  <div ref={statusAreaRef} className="flex flex-wrap items-center gap-2 overflow-visible">
                    <MentorCreditPill
                      status={creditStatus}
                      isLoading={isLoadingCreditStatus}
                    />

                    <MentorStatusPill
                      icon={FaGithub}
                      label={connectionState.githubConnected ? "GitHub synced" : "Sync GitHub"}
                      connected={connectionState.githubConnected}
                      reason="Sync GitHub so the mentor can personalize advice with your public projects, tech stack, README quality, and portfolio evidence."
                      actionLabel="Sync GitHub"
                      isOpen={activeStatus === "github"}
                      onToggle={() =>
                        setActiveStatus((current) => (current === "github" ? null : "github"))
                      }
                      onAction={() => handleNavigate("/portfolio/edit")}
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
                  {loadingMessageSessionId === activeSessionId && (
                    <div className="flex justify-start">
                      <div className="inline-flex items-center gap-2 rounded-xl border border-[#B9D8CC] bg-white px-4 py-3 text-sm font-semibold text-slate-500 shadow-sm">
                        <span className="h-2 w-2 animate-pulse rounded-full bg-[#2FA084]" />
                        Loading conversation...
                      </div>
                    </div>
                  )}

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
                <form
                  onSubmit={handleSubmit}
                  className="flex items-end gap-2 rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] p-2 shadow-sm focus-within:border-[#2FA084] focus-within:ring-4 focus-within:ring-[#2FA084]/10"
                >
                  <textarea
                    rows={2}
                    value={input}
                    disabled={isReplying}
                    onChange={(event) => setInput(event.target.value)}
                    placeholder={
                      creditStatus?.remainingCreditsToday <= 0
                        ? "Daily AI credits used up"
                        : "Ask anything about roles, projects, synced GitHub, or career growth..."
                    }
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
                    disabled={isReplying || !input.trim() || creditStatus?.remainingCreditsToday <= 0}
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


function MentorCreditPill({ status, isLoading }) {
  if (isLoading && !status) {
    return (
      <div className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] px-2.5 !text-[13px] font-bold text-slate-500 shadow-sm">
        <Loader2 size={14} className="animate-spin text-[#1F6F5F]" />
        <span>Loading credits</span>
      </div>
    );
  }

  if (!status) {
    return (
      <div
        className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-slate-200 bg-slate-50 px-2.5 !text-[13px] font-bold text-slate-500 shadow-sm"
        title="AI credit status is unavailable right now."
      >
        <Coins size={14} />
        <span>AI credits --/--</span>
      </div>
    );
  }

  const remaining = Number(status.remainingCreditsToday ?? 0);
  const limit = Number(status.dailyCreditLimit ?? 0);
  const isEmpty = remaining <= 0;

  return (
    <div
      className={`inline-flex h-9 items-center gap-1.5 rounded-lg border px-2.5 !text-[13px] font-bold shadow-sm ${
        isEmpty
          ? "border-rose-200 bg-rose-50 text-rose-700"
          : "border-[#B9D8CC] bg-[#F7F1E8] text-[#1F6F5F]"
      }`}
      title="Daily AI credits. Each mentor reply uses 1 credit."
    >
      <Coins size={14} />
      <span>{remaining}/{limit} credits</span>
    </div>
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
          <p className="whitespace-pre-wrap break-words">
            {renderInlineMentorMarkdown(message.content)}
          </p>
          <MentorSources sources={message.sources} />
        </div>
      </div>
    </div>
  );
}

function MentorSources({ sources = [] }) {
  if (!Array.isArray(sources) || sources.length === 0) {
    return null;
  }

  return (
    <div className="mt-3 border-t border-[#B9D8CC]/70 pt-2">
      <p className="mb-1.5 !text-[10px] font-black uppercase tracking-[0.12em] text-[#1F6F5F]">
        Based on context
      </p>

      <div className="flex flex-wrap gap-1.5">
        {sources.slice(0, 4).map((source, index) => (
          <span
            key={`${source.type || "source"}-${source.title || index}-${index}`}
            className="rounded-md border border-[#B9D8CC] bg-white/70 px-2 py-1 !text-[11px] font-bold text-slate-600"
            title={source.detail || source.title || "AI mentor context"}
          >
            {source.title || source.type || `Source ${index + 1}`}
          </span>
        ))}
      </div>
    </div>
  );
}
