WITH duplicate_category_config AS
(
    SELECT
        skill_gap_category_config_id,
        ROW_NUMBER() OVER
        (
            PARTITION BY
                roadmap_id,
                roadmap_version_id,
                category_name
            ORDER BY
                display_order,
                created_at,
                skill_gap_category_config_id
        ) AS duplicate_rank
    FROM public.skill_gap_category_config
)
DELETE FROM public.skill_gap_category_config config
USING duplicate_category_config duplicate
WHERE config.skill_gap_category_config_id = duplicate.skill_gap_category_config_id
  AND duplicate.duplicate_rank > 1;

ALTER TABLE IF EXISTS public.skill_gap_category_config
    DROP CONSTRAINT IF EXISTS uq_skill_gap_category;

ALTER TABLE IF EXISTS public.skill_gap_category_config
    ADD CONSTRAINT uq_skill_gap_category
    UNIQUE (roadmap_id, roadmap_version_id, category_name);
