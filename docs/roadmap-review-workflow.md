# Roadmap Review Workflow

This document describes the roadmap publishing workflow after adding the `reviewer` role.

## Purpose

Content Managers no longer publish roadmap versions directly. A Content Manager creates or edits a draft, adds a review changelog, then submits it to a Reviewer. If the Reviewer approves it, the new version is published. If the Reviewer rejects it, the Reviewer must provide a reason and the version returns to a state where it can be edited.

While the new version is in `pending_review`, learners still see the current `published` version. When the Reviewer approves it, the existing publish logic is reused: the new version becomes `published`, and the old version is marked as `archived` according to the existing versioning rules.

Patch releases are applied directly to existing learner enrollments after approval. Minor and major releases are not forced onto learners; the latest published roadmap response includes an `availableUpdate` object so the learner can migrate their enrollment when they are ready. Published roadmap responses also include `versionHistory` so learners can inspect changes across roadmap versions.

## Version Statuses

| Status | Meaning | Next Actor |
|---|---|---|
| `draft` | A draft that the Content Manager is creating or editing | Content Manager |
| `pending_review` | Submitted and locked for Reviewer review | Reviewer |
| `changes_requested` | Rejected by the Reviewer and requires further edits from the Content Manager | Content Manager |
| `published` | The version currently visible to learners | Learners can view it; Content Managers can create an update draft |
| `archived` | An old version that has been replaced | View history only |

`changes_requested` is treated as an editable draft, but the status name makes it clear that the version was returned after review.

## Main Workflow

1. The Content Manager creates a new roadmap or creates an update draft from a `published` version.
2. The Content Manager edits metadata, nodes, and skill/resource mappings.
3. The Content Manager clicks `Submit for review` and fills in a changelog.
4. The backend validates the version. If valid, the status changes from `draft`/`changes_requested` to `pending_review` and writes a `submitted` review event.
5. The Reviewer opens `/content/reviews` and reviews the version in the Reviewer workspace. The workspace shows the changelog, review checklist, read-only node outline, and review timeline.
6. The Reviewer chooses:
   - `Approve`: the backend only accepts versions with `pending_review` status, writes an `approved` review event, then publishes the new version and archives the old version.
   - `Request changes`: the Reviewer must enter a suggestion/reason. The backend writes a `rejected` review event and changes the version status to `changes_requested` so the Content Manager can continue editing it.

## Reviewer Workspace UX

The Reviewer workspace is a moderation surface, not an editor. It must not expose roadmap mutation controls such as metadata edits, node edits, skill/resource mapping changes, or graph layout edits.

Reviewers can:

| Action | Behavior |
|---|---|
| Inspect submitted roadmap content | Read-only roadmap metadata, validation, node outline, and review timeline |
| Read changelog | Shows the Content Manager's submit message as the version changelog |
| Suggest changes | Enter review feedback before requesting changes |
| Approve | Publishes the pending version |
| Request changes | Stores feedback as a `rejected` event and returns the version to `changes_requested` |

## Review Event Log

Every review action writes to `roadmap_version_review_event`.

| Event Type | Written By | Message Source | Purpose |
|---|---|---|---|
| `submitted` | Content Manager | `changeLog` in submit request | Explains what changed and what the Reviewer should verify |
| `approved` | Reviewer | System message | Records the approval decision before publishing |
| `rejected` | Reviewer | `reason` in reject request | Explains what the Content Manager must fix |

The Content Manager editor displays the timeline so a returned version includes the reject reason. The Reviewer workspace also displays the timeline to preserve review context across multiple submit/reject cycles. In both workspaces, `submitted` events are displayed as changelog entries.

## Learner Update Behavior

| Release Type | Enrollment Behavior |
|---|---|
| `patch` | Existing enrollments on replaced patch-line versions are remapped automatically to the approved patch version. Progress rows and progress events are remapped by `nodeType + slug`. |
| `minor` | Existing enrollments stay on the old version. The latest roadmap response exposes `availableUpdate`, and the learner can migrate manually. |
| `major` | Same as minor, but used for larger breaking content changes. The learner chooses when to migrate. |

Manual migration calls `POST /api/roadmap-enrollments/{roadmapEnrollmentId}/migrate` with a target published major/minor version. The service requires the enrollment to belong to the current user, requires the target version to belong to the same roadmap, and remaps progress by `nodeType + slug`.

## Learner Changelog Behavior

Published roadmap detail and graph responses include `versionHistory`. The learner roadmap viewer uses this data to show:

| Item | Source |
|---|---|
| Current learning version | Current enrollment version, or the displayed roadmap version when not enrolled |
| Latest roadmap version | Latest published version from `versionHistory` |
| Version changelog | Latest `submitted` review event message for each published or archived version |
| Update notice | `availableUpdate` when the learner is enrolled on an older minor/major version |

Learners can view version changes without becoming a reviewer or content manager. The changelog is informational; migration is still controlled by the existing enrollment migration endpoint for minor and major releases.

## Default Permissions

### content_manager

| Permission | Purpose |
|---|---|
| `roadmap_draft.view.own` | View owned roadmap versions in the content workspace |
| `roadmap_draft.create.own` | Create owned roadmap drafts, or create owned patch/minor/major update drafts |
| `roadmap_draft.update.own` | Edit owned metadata, nodes, and skill/resource mappings |
| `roadmap_draft.delete.own` | Delete owned draft or changes-requested versions |
| `roadmap_review.submit.own` | Submit an editable draft to a Reviewer |

### reviewer

| Permission | Purpose |
|---|---|
| `roadmap_review.view.any` | View the `pending_review` version queue |
| `roadmap_review.approve.any` | Approve and publish a new version |
| `roadmap_review.reject.any` | Reject a version and move it back to `changes_requested` |

### admin

The seeded Admin role is platform governance only. It is not assigned roadmap authoring or review permissions by default. To let an admin account author or review roadmaps, assign `content_manager` or `reviewer` explicitly in addition to `admin`.

## API Endpoints

| Endpoint | Permission |
|---|---|
| `GET /api/content/roadmaps` | `roadmap_draft.view.own` or `roadmap_review.view.any` |
| `GET /api/content/roadmaps/{roadmapId}` | `roadmap_draft.view.own` or `roadmap_review.view.any` |
| `POST /api/content/roadmaps` | `roadmap_draft.create.own` |
| `POST /api/content/roadmap-versions/{id}/clone-draft` | `roadmap_draft.create.own` |
| `POST /api/content/roadmap-versions/{id}/patch-draft` | `roadmap_draft.create.own` |
| `POST /api/content/roadmap-versions/{id}/minor-draft` | `roadmap_draft.create.own` |
| `PATCH /api/content/roadmap-versions/{id}/metadata` | `roadmap_draft.update.own` |
| `POST /api/content/roadmap-versions/{id}/nodes` | `roadmap_draft.update.own` |
| `DELETE /api/content/roadmap-versions/{id}` | `roadmap_draft.delete.own` |
| `POST /api/content/roadmap-versions/{id}/submit-review` | `roadmap_review.submit.own` |
| `POST /api/content/roadmap-versions/{id}/approve` | `roadmap_review.approve.any` |
| `POST /api/content/roadmap-versions/{id}/reject` | `roadmap_review.reject.any` |
| `POST /api/roadmap-enrollments/{id}/migrate` | `roadmap_enrollment.migrate.self` |

Node mutation endpoints under `/api/content/roadmap-nodes/*` require `roadmap_draft.update.own` and ownership enforcement.

### Review Request Bodies

Submit for review:

```json
{
  "changeLog": "Updated node descriptions, added deployment checklist, and fixed skill mappings."
}
```

Reject review:

```json
{
  "reason": "Please clarify the project completion criteria and replace the outdated resource."
}
```

Both fields are required, trimmed by the backend, and capped at 4000 characters.

## Database

`roadmap_version.status` accepts:

```text
draft, pending_review, changes_requested, published, archived
```

`roadmap_version_review_event` stores review timeline entries:

| Column | Purpose |
|---|---|
| `roadmap_version_review_event_id` | Review event id |
| `roadmap_version_id` | Version being reviewed |
| `actor_user_id` | User who submitted, approved, or rejected |
| `event_type` | `submitted`, `approved`, or `rejected` |
| `message` | Changelog or reject reason |
| `created_at` | Event timestamp |

Related migrations:

```text
database/migrations/027-add-roadmap-slug-and-title-constraints.sql
database/migrations/028-roadmap-review-workflow.sql
database/migrations/029-roadmap-review-events-and-enrollment-migration.sql
```

Seed:

```text
database/seeds/core/001-rbac-roles-permissions.seed.sql
```
