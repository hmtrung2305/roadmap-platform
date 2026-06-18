# Files, Modules, and Exceptions

## Why this matters

Python programs often need to read files, split code across modules, and handle errors. These features turn small scripts into maintainable programs that can interact with external data safely.

## Learning goals

- Read text files with `with open(...)`.
- Understand imports and modules.
- Handle expected errors with `try` and `except`.
- Avoid hiding errors that should be fixed.

## Core concepts

### Files and context managers

The `with` statement closes a file automatically, even if an error occurs. This is safer than manually opening and closing files.

### Modules

A module is a Python file that can be imported. Splitting code into modules keeps programs organized and makes functions reusable.

### Exceptions

Exceptions represent errors or unusual conditions. Catch specific exceptions when you can respond usefully. Let unexpected exceptions fail loudly during development.

### Data boundaries

File contents are external input. Validate and parse them carefully before trusting them, just like user input from a form or API.

## Worked example

```python
from pathlib import Path

path = Path("lessons.txt")

try:
with path.open("r", encoding="utf-8") as file:
    lessons = [line.strip() for line in file if line.strip()]
except FileNotFoundError:
lessons = []
print("No lesson file found yet.")

print(f"Loaded {len(lessons)} lessons")
```

The code handles a missing file specifically. It does not catch every possible exception, so unexpected problems remain visible.

## Applied practice

Create a text file with one lesson title per line. Write a Python script that reads the file, removes blank lines, and prints the number of lessons loaded.

## Common mistakes

- Opening files without closing them.
- Catching `Exception` everywhere and hiding real bugs.
- Importing from files with unclear side effects.
- Trusting file data without validation.

## Self-check questions

- Why is `with open(...)` preferred?
- What is a module?
- Why catch specific exceptions?
- Why should file data be validated?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
