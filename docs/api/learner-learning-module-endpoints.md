# Learner Learning Module Endpoint Specification

Base routes:

```text
/api/learning-modules
/api/learning-modules/{moduleId}/assistant/chat
```

Source controllers:

```text
LearningModulesController
LearningModuleChatController
```

Related services / DTOs:

```text
ILearnerLearningModuleService
ILearningModuleChatService
LearnerLearningModuleSummaryDto
LearnerLearningModuleOverviewDto
LearningModuleEnrollmentDto
LearningModuleLessonContentDto
UpdateLessonProgressRequestDto
UpdateLessonProgressResultDto
StartQuizAttemptResultDto
SubmitQuizAttemptRequestDto
QuizAttemptSummaryDto
QuizAttemptReviewDto
LearningModuleChatRequestDto
LearningModuleChatResponseDto
```

## Summary

Learner learning module endpoints support browsing published modules, enrollment, lesson reading, lesson progress, quiz attempts, and RAG-based assistant chat.

Published modules are publicly browsable. Reading lesson content, updating progress, taking quizzes, viewing attempts, and assistant chat require authentication.

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `GET` | `/api/learning-modules` | No | List published learning modules |
| `GET` | `/api/learning-modules/enrolled` | Yes | List current user's enrolled modules |
| `GET` | `/api/learning-modules/{slug}` | No | Get published or accessible archived module overview |
| `POST` | `/api/learning-modules/{moduleId}/enroll` | Yes | Enroll current user in a module |
| `GET` | `/api/learning-modules/{moduleId}/lessons/{lessonId}` | Yes | Read lesson markdown content |
| `PATCH` | `/api/learning-modules/{moduleId}/lessons/{lessonId}/progress` | Yes | Update lesson progress |
| `GET` | `/api/learning-modules/{moduleId}/quiz/attempts` | Yes | List quiz attempts |
| `POST` | `/api/learning-modules/{moduleId}/quiz/attempts` | Yes | Start or resume an in-progress quiz attempt |
| `POST` | `/api/learning-modules/{moduleId}/quiz/attempts/{attemptId}/submit` | Yes | Submit a quiz attempt |
| `GET` | `/api/learning-modules/{moduleId}/quiz/attempts/{attemptId}` | Yes | Get quiz attempt review |
| `POST` | `/api/learning-modules/{moduleId}/assistant/chat` | Yes | Ask the module assistant a RAG-grounded question |

## Authentication

| Requirement | Details |
|---|---|
| Public endpoints | Module list and overview |
| Authenticated endpoints | Enrollment, lesson content, progress, quiz, assistant chat |
| Auth type | Authenticated `access_token` cookie |
| User id source | `User.GetUserId()` |

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Date format | ISO 8601 datetime string |
| Error format | Shared `ApiErrorResponse` object |
| Module statuses | `published`, `archived` for learner-readable modules |
| Enrollment statuses | `in_progress`, `completed` |
| Lesson progress statuses | `in_progress`, `completed` |
| Quiz attempt statuses | `in_progress`, `submitted`, `abandoned` |

## `GET /api/learning-modules`

Returns published modules for browsing.

### Success Response

Status:

```text
200 OK
```

Body:

```json
[
  {
    "skillModuleId": "11111111-1111-1111-1111-111111111111",
    "skillId": "22222222-2222-2222-2222-222222222222",
    "skillName": "CSS",
    "title": "CSS Fundamentals",
    "slug": "css-fundamentals",
    "status": "published",
    "description": "Learn CSS fundamentals.",
    "difficultyLevel": "beginner",
    "estimatedHours": 4.5,
    "lessonCount": 3,
    "questionCount": 10,
    "enrollment": null
  }
]
```

### Rules

| Rule | Behavior |
|---|---|
| Module status | Only `published` modules are returned |
| Sorting | Newest published first, then title |
| Anonymous user | `enrollment = null` |

## `GET /api/learning-modules/enrolled`

Returns modules the current user is enrolled in.

### Success Response

Status:

```text
200 OK
```

Body:

```json
[
  {
    "skillModuleId": "11111111-1111-1111-1111-111111111111",
    "skillId": "22222222-2222-2222-2222-222222222222",
    "skillName": "CSS",
    "title": "CSS Fundamentals",
    "slug": "css-fundamentals",
    "status": "published",
    "description": "Learn CSS fundamentals.",
    "difficultyLevel": "beginner",
    "estimatedHours": 4.5,
    "lessonCount": 3,
    "questionCount": 10,
    "enrollment": {
      "skillModuleEnrollmentId": "33333333-3333-3333-3333-333333333333",
      "userId": "44444444-4444-4444-4444-444444444444",
      "skillModuleId": "11111111-1111-1111-1111-111111111111",
      "status": "in_progress",
      "startedAt": "2026-06-16T10:00:00Z",
      "completedAt": null,
      "lastAccessedLessonId": null,
      "progressPercent": 0,
      "lessonProgress": {}
    }
  }
]
```

### Rules

| Rule | Behavior |
|---|---|
| Included module statuses | `published` and `archived` when current user has enrollment |
| Archived modules | Returned after published modules |
| Enrollment missing | Module is not included |

## `GET /api/learning-modules/{slug}`

Returns module overview by slug.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `slug` | `string` | Yes | Module slug |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleId": "11111111-1111-1111-1111-111111111111",
  "skillId": "22222222-2222-2222-2222-222222222222",
  "skillName": "CSS",
  "title": "CSS Fundamentals",
  "slug": "css-fundamentals",
  "status": "published",
  "description": "Learn CSS fundamentals.",
  "difficultyLevel": "beginner",
  "estimatedHours": 4.5,
  "lessons": [
    {
      "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
      "title": "Selectors and Cascade",
      "summary": "Learn selectors, specificity, and cascade.",
      "orderIndex": 1,
      "estimatedHours": 1.5
    }
  ],
  "quiz": {
    "skillModuleQuizId": "66666666-6666-6666-6666-666666666666",
    "title": "CSS Fundamentals Quiz",
    "description": "Check your CSS understanding.",
    "questionCount": 10,
    "passingScorePercent": 70,
    "maxAttempts": null
  },
  "enrollment": null
}
```

### Rules

| Rule | Behavior |
|---|---|
| Published module | Visible to everyone |
| Archived module | Visible only to enrolled users |
| Module missing or inaccessible | Not found |
| Authenticated enrolled user | Includes `enrollment` |

## `POST /api/learning-modules/{moduleId}/enroll`

Enrolls the current user in a published module.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `moduleId` | `guid` | Yes | Skill module id |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleEnrollmentId": "33333333-3333-3333-3333-333333333333",
  "userId": "44444444-4444-4444-4444-444444444444",
  "skillModuleId": "11111111-1111-1111-1111-111111111111",
  "status": "in_progress",
  "startedAt": "2026-06-16T10:00:00Z",
  "completedAt": null,
  "lastAccessedLessonId": null,
  "progressPercent": 0,
  "lessonProgress": {}
}
```

### Rules

| Rule | Behavior |
|---|---|
| Module not published | Not found |
| Existing enrollment | Existing enrollment is returned |
| New enrollment | Creates enrollment with `status = in_progress` and `progressPercent = 0` |

## `GET /api/learning-modules/{moduleId}/lessons/{lessonId}`

Returns lesson markdown content for an enrolled user.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `moduleId` | `guid` | Yes | Skill module id |
| `lessonId` | `guid` | Yes | Lesson id |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
  "skillModuleId": "11111111-1111-1111-1111-111111111111",
  "title": "Selectors and Cascade",
  "slug": "selectors-and-cascade",
  "markdown": "# Selectors and Cascade\n\nLesson content...",
  "contentVersion": 1,
  "contentHash": "sha256-hash"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Enrollment missing | Conflict: start the module first |
| Lesson not in module | Not found |
| Module not published or archived | Not found |
| Markdown file missing | Not found through storage layer |
| Success | Reads markdown content from file storage |

## `PATCH /api/learning-modules/{moduleId}/lessons/{lessonId}/progress`

Updates lesson progress.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `status` | `string` | Yes | `in_progress` or `completed` |

Example:

```json
{
  "status": "completed"
}
```

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleEnrollmentId": "33333333-3333-3333-3333-333333333333",
  "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
  "lessonStatus": "completed",
  "progressPercent": 25,
  "enrollmentStatus": "in_progress",
  "updatedAt": "2026-06-16T10:00:00Z"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Invalid status | Conflict |
| Module missing | Not found |
| Lesson missing | Not found |
| Enrollment missing | Conflict: start the module before updating progress |
| Success | Updates `lessonProgress`, `lastAccessedLessonId`, and enrollment progress |
| All lessons completed and quiz passed or absent | Enrollment becomes `completed` |

## `GET /api/learning-modules/{moduleId}/quiz/attempts`

Lists quiz attempts for the current user.

### Success Response

Status:

```text
200 OK
```

Body:

```json
[
  {
    "skillModuleQuizAttemptId": "77777777-7777-7777-7777-777777777777",
    "skillModuleQuizId": "66666666-6666-6666-6666-666666666666",
    "attemptNo": 1,
    "status": "submitted",
    "startedAt": "2026-06-16T10:00:00Z",
    "submittedAt": "2026-06-16T10:10:00Z",
    "scorePercent": 80,
    "earnedPoints": 8,
    "totalPoints": 10,
    "passed": true
  }
]
```

### Rules

| Rule | Behavior |
|---|---|
| Enrollment missing | Conflict |
| Quiz missing | Not found |
| Success | Returns attempts ordered by newest `attemptNo` first |

## `POST /api/learning-modules/{moduleId}/quiz/attempts`

Starts a quiz attempt, or returns the existing in-progress attempt.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleQuizAttemptId": "77777777-7777-7777-7777-777777777777",
  "skillModuleQuizId": "66666666-6666-6666-6666-666666666666",
  "attemptNo": 1,
  "status": "in_progress",
  "startedAt": "2026-06-16T10:00:00Z",
  "quiz": {
    "skillModuleQuizId": "66666666-6666-6666-6666-666666666666",
    "skillModuleId": "11111111-1111-1111-1111-111111111111",
    "title": "CSS Fundamentals Quiz",
    "description": "Check your CSS understanding.",
    "passingScorePercent": 70,
    "maxAttempts": null,
    "status": "draft",
    "questions": [],
    "createdAt": "2026-06-16T10:00:00Z",
    "updatedAt": "2026-06-16T10:00:00Z"
  }
}
```

### Rules

| Rule | Behavior |
|---|---|
| Enrollment missing | Conflict |
| Quiz missing | Not found |
| Lessons incomplete | Conflict: complete all lessons first |
| In-progress attempt exists | Existing attempt is returned |
| Daily max attempts reached | Conflict |
| New attempt | Creates attempt with next `attemptNo` and `status = in_progress` |
| Learner quiz payload | Hides correct answers and explanations before submission |

## `POST /api/learning-modules/{moduleId}/quiz/attempts/{attemptId}/submit`

Submits answers for a quiz attempt.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `moduleId` | `guid` | Yes | Skill module id |
| `attemptId` | `guid` | Yes | Quiz attempt id |

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `answers` | `array` | Yes | Must include one answer per quiz question |
| `answers[].skillModuleQuizQuestionId` | `guid` | Yes | Question id |
| `answers[].selectedOptionId` | `guid` | Yes | Selected option id for that question |

Example:

```json
{
  "answers": [
    {
      "skillModuleQuizQuestionId": "88888888-8888-8888-8888-888888888888",
      "selectedOptionId": "99999999-9999-9999-9999-999999999999"
    }
  ]
}
```

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleQuizAttemptId": "77777777-7777-7777-7777-777777777777",
  "skillModuleQuizId": "66666666-6666-6666-6666-666666666666",
  "skillModuleEnrollmentId": "33333333-3333-3333-3333-333333333333",
  "userId": "44444444-4444-4444-4444-444444444444",
  "attemptNo": 1,
  "status": "submitted",
  "startedAt": "2026-06-16T10:00:00Z",
  "submittedAt": "2026-06-16T10:10:00Z",
  "scorePercent": 80,
  "earnedPoints": 8,
  "totalPoints": 10,
  "passed": true,
  "answers": []
}
```

### Rules

| Rule | Behavior |
|---|---|
| Attempt not found or not owned by user | Not found |
| Attempt already submitted | Conflict |
| Answers missing | Conflict |
| Not exactly one answer per question | Conflict |
| Duplicate question answers | Conflict |
| Option does not belong to question | Conflict |
| Success | Saves answers, calculates score, marks attempt `submitted` |
| Passing score reached | Counts quiz as complete for module progress |

## `GET /api/learning-modules/{moduleId}/quiz/attempts/{attemptId}`

Returns quiz attempt review for the current user.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleQuizAttemptId": "77777777-7777-7777-7777-777777777777",
  "skillModuleQuizId": "66666666-6666-6666-6666-666666666666",
  "skillModuleEnrollmentId": "33333333-3333-3333-3333-333333333333",
  "userId": "44444444-4444-4444-4444-444444444444",
  "attemptNo": 1,
  "status": "submitted",
  "startedAt": "2026-06-16T10:00:00Z",
  "submittedAt": "2026-06-16T10:10:00Z",
  "scorePercent": 80,
  "earnedPoints": 8,
  "totalPoints": 10,
  "passed": true,
  "answers": [
    {
      "skillModuleQuizAnswerId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "skillModuleQuizQuestionId": "88888888-8888-8888-8888-888888888888",
      "questionText": "Which CSS rule has higher specificity?",
      "questionExplanation": null,
      "selectedOptionId": "99999999-9999-9999-9999-999999999999",
      "selectedOptionText": "#main .card",
      "correctOptionId": null,
      "correctOptionText": null,
      "isCorrect": true,
      "earnedPoints": 1,
      "questionPoints": 1
    }
  ]
}
```

### Rules

| Rule | Behavior |
|---|---|
| Attempt not found or not owned by user | Not found |
| Current behavior | Does not expose `correctOptionId`, `correctOptionText`, or question explanations in review |

## `POST /api/learning-modules/{moduleId}/assistant/chat`

Asks the learning module assistant a RAG-grounded question.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `skillModuleLessonId` | `guid/null` | No | Preferred lesson context |
| `message` | `string` | Yes | User question |
| `recentMessages` | `array` | No | Recent chat context from the frontend |
| `recentMessages[].role` | `string` | No | Example: `user`, `assistant` |
| `recentMessages[].content` | `string` | No | Message content |

Example:

```json
{
  "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
  "message": "Can you explain specificity with examples?",
  "recentMessages": [
    {
      "role": "user",
      "content": "I am reading the selectors lesson."
    }
  ]
}
```

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "answer": "Specificity decides which CSS rule wins when multiple rules target the same element...",
  "sources": [
    {
      "skillModuleChunkId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
      "lessonTitle": "Selectors and Cascade",
      "heading": "Specificity",
      "contentPreview": "Specificity is calculated from selector parts...",
      "similarityScore": 0.82
    }
  ]
}
```

### Rules

| Rule | Behavior |
|---|---|
| Auth missing | Unauthorized |
| Module missing or inaccessible | Not found |
| Module has no indexed chunks | Conflict or assistant service error |
| AI credit limit exceeded | `429 Too Many Requests` with `creditStatus` |
| Success | Returns grounded answer and matching lesson chunks |

## Progress Calculation Notes

| Topic | Behavior |
|---|---|
| Total units | Lesson count plus 1 quiz unit when a quiz exists |
| Completed lesson unit | Lesson status is `completed` |
| Completed quiz unit | At least one submitted attempt has `passed = true` |
| Progress percent | Completed units divided by total units, rounded to 2 decimals |
| Enrollment completion | Progress at 100 sets enrollment status to `completed` |

## Summary

Learners browse public modules, enroll, complete lessons, then take the quiz after every lesson is completed. Assistant chat is authenticated and grounded in indexed module lesson chunks.
