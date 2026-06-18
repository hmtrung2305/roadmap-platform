# Values, Variables, and Types

## Why this matters

JavaScript programs work by storing values, naming them with variables, and applying operations based on type. Understanding types early makes later topics like arrays, objects, DOM events, and API responses much easier.

## Learning goals

- Declare variables with `const` and `let`.
- Recognize primitive values and objects.
- Explain basic type coercion issues.
- Use template literals for readable strings.

## Core concepts

### const and let

`const` means the variable binding cannot be reassigned. `let` means it can be reassigned. Prefer `const` by default because it makes code easier to reason about.

### Primitive values

Common primitives include string, number, boolean, null, undefined, bigint, and symbol. Primitive values are not objects, even though JavaScript lets you call some methods on strings and numbers through wrapper behavior.

### Objects are references

Objects and arrays are reference values. A `const` object can still have its properties changed because the binding is constant, not the object contents.

### Coercion

JavaScript sometimes converts values automatically. This can be useful, but it can also hide bugs. Use strict equality `===` instead of loose equality `==` unless you intentionally want coercion.

## Worked example

```js
const moduleTitle = "JavaScript Basics";
let completedLessons = 2;
const totalLessons = 4;

const progressText = `${completedLessons}/${totalLessons} lessons complete`;
const isComplete = completedLessons === totalLessons;

const module = {
  title: moduleTitle,
  progressText,
  isComplete,
};

module.isComplete = true; // allowed because the object contents are mutable
```

Use `const` when the variable should not be rebound. Use `let` when the value must change over time, such as a counter or form input state.

## Applied practice

Create variables for a quiz title, score, total questions, and pass status. Build a message using a template literal. Then change the score and recalculate the pass status.

## Common mistakes

- Using `var` in modern code without a specific reason.
- Thinking `const` makes objects deeply immutable.
- Comparing values with `==` and getting surprising coercion.
- Using strings for numeric data and then accidentally concatenating instead of adding.

## Self-check questions

- When should you use `const` versus `let`?
- Why can a `const` object still be mutated?
- What is type coercion?
- Why is `===` usually preferred?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
