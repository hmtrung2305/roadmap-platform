const suggestedQuestions = [
  "Tóm tắt tài liệu này giúp tôi",
  "Giải thích phần quan trọng nhất",
  "Cho ví dụ dễ hiểu hơn",
];

export default function SuggestedQuestions({ onSelect, disabled }) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
        Suggested questions
      </p>

      <div className="space-y-2">
        {suggestedQuestions.map((question) => (
          <button
            key={question}
            type="button"
            disabled={disabled}
            onClick={() => onSelect(question)}
            className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-left text-xs font-medium text-slate-600 transition hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {question}
          </button>
        ))}
      </div>
    </div>
  );
}