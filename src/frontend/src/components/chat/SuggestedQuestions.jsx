const suggestedQuestions = [
  "Summarize this document for me",
  "Explain the most important part",
  "Give me an easier example",
];

export default function SuggestedQuestions({ onSelect, disabled }) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-extrabold uppercase tracking-wide text-slate-400">
        Suggested questions
      </p>

      <div className="space-y-2">
        {suggestedQuestions.map((question) => (
          <button
            key={question}
            type="button"
            disabled={disabled}
            onClick={() => onSelect(question)}
            className="w-full rounded-xl border border-[#B9D8CC] bg-white px-3 py-2 text-left text-xs font-bold text-slate-600 transition hover:border-[#6FCF97] hover:bg-[#6FCF97]/20 hover:text-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {question}
          </button>
        ))}
      </div>
    </div>
  );
}
