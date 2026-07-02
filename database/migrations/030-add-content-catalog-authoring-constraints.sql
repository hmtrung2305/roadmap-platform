-- Adds normalized catalog authoring indexes.
-- Skill names/slugs remain globally unique.
-- Learning resource URLs are indexed for duplicate checks, but not unique because existing seed data reuses the same URL under multiple catalog rows.

CREATE UNIQUE INDEX IF NOT EXISTS uq_skill_name_normalized
    ON public.skill (lower(trim(name)));

CREATE UNIQUE INDEX IF NOT EXISTS uq_skill_slug_normalized
    ON public.skill (lower(trim(slug)));

DROP INDEX IF EXISTS public.uq_learning_resource_url_normalized;

CREATE INDEX IF NOT EXISTS ix_learning_resource_url_normalized
    ON public.learning_resource (lower(trim(url)));
