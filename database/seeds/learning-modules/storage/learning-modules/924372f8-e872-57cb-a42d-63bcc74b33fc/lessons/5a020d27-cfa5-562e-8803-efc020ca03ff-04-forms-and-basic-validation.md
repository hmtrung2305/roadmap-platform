# Forms and Basic Validation

## Why this matters

Forms collect user input. Good forms are explicit, accessible, and hard to misuse. HTML provides built-in form controls and validation features, but they only work well when labels, names, types, and constraints are used correctly.

## Learning goals

- Build a form with labels, inputs, names, and a submit button.
- Use input types like `email`, `password`, `number`, and `date`.
- Apply basic browser validation with `required`, `minlength`, `maxlength`, and `pattern`.
- Explain why frontend validation must not replace backend validation.

## Core concepts

### Labels connect meaning to controls

A label should be programmatically connected to its input using `for` and `id`, or by wrapping the input. Placeholder text is not a substitute because it disappears and may not be announced reliably.

### Names define submitted data

The `name` attribute is the key used when form data is submitted. Without a name, a control may appear on screen but not contribute useful submitted data.

### Input type matters

Input types help browsers show appropriate keyboards, validation, and semantics. For example, `type="email"` gives basic email format checking and mobile email keyboard behavior.

### Frontend and backend validation

Browser validation improves user experience, but it can be bypassed. The server must still validate every submitted value before trusting it.

## Worked example

```html
<form action="/register" method="post">
  <div>
<label for="email">Email</label>
<input id="email" name="email" type="email" required />
  </div>

  <div>
<label for="password">Password</label>
<input
  id="password"
  name="password"
  type="password"
  minlength="8"
  required
/>
  </div>

  <button type="submit">Create account</button>
</form>
```

The labels make each field understandable. The `name` values define submitted keys. The input types and constraints give immediate feedback before the request is sent.

## Applied practice

Create a simple contact form with name, email, topic, message, and submit button. Add labels and validation. Then try submitting invalid values to observe browser validation messages.

## Common mistakes

- Using placeholders instead of labels.
- Forgetting `name`, causing the field value not to be submitted correctly.
- Assuming browser validation is enough security.
- Using `type="text"` for every input even when a specific type exists.

## Self-check questions

- Why does every input need a label?
- What does the `name` attribute do?
- Why does the backend still need validation?
- When is `required` useful and when can it be annoying?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
