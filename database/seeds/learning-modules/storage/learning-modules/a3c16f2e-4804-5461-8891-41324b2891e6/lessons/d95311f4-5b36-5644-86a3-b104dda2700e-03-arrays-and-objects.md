# Arrays and Objects

## Why this matters

Most JavaScript applications manage collections of structured data. Arrays represent ordered lists. Objects represent named properties. Together, they model API responses, UI state, form data, and domain objects.

## Learning goals

- Create and read arrays and objects.
- Use array methods like `map`, `filter`, `find`, and `reduce`.
- Model related data with object properties.
- Avoid mutating state accidentally when a copy is safer.

## Core concepts

### Arrays as ordered collections

An array stores values in order. Each value has an index starting at zero. Arrays are good for lists of lessons, quiz questions, selected tags, or search results.

### Objects as named records

An object groups related fields under property names. A module object might have `id`, `title`, `status`, and `lessons`.

### Transforming arrays

`map` creates a new array by transforming every item. `filter` keeps matching items. `find` returns the first match. These methods are common in React and frontend data handling.

### Copying instead of mutating

When working with state, create new arrays or objects instead of changing existing ones. This makes changes easier to detect and prevents hidden side effects.

## Worked example

```js
const modules = [
  { title: "HTML Basics", status: "completed" },
  { title: "CSS Basics", status: "in_progress" },
  { title: "JavaScript Basics", status: "not_started" },
];

const activeModules = modules.filter(
  (module) => module.status !== "completed"
);

const titles = modules.map((module) => module.title);

const cssModule = modules.find((module) => module.title === "CSS Basics");
```

Each method returns data without manually managing indexes. This style is readable when the callback names match the business idea.

## Applied practice

Create an array of quiz question objects. Use `filter` to get unanswered questions, `map` to get question text, and `find` to locate a question by id.

## Common mistakes

- Using array indexes as stable ids.
- Mutating objects directly when state should be immutable.
- Using `map` for side effects instead of transformation.
- Forgetting that `find` can return `undefined`.

## Self-check questions

- When should you use an array versus an object?
- What does `map` return?
- How is `filter` different from `find`?
- Why can mutation be risky in UI state?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
