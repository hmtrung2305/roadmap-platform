import { Bot, Loader2, MessageCircle, Trash2, X } from "lucide-react";
import { useChatStore } from "../../stores/useChatStore";
import ChatInput from "./ChatInput";
import ChatMessage from "./ChatMessage";
import SuggestedQuestions from "./SuggestedQuestions";

const EMPTY_MESSAGES = [];

export default function AiChatPanel({
  resource,
  isOpen,
  width = 390,
  onStartResize,
  onClose,
}) {
  const resourceId = resource?.resourceId;

  const isSending = useChatStore((state) => state.isSending);
  const error = useChatStore((state) => state.error);
  const sendMessage = useChatStore((state) => state.sendMessage);
  const clearChatByResourceId = useChatStore(
    (state) => state.clearChatByResourceId,
  );
  const clearError = useChatStore((state) => state.clearError);

  const messages = useChatStore((state) => {
    if (!resourceId) return EMPTY_MESSAGES;
    return state.messagesByResourceId[resourceId] ?? EMPTY_MESSAGES;
  });

  const handleSend = async (text) => {
    if (!resourceId) return;

    await sendMessage({
      resourceId,
      prompt: text,
    });
  };

  const handleClear = () => {
    if (!resourceId) return;

    const confirmed = window.confirm("Bạn muốn xóa đoạn chat hiện tại?");
    if (!confirmed) return;

    clearChatByResourceId(resourceId);
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
        style={{ width }}
        className={`fixed right-0 top-16 z-40 flex h-[calc(100vh-4rem)] max-w-[calc(100vw-1rem)] flex-col border-l border-slate-200 bg-slate-50 shadow-xl transition-transform duration-300 ${
          isOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {/* Resize handle */}
        <div
          onMouseDown={onStartResize}
          className="absolute left-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-blue-300"
          title="Resize chat panel"
        />

        <div className="border-b border-slate-200 bg-white p-4">
          <div className="flex items-start justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-blue-50 text-blue-700">
                <Bot size={20} />
              </div>

              <div className="min-w-0">
                <h2 className="font-bold text-slate-900">AI Mentor</h2>
                <p className="line-clamp-1 text-xs text-slate-500">
                  Hỏi đáp theo tài liệu hiện tại
                </p>
              </div>
            </div>

            <div className="flex shrink-0 items-center gap-1">
              <button
                type="button"
                onClick={handleClear}
                disabled={!resourceId || messages.length === 0}
                className="rounded-xl p-2 text-slate-400 hover:bg-slate-100 hover:text-red-500 disabled:cursor-not-allowed disabled:opacity-40"
                title="Clear chat"
              >
                <Trash2 size={16} />
              </button>

              <button
                type="button"
                onClick={onClose}
                className="rounded-xl p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                title="Close chat"
              >
                <X size={16} />
              </button>
            </div>
          </div>

          {resource?.title && (
            <div className="mt-3 rounded-xl border border-blue-100 bg-blue-50 px-3 py-2">
              <p className="line-clamp-2 text-xs font-medium text-blue-700">
                {resource.title}
              </p>
            </div>
          )}
        </div>

        <div className="flex-1 overflow-y-auto p-4">
          {error && (
            <div className="mb-3 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">
              <div className="flex items-start justify-between gap-2">
                <span>{error}</span>

                <button
                  type="button"
                  onClick={clearError}
                  className="font-semibold hover:underline"
                >
                  Đóng
                </button>
              </div>
            </div>
          )}

          {messages.length === 0 ? (
            <div className="space-y-5">
              <div className="rounded-2xl border border-slate-200 bg-white p-4 text-sm text-slate-600 shadow-sm">
                <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-100 text-slate-600">
                  <MessageCircle size={18} />
                </div>

                <p className="font-semibold text-slate-900">
                  Bắt đầu hỏi về tài liệu này
                </p>

                <p className="mt-1 text-xs leading-5 text-slate-500">
                  AI sẽ dùng nội dung tài liệu hiện tại để trả lời. Hãy hỏi về
                  khái niệm, ví dụ, tóm tắt hoặc phần bạn chưa hiểu.
                </p>
              </div>

              <SuggestedQuestions onSelect={handleSend} disabled={isSending} />
            </div>
          ) : (
            <div className="space-y-3">
              {messages.map((message) => (
                <ChatMessage key={message.id} message={message} />
              ))}

              {isSending && (
                <div className="flex justify-start">
                  <div className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-500">
                    <Loader2 size={16} className="animate-spin" />
                    AI đang trả lời...
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <ChatInput onSend={handleSend} disabled={isSending || !resourceId} />
      </aside>
    </>
  );
}