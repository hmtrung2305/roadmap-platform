# Roadmap Review Workflow

This document describes the roadmap publishing workflow after adding the `reviewer` role.

## Purpose

Content Managers no longer publish roadmap versions directly. A Content Manager creates or edits a draft, then submits it to a Reviewer. If the Reviewer approves it, the new version is published. If the Reviewer rejects it, the version returns to a state where it can be edited.

While the new version is in `pending_review`, learners still see the current `published` version. When the Reviewer approves it, the existing publish logic is reused: the new version becomes `published`, and the old version is marked as `archived` according to the existing versioning rules.

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
3. The Content Manager clicks `Submit for review`.
4. The backend validates the version. If valid, the status changes from `draft`/`changes_requested` to `pending_review`.
5. The Reviewer opens `/content/reviews` and views the version in read-only mode.
6. The Reviewer chooses:
   - `Approve`: the backend only accepts versions with `pending_review` status, then publishes the new version and archives the old version.
   - `Reject`: the backend changes the version status to `changes_requested` so the Content Manager can continue editing it.

## Default Permissions

### content_manager

| Permission | Purpose |
|---|---|
| `roadmap_draft.view.any` | View the list/detail of roadmap versions in the content workspace |
| `roadmap_draft.create.any` | Create a new roadmap draft, or create patch/minor/major update drafts |
| `roadmap_draft.update.any` | Edit metadata, nodes, and skill/resource mappings |
| `roadmap_draft.delete.any` | Delete draft or changes-requested versions |
| `roadmap_review.submit.own` | Submit an editable draft to a Reviewer |

### reviewer

| Permission | Purpose |
|---|---|
| `roadmap_review.view.any` | View the `pending_review` version queue |
| `roadmap_review.approve.any` | Approve and publish a new version |
| `roadmap_review.reject.any` | Reject a version and move it back to `changes_requested` |

### admin

The seeded Admin role is assigned both permission groups above so that admins can support operations when needed.

## API Endpoints

| Endpoint | Permission |
|---|---|
| `GET /api/content/roadmaps` | `roadmap_draft.view.any` or `roadmap_review.view.any` |
| `GET /api/content/roadmaps/{roadmapId}` | `roadmap_draft.view.any` or `roadmap_review.view.any` |
| `POST /api/content/roadmaps` | `roadmap_draft.create.any` |
| `POST /api/content/roadmap-versions/{id}/clone-draft` | `roadmap_draft.create.any` |
| `POST /api/content/roadmap-versions/{id}/patch-draft` | `roadmap_draft.create.any` |
| `POST /api/content/roadmap-versions/{id}/minor-draft` | `roadmap_draft.create.any` |
| `PATCH /api/content/roadmap-versions/{id}/metadata` | `roadmap_draft.update.any` |
| `POST /api/content/roadmap-versions/{id}/nodes` | `roadmap_draft.update.any` |
| `DELETE /api/content/roadmap-versions/{id}` | `roadmap_draft.delete.any` |
| `POST /api/content/roadmap-versions/{id}/submit-review` | `roadmap_review.submit.own` |
| `POST /api/content/roadmap-versions/{id}/approve` | `roadmap_review.approve.any` |
| `POST /api/content/roadmap-versions/{id}/reject` | `roadmap_review.reject.any` |

Node mutation endpoints under `/api/content/roadmap-nodes/*` require `roadmap_draft.update.any`.

## Database

`roadmap_version.status` accepts:

```text
draft, pending_review, changes_requested, published, archived
```

Related migrations:

```text
database/migrations/027-add-roadmap-slug-and-title-constraints.sql
database/migrations/028-roadmap-review-workflow.sql
```

Seed:

```text
database/seeds/core/001-rbac-roles-permissions.seed.sql
```
