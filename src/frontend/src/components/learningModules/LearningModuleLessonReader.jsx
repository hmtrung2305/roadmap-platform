import DocumentReader from "../document/DocumentReader";
import LearningModuleMarkdown from "./LearningModuleMarkdown";

function buildFallbackMarkdown(title, message) {
  const safeTitle = title?.trim() || "Empty lesson";
  const safeMessage = message?.trim() || "No lesson content was found.";

  return `# ${safeTitle}\n\n${safeMessage}`;
}

export default function LearningModuleLessonReader({
  markdown,
  emptyTitle = "Empty lesson",
  emptyMessage = "No lesson content was found.",
  embedded = false,
}) {
  const content = markdown?.trim()
    ? markdown
    : buildFallbackMarkdown(emptyTitle, emptyMessage);

  if (!embedded) {
    return <DocumentReader markdownContent={content} />;
  }

  return (
    <article className="mx-auto w-full max-w-5xl px-1 py-1 sm:px-2">
      <LearningModuleMarkdown markdown={content} />
    </article>
  );
}
