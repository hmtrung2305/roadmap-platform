# Responsive CSS

## Why this matters

Responsive CSS adapts layouts to different screen sizes and input conditions. The goal is not to make every device look identical. The goal is to keep content usable, readable, and efficient across contexts.

## Learning goals

- Use relative units and flexible layouts.
- Write mobile-first media queries.
- Understand viewport width and breakpoints.
- Avoid fixed layouts that break on small screens.

## Core concepts

### Fluid by default

HTML naturally flows. Many responsive problems come from forcing fixed widths too early. Prefer `max-width`, percentages, and flexible layout primitives.

### Mobile-first CSS

Start with the small-screen layout, then add media queries for wider screens. This usually produces simpler CSS because single-column layouts are the default.

### Breakpoints

A breakpoint should be chosen where the design needs more space, not just because a common device width exists. Content should drive breakpoints.

### Responsive images and text

Large images should scale down, and long text should keep readable line length. `max-width: 100%` prevents images from overflowing their container.

## Worked example

```css
.container {
  width: min(100% - 2rem, 960px);
  margin-inline: auto;
}

.module-grid {
  display: grid;
  gap: 1rem;
}

@media (min-width: 768px) {
  .module-grid {
grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
```

This starts with one column for small screens and changes to two columns when enough width is available. The container prevents text from stretching too wide on large screens.

## Applied practice

Build a module card grid that uses one column on phones and two columns on tablets/desktops. Test it by resizing the browser and checking for overflow.

## Common mistakes

- Starting with desktop CSS and patching mobile last.
- Using fixed pixel widths everywhere.
- Choosing breakpoints without looking at content.
- Forgetting to test long titles or translated text.

## Self-check questions

- What does mobile-first mean?
- Why is `max-width` often safer than fixed `width`?
- How should breakpoints be chosen?
- What causes horizontal overflow?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
