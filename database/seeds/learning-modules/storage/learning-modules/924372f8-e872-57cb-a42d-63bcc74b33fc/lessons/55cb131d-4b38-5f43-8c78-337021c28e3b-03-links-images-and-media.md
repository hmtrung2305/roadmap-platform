# Links, Images, and Media

## Why this matters

Links and media connect a page to other resources. They look simple, but poor link text or missing image alternatives can make a page confusing or inaccessible. This lesson focuses on making links predictable and media understandable.

## Learning goals

- Create internal, external, email, and anchor links.
- Write meaningful link text.
- Use image `alt` text correctly.
- Explain the difference between content images and decorative images.

## Core concepts

### Link destination

The `href` attribute tells the browser where the link goes. It can point to another route, another website, a file, an email address, or an element id on the same page.

### Link text

Good link text describes the destination or action. Avoid vague text like "click here" because it is meaningless when read out of context.

### Image alternatives

The `alt` attribute describes the purpose of an image. If an image communicates important information, explain that information. If it is decorative, use `alt=""` so assistive technology can skip it.

### Responsive media basics

Use images at reasonable sizes and avoid embedding huge files when a small thumbnail is enough. Performance is part of usability.

## Worked example

```html
<a href="/roadmaps/frontend-developer">View the Frontend Developer roadmap</a>
<a href="mailto:support@example.com">Email support</a>
<a href="#quiz-rules">Jump to quiz rules</a>

<img
  src="/images/html-dom-tree.png"
  alt="Diagram showing an HTML document as a nested tree of elements"
/>

<img src="/images/green-divider.svg" alt="" />
```

The first image needs alternative text because it teaches a concept. The second image is decorative, so an empty `alt` tells assistive technology to ignore it.

## Applied practice

Build a small resource list with three links: one internal route, one external documentation page, and one email link. Add one meaningful image and one decorative image. Write different `alt` text for each case.

## Common mistakes

- Using raw URLs as link text when a readable phrase would be clearer.
- Opening every external link in a new tab without reason.
- Writing `alt="image"` or `alt="picture"`, which adds no useful information.
- Using images for text that should be actual HTML text.

## Self-check questions

- What makes link text meaningful?
- When should an image have empty alt text?
- What is the purpose of an anchor link?
- Why can large images hurt user experience?

## Chatbox test prompts

Use these prompts to test whether the AI assistant can retrieve and explain this lesson content:

- Summarize the main idea of this lesson in beginner-friendly language.
- Give me a small example based only on this lesson.
- What mistakes should I watch out for when applying this topic?
- Ask me three quiz-style questions about this lesson and explain the answers.

## Lesson takeaway

The important skill is not memorizing every syntax detail. The important skill is being able to explain what each part of the code is responsible for, recognize when the structure is wrong, and use the right concept when building a small feature.
