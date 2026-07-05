# Semantic HTML

## Why this matters

Semantic HTML means choosing elements based on meaning instead of appearance. A semantic element tells the browser, assistive technologies, and future developers what a section of content represents. This reduces the amount of custom markup and JavaScript needed to explain the page structure.

## Learning goals

- Use semantic page landmarks such as `header`, `nav`, `main`, `section`, `article`, `aside`, and `footer`.
- Choose heading levels that represent document outline.
- Explain why semantic HTML improves accessibility and maintainability.
- Avoid using only `div` and `span` for meaningful content.

## Core concepts

### Landmarks

Elements like `<header>`, `<nav>`, `<main>`, and `<footer>` create recognizable page regions. Screen reader users can jump between landmarks instead of listening to the whole page linearly.

### Headings as outline

Heading tags are not just font sizes. They describe hierarchy. A page should usually have one main `<h1>`, then nested `<h2>` and `<h3>` headings for sections and subsections.

### Section versus article

`<section>` groups related content under a theme. `<article>` represents self-contained content that could stand alone, such as a blog post, lesson card, or news item.

### Semantic does not mean styled

Browsers give default styles to headings and lists, but semantics are independent from appearance. Use CSS for appearance and HTML for meaning.

## Worked example

```html
<main>
  <section aria-labelledby="recommended-title">
<h2 id="recommended-title">Recommended Modules</h2>

<article>
  <h3>HTML Basics</h3>
  <p>Learn document structure, semantic markup, links, images, and forms.</p>
  <a href="/learning-modules/html-basics/overview">View module</a>
</article>
  </section>
</main>
```

This is better than a pile of generic `div` elements because each wrapper communicates purpose. The `section` groups recommended modules, the `article` represents one module card, and the headings form a readable outline.

## Applied practice

Take a page that uses only `div` elements and rewrite it using `header`, `nav`, `main`, `section`, `article`, and `footer`. Keep the visual output similar, but improve the meaning of the markup.

## Common mistakes

- Choosing headings based on visual size instead of hierarchy.
- Using `<section>` without a heading or clear topic.
- Using clickable `<div>` elements instead of buttons or links.
- Adding ARIA roles before checking whether a native semantic element already exists.

## Self-check questions

- Why is a real `<button>` usually better than a clickable `<div>`?
- When should you use `<article>` instead of `<section>`?
- How do headings help both users and machines?
- What does semantic HTML improve besides accessibility?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
