# DOM, Events, and Errors

## Why this matters

The DOM is the browser representation of an HTML document. JavaScript can read and update the DOM, then react to user actions through events. Error handling helps the interface fail gracefully when something goes wrong.

## Learning goals

- Select DOM elements safely.
- Attach event listeners.
- Update text and classes in response to events.
- Use `try...catch` for operations that may fail.

## Core concepts

### DOM selection

`document.querySelector` returns the first matching element or `null`. Always consider the null case before using the result.

### Events

Events represent user or browser actions such as clicks, input changes, submissions, and key presses. `addEventListener` connects a callback to an event.

### Updating the page

You can update text with `textContent`, form values with `value`, and styling through class changes. Avoid injecting untrusted HTML with `innerHTML`.

### Errors

Network requests, JSON parsing, and missing elements can fail. Good code handles failure states and gives the user a clear message instead of silently breaking.

## Worked example

```html
<button id="completeButton">Mark complete</button>
<p id="statusText">Lesson in progress</p>
```

```js
const button = document.querySelector("#completeButton");
const statusText = document.querySelector("#statusText");

if (button && statusText) {
  button.addEventListener("click", () => {
statusText.textContent = "Lesson completed";
statusText.classList.add("is-complete");
  });
}
```

The null check prevents runtime errors if the expected elements are not present on the page.

## Applied practice

Build a small lesson checklist. When the user clicks a button, update the status text and toggle a CSS class. Add a second button that intentionally reads missing data, then handle the error gracefully.

## Common mistakes

- Running DOM code before the element exists.
- Ignoring `null` from `querySelector`.
- Using `innerHTML` for untrusted user content.
- Showing no feedback when an async operation fails.

## Self-check questions

- What is the DOM?
- Why can `querySelector` return null?
- What does an event listener do?
- When should you use `try...catch`?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
