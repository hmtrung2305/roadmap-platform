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
        PARTITION BY category_data.roadmap_id
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
    category_name
)
DO NOTHING;