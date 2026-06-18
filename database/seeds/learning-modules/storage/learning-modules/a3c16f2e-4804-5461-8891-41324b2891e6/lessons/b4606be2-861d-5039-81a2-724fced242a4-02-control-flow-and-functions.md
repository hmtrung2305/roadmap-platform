# Control Flow and Functions

## Why this matters

Control flow decides which code runs and how many times it runs. Functions package reusable logic behind a name and inputs. Together, they let programs respond to data instead of executing the same instructions every time.

## Learning goals

- Use `if`, `else`, comparison operators, and logical operators.
- Use loops for repeated work.
- Write functions with parameters and return values.
- Separate calculation logic from display logic.

## Core concepts

### Conditionals

An `if` statement runs a block only when a condition is truthy. Conditions often use comparison operators like `>`, `<`, `===`, and logical operators like `&&`, `||`, and `!`.

### Loops

Loops repeat work over a range or collection. Use `for...of` when iterating values from an array. Use classic `for` loops when you need index control.

### Functions

A function should do one clear job. Parameters are inputs. A return value is the output. Functions make code easier to test because you can call them with different inputs.

### Pure calculation

A function that only calculates and returns a value is easier to reuse than one that directly manipulates the page or logs every result. Keep side effects intentional.

## Worked example

```js
function getPassStatus(score, totalQuestions) {
  const percentage = (score / totalQuestions) * 100;

  if (percentage >= 80) {
return "passed";
  }

  return "needs review";
}

const scores = [10, 7, 9];

for (const score of scores) {
  console.log(getPassStatus(score, 10));
}
```

The function hides the pass rule behind a name. If the pass threshold changes, only the function needs to change.

## Applied practice

Write a function that accepts a lesson progress percentage and returns `not started`, `in progress`, or `completed`. Test it with at least five different values.

## Common mistakes

- Forgetting to return a value from a function.
- Writing one giant function that handles validation, calculation, and UI updates together.
- Using assignment `=` when comparison `===` was intended.
- Creating loops with conditions that never become false.

## Self-check questions

- What is the difference between parameter and argument?
- Why are return values useful?
- When should you use `for...of`?
- How can condition order affect results?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
