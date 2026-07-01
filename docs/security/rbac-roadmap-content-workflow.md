# RBAC Roadmap Content Workflow

## Purpose

This document defines the roadmap-specific RBAC rules for the Content Manager console and Reviewer workflow.

## Roles

| Role | Roadmap responsibility |
|---|---|
| `content_manager` | Create and manage roadmaps they own |
| `reviewer` | Review, approve, or reject submitted roadmap versions across owners |
| `admin` | Platform governance only, no roadmap authoring or review by default |

A user that needs multiple responsibilities must receive multiple roles explicitly.

## Permission model

### Content Manager

| Permission | Scope |
|---|---|
| `roadmap_draft.view.own` | View only roadmaps where `roadmap.owner_user_id = current_user_id` |
| `roadmap_draft.create.own` | Create owned roadmaps and owned version drafts |
| `roadmap_draft.update.own` | Mutate owned drafts only |
| `roadmap_draft.delete.own` | Delete owned editable drafts only |
| `roadmap_review.submit.own` | Submit owned drafts for review |

The backend must enforce ownership in the service layer. Controller permission checks are not enough for `own` scope.

### Reviewer

| Permission | Scope |
|---|---|
| `roadmap_review.view.any` | View roadmap review queue and review details across owners |
| `roadmap_review.approve.any` | Approve and publish pending roadmap versions across owners |
| `roadmap_review.reject.any` | Reject pending roadmap versions across owners |

Reviewer permissions are intentionally separate from draft-edit permissions. Reviewers can review any submitted roadmap, but they do not edit drafts.

### Admin

Admin does not receive roadmap draft or roadmap review permissions by default. Admin can manage users, roles, and permissions. To let an admin account author or review roadmaps, assign `content_manager` or `reviewer` explicitly.

## Backend enforcement rules

| Operation | Permission | Required service rule |
|---|---|---|
| List roadmap workspace items | `roadmap_draft.view.own` or `roadmap_review.view.any` | Own permission scopes query by `owner_user_id`; review permission can include all |
| View roadmap detail | `roadmap_draft.view.own` or `roadmap_review.view.any` | Own permission scopes detail by `owner_user_id`; review permission can include all |
| Create roadmap | `roadmap_draft.create.own` | Set `roadmap.owner_user_id` to current user |
| Create clone, minor, or patch draft | `roadmap_draft.create.own` | Source roadmap must be owned by current user |
| Update roadmap metadata | `roadmap_draft.update.own` | Version roadmap must be owned by current user |
| Create, move, update, or delete node | `roadmap_draft.update.own` | Node roadmap must be owned by current user |
| Add or remove node skills/resources | `roadmap_draft.update.own` | Node roadmap must be owned by current user |
| Validate draft | `roadmap_draft.update.own` or `roadmap_review.view.any` | Own permission scopes by owner; review permission can validate any submitted version |
| Submit for review | `roadmap_review.submit.own` | Draft roadmap must be owned by current user |
| Approve or reject | `roadmap_review.approve.any` or `roadmap_review.reject.any` | Version must be in pending review status |

Ownership failures should behave like not-found responses instead of revealing that another owner's roadmap exists.

## Seed alignment

The core RBAC seed should map roadmap authoring permissions to `content_manager` only:

```text
roadmap_draft.view.own
roadmap_draft.create.own
roadmap_draft.update.own
roadmap_draft.delete.own
roadmap_review.submit.own
```

Reviewer permissions should map to `reviewer` only:

```text
roadmap_review.view.any
roadmap_review.approve.any
roadmap_review.reject.any
```

Legacy broad draft permissions such as `roadmap_draft.update.any` must not be assigned to built-in roles.
