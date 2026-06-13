import LearningModuleMarkdown from "./LearningModuleMarkdown";

export function titleFromMarkdown(text = "", fallback = "Untitled lesson") {
  const match = text.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : fallback;
}

export default function MarkdownRenderer({ markdown }) {
  return <LearningModuleMarkdown markdown={markdown} variant="preview" />;
}
