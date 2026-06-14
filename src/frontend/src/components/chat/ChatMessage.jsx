import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

const assistantMarkdownComponents = {
  p: ({ children, ...props }) => (
    <p {...props} className="my-2 first:mt-0 last:mb-0 leading-6">
      {children}
    </p>
  ),

  ul: ({ children, ...props }) => (
    <ul
      {...props}
      className="my-2 ml-5 list-disc space-y-1 marker:text-[#2FA084]"
    >
      {children}
    </ul>
  ),

  ol: ({ children, ...props }) => (
    <ol
      {...props}
      className="my-2 ml-5 list-decimal space-y-1 marker:font-semibold marker:text-[#2FA084]"
    >
      {children}
    </ol>
  ),

  li: ({ children, ...props }) => (
    <li {...props} className="pl-1 leading-6">
      {children}
    </li>
  ),

  a: ({ children, ...props }) => (
    <a
      {...props}
      target="_blank"
      rel="noreferrer"
      className="font-semibold text-[#1F6F5F] underline underline-offset-2 hover:text-[#2FA084]"
    >
      {children}
    </a>
  ),

  code: ({ inline, children, ...props }) =>
    inline ? (
      <code
        {...props}
        className="rounded bg-[#EAF8F1] px-1.5 py-0.5 text-[0.9em] font-semibold text-[#1F6F5F]"
      >
        {children}
      </code>
    ) : (
      <code {...props} className="text-xs text-white">
        {children}
      </code>
    ),

  pre: ({ children, ...props }) => (
    <pre
      {...props}
      className="my-3 max-w-full overflow-x-auto rounded-lg bg-[#18332D] p-3.5 text-xs text-white"
    >
      {children}
    </pre>
  ),

  blockquote: ({ children, ...props }) => (
    <blockquote
      {...props}
      className="my-3 rounded-lg border border-[#B9D8CC] bg-[#EAF8F1] px-3 py-2 text-[#18332D]"
    >
      {children}
    </blockquote>
  ),

  table: ({ children, ...props }) => (
    <div className="my-3 max-w-full overflow-x-auto rounded-lg border border-[#B9D8CC] bg-white">
      <table
        {...props}
        className="min-w-max border-collapse text-left text-[11px] leading-5"
      >
        {children}
      </table>
    </div>
  ),

  thead: ({ children, ...props }) => (
    <thead {...props} className="bg-[#EAF8F1] text-[#18332D]">
      {children}
    </thead>
  ),

  tr: ({ children, ...props }) => (
    <tr {...props} className="border-b border-[#B9D8CC] last:border-b-0">
      {children}
    </tr>
  ),

  th: ({ children, ...props }) => (
    <th
      {...props}
      className="whitespace-nowrap border-r border-[#B9D8CC] px-3 py-2 font-extrabold last:border-r-0"
    >
      {children}
    </th>
  ),

  td: ({ children, ...props }) => (
    <td
      {...props}
      className="whitespace-nowrap border-r border-[#B9D8CC] px-3 py-2 align-top last:border-r-0"
    >
      {children}
    </td>
  ),
};

export default function ChatMessage({ message }) {
  const isUser = message.role === "user";

  return (
    <div className={`flex min-w-0 ${isUser ? "justify-end" : "justify-start"}`}>
      <div
        className={`min-w-0 rounded-lg px-4 py-3 text-sm leading-6 shadow-sm ${
          isUser
            ? "max-w-[85%] bg-[#2FA084] text-white"
            : "w-full border border-[#B9D8CC] bg-white text-slate-700"
        }`}
      >
        {isUser ? (
          <p className="whitespace-pre-wrap">{message.content}</p>
        ) : (
          <div className="min-w-0 max-w-full overflow-hidden">
            <ReactMarkdown
              remarkPlugins={[remarkGfm]}
              components={assistantMarkdownComponents}
            >
              {message.content}
            </ReactMarkdown>
          </div>
        )}
      </div>
    </div>
  );
}
