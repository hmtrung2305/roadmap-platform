-- ============================================================
-- 010-job-market-jobs-api-fields.sql
-- Purpose:
--   Preserve typed Jobs API fields in job_posting so Job Market
--   data can be scaffolded and queried without parsing description.
-- ============================================================

BEGIN;

ALTER TABLE public.job_posting
    ADD COLUMN IF NOT EXISTS source_job_id varchar(120),
    ADD COLUMN IF NOT EXISTS category varchar(100),
    ADD COLUMN IF NOT EXISTS salary varchar(100),
    ADD COLUMN IF NOT EXISTS experience varchar(100),
    ADD COLUMN IF NOT EXISTS post_date_text varchar(80),
    ADD COLUMN IF NOT EXISTS source_updated_at timestamptz,
    ADD COLUMN IF NOT EXISTS requirements jsonb NOT NULL DEFAULT '[]'::jsonb,
    ADD COLUMN IF NOT EXISTS specialties jsonb NOT NULL DEFAULT '[]'::jsonb,
    ADD COLUMN IF NOT EXISTS benefits jsonb NOT NULL DEFAULT '[]'::jsonb;

UPDATE public.job_posting
SET requirements = '[]'::jsonb
WHERE requirements IS NULL OR jsonb_typeof(requirements) <> 'array';

UPDATE public.job_posting
SET specialties = '[]'::jsonb
WHERE specialties IS NULL OR jsonb_typeof(specialties) <> 'array';

UPDATE public.job_posting
SET benefits = '[]'::jsonb
WHERE benefits IS NULL OR jsonb_typeof(benefits) <> 'array';

ALTER TABLE public.job_posting
    ALTER COLUMN requirements SET DEFAULT '[]'::jsonb,
    ALTER COLUMN requirements SET NOT NULL,
    ALTER COLUMN specialties SET DEFAULT '[]'::jsonb,
    ALTER COLUMN specialties SET NOT NULL,
    ALTER COLUMN benefits SET DEFAULT '[]'::jsonb,
    ALTER COLUMN benefits SET NOT NULL;

ALTER TABLE public.job_posting
    DROP CONSTRAINT IF EXISTS chk_job_posting_requirements_json_array,
    DROP CONSTRAINT IF EXISTS chk_job_posting_specialties_json_array,
    DROP CONSTRAINT IF EXISTS chk_job_posting_benefits_json_array;

ALTER TABLE public.job_posting
    ADD CONSTRAINT chk_job_posting_requirements_json_array
        CHECK (jsonb_typeof(requirements) = 'array'),
    ADD CONSTRAINT chk_job_posting_specialties_json_array
        CHECK (jsonb_typeof(specialties) = 'array'),
    ADD CONSTRAINT chk_job_posting_benefits_json_array
        CHECK (jsonb_typeof(benefits) = 'array');

CREATE INDEX IF NOT EXISTS ix_job_posting_source_job_id
    ON public.job_posting(source_job_id);

CREATE INDEX IF NOT EXISTS ix_job_posting_category
    ON public.job_posting(category);

CREATE INDEX IF NOT EXISTS ix_job_posting_published_at
    ON public.job_posting(published_at);

COMMIT;
