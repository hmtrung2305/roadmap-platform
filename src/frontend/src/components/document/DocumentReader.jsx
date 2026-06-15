import LearningModuleMarkdown from "../learningModules/LearningModuleMarkdown";

export default function DocumentReader({ markdownContent }) {
  if (!markdownContent) {
    return (
      <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-white p-10 text-center shadow-sm">
        <h2 className="text-lg font-extrabold text-[#18332D]">No document content</h2>
        <p className="mt-2 text-sm text-slate-500">
          This document does not have markdown content to display yet.
        </p>
      </div>
    );
  }

  return (
    <article className="overflow-hidden rounded-lg border border-[#B9D8CC] bg-white shadow-[0_20px_60px_rgba(31,111,95,0.10)]">
      <div className="mx-auto w-full max-w-5xl px-6 py-9 sm:px-10 lg:px-12">
        <LearningModuleMarkdown markdown={markdownContent} />
      </div>
    </article>
  );
}
