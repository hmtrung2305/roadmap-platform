import { useEffect, useRef, useState } from "react";
import { Bot, ChevronDown, Coins, Loader2, Send, Trash2, X } from "lucide-react";
import ReactMarkdown from "react-markdown";
import rehypeKatex from "rehype-katex";
import remarkGfm from "remark-gfm";
import remarkMath from "remark-math";
import "katex/dist/katex.min.css";

import { useAiCreditStore } from "../../stores/useAiCreditStore";
import { useLearningModuleStore } from "../../stores/useLearningModuleStore";
import { getCreditStatus, getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { ModuleButton } from "../learningModules/learningModuleUi";

function ChatMarkdown({ content = "" }) {
  return (
    <ReactMarkdown
      remarkPlugins={[remarkGfm, remarkMath]}
      rehypePlugins={[rehypeKatex]}
      components={{
        h1: ({ children }) => (
          <h1 className="mb-2 text-base font-black leading-6 text-[#18332D]">
            {children}
          </h1>
        ),
        h2: ({ children }) => (
          <h2 className="mb-2 mt-3 text-sm font-black leading-6 text-[#18332D]">
            {children}
          </h2>
        ),
        h3: ({ children }) => (
          <h3 className="mb-1.5 mt-3 text-sm font-extrabold leading-6 text-[#18332D]">
            {children}
          </h3>
        ),
        p: ({ children }) => <p className="my-2 first:mt-0 last:mb-0">{children}</p>,
        ul: ({ children }) => <ul className="my-2 list-disc space-y-1 pl-5 marker:text-[#2FA084]">{children}</ul>,
        ol: ({ children }) => <ol className="my-2 list-decimal space-y-1 pl-5 marker:font-extrabold marker:text-[#2FA084]">{children}</ol>,
        li: ({ children }) => <li className="pl-1 leading-6">{children}</li>,
        strong: ({ children }) => <strong className="font-extrabold text-[#18332D]">{children}</strong>,
        em: ({ children }) => <em className="text-slate-600">{children}</em>,
        blockquote: ({ children }) => (
          <blockquote className="my-2 rounded-md border-l-4 border-[#6FCF97] bg-[#F7F1E8] px-3 py-2 text-slate-700">
            {children}
          </blockquote>
        ),
        a: ({ href, children }) => (
          <a
            href={href}
            target="_blank"
            rel="noreferrer"
            className="font-extrabold text-[#1F6F5F] underline decoration-[#6FCF97] decoration-2 underline-offset-4"
          >
            {children}
          </a>
        ),
        code: ({ className, children, ...props }) => {
          const isBlock = Boolean(className) || String(children).includes("\n");

          if (!isBlock) {
            return (
              <code
                className="rounded bg-[#EAF8F1] px-1.5 py-0.5 text-[0.9em] font-extrabold text-[#1F6F5F]"
                {...props}
              >
                {children}
              </code>
            );
          }

          return (
            <code
              className="block overflow-x-auto whitespace-pre rounded-md bg-[#18332D] p-3 text-xs leading-6 text-[#EEF7F1]"
              {...props}
            >
              {children}
            </code>
          );
        },
        pre: ({ children }) => <pre className="my-2 overflow-hidden rounded-md bg-[#18332D] p-0">{children}</pre>,
        table: ({ children }) => (
          <div className="my-2 max-w-full overflow-x-auto rounded-md border border-[#B9D8CC]">
            <table className="min-w-max border-collapse text-left text-xs">{children}</table>
          </div>
        ),
        th: ({ children }) => (
          <th className="border-b border-r border-[#B9D8CC] bg-[#EAF8F1] px-2 py-1.5 font-extrabold text-[#1F6F5F] last:border-r-0">
            {children}
          </th>
        ),
        td: ({ children }) => (
          <td className="border-b border-r border-[#DCEBE5] px-2 py-1.5 align-top last:border-r-0">
            {children}
          </td>
        ),
      }}
    >
      {content}
    </ReactMarkdown>
  );
}

function getSourceKey(source, index) {
  return [
    source.skillModuleChunkId,
    source.skillModuleLessonId,
    source.lessonTitle,
    source.heading,
    index,
  ]
    .filter(Boolean)
    .join("-");
}

function ChatSources({ sources = [] }) {
  const [isOpen, setIsOpen] = useState(false);

  if (!sources.length) {
    return null;
  }

  const sourceCount = sources.length;
  const label = sourceCount === 1 ? "source" : "sources";

  return (
    <div className="mt-3 border-t border-[#B9D8CC]/70 pt-2">
      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="flex w-full items-center justify-between gap-3 rounded-lg px-2 py-1.5 text-left text-[11px] font-extrabold uppercase tracking-[0.08em] text-[#1F6F5F] transition hover:bg-[#EAF8F1]"
        aria-expanded={isOpen}
      >
        <span>{sourceCount} {label}</span>
        <ChevronDown
          size={14}
          className={`shrink-0 transition-transform ${isOpen ? "rotate-180" : ""}`}
        />
      </button>

      {isOpen && (
        <div className="mt-2 space-y-1.5">
          {sources.map((source, index) => (
            <div
              key={getSourceKey(source, index)}
              className="rounded-lg border border-[#B9D8CC]/80 bg-[#F7F1E8] px-2.5 py-2 text-xs font-semibold leading-5 text-slate-700"
            >
              <div className="font-extrabold text-[#18332D]">
                {source.lessonTitle || `Source ${index + 1}`}
              </div>

              {source.heading && (
                <div className="mt-0.5 text-[11px] font-bold text-[#1F6F5F]">
                  {source.heading}
                </div>
              )}

              {typeof source.similarityScore === "number" && (
                <div className="mt-1 text-[10px] font-extrabold uppercase tracking-[0.08em] text-slate-500">
                  Match {(source.similarityScore * 100).toFixed(0)}%
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function CreditPill({ status, isLoading }) {
  if (isLoading && !status) {
    return (
      <div className="mt-1 inline-flex items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-2.5 py-1 text-[11px] font-extrabold text-slate-500">
        <Loader2 size={11} className="animate-spin" />
        Loading credits
      </div>
    );
  }

  if (!status) {
    return (
      <div className="mt-1 inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-[11px] font-extrabold text-slate-500">
        <Coins size={11} />
        Credits unavailable
      </div>
    );
  }

  const remaining = status.remainingCreditsToday ?? 0;
  const limit = status.dailyCreditLimit ?? 0;
  const isEmpty = remaining <= 0;

  return (
    <div
      className={`mt-1 inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-extrabold ${
        isEmpty
          ? "border-rose-200 bg-rose-50 text-rose-700"
          : "border-[#B9D8CC] bg-[#F7F1E8] text-[#1F6F5F]"
      }`}
      title="Daily AI credits"
    >
      <Coins size={11} />
      {remaining}/{limit} AI credits
    </div>
  );
}

export default function StudyRoomChatPanel({
  module,
  activeLessonId,
  isOpen,
  width,
  topOffset,
  onStartResize,
  onClose,
}) {
  const sendModuleChatMessage = useLearningModuleStore((state) => state.sendModuleChatMessage);
  const creditStatus = useAiCreditStore((state) => state.creditStatus);
  const isLoadingCredits = useAiCreditStore((state) => state.isLoadingCreditStatus);
  const loadCreditStatus = useAiCreditStore((state) => state.loadCreditStatus);
  const patchCreditStatus = useAiCreditStore((state) => state.patchCreditStatus);
  const invalidateCreditStatus = useAiCreditStore((state) => state.invalidateCreditStatus);

  const [messages, setMessages] = useState([
    {
      role: "assistant",
      content: "Ask about the current module material. I will answer from the lesson content.",
    },
  ]);
  const [draft, setDraft] = useState("");
  const [isSending, setIsSending] = useState(false);
  const messagesScrollRef = useRef(null);
  const messagesEndRef = useRef(null);

  useEffect(() => {
    if (isOpen) {
      loadCreditStatus().catch(() => {});
    }
  }, [isOpen, loadCreditStatus]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const frameId = window.requestAnimationFrame(() => {
      const container = messagesScrollRef.current;

      if (container) {
        container.scrollTo({
          top: container.scrollHeight,
          behavior: "smooth",
        });
      }

      messagesEndRef.current?.scrollIntoView({ block: "end" });
    });

    return () => window.cancelAnimationFrame(frameId);
  }, [messages, isSending, isOpen]);

  const handleSend = async () => {
    const message = draft.trim();

    if (!message || isSending) return;

    if (creditStatus?.remainingCreditsToday <= 0) {
      setMessages((items) => [
        ...items,
        {
          role: "assistant",
          content: "You have no AI credits left today. Try again after the daily reset.",
        },
      ]);
      return;
    }

    const recentMessages = messages.slice(-6).map((item) => ({
      role: item.role,
      content: item.content,
    }));

    setMessages((items) => [...items, { role: "user", content: message }]);
    setDraft("");

    try {
      setIsSending(true);

      const response = await sendModuleChatMessage(module.skillModuleId, {
        skillModuleLessonId: activeLessonId,
        message,
        recentMessages,
      });

      setMessages((items) => [
        ...items,
        {
          role: "assistant",
          content: response?.answer || "No answer was generated.",
          sources: response?.sources || [],
        },
      ]);

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

      setMessages((items) => [
        ...items,
        {
          role: "assistant",
          content: getFriendlyApiErrorMessage(error, "Unable to send this question right now."),
        },
      ]);
    } finally {
      setIsSending(false);
    }
  };

  const handleClear = () => {
    const confirmed = window.confirm("Clear this module chat?");
    if (!confirmed) return;

    setMessages([
      {
        role: "assistant",
        content: "Ask about the current module material. I will answer from the lesson content.",
      },
    ]);
  };

  return (
    <>
      {isOpen && (
        <div
          className="fixed inset-0 z-30 bg-slate-950/20 xl:hidden"
          onClick={onClose}
        />
      )}

      <aside
        style={{
          width,
          top: topOffset,
          height: `calc(100vh - ${topOffset}px)`,
        }}
        className={`fixed right-0 z-40 flex max-w-[calc(100vw-1rem)] flex-col border-l border-[#B9D8CC] bg-[#F7F1E8]/95 shadow-xl shadow-emerald-900/10 backdrop-blur-xl transition-[transform,top,height] duration-300 ${
          isOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        <div
          onMouseDown={onStartResize}
          className="absolute left-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-[#6FCF97]"
          title="Resize chat panel"
        />

        <div className="border-b border-[#B9D8CC] bg-white p-4">
          <div className="flex items-start justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <Bot size={20} />
              </div>

              <div className="min-w-0">
                <h2 className="font-extrabold text-[#18332D]">Module chat</h2>
                <CreditPill status={creditStatus} isLoading={isLoadingCredits} />
              </div>
            </div>

            <div className="flex shrink-0 items-center gap-1">
              <button
                type="button"
                onClick={handleClear}
                className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-red-500"
                title="Clear chat"
              >
                <Trash2 size={16} />
              </button>

              <button
                type="button"
                onClick={onClose}
                className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                title="Close chat"
              >
                <X size={16} />
              </button>
            </div>
          </div>
        </div>

        <div ref={messagesScrollRef} className="min-h-0 flex-1 overflow-y-auto p-4">
          <div className="flex min-h-full flex-col justify-end gap-3">
            {messages.map((message, index) => {
              const isAssistant = message.role === "assistant";

              return (
                <div
                  key={`${message.role}-${index}`}
                  className={`flex w-full ${isAssistant ? "justify-start" : "justify-end"}`}
                >
                  <div
                    className={`max-w-[92%] rounded-2xl px-3.5 py-3 text-sm font-medium leading-6 shadow-sm ${
                      isAssistant
                        ? "rounded-bl-md border border-[#B9D8CC]/70 bg-white text-slate-700"
                        : "rounded-br-md bg-[#2FA084] text-white"
                    }`}
                  >
                    {isAssistant ? (
                      <div className="prose-chat max-w-none">
                        <ChatMarkdown content={message.content} />
                      </div>
                    ) : (
                      <p className="whitespace-pre-wrap break-words">{message.content}</p>
                    )}

                    <ChatSources sources={message.sources} />
                  </div>
                </div>
              );
            })}

            {isSending && (
              <div className="flex w-full justify-start">
                <div className="inline-flex items-center gap-2 rounded-2xl rounded-bl-md border border-[#B9D8CC]/70 bg-white px-3.5 py-2.5 text-sm font-bold text-slate-600 shadow-sm">
                  <Loader2 className="animate-spin" size={15} />
                  Thinking...
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>
        </div>

        <div className="border-t border-[#B9D8CC] bg-white p-3">
          <textarea
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                handleSend();
              }
            }}
            className="min-h-20 w-full resize-none rounded-xl border border-[#B9D8CC] bg-white px-3 py-2.5 text-sm font-semibold leading-6 outline-none transition placeholder:text-slate-500 focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
            placeholder={creditStatus?.remainingCreditsToday <= 0 ? "Daily AI credits used up" : "Ask about this lesson"}
          />

          <ModuleButton
            className="mt-2 w-full"
            onClick={handleSend}
            disabled={isSending || !draft.trim() || creditStatus?.remainingCreditsToday <= 0}
          >
            <Send size={14} />
            {isSending ? "Sending..." : "Send"}
          </ModuleButton>
        </div>
      </aside>
    </>
  );
}
