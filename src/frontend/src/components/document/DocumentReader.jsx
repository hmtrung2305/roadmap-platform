import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

export default function DocumentReader({ markdownContent }) {
  if (!markdownContent) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center">
        <h2 className="text-lg font-semibold text-slate-900">
          Không có nội dung tài liệu
        </h2>
        <p className="mt-2 text-sm text-slate-500">
          Tài liệu này chưa có nội dung markdown để hiển thị.
        </p>
      </div>
    );
  }

  return (
    <article className="rounded-2xl border border-slate-200 bg-white px-8 py-7 shadow-sm">
      <div className="prose prose-slate max-w-none prose-headings:scroll-mt-24 prose-h1:text-3xl prose-h1:font-bold prose-h2:mt-8 prose-h2:text-2xl prose-h2:font-bold prose-h3:text-xl prose-p:leading-7 prose-pre:rounded-xl prose-pre:bg-slate-950 prose-code:text-sm">
        <ReactMarkdown remarkPlugins={[remarkGfm]}>
          {markdownContent}
        </ReactMarkdown>
      </div>
    </article>
  );
}