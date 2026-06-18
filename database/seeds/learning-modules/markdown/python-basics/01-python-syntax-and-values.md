# Python Syntax and Values

## Why this matters

Python emphasizes readable syntax, indentation, and expressive built-in types. This lesson introduces the basic structure of Python code and the values most beginner programs use.

## Learning goals

- Write simple Python statements with correct indentation.
- Use strings, numbers, booleans, `None`, lists, and dictionaries at a basic level.
- Assign variables without explicit type declarations.
- Use f-strings for readable output.

## Core concepts

### Indentation is syntax

In Python, indentation defines blocks. A missing or extra indent changes program structure or causes an error. This differs from languages that use braces.

### Dynamic typing

Python variables do not need declared types. A variable name points to a value, and the value has a type at runtime.

### Common values

Strings store text, integers and floats store numbers, booleans store true/false, and `None` represents no value. Lists and dictionaries store collections.

### Readable output

F-strings let you place variables directly inside strings. This is clearer than manual concatenation for status messages and reports.

## Worked example

```python
module_title = "Python Basics"
completed_lessons = 2
total_lessons = 4

progress_text = f"{module_title}: {completed_lessons}/{total_lessons} lessons complete"
is_complete = completed_lessons == total_lessons

print(progress_text)
print(is_complete)
```

Python does not require `int` or `string` declarations. The values still have types, but Python determines them at runtime.

## Applied practice

Create variables for a module title, lesson count, completed count, and pass status. Print a formatted progress sentence using an f-string.

## Common mistakes

- Mixing tabs and spaces.
- Assuming dynamic typing means values have no types.
- Forgetting quotes around strings.
- Using string concatenation when an f-string would be clearer.

## Self-check questions

- Why does indentation matter in Python?
- What is dynamic typing?
- What does `None` represent?
- Why are f-strings useful?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
