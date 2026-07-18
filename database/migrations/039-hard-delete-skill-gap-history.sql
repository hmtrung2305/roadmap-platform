DELETE FROM public.skill_gap_analysis_history
WHERE is_deleted = TRUE;

ALTER TABLE public.skill_gap_analysis_history
    DROP COLUMN deleted_at,
    DROP COLUMN is_deleted;

CREATE INDEX IF NOT EXISTS ix_skill_gap_history_user_created_at
    ON public.skill_gap_analysis_history
        (user_id, created_at DESC, skill_gap_analysis_history_id DESC);
