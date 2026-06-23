ALTER TABLE public.skill_gap_analysis_history
ADD COLUMN is_deleted BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE public.skill_gap_analysis_history
ADD COLUMN deleted_at TIMESTAMPTZ NULL;


ALTER TABLE skill_gap_analysis_history
ADD COLUMN roadmap_version_number INT NOT NULL DEFAULT 1;

ALTER TABLE skill_gap_analysis_history
ADD COLUMN roadmap_version_title VARCHAR(255) NOT NULL DEFAULT '';

