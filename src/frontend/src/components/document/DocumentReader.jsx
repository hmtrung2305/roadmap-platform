import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

function isCodeBlock(className, children) {
  return Boolean(className) || String(children).includes("\n");
}

export default function DocumentReader({ markdownContent }) {
  if (!markdownContent) {
    return (
      <div className="rounded-3xl border border-dashed border-[#B9D8CC] bg-white p-10 text-center shadow-sm">
        <h2 className="text-lg font-extrabold text-[#18332D]">
          No document content
        </h2>
        <p className="mt-2 text-sm text-slate-500">
          This document does not have markdown content to display yet.
        </p>
      </div>
    );
  }

  return (
    <article className="overflow-hidden rounded-[2rem] border border-[#B9D8CC] bg-white shadow-[0_20px_60px_rgba(31,111,95,0.10)]">
      <div className="border-b border-[#DCEBE5] bg-gradient-to-r from-[#F7F1E8] via-white to-[#EEF7F1] px-7 py-5">
        <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
          Reading Mode
        </p>
        <p className="mt-1 text-sm font-medium text-slate-500">
          Clean markdown view with highlighted sections, tables, quotes, and code blocks.
        </p>
      </div>

      <div className="px-7 py-8 sm:px-10">
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          components={{
            h1: ({ children }) => (
              <h1 className="mb-7 border-b border-[#DCEBE5] pb-5 text-4xl font-extrabold tracking-tight text-[#18332D]">
                {children}
              </h1>
            ),
            h2: ({ children }) => (
              <h2 className="mb-4 mt-10 flex items-center gap-3 text-2xl font-extrabold tracking-tight text-[#18332D]">
                <span className="h-7 w-1.5 rounded-full bg-[#2FA084]" />
                {children}
              </h2>
            ),
            h3: ({ children }) => (
              <h3 className="mb-3 mt-7 text-xl font-extrabold text-[#1F6F5F]">
                {children}
              </h3>
            ),
            h4: ({ children }) => (
              <h4 className="mb-2 mt-6 text-base font-extrabold uppercase tracking-[0.08em] text-slate-700">
                {children}
              </h4>
            ),
            p: ({ children }) => (
              <p className="my-4 text-[15px] leading-8 text-slate-700">
                {children}
              </p>
            ),
            ul: ({ children }) => (
              <ul className="my-5 list-disc space-y-3 pl-6 text-slate-700 marker:text-[#6FCF97]">
                {children}
              </ul>
            ),
            ol: ({ children }) => (
              <ol className="my-5 list-decimal space-y-3 pl-6 text-slate-700 marker:font-extrabold marker:text-[#2FA084]">
                {children}
              </ol>
            ),
            li: ({ children }) => (
              <li className="pl-2 text-[15px] leading-8 text-slate-700">
                {children}
              </li>
            ),
            blockquote: ({ children }) => (
              <blockquote className="my-6 rounded-2xl border-l-4 border-[#2FA084] bg-[#EEF7F1] px-5 py-4 text-slate-700">
                {children}
              </blockquote>
            ),
            code: ({ className, children, ...props }) => {
              if (!isCodeBlock(className, children)) {
                return (
                  <code
                    className="rounded-md bg-[#EEF7F1] px-1.5 py-0.5 text-[0.9em] font-extrabold text-[#1F6F5F]"
                    {...props}
                  >
                    {children}
                  </code>
                );
              }

              return (
                <code
                  className="block overflow-x-auto whitespace-pre rounded-2xl bg-[#18332D] p-4 text-sm leading-7 text-[#EEF7F1]"
                  {...props}
                >
                  {children}
                </code>
              );
            },
            pre: ({ children }) => (
              <pre className="my-6 overflow-hidden rounded-2xl bg-[#18332D] p-0 shadow-lg shadow-emerald-900/10">
                {children}
              </pre>
            ),
            table: ({ children }) => (
              <div className="my-7 overflow-x-auto rounded-2xl border border-[#B9D8CC]">
                <table className="w-full border-collapse text-left text-sm">
                  {children}
                </table>
              </div>
            ),
            thead: ({ children }) => (
              <thead className="bg-[#EEF7F1] text-[#1F6F5F]">
                {children}
              </thead>
            ),
            th: ({ children }) => (
              <th className="border-b border-[#B9D8CC] px-4 py-3 font-extrabold">
                {children}
              </th>
            ),
            td: ({ children }) => (
              <td className="border-b border-[#DCEBE5] px-4 py-3 text-slate-700">
                {children}
              </td>
            ),
            hr: () => <hr className="my-9 border-[#DCEBE5]" />,
            a: ({ href, children }) => (
              <a
                href={href}
                target="_blank"
                rel="noreferrer"
                className="font-bold text-[#1F6F5F] underline decoration-[#6FCF97] underline-offset-4 hover:text-[#2FA084]"
              >
                {children}
              </a>
            ),
          }}
        >
          {markdownContent}
        </ReactMarkdown>
      </div>
    </article>
  );
}
