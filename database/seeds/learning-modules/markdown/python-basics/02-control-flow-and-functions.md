# Control Flow and Functions

## Why this matters

Python control flow uses readable keywords and indentation to branch and repeat work. Functions group reusable logic and make programs easier to test.

## Learning goals

- Use `if`, `elif`, and `else`.
- Loop through ranges and lists.
- Write functions with parameters and return values.
- Use clear function names for business rules.

## Core concepts

### Conditionals

Python uses `if`, `elif`, and `else` for branching. Conditions can combine comparisons with `and`, `or`, and `not`.

### Loops

Use `for item in collection` to loop over values directly. Use `range()` when a numeric sequence is needed.

### Functions

A function starts with `def`, receives parameters, and can return a result. Functions should usually do one clear job.

### Truthiness

Python treats empty strings, empty lists, zero, and `None` as falsey. This is useful but should be used carefully when zero is a valid value.

## Worked example

```python
def get_status(completed, total):
if completed == 0:
    return "not started"
elif completed >= total:
    return "completed"
else:
    return "in progress"

lesson_counts = [0, 2, 4]

for count in lesson_counts:
print(get_status(count, 4))
```

The function returns a status string instead of printing directly. That makes it easier to reuse in a web API, CLI script, or test.

## Applied practice

Write a function that receives a quiz score and total questions. Return `passed` if the score is at least 80 percent, otherwise return `needs review`.

## Common mistakes

- Forgetting the colon after `if`, `for`, or `def`.
- Using inconsistent indentation.
- Printing inside every function instead of returning values.
- Using a mutable default argument like `items=[]` without understanding the consequences.

## Self-check questions

- What does `elif` mean?
- Why should functions return values?
- When should you loop over values directly?
- What values are falsey in Python?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
