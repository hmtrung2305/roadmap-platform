export function titleFromMarkdown(text = "", fallback = "Untitled lesson") {
  const match = text.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : fallback;
}