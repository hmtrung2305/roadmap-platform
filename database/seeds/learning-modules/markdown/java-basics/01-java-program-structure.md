# Java Program Structure

## Why this matters

Java programs are organized around classes, methods, packages, and strongly typed declarations. Even a small Java program has a clear structure that the compiler checks before the program runs.

## Learning goals

- Explain the role of classes and the `main` method.
- Understand compilation and execution at a high level.
- Use packages and imports conceptually.
- Read a simple Java program without guessing.

## Core concepts

### Class as top-level unit

Most Java code lives inside classes. The file name usually matches the public class name. This convention helps the compiler and developers locate code.

### main method

`public static void main(String[] args)` is a common entry point. It tells the JVM where to start running a standalone program.

### Compilation

Java source code is compiled into bytecode. The JVM runs bytecode. This extra compile step catches many syntax and type errors before execution.

### Packages and imports

Packages organize code into namespaces. Imports let one file refer to classes from another package without writing the full qualified name every time.

## Worked example

```java
package com.example.learning;

public class StudyTracker {
public static void main(String[] args) {
    String moduleTitle = "Java Basics";
    int completedLessons = 2;
    int totalLessons = 4;

    System.out.println(moduleTitle + ": " + completedLessons + "/" + totalLessons);
}
}
```

This class has one entry point. The variables are declared with explicit types, and the program prints a simple progress message.

## Applied practice

Create a class named `ModuleProgress` with a `main` method. Store a module title, completed count, and total count, then print a formatted progress message.

## Common mistakes

- Using a public class name that does not match the file name.
- Forgetting semicolons.
- Placing executable statements outside methods.
- Confusing compile-time errors with runtime exceptions.

## Self-check questions

- What does the `main` method do?
- Why does Java compile before running?
- What is a package?
- Why do Java variables need declared types?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
