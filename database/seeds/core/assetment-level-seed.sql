INSERT INTO assessment_level
(
    assessment_level_id,
    career_role_id,
    name,
    slug,
    sort_order
)
SELECT
    gen_random_uuid(),
    cr.career_role_id,
    lvl.name,
    lvl.slug,
    lvl.sort_order
FROM career_role cr
CROSS JOIN
(
    VALUES
        ('Beginner', 'beginner', 1),
        ('Intermediate', 'intermediate', 2),
        ('Advanced', 'advanced', 3)
) AS lvl(name, slug, sort_order)
ON CONFLICT (career_role_id, slug)
DO NOTHING;


