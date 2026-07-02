-- ============================================================
-- 028 - Roadmap review workflow
-- Adds pending review states so Content Managers submit edited
-- roadmap versions and Reviewers approve them into published.
-- ============================================================

BEGIN;

ALTER TABLE public.roadmap_version
    DROP CONSTRAINT IF EXISTS chk_roadmap_version_status;

ALTER TABLE public.roadmap_version
    ADD CONSTRAINT chk_roadmap_version_status
    CHECK (status IN ('draft', 'pending_review', 'changes_requested', 'published', 'archived'));

CREATE INDEX IF NOT EXISTS ix_roadmap_version_review_queue
    ON public.roadmap_version(status, updated_at DESC)
    WHERE status IN ('pending_review', 'changes_requested');

COMMIT;
