# HTML Document Structure

## Why this matters

HTML is the structural layer of a web page. A browser reads HTML as a tree of elements, then combines it with CSS and JavaScript to render and run the page. This lesson focuses on the page skeleton and the rules that make HTML readable to browsers, assistive technologies, search engines, and other developers.

## Learning goals

- Explain the difference between elements, tags, attributes, and text content.
- Write a valid HTML document skeleton from memory.
- Describe why `doctype`, `html`, `head`, `meta`, `title`, and `body` exist.
- Identify common structure mistakes that make pages harder to maintain.

## Core concepts

### The document tree

HTML is nested. The outer document contains an `<html>` element. Inside it, the `<head>` stores metadata and the `<body>` stores visible content. Every nested element becomes a node in the browser DOM tree. A clean tree makes styling, scripting, and accessibility easier because tools can infer where content begins and ends.

### Tags, elements, and attributes

A tag is the markup syntax such as `<p>` or `</p>`. An element is the full unit created by the opening tag, content, and closing tag. Attributes provide extra information such as `href`, `src`, `alt`, `lang`, `class`, and `id`. Attributes should describe behavior or meaning, not replace actual content.

### Head versus body

The `<head>` is not for visible page content. It contains metadata such as character encoding, viewport behavior, page title, linked CSS files, and SEO descriptions. The `<body>` contains the content the learner or user interacts with.

### Whitespace and indentation

Browsers collapse most whitespace in normal text, but humans still depend on indentation. Use indentation to show parent-child relationships. A document can technically render while being badly formatted, but poor structure makes debugging and collaboration much harder.

## Worked example

```html
<!doctype html>
<html lang="en">
  <head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Study Tracker</title>
  </head>
  <body>
<header>
  <h1>Study Tracker</h1>
</header>

<main>
  <p>Track lessons, quizzes, and daily learning progress.</p>
</main>
  </body>
</html>
```

In this example, the document declares modern HTML with `<!doctype html>`, sets the language with `lang="en"`, tells the browser to use UTF-8 text encoding, and configures the viewport so the page behaves correctly on mobile screens. The visible content is inside `<body>`, while metadata is inside `<head>`.

## Applied practice

Create a new HTML file for a learning dashboard. Include a document title, a header, a main section, and one paragraph explaining what the dashboard does. Then inspect the page in browser dev tools and find the DOM tree that matches your source code.

## Common mistakes

- Putting visible text inside `<head>`.
- Forgetting the viewport meta tag and then wondering why mobile layout looks zoomed out.
- Using random nested `<div>` elements before understanding semantic structure.
- Treating `id` as a general styling hook when a reusable `class` would be more appropriate.

## Self-check questions

- Why does the browser need a doctype?
- What belongs in `<head>` versus `<body>`?
- What is the difference between a tag and an element?
- Why should the `lang` attribute be set on `<html>`?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
