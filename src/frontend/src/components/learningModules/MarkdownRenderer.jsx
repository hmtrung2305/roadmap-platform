import LearningModuleMarkdown from "./LearningModuleMarkdown";

export default function MarkdownRenderer({ markdown }) {
  return <LearningModuleMarkdown markdown={markdown} variant="preview" />;
}
