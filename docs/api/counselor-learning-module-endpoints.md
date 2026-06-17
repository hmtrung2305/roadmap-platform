# Counselor Learning Module Endpoint Specification

Base routes:

```text
/api/counselor/learning-modules
/api/counselor/learning-modules/{moduleId}/lessons
/api/counselor/learning-modules/{moduleId}/quiz
```

Source controllers:

```text
CounselorLearningModulesController
CounselorLearningModuleLessonsController
CounselorLearningModuleQuizController
```

Related services / DTOs:

```text
ICounselorLearningModuleService
ILearningModuleLessonService
ILearningModuleQuizService
CreateLearningModuleRequestDto
UpdateLearningModuleRequestDto
CounselorLearningModuleSummaryDto
CounselorLearningModuleDetailDto
LearningModuleLessonDto
BulkUploadLessonsRequestDto
BulkUploadLessonsResultDto
UpdateLearningModuleLessonRequestDto
ReorderLessonsRequestDto
UpsertQuizRequestDto
UpsertQuizQuestionRequestDto
ReorderQuizQuestionsRequestDto
LearningModuleQuizDto
LearningModuleQuizQuestionDto
PublishLearningModuleResultDto
LearningModulePreviewDto
```

## Summary

Counselor endpoints manage learning module authoring.

Counselors create draft modules, upload markdown lessons, manage quiz questions, publish modules after readiness validation, archive published modules, and delete drafts.

Only draft modules are editable. Published modules can be archived. Archived modules are not editable through current endpoints.

## Endpoint Summary

| Method | Endpoint | Auth Required | Rate Limit | Purpose |
|---|---|---:|---|---|
| `GET` | `/api/counselor/learning-modules` | Yes | Default | List current counselor's modules |
| `POST` | `/api/counselor/learning-modules` | Yes | `AdminMutation` | Create draft module |
| `GET` | `/api/counselor/learning-modules/{moduleId}` | Yes | Default | Get module detail |
| `PATCH` | `/api/counselor/learning-modules/{moduleId}` | Yes | `AdminMutation` | Update draft module overview |
| `DELETE` | `/api/counselor/learning-modules/{moduleId}` | Yes | `AdminMutation` | Delete draft module |
| `POST` | `/api/counselor/learning-modules/{moduleId}/publish` | Yes | `AdminMutation` | Publish draft module |
| `POST` | `/api/counselor/learning-modules/{moduleId}/archive` | Yes | `AdminMutation` | Archive published module |
| `GET` | `/api/counselor/learning-modules/{moduleId}/preview` | Yes | Default | Preview module as learner-style summary |
| `POST` | `/api/counselor/learning-modules/{moduleId}/lessons/bulk` | Yes | `UploadExpensive` | Bulk upload markdown lessons |
| `PATCH` | `/api/counselor/learning-modules/{moduleId}/lessons/reorder` | Yes | `AdminMutation` | Reorder lessons |
| `PATCH` | `/api/counselor/learning-modules/{moduleId}/lessons/{lessonId}` | Yes | `AdminMutation` | Update lesson metadata |
| `PUT` | `/api/counselor/learning-modules/{moduleId}/lessons/{lessonId}/content` | Yes | `UploadExpensive` | Replace lesson markdown |
| `POST` | `/api/counselor/learning-modules/{moduleId}/lessons/{lessonId}/reindex` | Yes | `AiExpensive` | Reindex lesson chunks |
| `GET` | `/api/counselor/learning-modules/{moduleId}/lessons/{lessonId}/preview` | Yes | Default | Preview lesson markdown |
| `DELETE` | `/api/counselor/learning-modules/{moduleId}/lessons/{lessonId}` | Yes | `AdminMutation` | Delete draft lesson |
| `PUT` | `/api/counselor/learning-modules/{moduleId}/quiz` | Yes | `AdminMutation` | Create or update quiz |
| `POST` | `/api/counselor/learning-modules/{moduleId}/quiz/questions` | Yes | `AdminMutation` | Add quiz question |
| `PATCH` | `/api/counselor/learning-modules/{moduleId}/quiz/questions/{questionId}` | Yes | `AdminMutation` | Update quiz question |
| `PATCH` | `/api/counselor/learning-modules/{moduleId}/quiz/questions/reorder` | Yes | `AdminMutation` | Reorder quiz questions |
| `DELETE` | `/api/counselor/learning-modules/{moduleId}/quiz/questions/{questionId}` | Yes | `AdminMutation` | Delete quiz question |

## Authentication

| Requirement | Details |
|---|---|
| Auth required | Yes |
| Auth type | Authenticated `access_token` cookie |
| User id source | `User.GetUserId()` |
| Ownership | Queries are scoped by `createdByUserId` |

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Date format | ISO 8601 datetime string |
| Error format | Shared `ApiErrorResponse` object |
| Module statuses | `draft`, `published`, `archived` |
| Lesson indexing statuses | `pending`, `indexing`, `indexed`, `failed`, `needs_reindex` |
| Question type | Current supported value: `single_choice` |

## `GET /api/counselor/learning-modules`

Lists modules created by the current counselor.

### Query Parameters

| Parameter | Type | Required | Default | Notes |
|---|---|---:|---|---|
| `status` | `string` | No | `null` | Optional status filter, such as `draft`, `published`, or `archived` |

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
    "skillSlug": "css",
    "title": "CSS Fundamentals",
    "slug": "css-fundamentals",
    "description": "Learn CSS fundamentals.",
    "difficultyLevel": "beginner",
    "estimatedHours": 4.5,
    "status": "draft",
    "lessonCount": 3,
    "questionCount": 10,
    "hasQuiz": true,
    "publishedAt": null,
    "archivedAt": null,
    "createdAt": "2026-06-16T10:00:00Z",
    "updatedAt": "2026-06-16T10:00:00Z"
  }
]
```

### Rules

| Rule | Behavior |
|---|---|
| `status` provided | Filters by exact status string |
| Ownership | Only modules created by current counselor are returned |
| Sorting | Newest updated first in service behavior |

## `POST /api/counselor/learning-modules`

Creates a draft module.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `skillId` | `guid` | Yes | Must reference an existing skill |
| `title` | `string` | Yes | Max 200 chars |
| `slug` | `string/null` | No | Max 200 chars; service can generate from title |
| `description` | `string/null` | No | Optional |
| `difficultyLevel` | `string/null` | No | Max 30 chars |
| `estimatedHours` | `number/null` | No | Optional |

Example:

```json
{
  "skillId": "22222222-2222-2222-2222-222222222222",
  "title": "CSS Fundamentals",
  "slug": "css-fundamentals",
  "description": "Learn CSS fundamentals.",
  "difficultyLevel": "beginner",
  "estimatedHours": 4.5
}
```

### Success Response

Status:

```text
201 Created
```

Body:

```json
{
  "skillModuleId": "11111111-1111-1111-1111-111111111111",
  "skillId": "22222222-2222-2222-2222-222222222222",
  "skillName": "CSS",
  "skillSlug": "css",
  "title": "CSS Fundamentals",
  "slug": "css-fundamentals",
  "description": "Learn CSS fundamentals.",
  "difficultyLevel": "beginner",
  "estimatedHours": 4.5,
  "status": "draft",
  "createdByUserId": "33333333-3333-3333-3333-333333333333",
  "publishedAt": null,
  "archivedAt": null,
  "createdAt": "2026-06-16T10:00:00Z",
  "updatedAt": "2026-06-16T10:00:00Z"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Skill not found | Not found |
| Missing title | Validation error |
| Missing skill id | Validation error |
| Success | Creates `skill_module` with `status = draft` |

## `GET /api/counselor/learning-modules/{moduleId}`

Returns full counselor detail for one module.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `moduleId` | `guid` | Yes | Module owned by current counselor |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "module": {
    "skillModuleId": "11111111-1111-1111-1111-111111111111",
    "skillId": "22222222-2222-2222-2222-222222222222",
    "skillName": "CSS",
    "skillSlug": "css",
    "title": "CSS Fundamentals",
    "slug": "css-fundamentals",
    "description": "Learn CSS fundamentals.",
    "difficultyLevel": "beginner",
    "estimatedHours": 4.5,
    "status": "draft",
    "createdByUserId": "33333333-3333-3333-3333-333333333333",
    "publishedAt": null,
    "archivedAt": null,
    "createdAt": "2026-06-16T10:00:00Z",
    "updatedAt": "2026-06-16T10:00:00Z"
  },
  "lessons": [],
  "quiz": null,
  "publishReadiness": {
    "canPublish": false,
    "errors": ["At least 3 lessons are required."]
  }
}
```

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Success | Returns module, lessons, quiz, and publish readiness |

## `PATCH /api/counselor/learning-modules/{moduleId}`

Updates draft module overview data.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `skillId` | `guid/null` | No | New skill id, if changing skill |
| `title` | `string/null` | No | Max 200 chars |
| `slug` | `string/null` | No | Max 200 chars |
| `description` | `string/null` | No | Optional |
| `difficultyLevel` | `string/null` | No | Max 30 chars |
| `estimatedHours` | `number/null` | No | Optional |
| `metadata` | `object/null` | No | Stored as module metadata |

Example:

```json
{
  "title": "CSS Fundamentals Updated",
  "difficultyLevel": "beginner",
  "estimatedHours": 5
}
```

### Success Response

Status:

```text
200 OK
```

Body: `SkillModuleDto`.

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Module is not draft | Conflict: only draft modules can be edited |
| New skill not found | Not found |
| Success | Updates module overview and `updatedAt` |

## `DELETE /api/counselor/learning-modules/{moduleId}`

Deletes a draft module.

### Success Response

Status:

```text
204 No Content
```

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Module is not draft | Conflict |
| Success | Deletes the draft module |

## `POST /api/counselor/learning-modules/{moduleId}/publish`

Publishes a draft module after readiness validation.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleId": "11111111-1111-1111-1111-111111111111",
  "status": "published",
  "publishedAt": "2026-06-16T10:00:00Z",
  "readiness": {
    "canPublish": true,
    "errors": []
  }
}
```

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Module is not draft | Conflict |
| Readiness fails | Conflict with readiness error messages |
| Success | Sets `status = published`, sets `publishedAt`, clears `archivedAt` |

### Publish Readiness Rules

| Rule | Required |
|---|---:|
| Module has at least 3 lessons | Yes |
| Module has a quiz | Yes |
| Quiz has at least 10 questions | Yes |
| Indexed lessons have generated chunks | Yes |

> [!IMPORTANT]
> Publishing is blocked until the module satisfies readiness rules.

## `POST /api/counselor/learning-modules/{moduleId}/archive`

Archives a published module.

### Success Response

Status:

```text
200 OK
```

Body: `SkillModuleDto` with `status = archived`.

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Module is not published | Conflict |
| Success | Sets `status = archived`, sets `archivedAt`, updates `updatedAt` |

## `GET /api/counselor/learning-modules/{moduleId}/preview`

Returns learner-style preview for a counselor-owned module.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "skillModuleId": "11111111-1111-1111-1111-111111111111",
  "title": "CSS Fundamentals",
  "slug": "css-fundamentals",
  "description": "Learn CSS fundamentals.",
  "difficultyLevel": "beginner",
  "estimatedHours": 4.5,
  "skillName": "CSS",
  "lessons": [],
  "quiz": null
}
```

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Success | Returns preview without requiring published status |

## `POST /api/counselor/learning-modules/{moduleId}/lessons/bulk`

Bulk uploads markdown lesson files and metadata.

### Request Body

Content type:

```text
multipart/form-data
```

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `lessonsJson` | `string` | Yes | JSON string matching `BulkUploadLessonsRequestDto` |
| `files` | `file[]` | Yes | Markdown files; max request size 100 MB |

`lessonsJson` example:

```json
{
  "lessons": [
    {
      "clientId": "lesson-1",
      "fileName": "selectors.md",
      "title": "Selectors and Cascade",
      "slug": "selectors-and-cascade",
      "summary": "Learn selectors, specificity, and cascade.",
      "orderIndex": 1,
      "estimatedHours": 1.5
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
  "lessons": [
    {
      "clientId": "lesson-1",
      "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
      "title": "Selectors and Cascade",
      "slug": "selectors-and-cascade",
      "orderIndex": 1,
      "markdownFileName": "selectors.md",
      "markdownFileKey": "learning-modules/1111/selectors.md",
      "contentHash": "sha256-hash",
      "contentSizeBytes": 1200,
      "contentVersion": 1,
      "indexingStatus": "indexed",
      "indexedAt": "2026-06-16T10:00:00Z",
      "indexingError": null,
      "chunksGenerated": 4
    }
  ],
  "failedLessons": [],
  "uploadedCount": 1,
  "failedCount": 0,
  "hasFailures": false
}
```

### Rules

| Rule | Behavior |
|---|---|
| `lessonsJson` cannot be deserialized | Bad request |
| Module not found or not owned | Not found |
| Module is not draft | Conflict |
| No lesson metadata | Conflict |
| No files | Conflict |
| Duplicate client ids | Conflict |
| Duplicate uploaded file names | Conflict |
| Non-markdown file | Conflict |
| Empty markdown file | Conflict |
| Indexing succeeds | Lesson returns `indexingStatus = indexed` and chunk count |
| Indexing fails | Lesson can return `failed` status with indexing error |

## `PATCH /api/counselor/learning-modules/{moduleId}/lessons/reorder`

Reorders lessons.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `lessons` | `array` | Yes | Must include every lesson in the module |
| `lessons[].skillModuleLessonId` | `guid` | Yes | Existing lesson id |
| `lessons[].orderIndex` | `number` | Yes | Positive and unique |

Example:

```json
{
  "lessons": [
    {
      "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
      "orderIndex": 1
    }
  ]
}
```

### Success Response

Status:

```text
200 OK
```

Body: `LearningModuleLessonDto[]`.

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Module is not draft | Conflict |
| Empty reorder list | Conflict |
| Missing existing lesson | Conflict |
| Order values not positive | Conflict |
| Order values not unique | Conflict |
| Success | Updates lesson order |

## `PATCH /api/counselor/learning-modules/{moduleId}/lessons/{lessonId}`

Updates draft lesson metadata.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `title` | `string/null` | No | Max 200 chars |
| `slug` | `string/null` | No | Max 200 chars |
| `summary` | `string/null` | No | Optional |
| `estimatedHours` | `number/null` | No | Optional |

Example:

```json
{
  "title": "Selectors and Cascade",
  "summary": "Learn selectors and specificity.",
  "estimatedHours": 1.5
}
```

### Success Response

Status:

```text
200 OK
```

Body: `LearningModuleLessonDto`.

### Rules

| Rule | Behavior |
|---|---|
| Module or lesson not found | Not found |
| Module is not draft | Conflict |
| Success | Updates lesson metadata |

## `PUT /api/counselor/learning-modules/{moduleId}/lessons/{lessonId}/content`

Replaces a lesson markdown file.

### Request Body

Content type:

```text
multipart/form-data
```

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `file` | `file` | Yes | Markdown file; max request size 50 MB |

### Success Response

Status:

```text
200 OK
```

Body: `LearningModuleLessonDto`.

### Rules

| Rule | Behavior |
|---|---|
| Uploaded file length is `0` | Bad request |
| Module or lesson not found | Not found |
| Module is not draft | Conflict |
| Non-markdown file | Conflict |
| Content changed | Increments `contentVersion` and reindexes chunks |

## `POST /api/counselor/learning-modules/{moduleId}/lessons/{lessonId}/reindex`

Reindexes lesson chunks for RAG search.

### Success Response

Status:

```text
200 OK
```

Body: `LearningModuleLessonDto`.

### Rules

| Rule | Behavior |
|---|---|
| Module or lesson not found | Not found |
| Module is not draft | Conflict |
| Gemini API key missing | Invalid request |
| Success | Deletes old chunks, creates new chunks, updates indexing status |

## `GET /api/counselor/learning-modules/{moduleId}/lessons/{lessonId}/preview`

Returns lesson markdown content for counselor preview.

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
| Module or lesson not found | Not found |
| Success | Reads markdown content from storage |

## `DELETE /api/counselor/learning-modules/{moduleId}/lessons/{lessonId}`

Deletes a lesson from a draft module.

### Success Response

Status:

```text
204 No Content
```

### Rules

| Rule | Behavior |
|---|---|
| Module or lesson not found | Not found |
| Module is not draft | Conflict |
| Success | Deletes draft lesson and related data |

## `PUT /api/counselor/learning-modules/{moduleId}/quiz`

Creates or updates the module quiz.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `title` | `string` | Yes | Max 200 chars |
| `description` | `string/null` | No | Optional |
| `passingScorePercent` | `number` | Yes | Must be between 1 and 100 |
| `maxAttempts` | `number/null` | No | Daily submitted attempt limit when set |

Example:

```json
{
  "title": "CSS Fundamentals Quiz",
  "description": "Check your CSS understanding.",
  "passingScorePercent": 70,
  "maxAttempts": null
}
```

### Success Response

Status:

```text
200 OK
```

Body: `LearningModuleQuizDto`.

### Rules

| Rule | Behavior |
|---|---|
| Module not found or not owned | Not found |
| Module is not draft | Conflict |
| Passing score outside 1-100 | Conflict |
| Success | Creates or updates quiz |

## `POST /api/counselor/learning-modules/{moduleId}/quiz/questions`

Adds a quiz question.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `questionText` | `string` | Yes | Required |
| `questionType` | `string` | No | Current supported value: `single_choice` |
| `explanation` | `string/null` | No | Optional |
| `orderIndex` | `number` | Yes | Question order |
| `points` | `number` | Yes | Defaults to `1` |
| `options` | `array` | Yes | At least 2 options |
| `options[].skillModuleQuizOptionId` | `guid/null` | No | Used for update requests |
| `options[].optionText` | `string` | Yes | Required |
| `options[].isCorrect` | `boolean` | Yes | Exactly one correct option for single-choice |
| `options[].explanation` | `string/null` | No | Optional |
| `options[].orderIndex` | `number` | Yes | Unique positive order among provided options |

Example:

```json
{
  "questionText": "Which selector targets an id?",
  "questionType": "single_choice",
  "explanation": "IDs use the # prefix.",
  "orderIndex": 1,
  "points": 1,
  "options": [
    {
      "optionText": "#main",
      "isCorrect": true,
      "explanation": "Correct.",
      "orderIndex": 1
    },
    {
      "optionText": ".main",
      "isCorrect": false,
      "explanation": "This targets a class.",
      "orderIndex": 2
    }
  ]
}
```

### Success Response

Status:

```text
201 Created
```

Body: `LearningModuleQuizQuestionDto`.

### Rules

| Rule | Behavior |
|---|---|
| Quiz missing | Not found |
| Module is not draft | Conflict |
| Question text missing | Conflict |
| Less than 2 options | Conflict |
| Option text missing | Conflict |
| Duplicate option order | Conflict |
| Unsupported question type | Conflict |
| Single-choice correct count not exactly 1 | Conflict |

## `PATCH /api/counselor/learning-modules/{moduleId}/quiz/questions/{questionId}`

Updates a quiz question.

### Request Body

Same shape as `POST /api/counselor/learning-modules/{moduleId}/quiz/questions`.

### Success Response

Status:

```text
200 OK
```

Body: `LearningModuleQuizQuestionDto`.

### Rules

| Rule | Behavior |
|---|---|
| Quiz missing | Not found |
| Question missing | Not found |
| Module is not draft | Conflict |
| Validation rules | Same as adding a question |
| Concurrency update issue | Conflict |

## `PATCH /api/counselor/learning-modules/{moduleId}/quiz/questions/reorder`

Reorders quiz questions.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `questions` | `array` | Yes | Must include every question in the quiz |
| `questions[].skillModuleQuizQuestionId` | `guid` | Yes | Existing question id |
| `questions[].orderIndex` | `number` | Yes | Positive and unique |

Example:

```json
{
  "questions": [
    {
      "skillModuleQuizQuestionId": "77777777-7777-7777-7777-777777777777",
      "orderIndex": 1
    }
  ]
}
```

### Success Response

Status:

```text
200 OK
```

Body: `LearningModuleQuizQuestionDto[]`.

### Rules

| Rule | Behavior |
|---|---|
| Quiz missing | Not found |
| Module is not draft | Conflict |
| Empty reorder list | Conflict |
| Missing existing question | Conflict |
| Order values not positive | Conflict |
| Order values not unique | Conflict |

## `DELETE /api/counselor/learning-modules/{moduleId}/quiz/questions/{questionId}`

Deletes a quiz question.

### Success Response

Status:

```text
204 No Content
```

### Rules

| Rule | Behavior |
|---|---|
| Quiz missing | Not found |
| Question missing | Not found |
| Module is not draft | Conflict |
| Success | Deletes question and its options |

## Shared DTOs

### `LearningModuleLessonDto`

```json
{
  "skillModuleLessonId": "55555555-5555-5555-5555-555555555555",
  "skillModuleId": "11111111-1111-1111-1111-111111111111",
  "title": "Selectors and Cascade",
  "slug": "selectors-and-cascade",
  "summary": "Learn selectors and specificity.",
  "orderIndex": 1,
  "estimatedHours": 1.5,
  "markdownFileKey": "learning-modules/1111/selectors.md",
  "markdownFileName": "selectors.md",
  "contentHash": "sha256-hash",
  "contentSizeBytes": 1200,
  "contentVersion": 1,
  "indexingStatus": "indexed",
  "indexedAt": "2026-06-16T10:00:00Z",
  "indexingError": null,
  "chunkCount": 4,
  "createdAt": "2026-06-16T10:00:00Z",
  "updatedAt": "2026-06-16T10:00:00Z"
}
```

### `LearningModuleQuizDto`

```json
{
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
```

## Implementation Notes

| Topic | Notes |
|---|---|
| Draft-only editing | Module, lesson, and quiz edits require module status `draft` |
| Publishing | Requires readiness validation before status changes to `published` |
| Archiving | Only `published` modules can be archived |
| Lesson storage | Markdown files are saved through `IFileStorage` |
| RAG indexing | Markdown is chunked and embedded for assistant search |
| Partial bulk upload | Result can include both uploaded lessons and failed lessons |

## Summary

Counselor endpoints are draft-centric. Create the module, upload at least 3 markdown lessons, create a quiz with at least 10 questions, then publish after readiness passes.
