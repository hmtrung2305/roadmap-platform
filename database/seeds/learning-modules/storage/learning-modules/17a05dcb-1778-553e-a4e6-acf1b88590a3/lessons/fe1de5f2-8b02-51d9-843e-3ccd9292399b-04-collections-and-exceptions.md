# Collections and Exceptions

## Why this matters

Collections store groups of values, and exceptions represent unexpected or invalid runtime conditions. Java provides collection interfaces and structured error handling so programs can work with lists, maps, and failure states safely.

## Learning goals

- Use `List` and `Map` for common data structures.
- Iterate collections safely.
- Understand checked and unchecked exceptions at a basic level.
- Use `try`, `catch`, and validation intentionally.

## Core concepts

### List

A `List` stores ordered values and can grow dynamically. It is common for lessons, modules, quiz questions, and search results.

### Map

A `Map` stores key-value pairs. It is useful when values need to be found by an id, slug, or name.

### Exceptions

An exception interrupts normal flow when something goes wrong. Some exceptions are programming bugs, while others represent expected failure modes like missing files.

### Validate before failing

Not every invalid state should become an exception. Business validation can return a clear result before trying an operation that might fail.

## Worked example

```java
import java.util.ArrayList;
import java.util.List;

List<String> lessons = new ArrayList<>();
lessons.add("Java Program Structure");
lessons.add("Variables and Control Flow");

for (String lesson : lessons) {
System.out.println(lesson);
}
```

```java
try {
int percentage = (score * 100) / totalQuestions;
System.out.println(percentage);
} catch (ArithmeticException ex) {
System.out.println("Total questions cannot be zero.");
}
```

The collection example shows dynamic storage. The exception example handles division by zero, though validating `totalQuestions` before division would be even clearer.

## Applied practice

Create a list of lesson titles and print them. Then create a map from lesson slug to title. Add validation so a lookup for a missing slug returns a clear message.

## Common mistakes

- Using arrays when a growing list is needed.
- Catching broad `Exception` without a reason.
- Using exceptions for normal control flow.
- Ignoring the possibility of missing keys in a map.

## Self-check questions

- When should you use `List`?
- When is `Map` useful?
- What does an exception do?
- Why should validation often happen before a risky operation?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
