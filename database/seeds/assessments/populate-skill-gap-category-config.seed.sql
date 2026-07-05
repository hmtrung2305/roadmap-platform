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

DO $$
DECLARE
    expected_unique_columns text[] := ARRAY[
        'roadmap_id',
        'roadmap_version_id',
        'category_name'
    ];
    named_constraint_matches boolean;
    matching_unique_exists boolean;
BEGIN
    SELECT EXISTS
    (
        SELECT 1
        FROM pg_constraint constraint_row
        INNER JOIN pg_class table_row
            ON table_row.oid = constraint_row.conrelid
        INNER JOIN pg_namespace namespace_row
            ON namespace_row.oid = table_row.relnamespace
        WHERE namespace_row.nspname = 'public'
          AND table_row.relname = 'skill_gap_category_config'
          AND constraint_row.conname = 'uq_skill_gap_category'
          AND constraint_row.contype = 'u'
          AND
          (
              SELECT ARRAY_AGG(attribute_row.attname::text ORDER BY constraint_column.ordinality)
              FROM UNNEST(constraint_row.conkey) WITH ORDINALITY constraint_column(attnum, ordinality)
              INNER JOIN pg_attribute attribute_row
                  ON attribute_row.attrelid = constraint_row.conrelid
                 AND attribute_row.attnum = constraint_column.attnum
          ) = expected_unique_columns
    )
    INTO named_constraint_matches;

    IF NOT named_constraint_matches THEN
        ALTER TABLE IF EXISTS public.skill_gap_category_config
            DROP CONSTRAINT IF EXISTS uq_skill_gap_category;
    END IF;

    SELECT EXISTS
    (
        SELECT 1
        FROM pg_constraint constraint_row
        INNER JOIN pg_class table_row
            ON table_row.oid = constraint_row.conrelid
        INNER JOIN pg_namespace namespace_row
            ON namespace_row.oid = table_row.relnamespace
        WHERE namespace_row.nspname = 'public'
          AND table_row.relname = 'skill_gap_category_config'
          AND constraint_row.contype = 'u'
          AND
          (
              SELECT ARRAY_AGG(attribute_row.attname::text ORDER BY constraint_column.ordinality)
              FROM UNNEST(constraint_row.conkey) WITH ORDINALITY constraint_column(attnum, ordinality)
              INNER JOIN pg_attribute attribute_row
                  ON attribute_row.attrelid = constraint_row.conrelid
                 AND attribute_row.attnum = constraint_column.attnum
          ) = expected_unique_columns
    )
    INTO matching_unique_exists;

    IF NOT matching_unique_exists THEN
        ALTER TABLE public.skill_gap_category_config
            ADD CONSTRAINT uq_skill_gap_category
            UNIQUE (roadmap_id, roadmap_version_id, category_name);
    END IF;
END $$;

INSERT INTO public.skill_gap_category_config
(
    roadmap_id,
    roadmap_version_id,
    category_name,
    display_order
)
SELECT
    category_data.roadmap_id,
    category_data.roadmap_version_id,
    category_data.category_name,
    ROW_NUMBER() OVER
    (
        PARTITION BY
            category_data.roadmap_id,
            category_data.roadmap_version_id
        ORDER BY category_data.category_name
    ) AS display_order
FROM
(
    SELECT DISTINCT
        rv.roadmap_id,
        rv.roadmap_version_id,
        s.category AS category_name
    FROM public.roadmap_version rv
    INNER JOIN public.roadmap_node rn
        ON rn.roadmap_version_id = rv.roadmap_version_id
    INNER JOIN public.roadmap_node_skill rns
        ON rns.roadmap_node_id = rn.roadmap_node_id
    INNER JOIN public.skill s
        ON s.skill_id = rns.skill_id
    WHERE rv.status = 'published'
      AND s.category IS NOT NULL
      AND TRIM(s.category) <> ''
) AS category_data
ON CONFLICT
(
    roadmap_id,
    roadmap_version_id,
    category_name
)
DO NOTHING;
