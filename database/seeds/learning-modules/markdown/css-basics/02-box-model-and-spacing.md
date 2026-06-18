# Box Model and Spacing

## Why this matters

Every visible element is treated as a rectangular box. The CSS box model explains how content, padding, border, and margin combine to create the final size and spacing of that box.

## Learning goals

- Identify content, padding, border, and margin.
- Explain how `box-sizing` changes width calculations.
- Choose margin or padding intentionally.
- Diagnose spacing problems with browser dev tools.

## Core concepts

### Content box

The content area holds text, images, or child elements. When `box-sizing: content-box`, width and height apply only to this content area.

### Padding and border

Padding is internal space between content and border. Border surrounds padding and content. Both make the element visually larger unless `box-sizing: border-box` is used.

### Margin

Margin is external space around the element. It separates one element from neighboring elements. Vertical margins can collapse in normal document flow, which surprises many beginners.

### Border-box sizing

Many modern projects set `box-sizing: border-box` globally so the declared width includes content, padding, and border. This makes layout math easier.

## Worked example

```css
*,
*::before,
*::after {
  box-sizing: border-box;
}

.card {
  width: 320px;
  padding: 24px;
  border: 1px solid #d1d5db;
  margin-bottom: 16px;
}
```

With `border-box`, the card remains 320px wide total. Without it, the content width is 320px, and padding plus border make the rendered box wider.

## Applied practice

Build two cards with the same width. Give one `box-sizing: content-box` and the other `box-sizing: border-box`. Add padding and border, then compare their rendered sizes in dev tools.

## Common mistakes

- Using margin when padding is needed inside the element.
- Using padding when margin is needed between elements.
- Forgetting that borders affect size.
- Not checking the box model panel in dev tools.

## Self-check questions

- What is the difference between margin and padding?
- Why is `border-box` commonly used?
- What is margin collapse?
- How can dev tools help debug spacing?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
