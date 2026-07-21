# Repository Basics and Status

## Overview

Understand repositories, tracked files, staging, and working tree status. This lesson is part of **Git Basics**. It is written for beginners, but it uses realistic development examples so the learning assistant can answer questions about actual code and workflow decisions.

## Learning goals

By the end of this lesson, you should be able to:

- Explain the purpose of the main concept in your own words.
- Recognize the concept in a small code or workflow example.
- Apply the concept to a simple practice task.
- Identify at least one common beginner mistake.

## Core concepts

- repository
- working tree
- staging area
- git status

These concepts are connected. Do not memorize them as isolated terms. In real projects, you use them together to make code easier to read, safer to change, and easier to debug.

## Worked example

```bash
git status
git diff
git add src/features/module-editor.js
git commit -m "feat: improve module editor validation"
git switch main
git merge feature/module-editor-validation
```

Read the example slowly. Identify the inputs, the named pieces, and the result. Then ask why each line exists. Good beginner practice is not copying the example once. Good practice is changing one small thing and predicting what should happen.

## Explanation

The important habit is to make intent visible. Clear names, small steps, and explicit boundaries reduce the chance of hidden errors. When a project grows, these basics become more important because many bugs come from confusing assumptions rather than complex algorithms.

When you are unsure, use a repeatable debugging path:

1. State what you expected to happen.
2. State what actually happened.
3. Inspect the smallest part of the example that could explain the difference.
4. Change one thing at a time.
5. Re-run and compare the result.

## Applied practice

Create a small example related to this lesson. Keep it intentionally simple. Your goal is to prove that you understand the concept, not to build a full application.

Suggested task:

- Recreate the worked example in your own file.
- Rename the variables or entities to a different domain.
- Add one extra case.
- Write one sentence explaining why your change still follows the same concept.

## Common mistakes

- Trying to learn syntax without understanding the reason behind it.
- Changing several things at once and making debugging harder.
- Ignoring error messages instead of reading the first useful line.
- Copying examples without testing small variations.

## Self-check questions

- What problem does this concept solve?
- What would break if this concept were used incorrectly?
- Which part of the worked example is most important?
- How would you explain this lesson to someone who just started programming?

## Chatbox test prompts

Use these prompts to test the module assistant:

- "Explain Repository Basics and Status with a smaller example."
- "What are the common mistakes in this lesson?"
- "Give me a practice exercise based on the worked example."
- "Ask me three quiz questions about this lesson."

## Takeaway

Repository Basics and Status is a foundation skill. Master it by reading examples, changing them carefully, and explaining your reasoning out loud.
