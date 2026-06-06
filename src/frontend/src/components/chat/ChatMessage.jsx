import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

export default function ChatMessage({ message }) {
  const isUser = message.role === "user";

  return (
    <div className={`flex ${isUser ? "justify-end" : "justify-start"}`}>
      <div
        className={`max-w-[85%] rounded-2xl px-4 py-3 text-sm leading-6 shadow-sm ${
          isUser
            ? "bg-[#2FA084] text-white"
            : "border border-[#B9D8CC] bg-white text-slate-700"
        }`}
      >
        {isUser ? (
          <p className="whitespace-pre-wrap">{message.content}</p>
        ) : (
          <div className="prose prose-slate max-w-none prose-p:my-1 prose-ul:my-1 prose-ol:my-1 prose-pre:my-2 prose-pre:rounded-xl prose-pre:bg-[#18332D] prose-code:text-xs prose-a:text-[#1F6F5F]">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {message.content}
            </ReactMarkdown>
          </div>
        )}
      </div>
    </div>
  );
}
