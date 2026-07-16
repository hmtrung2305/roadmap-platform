-- Refresh demo roadmap and published learning-module ownership without
-- recreating content, enrollments, learner progress, or review history.
-- Safe to rerun after database/seeds/core/dev-users.seed.sql.

BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT required.username_normalized
        FROM (VALUES
            ('contentmanager'),
            ('contentmanager2'),
            ('contentmanager3'),
            ('contentmanager4')
        ) AS required(username_normalized)
        LEFT JOIN public."user" seeded_user
            ON seeded_user.username_normalized = required.username_normalized
        WHERE seeded_user.user_id IS NULL
    ) THEN
        RAISE EXCEPTION 'Missing one or more demo content manager users. Run dev-users.seed.sql first.';
    END IF;
END $$;

WITH roadmap_authors(roadmap_slug, username_normalized) AS (
    VALUES
        ('backend-developer-roadmap', 'contentmanager'),
        ('cyber-security-expert-roadmap', 'contentmanager'),
        ('database-engineer-roadmap', 'contentmanager'),
        ('full-stack-developer-roadmap', 'contentmanager'),

        ('frontend-developer-roadmap', 'contentmanager2'),
        ('game-developer-roadmap', 'contentmanager2'),
        ('mobile-developer-roadmap', 'contentmanager2'),
        ('qa-engineer-roadmap', 'contentmanager2'),

        ('cloud-engineer-roadmap', 'contentmanager3'),
        ('devops-engineer-roadmap', 'contentmanager3'),
        ('network-engineer-roadmap', 'contentmanager3'),
        ('site-reliability-engineer-roadmap', 'contentmanager3'),

        ('ai-engineer-roadmap', 'contentmanager4'),
        ('business-intelligence-analyst-roadmap', 'contentmanager4'),
        ('data-analyst-roadmap', 'contentmanager4'),
        ('data-engineer-roadmap', 'contentmanager4'),
        ('data-scientist-roadmap', 'contentmanager4'),
        ('machine-learning-engineer-roadmap', 'contentmanager4')
)
UPDATE public.roadmap roadmap
SET
    owner_user_id = author.user_id,
    updated_at = now()
FROM roadmap_authors mapping
JOIN public."user" author
    ON author.username_normalized = mapping.username_normalized
WHERE roadmap.slug = mapping.roadmap_slug
  AND roadmap.owner_user_id IS DISTINCT FROM author.user_id;

UPDATE public.skill_module module
SET
    created_by_user_id = author.user_id,
    updated_at = now()
FROM public.skill skill
JOIN public."user" author
    ON author.username_normalized = CASE
        WHEN skill.slug IN ('java', 'csharp') THEN 'contentmanager'
        WHEN skill.slug IN ('python', 'sql') THEN 'contentmanager4'
        ELSE 'contentmanager2'
    END
WHERE module.skill_id = skill.skill_id
  AND module.status = 'published'
  AND module.metadata ->> 'source' = 'database/seeds/learning-modules'
  AND module.created_by_user_id IS DISTINCT FROM author.user_id;

COMMIT;
