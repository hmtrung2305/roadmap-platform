# Selectors and the Cascade

## Why this matters

CSS selects HTML elements and applies declarations to them. When multiple rules target the same element, the browser uses the cascade to decide which value wins. Understanding selectors and cascade rules prevents random trial-and-error styling.

## Learning goals

- Use type, class, id, descendant, child, and pseudo-class selectors.
- Explain specificity and source order.
- Understand inheritance versus direct assignment.
- Debug why a CSS rule did or did not apply.

## Core concepts

### Selectors target elements

A selector describes which elements receive a rule. Type selectors target element names, class selectors target reusable classes, id selectors target one unique element, and combinators target relationships.

### Specificity

Specificity is the weight of a selector. Inline styles are strongest, then ids, then classes/attributes/pseudo-classes, then element selectors. Higher specificity usually wins over lower specificity.

### Source order

When two matching rules have the same specificity, the later rule wins. This makes file order and import order important.

### Inheritance

Some properties, such as `color` and `font-family`, inherit from parent elements. Others, such as `margin`, `padding`, and `border`, do not. Confusing inheritance with cascade is a common beginner mistake.

## Worked example

```css
body {
  font-family: system-ui, sans-serif;
  color: #1f2937;
}

.card {
  border: 1px solid #d1d5db;
  padding: 1rem;
}

.card a {
  color: #047857;
}

.card a:hover {
  text-decoration: underline;
}
```

The `.card a` selector targets links inside cards. The hover selector applies only while the pointer is over the link. The body text color may be inherited by many elements, but the link color is assigned directly by `.card a`.

## Applied practice

Create three cards with the same class. Style all cards using `.card`, then style only links inside cards. Add a hover style and inspect it in browser dev tools.

## Common mistakes

- Using ids for reusable styling.
- Adding `!important` instead of understanding specificity.
- Expecting margin or padding to inherit.
- Writing selectors that are too broad and accidentally affect the whole page.

## Self-check questions

- What is specificity?
- When does source order decide the winner?
- Why are classes preferred for reusable styling?
- Which common CSS properties inherit?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
