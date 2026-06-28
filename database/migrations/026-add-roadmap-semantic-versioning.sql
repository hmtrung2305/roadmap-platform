-- ============================================================
-- 026 - Add roadmap semantic versioning
-- Adds semantic version fields to roadmap_version and backfills
-- existing versions from the legacy version_number column.
-- ============================================================

BEGIN;

ALTER TABLE public.roadmap_version
    ADD COLUMN IF NOT EXISTS major_version int,
    ADD COLUMN IF NOT EXISTS minor_version int,
    ADD COLUMN IF NOT EXISTS patch_version int,
    ADD COLUMN IF NOT EXISTS release_type varchar(30),
    ADD COLUMN IF NOT EXISTS created_from_version_id uuid,
    ADD COLUMN IF NOT EXISTS updated_at timestamptz;

UPDATE public.roadmap_version
SET
    major_version = COALESCE(major_version, CASE WHEN version_number > 0 THEN version_number ELSE 1 END),
    minor_version = COALESCE(minor_version, 0),
    patch_version = COALESCE(patch_version, 0),
    release_type = COALESCE(
        release_type,
        CASE
            WHEN version_number = 1 THEN 'initial'
            ELSE 'major'
        END
    ),
    updated_at = COALESCE(updated_at, created_at, now());

WITH previous_versions AS (
    SELECT
        current_version.roadmap_version_id,
        previous_version.roadmap_version_id AS previous_roadmap_version_id
    FROM public.roadmap_version current_version
    JOIN public.roadmap_version previous_version
        ON previous_version.roadmap_id = current_version.roadmap_id
        AND previous_version.version_number = current_version.version_number - 1
)
UPDATE public.roadmap_version roadmap_version
SET created_from_version_id = previous_versions.previous_roadmap_version_id
FROM previous_versions
WHERE roadmap_version.roadmap_version_id = previous_versions.roadmap_version_id
    AND roadmap_version.created_from_version_id IS NULL;

ALTER TABLE public.roadmap_version
    ALTER COLUMN major_version SET DEFAULT 1,
    ALTER COLUMN minor_version SET DEFAULT 0,
    ALTER COLUMN patch_version SET DEFAULT 0,
    ALTER COLUMN release_type SET DEFAULT 'initial',
    ALTER COLUMN updated_at SET DEFAULT now();

ALTER TABLE public.roadmap_version
    ALTER COLUMN major_version SET NOT NULL,
    ALTER COLUMN minor_version SET NOT NULL,
    ALTER COLUMN patch_version SET NOT NULL,
    ALTER COLUMN release_type SET NOT NULL,
    ALTER COLUMN updated_at SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_roadmap_version_created_from_version'
            AND conrelid = 'public.roadmap_version'::regclass
    ) THEN
        ALTER TABLE public.roadmap_version
            ADD CONSTRAINT fk_roadmap_version_created_from_version
            FOREIGN KEY (created_from_version_id)
            REFERENCES public.roadmap_version(roadmap_version_id)
            ON DELETE SET NULL;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_roadmap_version_semver'
            AND conrelid = 'public.roadmap_version'::regclass
    ) THEN
        ALTER TABLE public.roadmap_version
            ADD CONSTRAINT uq_roadmap_version_semver
            UNIQUE (roadmap_id, major_version, minor_version, patch_version);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'chk_roadmap_version_semver'
            AND conrelid = 'public.roadmap_version'::regclass
    ) THEN
        ALTER TABLE public.roadmap_version
            ADD CONSTRAINT chk_roadmap_version_semver
            CHECK (
                major_version >= 1
                AND minor_version >= 0
                AND patch_version >= 0
            );
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'chk_roadmap_version_release_type'
            AND conrelid = 'public.roadmap_version'::regclass
    ) THEN
        ALTER TABLE public.roadmap_version
            ADD CONSTRAINT chk_roadmap_version_release_type
            CHECK (release_type IN ('initial', 'patch', 'minor', 'major'));
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_roadmap_version_created_from_version_id
    ON public.roadmap_version(created_from_version_id);

COMMIT;
