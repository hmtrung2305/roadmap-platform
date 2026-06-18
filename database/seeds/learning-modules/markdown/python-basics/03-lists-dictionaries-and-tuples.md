# Lists, Dictionaries, and Tuples

## Why this matters

Python provides expressive collection types for organizing data. Lists hold ordered, changeable sequences. Dictionaries map keys to values. Tuples hold ordered values that are usually treated as fixed.

## Learning goals

- Use lists for ordered collections.
- Use dictionaries for named fields and lookup by key.
- Use tuples for fixed grouped values.
- Apply list comprehensions carefully.

## Core concepts

### Lists

A list is ordered and mutable. It is good for lesson titles, quiz answers, user ids, or rows loaded from a file.

### Dictionaries

A dictionary stores key-value pairs. It is good for objects such as modules, users, and settings. Keys should be stable and meaningful.

### Tuples

A tuple is ordered and immutable. Use it for fixed pairs or grouped return values when the meaning is obvious.

### Comprehensions

A list comprehension creates a new list from an iterable. It is concise for simple transformations but can become unreadable if overused.

## Worked example

```python
modules = [
{"title": "HTML Basics", "status": "completed"},
{"title": "CSS Basics", "status": "in_progress"},
{"title": "Python Basics", "status": "not_started"},
]

active_modules = [module for module in modules if module["status"] != "completed"]
titles = [module["title"] for module in modules]

for module in active_modules:
print(module["title"])
```

This mirrors common API data. Each module is a dictionary, and the full list stores multiple module records.

## Applied practice

Create a list of quiz question dictionaries. Filter unanswered questions and print only their text. Then create a dictionary that maps question id to question data.

## Common mistakes

- Using list indexes when dictionary keys would be clearer.
- Changing a list while looping over it.
- Overusing list comprehensions for complicated logic.
- Assuming dictionary keys always exist.

## Self-check questions

- When should you use a list?
- When should you use a dictionary?
- What makes a tuple different from a list?
- What does a list comprehension return?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
