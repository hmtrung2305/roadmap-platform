# Variables, Types, and Control Flow

## Why this matters

Java is statically typed, so variable types are known at compile time. This affects how values are stored, compared, transformed, and passed through conditions and loops.

## Learning goals

- Declare primitive and reference variables.
- Use `if`, `else`, `switch`, and loops.
- Compare strings correctly.
- Write simple methods for repeated logic.

## Core concepts

### Primitive and reference types

Primitive types like `int`, `double`, `boolean`, and `char` store simple values. Reference types like `String`, arrays, and custom objects refer to objects.

### Type safety

The compiler prevents many invalid operations. For example, assigning a string to an `int` variable fails before the program runs.

### String comparison

Use `.equals()` for string content comparison. The `==` operator compares references for objects, not textual equality.

### Loops and conditions

Control flow lets a program branch and repeat. Use loops for repeated work and conditions for business rules.

## Worked example

```java
public static String getStatus(int completed, int total) {
if (completed == 0) {
    return "not started";
}

if (completed >= total) {
    return "completed";
}

return "in progress";
}

String status = getStatus(2, 4);
System.out.println(status);
```

The method receives typed inputs and returns a typed output. The condition order matters because `completed == 0` should be handled before general in-progress logic.

## Applied practice

Write a method that receives a quiz score and total questions. Return `passed` if the score is at least 80 percent, otherwise return `needs review`.

## Common mistakes

- Comparing strings with `==`.
- Using integer division when a decimal percentage is needed.
- Forgetting braces around multi-line conditions.
- Writing duplicated status logic instead of extracting a method.

## Self-check questions

- What is static typing?
- How are primitive and reference types different?
- Why should strings use `.equals()`?
- How can integer division cause bugs?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
