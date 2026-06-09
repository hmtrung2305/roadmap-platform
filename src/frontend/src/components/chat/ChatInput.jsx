import { SendHorizontal } from "lucide-react";
import { useState } from "react";

export default function ChatInput({ onSend, disabled }) {
  const [input, setInput] = useState("");

  const handleSubmit = async (event) => {
    event.preventDefault();

    const text = input.trim();
    if (!text) return;

    setInput("");
    await onSend(text);
  };

  return (
    <form onSubmit={handleSubmit} className="border-t border-[#B9D8CC] bg-white p-4">
      <div className="flex items-end gap-2 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] p-2 shadow-sm">
        <textarea
          rows={2}
          value={input}
          disabled={disabled}
          onChange={(event) => setInput(event.target.value)}
          placeholder="Ask about this document..."
          className="max-h-28 min-h-10 flex-1 resize-none bg-transparent px-2 py-2 text-sm text-slate-700 outline-none placeholder:text-slate-400 disabled:cursor-not-allowed"
          onKeyDown={(event) => {
            if (event.key === "Enter" && !event.shiftKey) {
              event.preventDefault();
              event.currentTarget.form?.requestSubmit();
            }
          }}
        />

        <button
          type="submit"
          disabled={disabled || !input.trim()}
          className="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#2FA084] text-white transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-[#6FCF97]"
        >
          <SendHorizontal size={18} />
        </button>
      </div>

      <p className="mt-2 text-center text-[11px] text-slate-400">
        Enter to send, Shift + Enter for a new line
      </p>
    </form>
  );
}
