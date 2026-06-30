BEGIN;

-- Add a stable public slug for each roadmap.
ALTER TABLE public.roadmap
ADD COLUMN IF NOT EXISTS slug text;

-- Backfill and normalize roadmap slugs.
-- The loop keeps slugs unique globally by appending -2, -3, ... when needed.
DO $$
DECLARE
    roadmap_record record;
    base_slug text;
    candidate_slug text;
    suffix integer;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS tmp_roadmap_slug_backfill
    (
        slug text PRIMARY KEY
    ) ON COMMIT DROP;

    TRUNCATE tmp_roadmap_slug_backfill;

    FOR roadmap_record IN
        SELECT roadmap_id, title, slug
        FROM public.roadmap
        ORDER BY created_at NULLS LAST, roadmap_id
    LOOP
        base_slug := lower(
            regexp_replace(
                btrim(coalesce(nullif(roadmap_record.slug, ''), roadmap_record.title, 'roadmap')),
                '[^a-zA-Z0-9]+',
                '-',
                'g'
            )
        );

        base_slug := regexp_replace(base_slug, '(^-|-$)', '', 'g');

        IF base_slug IS NULL OR base_slug = '' THEN
            base_slug := 'roadmap-' || substring(roadmap_record.roadmap_id::text from 1 for 8);
        END IF;

        candidate_slug := base_slug;
        suffix := 2;

        WHILE EXISTS (
            SELECT 1
            FROM tmp_roadmap_slug_backfill
            WHERE slug = candidate_slug
        ) LOOP
            candidate_slug := base_slug || '-' || suffix;
            suffix := suffix + 1;
        END LOOP;

        INSERT INTO tmp_roadmap_slug_backfill (slug)
        VALUES (candidate_slug);

        UPDATE public.roadmap
        SET slug = candidate_slug,
            updated_at = COALESCE(updated_at, now())
        WHERE roadmap_id = roadmap_record.roadmap_id;
    END LOOP;
END $$;

-- Fail with a clear error before adding the title uniqueness constraint.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM public.roadmap
        GROUP BY career_role_id, lower(btrim(title))
        HAVING COUNT(*) > 1
    ) THEN
        RAISE EXCEPTION 'Duplicate roadmap titles exist within the same career role. Rename duplicates before applying uq_roadmap_career_role_title.';
    END IF;
END $$;

ALTER TABLE public.roadmap
ALTER COLUMN slug SET NOT NULL;

-- Public roadmap slugs are globally unique so /roadmaps/{slug} is unambiguous.
CREATE UNIQUE INDEX IF NOT EXISTS uq_roadmap_slug
ON public.roadmap (slug);

-- A career role may have multiple roadmaps, but not duplicate roadmap titles.
CREATE UNIQUE INDEX IF NOT EXISTS uq_roadmap_career_role_title
ON public.roadmap (career_role_id, lower(btrim(title)));

COMMIT;
