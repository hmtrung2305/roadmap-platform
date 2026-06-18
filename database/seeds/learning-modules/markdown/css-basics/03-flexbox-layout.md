# Flexbox Layout

## Why this matters

Flexbox is a one-dimensional layout system for arranging items in a row or column. It is useful for nav bars, cards, button groups, form rows, and any layout where items need alignment and flexible spacing.

## Learning goals

- Create a flex container with `display: flex`.
- Use main axis and cross axis terminology.
- Apply `justify-content`, `align-items`, `gap`, `flex-wrap`, and `flex`.
- Know when Flexbox is a better choice than Grid.

## Core concepts

### Container and items

Flex behavior starts on the parent container. The direct children become flex items. Styling grandchildren requires separate rules or nested layout.

### Main and cross axis

The main axis follows `flex-direction`. In a row, it is horizontal. In a column, it is vertical. `justify-content` works on the main axis; `align-items` works on the cross axis.

### Gap instead of margins

`gap` adds consistent spacing between flex items without creating extra spacing before the first item or after the last item.

### Growth and shrinkage

The `flex` shorthand controls how items grow, shrink, and choose a base size. `flex: 1` is common when items should share available space.

## Worked example

```css
.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.card-list {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
}

.card {
  flex: 1 1 240px;
}
```

The toolbar separates groups along the main axis and centers them on the cross axis. The card list wraps cards onto new rows when there is not enough space.

## Applied practice

Create a navigation bar with a logo on the left and links on the right. Then create a card row that wraps on smaller screens using `flex-wrap` and `gap`.

## Common mistakes

- Putting `justify-content` on the child instead of the flex container.
- Forgetting that `align-items` changes direction when `flex-direction` changes.
- Using margins between every item instead of `gap`.
- Trying to create a full two-dimensional page layout with Flexbox when Grid would be clearer.

## Self-check questions

- What is the main axis?
- What does `gap` solve?
- What happens when `flex-wrap` is enabled?
- When might CSS Grid be better than Flexbox?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
