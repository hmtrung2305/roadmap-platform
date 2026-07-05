# Classes, Objects, and Methods

## Why this matters

Object-oriented Java code models related data and behavior together. A class defines the blueprint, while an object is a runtime instance of that blueprint.

## Learning goals

- Create a simple class with fields and methods.
- Use constructors to initialize objects.
- Explain encapsulation and access modifiers.
- Understand instance methods versus static methods.

## Core concepts

### Class versus object

A class describes structure and behavior. An object is one actual instance. For example, `LearningModule` is a class, while `htmlBasics` is one object.

### Fields and methods

Fields store object state. Methods define behavior. Good classes keep related state and behavior together.

### Constructors

A constructor runs when a new object is created. It should initialize required fields so the object starts in a valid state.

### Encapsulation

Use private fields and public methods to control how state changes. This prevents other code from putting an object into an invalid state.

## Worked example

```java
public class LearningModule {
private final String title;
private int completedLessons;
private final int totalLessons;

public LearningModule(String title, int totalLessons) {
    this.title = title;
    this.totalLessons = totalLessons;
    this.completedLessons = 0;
}

public void completeLesson() {
    if (completedLessons < totalLessons) {
        completedLessons++;
    }
}

public String getProgressText() {
    return title + ": " + completedLessons + "/" + totalLessons;
}
}
```

The fields are private, so outside code cannot directly set `completedLessons` to an invalid number. The method controls the update rule.

## Applied practice

Create a `QuizAttempt` class with score, total questions, and a method that returns whether the attempt passed. Keep fields private.

## Common mistakes

- Making every field public.
- Creating objects that can start with invalid state.
- Putting all code in `main` instead of modeling concepts with classes.
- Using static methods when object state is needed.

## Self-check questions

- What is the difference between a class and an object?
- Why use constructors?
- What does encapsulation protect?
- When should a method be an instance method?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
